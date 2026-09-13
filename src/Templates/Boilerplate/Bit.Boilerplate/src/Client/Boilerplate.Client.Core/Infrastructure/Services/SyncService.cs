using CommunityToolkit.Datasync.Client.Offline;

namespace Boilerplate.Client.Core.Infrastructure.Services;

/// <summary>
/// Synchronize client offline database changes with the server,
/// and pull server changes to the client offline database.
/// </summary>
public partial class SyncService : IAsyncDisposable
{
    [AutoInject] private SnackBarService snackBarService = default!;
    [AutoInject] private ClientExceptionHandlerBase exceptionHandler = default!;
    [AutoInject] private ITelemetryContext telemetryContext = default!;
    [AutoInject] private IStringLocalizer<AppStrings> localizer = default!;
    [AutoInject] private IDbContextFactory<AppOfflineDbContext> dbContextFactory = default!;

    /// <summary>
    /// A push runs inline after every add / edit / delete, takes no caller cancellation token and blocks the UI until
    /// it returns, so it gets a short budget. A pull can page through the whole table on its first run, so it gets a
    /// longer one - its callers pass their own token and show a loading indicator.
    /// </summary>
    private static readonly TimeSpan pushTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan pullTimeout = TimeSpan.FromSeconds(30);

    private CancellationTokenSource cts = new();

    public async Task Push()
    {
        await Sync(default, false);
    }

    public async Task Pull(CancellationToken cancellationToken)
    {
        await Sync(cancellationToken, true);
    }

    public async Task Sync(CancellationToken cancellationToken)
    {
        await Sync(cancellationToken, true);
    }

    private async Task Sync(CancellationToken cancellationToken, bool pullRecentChanges = true)
    {
        // Known offline: the local changes stay queued in the offline database and the next sync pushes them.
        // A null IsOnline means "not known yet" and must still be attempted - the sync request itself is what
        // resolves it (ExceptionDelegatingHandler publishes IS_ONLINE_CHANGED for every internal request).
        if (telemetryContext.IsOnline is false) return;

        // Exchanged rather than read-then-assigned so that two syncs starting together cannot both take the same
        // previous source, leaving one of them cancelling and disposing a source the other is still using. TryCancel
        // keeps the cancellation asynchronous, which is what the rest of the codebase does.
        var operationCts = new CancellationTokenSource(pullRecentChanges ? pullTimeout : pushTimeout);
        using var previousCts = Interlocked.Exchange(ref cts, operationCts);
        await previousCts.TryCancel();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, operationCts.Token);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(linkedCts.Token);

        await Sync(dbContext, pullRecentChanges, linkedCts.Token);
    }

    /// <summary>
    /// Synchronize local changes to the server and pull down new changes from the server.
    /// It doesn't support pulling down changes if there are local changes,
    /// so separated pull method is not relevant here.
    /// </summary>
    public async Task Sync(AppOfflineDbContext dbContext, bool pullRecentChanges, CancellationToken cancellationToken)
    {
        try
        {
            var pushResult = await dbContext.PushAsync(cancellationToken); // Push changes to server step is always required.

            if (pushResult.IsSuccessful is false)
                throw new BadRequestException(localizer[nameof(AppStrings.SyncPushFailed)])
                    .WithData(pushResult.FailedRequests.ToDictionary(fr => fr.Key, fr => (object?)$"{fr.Value.ReasonPhrase} - {fr.Value.StatusCode}"));

            if (pullRecentChanges)
            {
                var pullResult = await dbContext.PullAsync(cancellationToken);

                // A cancelled pull is not a failed pull: the Datasync client turns the cancellation into a local
                // exception on the result instead of throwing, so without this check our own timeout - or a newer
                // sync superseding this one - is reported to the user as "pulling changes from server failed".
                // FailedRequests and LocalExceptions are recorded independently, so a cancellation can sit alongside
                // a genuine failure - only a result that is *nothing but* cancellation is suppressed.
                if (pullResult.IsSuccessful is false &&
                    (pullResult.FailedRequests.Count > 0 ||
                     pullResult.LocalExceptions.Values.All(exp => exp is OperationCanceledException) is false))
                    throw new BadRequestException(localizer[nameof(AppStrings.SyncPullFailed)])
                        .WithData(pullResult.FailedRequests.ToDictionary(fr => fr.Key.ToString(), fr => (object?)$"{fr.Value.ReasonPhrase} - {fr.Value.StatusCode}"))
                        .WithData(pullResult.LocalExceptions.ToDictionary(le => le.Key.ToString(), le => (object?)le.Value.Message));
            }

            if (pushResult.CompletedOperations > 0)
                snackBarService.Success(localizer[nameof(AppStrings.SyncPushSuccess), pushResult.CompletedOperations]);
        }
        catch (TransientException)
        {
            // Simply ignore connection exceptions during sync
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp, displayKind: telemetryContext.IsOnline is true ? ExceptionDisplayKind.NonInterrupting : ExceptionDisplayKind.None);
        }
    }

    public async ValueTask DisposeAsync()
    {
        using var currentCts = cts;
        await currentCts.TryCancel();
    }
}
