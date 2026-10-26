using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Samples.Core.Data;

/// <summary>
/// The one database every page uses, opened on first use and kept for as long as the container that registers it -
/// a singleton. The pages are the same in every host, but where the rows live is not, so each host registers its own
/// subclass: <c>ServerSampleDatabase</c> keeps them in files on the server, <see cref="BrowserSampleDatabase"/> in the
/// browser tab (the Wasm and Auto clients), <c>MauiSampleDatabase</c> in the app's data directory, and
/// <see cref="UnreachableSampleDatabase"/> stands in where the pages render on a server but the database lives in
/// the browser (the Wasm and Auto servers).
/// <para>
/// The pages read and write only inside <see cref="UseAsync{T}(Func{BlazeDbDatabase, Task{T}})"/>, never through a
/// database or a table they keep: the engine assumes one writer and no concurrent readers, and on a server, where
/// circuits run on many threads, this is the one place to serialize them (see <see cref="RunExclusiveAsync"/>).
/// </para>
/// </summary>
public abstract class SampleDatabase : IAsyncDisposable
{
    /// <summary>The database's name in the browser: its OPFS directory or IndexedDB database, and its Web Lock.</summary>
    public const string Name = "bit-blazedb-samples";

    private Task<Opened>? _opening;
    private bool _disposed;

    /// <summary>What keeps the rows - files, OPFS, IndexedDB or memory - once the database is open.</summary>
    public string Backend { get; protected set; } = "Not open yet";

    /// <summary>Where exactly, or why there.</summary>
    public string? BackendNote { get; protected set; }

    /// <summary>Why the pages cannot use the database where they render now, or null when they can.</summary>
    public virtual string? Unreachable => null;

    /// <summary>Runs <paramref name="work"/> against the database, opening it the first time.</summary>
    public virtual async Task<T> UseAsync<T>(Func<BlazeDbDatabase, Task<T>> work)
    {
        T result = default!;
        await RunExclusiveAsync(async () => result = await work(await GetAsync()));
        return result;
    }

    /// <inheritdoc cref="UseAsync{T}(Func{BlazeDbDatabase, Task{T}})"/>
    public Task<T> UseAsync<T>(Func<BlazeDbDatabase, T> work) => UseAsync<T>(db => Task.FromResult(work(db)));

    /// <inheritdoc cref="UseAsync{T}(Func{BlazeDbDatabase, Task{T}})"/>
    public Task UseAsync(Action<BlazeDbDatabase> work) => UseAsync<bool>(db =>
    {
        work(db);
        return Task.FromResult(true);
    });

    /// <summary>
    /// Runs <paramref name="work"/> - a page's use of the database, its first open, or its disposal - with the
    /// database to itself. WebAssembly has one thread, and the MAUI app's components render on the UI thread, so
    /// by default it simply runs; a host whose callers run on many threads overrides this to take turns.
    /// </summary>
    protected virtual Task RunExclusiveAsync(Func<Task> work) => work();

    /// <summary>
    /// The storage this host keeps the database in, and what the pages say about it. It is disposed after the
    /// database, when it is disposable. When it cannot be created, or the database cannot be opened in it, the
    /// rows are kept in memory instead.
    /// </summary>
    protected abstract ValueTask<SampleStorage> CreateStorageAsync();

    // A failed open is not kept: the next page to ask tries again rather than getting the same failure forever.
    // That is decided here, not in OpenAsync's catch: a storage that fails synchronously fails OpenAsync before it
    // has returned its task, so its catch would run before ??= stored that task - and clear nothing.
    private async Task<BlazeDbDatabase> GetAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_opening is { IsFaulted: true } or { IsCanceled: true })
        {
            _opening = null;
        }

        return (await (_opening ??= OpenAsync())).Database;
    }

    private async Task<Opened> OpenAsync()
    {
        SampleStorage? storage = null;
        try
        {
            storage = await CreateStorageAsync();
            return await OpenAsync(storage);
        }
        catch (Exception exception) when (storage?.Storage is not BlazeDbInMemoryStorage)
        {
            // Whatever the attempt took goes - above all its lock, or a retry would find this process (or tab)
            // holding it - and the pages carry on in memory rather than failing on every route: a corrupt store,
            // a browser without Web Locks or a directory the process cannot write all end up here.
            (storage?.Storage as IDisposable)?.Dispose();

            return await OpenAsync(new SampleStorage(new BlazeDbInMemoryStorage(), "Memory",
                $"The database could not be opened where this host keeps it ({exception.Message}), so its rows are kept in memory for as long as this runs."));
        }
    }

    private async Task<Opened> OpenAsync(SampleStorage storage)
    {
        var database = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = storage.Storage }
            .AddTable(TodoItem.Table)
            // Runs once per database, the first time it opens, and moves its UserVersion to 1: seeding as a
            // migration means that deleting every row does not bring the sample rows back next time.
            .AddMigration(1, SampleData.Seed));

        // Said only once the database is open in it, so the pages never name a backend - or a lock - it is not using.
        Backend = storage.Backend;
        BackendNote = storage.Note;

        return new(database, storage.Storage);
    }

    public virtual async ValueTask DisposeAsync() => await RunExclusiveAsync(async () =>
    {
        if (_disposed) return;
        _disposed = true;

        if (_opening is null) return;

        Opened opened;
        try
        {
            // An open still in flight finishes first: disposing the storage under it would leave a database
            // whose background flusher writes to storage that is gone.
            opened = await _opening;
        }
        catch
        {
            // A failed open has released whatever it took already.
            return;
        }

        // The database first - its last flush needs the storage - and only then the storage, which releases the
        // lock it holds: the directory's on a server or a device, the tab's Web Lock in a browser.
        try
        {
            await opened.Database.DisposeAsync();
        }
        finally
        {
            (opened.Storage as IDisposable)?.Dispose();
        }
    });

    /// <summary>Where a host keeps the rows: the storage, its name, and where exactly or why there.</summary>
    protected sealed record SampleStorage(IBlazeDbStorage Storage, string Backend, string Note);

    private sealed record Opened(BlazeDbDatabase Database, IBlazeDbStorage Storage);
}
