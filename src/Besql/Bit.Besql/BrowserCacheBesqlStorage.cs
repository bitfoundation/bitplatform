using Microsoft.JSInterop;

namespace Bit.Besql;

/// <summary>
/// <inheritdoc cref="IBesqlStorage"/>
/// </summary>
public sealed class BrowserCacheBesqlStorage(IJSRuntime jsRuntime) : IBesqlStorage
{
    HashSet<string>? pausedFilesList;

    public async Task SyncFromBrowserCacheStorageToDotNet(string fileName)
    {
        await jsRuntime.InvokeVoidAsync("BitBesql.syncFromBrowserCacheStorageToDotNet", fileName).ConfigureAwait(false);
    }

    public async Task SyncFromDotNetToBrowserCacheStorage(string fileName)
    {
        if (pausedFilesList is not null)
        {
            pausedFilesList.Add(fileName);
            return;
        }

        await jsRuntime.InvokeVoidAsync("BitBesql.syncFromDotNetToBrowserCacheStorage", fileName).ConfigureAwait(false);
    }

    public void PauseSyncFromDotNetToBrowserCacheStorage()
    {
        pausedFilesList = [];
    }

    public async Task ResumeSyncFromDotNetToBrowserCacheStorage()
    {
        if (pausedFilesList is null)
            throw new InvalidOperationException("bit Besql storage is not paused.");

        var files = pausedFilesList;
        pausedFilesList = null;

        foreach (var file in files)
        {
            await SyncFromDotNetToBrowserCacheStorage(file).ConfigureAwait(false);
        }
    }
}
