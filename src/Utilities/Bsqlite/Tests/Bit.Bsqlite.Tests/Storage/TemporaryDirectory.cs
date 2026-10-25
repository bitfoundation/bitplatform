namespace Bit.Bsqlite.Storage.Tests;

/// <summary>A folder of its own under the temp directory, deleted with everything in it on disposal.</summary>
internal sealed class TemporaryDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(),
        "bit-bsqlite-tests",
        Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch (IOException)
        {
            // A handle still closing; the temp directory is cleaned eventually either way.
        }
    }
}
