namespace Bit.BlazorUI;

/// <summary>
/// A materialized group of rows produced by grouping. Groups can be nested to any depth
/// (multi-level grouping); leaf groups carry the actual <see cref="Items"/> while parent
/// groups carry <see cref="SubGroups"/>.
/// </summary>
public sealed class BitDataGridGroup<TItem>
{
    /// <summary>The identifier of the column whose values define this group.</summary>
    public required string ColumnId { get; init; }
    public required object? Key { get; init; }

    /// <summary>The display text for this group's key, shown in the group header row.</summary>
    public string KeyText { get; init; } = string.Empty;

    /// <summary>Zero-based nesting depth (0 = top level).</summary>
    public int Level { get; init; }

    /// <summary>Stable, unique path identifying this group across the whole tree (used for collapse state).</summary>
    public required string Path { get; init; }

    /// <summary>
    /// The rows held directly by this group. Only populated for leaf groups (those without
    /// <see cref="SubGroups"/>); parent groups leave this empty and expose their rows through their
    /// nested subgroups, so a row is referenced once per tree rather than on every ancestor level.
    /// Use <see cref="Count"/> and <see cref="Aggregates"/> for parent-group summary data.
    /// </summary>
    public IReadOnlyList<TItem> Items { get; init; } = Array.Empty<TItem>();

    /// <summary>Child groups when this group is further grouped; empty for leaf groups.</summary>
    public IReadOnlyList<BitDataGridGroup<TItem>> SubGroups { get; init; } = Array.Empty<BitDataGridGroup<TItem>>();

    /// <summary>Aggregate values computed for this group (e.g. column sums or averages).</summary>
    public IReadOnlyList<BitDataGridAggregateResult> Aggregates { get; init; } = Array.Empty<BitDataGridAggregateResult>();

    public bool HasSubGroups => SubGroups.Count > 0;

    /// <summary>Total number of leaf rows in this group (including all nested subgroups).</summary>
    public required int Count { get; init; }
}
