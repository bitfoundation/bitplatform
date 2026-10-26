using Bit.BlazeDb.Samples.Core.Data;

namespace Bit.BlazeDb.Samples.Maui;

/// <summary>
/// The database of the MAUI app. .NET runs in the app's own process, not in the WebView, so the browser backends do not
/// apply even though the pages render in one: the rows persist to files in the app's data directory. The components
/// render on the UI thread, which serializes their use of the database - nothing else needs to. The directory stays
/// locked for as long as the app runs, and commits reach the files within the flush interval (100 ms by default), so
/// an app the system ends without warning loses at most that much.
/// </summary>
public sealed class MauiSampleDatabase() : FileSampleDatabase(FileSystem.AppDataDirectory)
{
    protected override string Describe(string directory) => $"{directory}, in this app's data directory.";
}
