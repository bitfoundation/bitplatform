using System.Runtime.Versioning;
using Bit.BlazeDb;
using Bit.BlazeDb.Browser;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Demo.Client.Data;

public enum StorageMode
{
    Opening,
    Opfs,
    LockedFallback,
    UnavailableFallback,
}

/// <summary>
/// Opens the demo's primary database once per app lifetime.
///
/// Storage is OPFS (Origin Private File System) when the browser allows it and
/// this tab wins the Web Locks election. Otherwise the demo degrades to
/// <see cref="BlazeDbInMemoryStorage"/> so every page still works - just without
/// durability across reloads.
/// </summary>
[SupportedOSPlatform("browser")]
public sealed class DemoDbService : IAsyncDisposable
{
    public const string DatabaseName = "blazedb-demo";

    private Task<BlazeDbDatabase>? _opening;
    private BlazeDbDatabase? _db;
    private BlazeDbOpfsStorage? _opfs;

    public StorageMode Mode { get; private set; } = StorageMode.Opening;

    /// <summary>Set when OPFS could not be used, so the UI can explain why.</summary>
    public string? StorageNote { get; private set; }

    /// <summary>Records every <see cref="IBlazeDbStorage"/> call the engine makes.</summary>
    public InstrumentedStorage? Trace { get; private set; }

    public bool IsOpen => _db is not null;

    public bool IsPersistent => Mode == StorageMode.Opfs;

    public string StorageLabel => Mode switch
    {
        StorageMode.Opfs => "OPFS",
        StorageMode.LockedFallback => "In-memory (another tab owns OPFS)",
        StorageMode.UnavailableFallback => "In-memory (OPFS unavailable)",
        _ => "Opening…",
    };

    public BlazeDbDatabase Db => _db ?? throw new InvalidOperationException("The database is not open yet.");

    public BlazeDbTable<Guid, TodoEntry> Todos => Db.GetTable(TodoEntry.Table);

    public BlazeDbTable<string, Setting> Settings => Db.GetTable(Setting.Table);

    /// <summary>Idempotent and safe to call concurrently from several components.</summary>
    public Task<BlazeDbDatabase> GetDatabaseAsync() => _opening ??= OpenAsync();

    private async Task<BlazeDbDatabase> OpenAsync()
    {
        try
        {
            return await OpenCoreAsync();
        }
        catch
        {
            // Do not cache a failed attempt: the next page to ask gets a fresh try instead of the
            // same faulted task forever. Whatever the attempt had already opened is released, or the
            // retry would find this tab holding its own writer lock.
            _opening = null;
            try
            {
                await DisposeAsync();
            }
            catch
            {
                // The original failure is the one worth reporting.
            }
            throw;
        }
    }

    private async Task<BlazeDbDatabase> OpenCoreAsync()
    {
        IBlazeDbStorage backend;

        try
        {
            _opfs = await BlazeDbOpfsStorage.CreateAsync(DatabaseName);
            backend = _opfs;
            Mode = StorageMode.Opfs;
        }
        catch (BlazeDbDatabaseLockedException ex)
        {
            Mode = StorageMode.LockedFallback;
            StorageNote = ex.Message;
            backend = new BlazeDbInMemoryStorage();
        }
        catch (Exception ex)
        {
            Mode = StorageMode.UnavailableFallback;
            StorageNote = ex.Message;
            backend = new BlazeDbInMemoryStorage();
        }

        Trace = new InstrumentedStorage(backend);

        try
        {
            _db = await DemoSandbox.OpenAsync(
                Trace,
                flushInterval: TimeSpan.FromMilliseconds(100),
                checkpointWalSize: 1 * 1024 * 1024);
        }
        catch (BlazeDbCorruptDatabaseException ex)
        {
            // Persisted data from an older or interrupted session cannot be read.
            // Rather than dead-ending the demo, start clean in memory.
            Mode = StorageMode.UnavailableFallback;
            StorageNote = $"Persisted data could not be recovered ({ex.Message}). Started a fresh in-memory database.";
            _opfs?.Dispose();
            _opfs = null;
            Trace = new InstrumentedStorage(new BlazeDbInMemoryStorage());
            _db = await DemoSandbox.OpenAsync(
                Trace,
                flushInterval: TimeSpan.FromMilliseconds(100),
                checkpointWalSize: 1 * 1024 * 1024);
        }

        // Seed once per database, remembered in the settings table - not "whenever the table is
        // empty", which would resurrect the sample rows after a visitor deliberately deleted them all.
        var todos = _db.GetTable(TodoEntry.Table);
        var settings = _db.GetTable(Setting.Table);
        if (settings.Get(SeededSetting) is null)
        {
            if (todos.Count == 0)
            {
                DemoData.Seed(_db, todos);
            }
            settings.Upsert(new Setting { Name = SeededSetting, Value = "true", UpdatedAt = DateTime.UtcNow });
            await _db.FlushAsync();
        }

        return _db;
    }

    private const string SeededSetting = "demo.seeded";

    public string GetSetting(string name, string fallback) =>
        Settings.Get(name)?.Value ?? fallback;

    public void SetSetting(string name, string value) =>
        Settings.Upsert(new Setting { Name = name, Value = value, UpdatedAt = DateTime.UtcNow });

    public async ValueTask DisposeAsync()
    {
        if (_db is not null)
        {
            await _db.DisposeAsync();
            _db = null;
        }

        _opfs?.Dispose();
        _opfs = null;
    }
}
