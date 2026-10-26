using Bit.BlazeDb.Samples.Core.Data;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Samples.Maui;

/// <summary>
/// The database of the MAUI app. .NET runs in the app's own process, not in the WebView, so the browser backends do not
/// apply even though the pages render in one: the rows persist to files in the app's data directory. The components
/// render on the UI thread, which serializes their use of the database - nothing else needs to.
/// </summary>
public sealed class MauiSampleDatabase : SampleDatabase
{
    protected override ValueTask<IBlazeDbStorage> CreateStorageAsync()
    {
        var directory = Path.Combine(FileSystem.AppDataDirectory, Name);

        Backend = "Files";
        BackendNote = $"{directory}, in this app's data directory.";

        // Locks the directory for as long as the app runs. Commits reach the files within the flush interval
        // (100 ms by default), so an app the system ends without warning loses at most that much.
        return ValueTask.FromResult<IBlazeDbStorage>(new BlazeDbFileStorage(directory));
    }
}
