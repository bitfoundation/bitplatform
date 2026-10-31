namespace Boilerplate.Workbench.Infrastructure.Services;

public sealed partial class WorkbenchStateStorage : IBlexStorage, IAsyncDisposable
{
    private const string DatabaseName = "bit-workbench";
    private const string StoreName = "state";

    [AutoInject] private IJSRuntime jsRuntime = default!;
    [AutoInject] private IndexedDb indexedDb = default!;
    [AutoInject] private NavigationManager navigationManager = default!;

    private string? keyPrefix;
    private bool? isCapture;
    private Task<IndexedDbHandle>? opening;

    public async ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var database = await Open();

        return database is null ? null : await database.Get<string>(StoreName, $"{KeyPrefix}{key}");
    }

    public async ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        if (await Open() is { } database)
        {
            await database.Put(StoreName, value, $"{KeyPrefix}{key}");
        }
    }

    public async ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (await Open() is { } database)
        {
            await database.Delete(StoreName, $"{KeyPrefix}{key}");
        }
    }

    private string KeyPrefix => keyPrefix ??= WorkbenchUrls.ReadQuery(navigationManager.Uri, WorkbenchUrls.PaneQueryKey) is { } paneId ? $"pane-{paneId}/" : "shell/";

    private bool IsCapture => isCapture ??= WorkbenchUrls.ReadQuery(navigationManager.Uri, WorkbenchUrls.SnapshotQueryKey) is not null;

    private async ValueTask<IndexedDbHandle?> Open()
    {
        if (IsCapture || jsRuntime.IsRuntimeInvalid())
            return null;

        opening ??= indexedDb.Open(DatabaseName, 1, [new() { Name = StoreName }]).AsTask();

        return await opening;
    }

    public async ValueTask DisposeAsync()
    {
        if (opening is { IsCompletedSuccessfully: true })
        {
            try
            {
                await opening.Result.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }
}
