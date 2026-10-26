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
/// The pages read and write only inside <see cref="UseAsync{T}(Func{BlazeDbDatabase, T})"/>, never through a
/// database or a table they keep: the engine assumes one writer and no concurrent readers, and on a server, where
/// circuits run on many threads, this is the one place to serialize them.
/// </para>
/// </summary>
public abstract class SampleDatabase : IAsyncDisposable
{
    /// <summary>The database's name in the browser: its OPFS directory or IndexedDB database, and its Web Lock.</summary>
    public const string Name = "bit-blazedb-samples";

    private Task<BlazeDbDatabase>? _opening;
    private IBlazeDbStorage? _storage;

    /// <summary>What keeps the rows - files, OPFS, IndexedDB or memory - once the database is open.</summary>
    public string Backend { get; protected set; } = "Not open yet";

    /// <summary>Where exactly, or why there.</summary>
    public string? BackendNote { get; protected set; }

    /// <summary>Why the pages cannot use the database where they render now, or null when they can.</summary>
    public virtual string? Unreachable => null;

    /// <summary>Runs <paramref name="work"/> against the database, opening it the first time.</summary>
    public virtual async Task<T> UseAsync<T>(Func<BlazeDbDatabase, T> work) => work(await GetAsync());

    /// <inheritdoc cref="UseAsync{T}(Func{BlazeDbDatabase, T})"/>
    public virtual async Task<T> UseAsync<T>(Func<BlazeDbDatabase, Task<T>> work) => await work(await GetAsync());

    /// <summary>The storage this host keeps the database in. It is disposed after the database, when it is disposable.</summary>
    protected abstract ValueTask<IBlazeDbStorage> CreateStorageAsync();

    private Task<BlazeDbDatabase> GetAsync() => _opening ??= OpenAsync();

    private async Task<BlazeDbDatabase> OpenAsync()
    {
        try
        {
            _storage = await CreateStorageAsync();

            return await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = _storage }
                .AddTable(TodoItem.Table)
                // Runs once per database, the first time it opens, and moves its UserVersion to 1: seeding as a
                // migration means that deleting every row does not bring the sample rows back next time.
                .AddMigration(1, SampleData.Seed));
        }
        catch
        {
            // The next page to ask tries again rather than getting the same failure forever, and the storage
            // goes, or the retry would find this process (or tab) holding its lock.
            _opening = null;
            (_storage as IDisposable)?.Dispose();
            _storage = null;
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        // The database first - its last flush needs the storage - and only then the storage, which releases the
        // lock it holds: the directory's on a server or a device, the tab's Web Lock in a browser.
        if (_opening is { IsCompletedSuccessfully: true } opening)
        {
            await opening.Result.DisposeAsync();
        }

        (_storage as IDisposable)?.Dispose();
    }
}
