using Bit.BlazeDb;
using Bit.BlazeDb.Samples.Core.Data;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Samples.Server;

/// <summary>
/// The database of the Server host: it lives in the server process and persists to files, and every circuit - every
/// browser window - shares it, so a todo added in one shows up in the others. The engine assumes one writer and no
/// concurrent readers, while circuits run on many threads, so every use of it takes one lock. That is enough for a
/// sample; a store many users write to at once is a database server's job, and BlazeDb is one user's local store.
/// </summary>
public sealed class ServerSampleDatabase(IWebHostEnvironment environment) : SampleDatabase
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public override async Task<T> UseAsync<T>(Func<BlazeDbDatabase, T> work)
    {
        await _gate.WaitAsync();
        try
        {
            // The changes a write makes are delivered - live queries re-run, Changed handlers run - on this thread
            // before the write returns, so the other circuits' reads they trigger are inside the lock as well.
            return await base.UseAsync(work);
        }
        finally
        {
            _gate.Release();
        }
    }

    public override async Task<T> UseAsync<T>(Func<BlazeDbDatabase, Task<T>> work)
    {
        await _gate.WaitAsync();
        try
        {
            return await base.UseAsync(work);
        }
        finally
        {
            _gate.Release();
        }
    }

    protected override ValueTask<IBlazeDbStorage> CreateStorageAsync()
    {
        var directory = Path.Combine(environment.ContentRootPath, "App_Data", Name);

        Backend = "Files";
        BackendNote = $"{directory} on the server, shared by every browser window connected to it.";

        // Locks the directory for as long as it lives: a second process cannot open it as a writer.
        return ValueTask.FromResult<IBlazeDbStorage>(new BlazeDbFileStorage(directory));
    }
}
