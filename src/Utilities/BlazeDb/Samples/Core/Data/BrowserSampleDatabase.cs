using System.Runtime.Versioning;
using Bit.BlazeDb.Browser;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Samples.Core.Data;

/// <summary>
/// The database of the Wasm and Auto clients: it lives in the browser tab, in managed memory, and persists to OPFS -
/// or to IndexedDB where the page cannot write OPFS (Firefox private windows, Safari before 26). WebAssembly has one
/// thread, so nothing needs serializing. Only one tab can be the writer: a second one, which finds the Web Lock
/// taken, keeps its rows in memory instead.
/// </summary>
[SupportedOSPlatform("browser")]
public sealed class BrowserSampleDatabase : SampleDatabase
{
    protected override async ValueTask<IBlazeDbStorage> CreateStorageAsync()
    {
        try
        {
            if (await BlazeDbIndexedDbStorage.IsOpfsAvailableAsync())
            {
                var opfs = await BlazeDbOpfsStorage.CreateAsync(Name);
                Backend = "OPFS";
                BackendNote = $"The '{Name}' directory of this site's origin private file system. This tab holds its writer lock.";
                return opfs;
            }

            var indexedDb = await BlazeDbIndexedDbStorage.CreateAsync(Name);
            Backend = "IndexedDB";
            BackendNote = $"The '{Name}' IndexedDB database: this browser cannot write OPFS from the page. This tab holds its writer lock.";
            return indexedDb;
        }
        catch (BlazeDbDatabaseLockedException)
        {
            Backend = "Memory";
            BackendNote = "Another tab of this site holds the database's writer lock, so this one keeps its rows in memory until it closes.";
            return new BlazeDbInMemoryStorage();
        }
    }
}
