using Bit.BlazeDb.Samples.Core.Data;

namespace Bit.BlazeDb.Samples.Server;

/// <summary>
/// The database of the Server host: it lives in the server process and persists to files, and every circuit - every
/// browser window - shares it, so a todo added in one shows up in the others. The engine assumes one writer and no
/// concurrent readers, while circuits run on many threads, so every use of it takes one lock. That is enough for a
/// sample; a store many users write to at once is a database server's job, and BlazeDb is one user's local store.
/// <para>
/// The changes a write makes are delivered on its thread before the write returns - every live query re-runs, every
/// Changed handler runs - so the other circuits' reads they trigger are inside the lock as well: they read the
/// table, and the lock is what keeps a write out while they do. Their re-renders are not: the pages queue those on
/// their own circuits rather than render on the writer's thread.
/// </para>
/// </summary>
public sealed class ServerSampleDatabase(IWebHostEnvironment environment)
    : FileSampleDatabase(Path.Combine(environment.ContentRootPath, "App_Data"))
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    protected override async Task RunExclusiveAsync(Func<Task> work)
    {
        await _gate.WaitAsync();
        try
        {
            await work();
        }
        finally
        {
            _gate.Release();
        }
    }

    protected override string Describe(string directory) =>
        $"{directory} on the server, shared by every browser window connected to it.";

    public override async ValueTask DisposeAsync()
    {
        // Takes the lock like any use, so it waits for the one in progress; any use after it finds the database
        // disposed.
        await base.DisposeAsync();
        _gate.Dispose();
    }
}
