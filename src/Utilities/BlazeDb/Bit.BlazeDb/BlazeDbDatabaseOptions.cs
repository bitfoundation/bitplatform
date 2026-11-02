using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb;

/// <summary>
/// Settings for <see cref="BlazeDbDatabase.OpenAsync"/>. Tables must be added with
/// <see cref="AddTable"/> (or come from the EF Core model when using <c>UseBlazeDb</c>).
/// </summary>
public sealed class BlazeDbDatabaseOptions
{
    /// <summary>
    /// Storage backend for durability. When null the database is purely in-memory (useful for
    /// tests and caches); all data is lost when the instance goes away.
    /// </summary>
    public IBlazeDbStorage? Storage { get; set; }

    /// <summary>
    /// How often the background flusher writes buffered WAL commits to storage.
    /// Committed data lives in memory immediately; this is only the durability lag.
    /// </summary>
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// When the WAL file grows beyond this size, the flusher writes a full snapshot and starts
    /// a fresh WAL (checkpoint/compaction).
    /// </summary>
    public long CheckpointWalSize { get; set; } = 4 * 1024 * 1024;

    /// <summary>
    /// Headroom to leave free in the origin's storage allowance. The engine refuses to flush once
    /// a write would eat into this margin, so there is room left for the browser and for the
    /// snapshot a later checkpoint has to write. Only consulted when the backend implements
    /// <see cref="IBlazeDbQuotaAwareStorage"/>.
    /// </summary>
    public long QuotaReserveBytes { get; set; } = 4 * 1024 * 1024;

    /// <summary>
    /// Invoked when a flush is refused because storage is nearly full, after the engine has
    /// already tried compacting. The application can use it to prompt the user, prune data, or
    /// request persistent storage. Called from the background flusher, so keep it quick and
    /// marshal to the UI thread yourself if needed.
    /// </summary>
    public Action<BlazeDbStorageQuota>? OnQuotaPressure { get; set; }

    /// <summary>
    /// Opens the database as a read-only replica: state is recovered from storage as usual, but
    /// nothing is ever written back and any attempt to mutate a table throws. This is what a
    /// second browser tab uses - only one tab may hold the writer lock, and the others follow
    /// along by reloading (see <see cref="BlazeDbDatabase.ReloadAsync"/>).
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Invoked after a checkpoint has been made durable, with the new generation number. A writer
    /// tab uses this to tell replicas that a fresh snapshot is available to reload from.
    /// </summary>
    public Action<ulong>? OnCheckpoint { get; set; }

    /// <summary>Descriptors of tables this database will own. Prefer <see cref="AddTable"/>.</summary>
    public List<BlazeDbTableDescriptor> Tables { get; } = [];

    /// <summary>Data migrations to run on open; see <see cref="AddMigration"/>.</summary>
    public List<BlazeDbMigration> Migrations { get; } = [];

    /// <summary>
    /// Registers a data migration: when the database is opened with a <see cref="BlazeDbDatabase.UserVersion"/>
    /// below <paramref name="version"/>, <paramref name="migrate"/> runs - after recovery, before
    /// <see cref="BlazeDbDatabase.OpenAsync"/> returns - in a transaction that also moves the user version
    /// to <paramref name="version"/>. Migrations run lowest version first, each at most once, and one that
    /// throws fails the open and leaves the database at the version before it.
    /// <para>
    /// Field-level changes need no migration: a property added later reads as its default from older rows,
    /// and one removed is skipped (see "Schema evolution" in the guide). A migration is for changes to the
    /// data itself - filling a new property, splitting one, moving rows between tables. A read-only replica
    /// does not run them; the writer does, and the replica reads the result.
    /// </para>
    /// </summary>
    public BlazeDbDatabaseOptions AddMigration(long version, Action<BlazeDbDatabase> migrate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(version);
        ArgumentNullException.ThrowIfNull(migrate);
        Migrations.Add(new BlazeDbMigration(version, migrate));
        return this;
    }

    /// <summary>Registers a table. The descriptor is typically the generated <c>Table</c> member on a <c>[BlazeDbTable]</c> type.</summary>
    public BlazeDbDatabaseOptions AddTable(BlazeDbTableDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        Tables.Add(descriptor);
        return this;
    }

    /// <summary>Rejects settings the engine cannot run with, before anything is opened or recovered.</summary>
    internal void Validate()
    {
        // The upper bound is the timer's: PeriodicTimer accepts at most uint.MaxValue - 1 milliseconds.
        if (FlushInterval <= TimeSpan.Zero || FlushInterval.TotalMilliseconds > uint.MaxValue - 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(FlushInterval), FlushInterval, "The flush interval must be positive and below ~49.7 days.");
        }
        if (CheckpointWalSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(CheckpointWalSize), CheckpointWalSize, "The checkpoint WAL size must be positive.");
        }
        if (QuotaReserveBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(QuotaReserveBytes), QuotaReserveBytes, "The quota reserve cannot be negative.");
        }
        if (ReadOnly && Storage is null)
        {
            throw new ArgumentException("A read-only replica needs a storage backend to read from.", nameof(ReadOnly));
        }
        foreach (var table in Tables)
        {
            if (table.Name.StartsWith(BlazeDbMetaTable.ReservedPrefix, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Table name '{table.Name}' starts with '{BlazeDbMetaTable.ReservedPrefix}', which is reserved for the engine's own tables.",
                    nameof(Tables));
            }
        }
        var versions = new HashSet<long>();
        foreach (var migration in Migrations)
        {
            if (!versions.Add(migration.Version))
            {
                throw new ArgumentException($"More than one migration is registered for version {migration.Version}.", nameof(Migrations));
            }
        }
    }
}
