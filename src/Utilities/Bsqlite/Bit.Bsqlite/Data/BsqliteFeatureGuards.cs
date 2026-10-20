using System.Text.RegularExpressions;

namespace Bit.Bsqlite.Data;

/// <summary>
/// SQL restrictions that are either product-wide or declared by the selected backend's capabilities.
/// </summary>
/// <remarks>
/// WAL is product-wide: no web VFS can provide the shared memory it needs. <c>ATTACH</c> and
/// <c>PRAGMA page_size</c> are capability-gated so a backend that cannot honour them fails here
/// rather than corrupting silently.
/// </remarks>
public static partial class BsqliteFeatureGuards
{
    /// <summary>
    /// Throws when <paramref name="commandText"/> asks for something this product cannot honour.
    /// </summary>
    public static void EnsureSupported(string? commandText)
        => EnsureSupported(commandText, BsqliteRuntimeLimits.Unrestricted);

    /// <summary>
    /// Throws when <paramref name="commandText"/> asks for something
    /// <paramref name="limits"/> cannot honour.
    /// </summary>
    public static void EnsureSupported(string? commandText, BsqliteRuntimeLimits limits)
    {
        if (string.IsNullOrWhiteSpace(commandText))
        {
            return;
        }

        // Every guarded statement contains one of these words. Checking for them first keeps the
        // regular expressions off the hot path, where almost no SQL mentions either.
        if (!commandText.Contains("pragma", StringComparison.OrdinalIgnoreCase)
            && !commandText.Contains("attach", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (WalJournalMode().IsMatch(commandText))
        {
            throw new BsqliteException(
                "WAL mode is not available in the browser: WebAssembly has no shared-memory "
                + "primitives for it, and no web VFS implements it. Bsqlite uses DELETE or "
                + "TRUNCATE journaling. Concurrent reads, where a backend offers them, are a VFS "
                + "capability rather than a journal-mode setting.");
        }

        if (!limits.SupportsMultiDatabaseTransactions && Attach().IsMatch(commandText))
        {
            throw new BsqliteException(
                "ATTACH is not available on this storage backend: it cannot run a transaction "
                + "that spans more than one database. Open each database on its own connection.");
        }

        if (!limits.CanChangePageSize && PageSizeAssignment().IsMatch(commandText))
        {
            throw new BsqliteException(
                "PRAGMA page_size cannot be changed on this storage backend: the page size is "
                + "fixed to the backend's block size.");
        }
    }

    [GeneratedRegex(
        @"pragma\s+journal_mode\s*=\s*['""`]?wal\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WalJournalMode();

    [GeneratedRegex(
        @"(?:^|;)\s*attach(?:\s+database)?\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Attach();

    [GeneratedRegex(
        @"pragma\s+page_size\s*=",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PageSizeAssignment();
}
