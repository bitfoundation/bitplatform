using System.Text.Json;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// Where the pages' stores save their state: the browser's IndexedDB, under a key prefix per view, so a view comes back as it
/// was left. A preview that restores a snapshot for a screenshot reads and writes nothing.
/// </summary>
public sealed partial class WorkbenchStateStorage : IBlexStorage, IAsyncDisposable
{
    private const string DatabaseName = "bit-workbench";
    private const string StoreName = "state";
    private const int MaxFailures = 3;

    [AutoInject] private IJSRuntime jsRuntime = default!;
    [AutoInject] private IndexedDb indexedDb = default!;
    [AutoInject] private NavigationManager navigationManager = default!;
    [AutoInject] private ILogger<WorkbenchStateStorage> logger = default!;

    private string? keyPrefix;
    private bool? isCapture;
    private int failures;
    private Task<IndexedDbHandle?>? opening;
    private readonly HashSet<string> droppedKeys = [];

    /// <summary>The saved state of a store, or null when nothing is saved or the storage fails.</summary>
    public async ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (await Open(cancellationToken) is not { } database)
            return null;

        try
        {
            return await database.Get<string>(StoreName, $"{KeyPrefix}{key}").AsTask().WaitAsync(cancellationToken);
        }
        catch (JsonException exp)
        {
            // A record that isn't a string counts as nothing saved; the store's next save replaces it.
            logger.LogWarning(exp, "The saved state of {Key} can't be read, so the store starts from its defaults.", key);
            return null;
        }
        catch (Exception exp) when (IsStorageFailure(exp, cancellationToken))
        {
            Fail(exp);
            return null;
        }
    }

    /// <summary>Saves a store's state; a failure leaves the state unsaved instead of breaking the view.</summary>
    public async ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        // A state too big to come back over the circuit is dropped, so the store starts from its defaults next time.
        if (value.Length > WorkbenchSnapshots.MaxLength)
        {
            if (droppedKeys.Add(key))
            {
                logger.LogWarning("The state of {Key} is too big to save, so it is not kept.", key);
            }

            await RemoveAsync(key, cancellationToken);
            return;
        }

        if (await Open(cancellationToken) is not { } database)
            return;

        try
        {
            await database.Put(StoreName, value, $"{KeyPrefix}{key}").AsTask().WaitAsync(cancellationToken);
        }
        catch (Exception exp) when (IsStorageFailure(exp, cancellationToken))
        {
            Fail(exp);
        }
    }

    /// <summary>Removes a store's saved state; a failure is logged and otherwise ignored.</summary>
    public async ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (await Open(cancellationToken) is not { } database)
            return;

        try
        {
            await database.Delete(StoreName, $"{KeyPrefix}{key}").AsTask().WaitAsync(cancellationToken);
        }
        catch (Exception exp) when (IsStorageFailure(exp, cancellationToken))
        {
            Fail(exp);
        }
    }

    /// <summary>pane-{id}/ for a view's preview, shell/ for everything else.</summary>
    private string KeyPrefix => keyPrefix ??= WorkbenchUrls.ReadQuery(navigationManager.Uri, WorkbenchUrls.PaneQueryKey) is { } paneId ? $"pane-{paneId}/" : "shell/";

    /// <summary>Whether this preview restores a snapshot for a screenshot.</summary>
    private bool IsCapture => isCapture ??= WorkbenchUrls.ReadQuery(navigationManager.Uri, WorkbenchUrls.SnapshotQueryKey) is not null;

    /// <summary>
    /// Opens the database once per circuit; null while prerendering, in a capture, and once the storage has failed
    /// too often, so the view then works without saved state.
    /// </summary>
    private async ValueTask<IndexedDbHandle?> Open(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (IsCapture || failures >= MaxFailures || jsRuntime.IsRuntimeInvalid())
            return null;

        var task = opening ??= OpenOrRecreate();

        try
        {
            return await task.WaitAsync(cancellationToken);
        }
        catch (Exception exp) when (IsStorageFailure(exp, cancellationToken))
        {
            Fail(exp);
            return null;
        }
    }

    /// <summary>
    /// Opens the database. One this version can't open, such as one at a higher version, is deleted and created again;
    /// when that fails too, the storage is given up for this circuit.
    /// </summary>
    private async Task<IndexedDbHandle?> OpenOrRecreate()
    {
        try
        {
            return await OpenDatabase();
        }
        catch (JSException exp)
        {
            logger.LogWarning(exp, "The views' saved state can't be opened, so it is cleared.");
        }

        try
        {
            await indexedDb.DeleteDatabase(DatabaseName);

            return await OpenDatabase();
        }
        catch (JSException exp)
        {
            logger.LogWarning(exp, "The views' saved state is unavailable in this browser, so nothing is saved.");
            failures = MaxFailures;
            return null;
        }
    }

    private async Task<IndexedDbHandle?> OpenDatabase() => await indexedDb.Open(DatabaseName, 1, [new() { Name = StoreName }]);

    /// <summary>
    /// Whether an exception is the storage failing (JavaScript, a lost circuit or an interop timeout)
    /// rather than the caller cancelling.
    /// </summary>
    private static bool IsStorageFailure(Exception exception, CancellationToken cancellationToken)
    {
        return exception is JSException or JSDisconnectedException
            || (exception is OperationCanceledException && cancellationToken.IsCancellationRequested is false);
    }

    /// <summary>Counts a failed call and drops the connection, which the next call opens again.</summary>
    private void Fail(Exception exception)
    {
        logger.LogWarning(exception, "Reading or writing the views' saved state failed.");

        failures++;

        if (Interlocked.Exchange(ref opening, null) is { IsCompletedSuccessfully: true, Result: { } database })
        {
            _ = Close(database);
        }
    }

    private static async Task Close(IndexedDbHandle database)
    {
        try
        {
            await database.DisposeAsync();
        }
        catch (Exception exp) when (exp is JSException or JSDisconnectedException or OperationCanceledException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (opening is { IsCompletedSuccessfully: true, Result: { } database })
        {
            await Close(database);
        }
    }
}
