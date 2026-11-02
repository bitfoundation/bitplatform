using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Samples.Core.Data;

/// <summary>
/// The database of the hosts whose .NET runs outside a browser - <c>ServerSampleDatabase</c> in the server process,
/// <c>MauiSampleDatabase</c> in the app's - persisted to files in a directory under the root it is given. Only
/// one process can be the writer: a second one, which finds the directory locked, keeps its rows in memory instead.
/// </summary>
public abstract class FileSampleDatabase(string root) : SampleDatabase
{
    /// <summary>Where <paramref name="directory"/> is, and whose rows it keeps, for the pages to say.</summary>
    protected abstract string Describe(string directory);

    protected override ValueTask<SampleStorage> CreateStorageAsync()
    {
        var directory = Path.Combine(root, Name);

        try
        {
            // Locks the directory for as long as it lives: a second process cannot open it as a writer.
            return ValueTask.FromResult(new SampleStorage(new BlazeDbFileStorage(directory), "Files", Describe(directory)));
        }
        catch (BlazeDbDatabaseLockedException)
        {
            // Another process - a second dotnet run, or a second instance of the app - holds the lock.
            return ValueTask.FromResult(new SampleStorage(new BlazeDbInMemoryStorage(), "Memory",
                $"Another process holds the writer lock of {directory}, so this one keeps its rows in memory until it exits."));
        }
    }
}
