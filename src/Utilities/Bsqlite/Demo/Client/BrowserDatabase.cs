using Bit.Bsqlite.Demo.Data;
using Bit.Bsqlite;
using Bit.Bsqlite.Data;
using Bit.Bsqlite.EntityFrameworkCore;
using Bit.Bsqlite.Interop;
using Bit.Bsqlite.Storage;

namespace Bit.Bsqlite.Demo.Client;

public enum StorageSwitchMode
{
    /// <summary>Copy the SQLite image, flip the sticky binding, delete the source.</summary>
    Migrate,

    /// <summary>Leave the old file (if any) and open an empty database on the target backend.</summary>
    Fresh,
}

/// <summary>
/// The sample's view of the library's shared <see cref="BsqliteDatabase"/>: hands out contexts
/// and drives the storage-switching UI. Everything that is not UI plumbing lives in the library.
/// </summary>
public sealed class BrowserDatabase(
    BsqliteDatabase database,
    IBsqliteDbContextFactory<AppDbContext> contexts,
    IEnumerable<IBsqliteStorageProvider> providers,
    IBsqliteStorageBindingStore bindings,
    BsqliteStorageMigrator migrator)
{
    public const string DatabaseName = "app.db";

    public BsqliteDatabase Database { get; } = database;

    public IReadOnlyList<IBsqliteStorageProvider> Providers { get; } = [.. providers];

    public IBsqliteStorageBindingStore Bindings { get; } = bindings;

    public BsqliteSession? Session => Database.Session;

    public BsqliteStorageResolution? Resolution => Database.Resolution;

    public BsqliteConnection? Connection => Database.Connection;

    /// <summary>Raised before the worker is torn down so pages can drop live queries and contexts.</summary>
    public event Func<Task>? BeforeSessionReset
    {
        add => Database.Closing += value;
        remove => Database.Closing -= value;
    }

    /// <summary>Raised after a session opens so pages can (re)load.</summary>
    public event Func<Task>? AfterSessionReset
    {
        add => Database.Opened += value;
        remove => Database.Opened -= value;
    }

    public async Task<BsqliteSession> EnsureSessionAsync(CancellationToken cancellationToken = default)
        => await Database.OpenAsync(cancellationToken);

    public Task<AppDbContext> OpenContextAsync(CancellationToken cancellationToken = default)
        => contexts.CreateDbContextAsync(cancellationToken);

    /// <summary>
    /// Whether the data can be moved between the two backends. A non-persistent backend keeps the
    /// database in the worker's own heap, where its admin API cannot reach: a move onto it would delete
    /// the source and open an empty database, and a move off it would have nothing to copy.
    /// </summary>
    public static bool CanMoveBetween(IBsqliteStorageProvider? source, IBsqliteStorageProvider target)
        => source is null || (source.Capabilities.IsPersistent && target.Capabilities.IsPersistent);

    /// <summary>
    /// Moves or recreates <see cref="DatabaseName"/> on <paramref name="targetProviderName"/>.
    /// </summary>
    public async Task SwitchStorageAsync(
        string targetProviderName,
        StorageSwitchMode mode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetProviderName);

        var target = Providers.FirstOrDefault(p =>
            string.Equals(p.Name, targetProviderName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"No registered provider named '{targetProviderName}'.");

        var source = Resolution?.Provider ?? await BoundProviderAsync(cancellationToken);

        if (mode is StorageSwitchMode.Migrate && !CanMoveBetween(source, target))
        {
            throw new InvalidOperationException(
                $"The data cannot move between {source!.Name} and {target.Name}: a database on a "
                + "non-persistent backend lives in the worker's memory, out of reach of the copy. Start empty instead.");
        }

        await Database.WhileClosedAsync(async ct =>
        {
            if (mode is StorageSwitchMode.Migrate
                && source is not null
                && !string.Equals(source.Name, target.Name, StringComparison.OrdinalIgnoreCase)
                && await source.Admin.ExistsAsync(DatabaseName, ct))
            {
                await migrator.MigrateAsync(DatabaseName, source, target, Bindings, ct);
            }
            else
            {
                await target.Admin.DeleteAsync(DatabaseName, ct);
                await Bindings.SetProviderNameAsync(DatabaseName, target.Name, ct);
            }
        }, cancellationToken);

        await Database.OpenAsync(cancellationToken);
    }

    /// <summary>
    /// Runs <paramref name="work"/> with the worker torn down, so admin import/delete can replace the
    /// file without racing an open connection - or a page reopening it halfway through - then reopens
    /// if it was open.
    /// </summary>
    public Task WhileClosedAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken = default)
        => Database.WhileClosedAsync(work, cancellationToken);

    public async Task<object?> ExecuteScalarAsync(
        string sql,
        CancellationToken cancellationToken = default)
    {
        var session = await EnsureSessionAsync(cancellationToken);
        await using var command = session.Connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync(cancellationToken);
    }

    public async Task ExecuteNonQueryAsync(
        string sql,
        CancellationToken cancellationToken = default)
    {
        var session = await EnsureSessionAsync(cancellationToken);
        await using var command = session.Connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// The provider the sticky binding names, whether or not a session is open - which is how a
    /// database that no longer opens can still be found, and deleted.
    /// </summary>
    public async Task<IBsqliteStorageProvider?> BoundProviderAsync(
        CancellationToken cancellationToken = default)
    {
        var name = await Bindings.GetProviderNameAsync(DatabaseName, cancellationToken);
        return name is null
            ? null
            : Providers.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
