namespace Bit.Besql;

public interface IBesqlStorage
{
    /// <summary>
    /// In WebAssembly, when working with an SQLite database file using APIs like System.IO.File or Entity Framework Core's DbContext,
    /// the file is written to memory via emscripten. This method ensures that the in-memory data is saved to the browser's cache storage
    /// to make it persistent.
    /// </summary>
    Task SyncFromDotNetToBrowserCacheStorage(string filename);

    /// <summary>
    /// When the application starts, this method reads the SQLite database file from the browser's cache storage (previously saved using
    /// <see cref="SyncFromDotNetToBrowserCacheStorage"/>) and loads it into memory. This allows Entity Framework Core and other .NET APIs,
    /// such as System.IO.File, to access the database.
    /// </summary>
    Task SyncFromBrowserCacheStorageToDotNet(string filename);

    /// <summary>
    /// Whenever changes are made to the SQLite database using Entity Framework Core, <see cref="SyncFromDotNetToBrowserCacheStorage"/> is
    /// automatically called to ensure changes are synced to the browser's cache storage. Although this process is throttled (e.g., multiple
    /// SaveChangeAsync calls result in a single sync), you may want to pause the sync process temporarily when making numerous database changes.
    /// Call <see cref="ResumeSyncFromDotNetToBrowserCacheStorage"/> to resume syncing after changes are complete.
    /// </summary>
    void PauseSyncFromDotNetToBrowserCacheStorage();

    /// <summary>
    /// Resumes the sync process that was paused using <see cref="PauseSyncFromDotNetToBrowserCacheStorage"/>.
    /// </summary>
    Task ResumeSyncFromDotNetToBrowserCacheStorage();
}
