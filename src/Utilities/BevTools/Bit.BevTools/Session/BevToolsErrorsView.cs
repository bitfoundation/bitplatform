namespace Bit.BevTools;

internal sealed class BevToolsErrorsView
{
    /// <summary>One row per group of errors that are one problem (<see cref="BevToolsErrorGroups"/>).</summary>
    public bool ByGroup { get; set; }

    /// <summary>Only the errors of this <see cref="BevToolsErrorRecord.Group"/>, shown by its <see cref="GroupTitle"/>.</summary>
    public string? Group { get; set; }

    public string? GroupTitle { get; set; }

    public BevToolsSortState Errors { get; } = new("last", ascending: false);

    public BevToolsSortState Groups { get; } = new("occurrences", ascending: false);
}
