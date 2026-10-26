using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Samples.Core.Data;

/// <summary>
/// Registered by the servers of the Wasm and Auto hosts. The pages render there while they are prerendered - and,
/// on Auto, through the whole first visit, on Interactive Server while the WebAssembly runtime downloads - but their
/// database lives in the browser tab (<see cref="BrowserSampleDatabase"/>), and OPFS, IndexedDB and Web Locks do
/// not exist on a server. So the pages render a placeholder here instead of opening anything.
/// </summary>
public sealed class UnreachableSampleDatabase : SampleDatabase
{
    public UnreachableSampleDatabase()
    {
        Backend = "In the browser";
        BackendNote = "OPFS or IndexedDB, opened once the page runs on WebAssembly.";
    }

    public override string? Unreachable => "The database lives in the browser tab, and this page is being rendered on the server.";

    public override Task<T> UseAsync<T>(Func<BlazeDbDatabase, T> work) => throw new InvalidOperationException(Unreachable);

    public override Task<T> UseAsync<T>(Func<BlazeDbDatabase, Task<T>> work) => throw new InvalidOperationException(Unreachable);

    protected override ValueTask<IBlazeDbStorage> CreateStorageAsync() => throw new InvalidOperationException(Unreachable);
}
