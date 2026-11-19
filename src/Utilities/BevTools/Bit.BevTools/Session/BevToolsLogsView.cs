namespace Bit.BevTools;

internal sealed class BevToolsLogsView
{
    /// <summary>One row per category, with its counts per level, instead of one per entry.</summary>
    public bool ByCategory { get; set; }

    /// <summary>One row per database command (Entity Framework Core's, by SQL) instead of one per entry.</summary>
    public bool ByCommand { get; set; }

    /// <summary>Only the entries of this command (its SQL, whitespace collapsed), when one was opened from the Database view.</summary>
    public string? Command { get; set; }
}
