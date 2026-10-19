namespace Bit.Bsqlite.Data;

/// <summary>
/// What the current storage backend can honour. The core enforces these generically so a new
/// backend never needs a special case in the command layer.
/// </summary>
public readonly record struct BsqliteRuntimeLimits
{
    /// <summary>No backend-specific restrictions beyond the product-wide WAL ban.</summary>
    public static BsqliteRuntimeLimits Unrestricted { get; } = new()
    {
        SupportsMultiDatabaseTransactions = true,
        CanChangePageSize = true,
    };

    /// <summary>
    /// Whether a transaction may span <c>ATTACH</c>ed databases. <c>OPFSCoopSyncVFS</c> cannot.
    /// </summary>
    public bool SupportsMultiDatabaseTransactions { get; init; }

    /// <summary>
    /// Whether <c>PRAGMA page_size</c> may be changed. Block-oriented backends pin the page size
    /// to their block size.
    /// </summary>
    public bool CanChangePageSize { get; init; }
}
