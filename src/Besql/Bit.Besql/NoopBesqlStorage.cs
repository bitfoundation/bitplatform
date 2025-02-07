namespace Bit.Besql;

/// <summary>
/// <inheritdoc cref="IBesqlStorage"/>
/// </summary>
internal class NoopBesqlStorage : IBesqlStorage
{
    public Task SyncFromBrowserCacheStorageToDotNet(string filename)
    {
        return Task.CompletedTask;
    }

    public Task SyncFromDotNetToBrowserCacheStorage(string filename)
    {
        return Task.CompletedTask;
    }

    public void PauseSyncFromDotNetToBrowserCacheStorage()
    {
    }

    public Task ResumeSyncFromDotNetToBrowserCacheStorage()
    {
        return Task.CompletedTask;
    }
}
