namespace Bit.Bsqlite;

/// <summary>
/// Which storage backends to use, in order, and how much latitude selection has when the first
/// choice is unavailable.
/// </summary>
public sealed record BsqliteStorageSelection
{
    internal BsqliteStorageSelection(
        IReadOnlyList<string> candidates,
        bool allowNonPersistentFallback,
        BsqliteStorageMigrationMode migrationMode)
    {
        Candidates = candidates;
        AllowNonPersistentFallback = allowNonPersistentFallback;
        MigrationMode = migrationMode;
    }

    /// <summary>
    /// Backend names in preference order, most preferred first. Never empty.
    /// </summary>
    public IReadOnlyList<string> Candidates { get; }

    /// <summary>
    /// Whether selection may fall back to a backend that does not survive a reload. Off by default:
    /// silently demoting a persistent database to a volatile one loses the user's data at the next
    /// refresh, which is not a decision a fallback rule should be allowed to make on its own.
    /// </summary>
    public bool AllowNonPersistentFallback { get; }

    /// <summary>What to do when a better backend becomes available for an existing database.</summary>
    public BsqliteStorageMigrationMode MigrationMode { get; }

    /// <summary>
    /// Whether the choice is strict, meaning a single candidate that must be available or the open
    /// fails. A lone candidate is never quietly substituted.
    /// </summary>
    public bool IsStrict => Candidates.Count == 1;
}
