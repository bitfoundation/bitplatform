using System.Runtime.Versioning;
using Bit.BlazeDb.Browser;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Samples.Lazy.Client.Data;

/// <summary>
/// The tab's database, opened the first time a page asks for it and shared by every page after that: the README's
/// <c>AppDatabase</c> for an app that lazy loads Bit.BlazeDb. Program.cs registers it before the assembly is downloaded,
/// so none of its own fields is of the engine's types; they live in <see cref="Opened"/>, which the runtime lays out only
/// once a page asks for the database. Its methods may take and return the engine's types: only fields count.
/// <para>
/// The server registers it too, for the prerender of the todos page, which asks for the database only once it runs in
/// the browser: <see cref="GetAsync"/> is browser-only, as the backends it opens are.
/// </para>
/// </summary>
public sealed class AppDatabase : IAsyncDisposable
{
    private Opened? _opened;   // not a Task<BlazeDbDatabase>: Program.cs names AppDatabase before Bit.BlazeDb loads

    [SupportedOSPlatform("browser")]
    public Task<BlazeDbDatabase> GetAsync()
    {
        if (_opened is null || _opened.Failed) _opened = new Opened(); // a failed open is tried again
        return _opened.Database;
    }

    /// <summary>Where the rows are kept - OPFS or IndexedDB - once the database is open.</summary>
    public string? Backend => _opened?.Backend;

    public async ValueTask DisposeAsync()
    {
        if (_opened is { Failed: false }) await _opened.DisposeAsync(); // a failed open has released the lock already
    }

    // The engine's types live here, laid out only once a page asks for the database.
    private sealed class Opened : IAsyncDisposable
    {
        private IBlazeDbStorage? _storage;

        [SupportedOSPlatform("browser")]
        public Opened() => Database = OpenAsync();

        public Task<BlazeDbDatabase> Database { get; }

        public bool Failed => Database.IsFaulted || Database.IsCanceled;

        public string? Backend { get; private set; }

        [SupportedOSPlatform("browser")]
        private async Task<BlazeDbDatabase> OpenAsync()
        {
            // OPFS, or IndexedDB where the page cannot write OPFS (the README's "Without OPFS"): a Playwright WebKit
            // context, like a private window, refuses OPFS, and the end-to-end suite drives this sample in WebKit too.
            // Either takes this tab's writer lock.
            var opfs = await BlazeDbIndexedDbStorage.IsOpfsAvailableAsync();
            _storage = opfs ? await BlazeDbOpfsStorage.CreateAsync("app") : await BlazeDbIndexedDbStorage.CreateAsync("app");
            try
            {
                var database = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = _storage }.AddTable(TodoItem.Table));
                Backend = opfs ? "OPFS" : "IndexedDB";
                return database;
            }
            catch
            {
                (_storage as IDisposable)?.Dispose();               // releases the lock, for the next attempt or tab
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await (await Database).DisposeAsync();              // flushes what is still pending
            }
            finally
            {
                (_storage as IDisposable)?.Dispose();               // and only then releases the lock
            }
        }
    }
}
