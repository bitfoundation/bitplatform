using Boilerplate.Client.Core.Data;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Datasync.Client.Offline;

namespace Boilerplate.Client.Core.Services;

/// <summary>
/// Syncronize client offline database changes with the server,
/// and pull server changes to the client offline database.
/// </summary>
public partial class SyncService : IAsyncDisposable
{
    [AutoInject] private SnackBarService snackBarService = default!;
    [AutoInject] private IExceptionHandler exceptionHandler = default!;
    [AutoInject] private ITelemetryContext telemetryContext = default!;
    [AutoInject] private IStringLocalizer<AppStrings> localizer = default!;
    [AutoInject] private IDbContextFactory<AppOfflineDbContext> dbContextFactory = default!;

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
        using var localCts = cts;
        await localCts.TryCancel();

        cts = new();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

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
                    .WithData(pushResult.FailedRequests.ToDictionary(fr => fr.Key, fr => (object?)fr.Value.ReasonPhrase));

            if (pullRecentChanges)
            {
                var pullResult = await dbContext.PullAsync(cancellationToken);

                if (pullResult.IsSuccessful is false)
                    throw new BadRequestException(localizer[nameof(AppStrings.SyncPullFailed)])
                        .WithData(pullResult.FailedRequests.ToDictionary(fr => fr.Key.ToString(), fr => (object?)fr.Value.ReasonPhrase));
            }

            if (pushResult.CompletedOperations > 0)
                snackBarService.Success(localizer[nameof(AppStrings.SyncPushSuccess), pushResult.CompletedOperations]);
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp, displayKind: telemetryContext.IsOnline is true ? ExceptionDisplayKind.NonInterrupting : ExceptionDisplayKind.None);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await cts.TryCancel();
    }
}
