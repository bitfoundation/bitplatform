namespace Bit.BlazorUI;

/// <summary>
/// Context passed to a resource-column template for one visible row. Reflects the
/// current hierarchy state so the template can indent by <see cref="Depth"/> and
/// react to group rows.
/// </summary>
public sealed class BitResourceTimelineRowContext
{
    /// <summary>The resource this row represents.</summary>
    public required BitResourceTimelineResource Resource { get; init; }

    /// <summary>Zero-based depth in the resource hierarchy (0 for root rows).</summary>
    public int Depth { get; init; }

    /// <summary>True when this resource has children (i.e. it is a group header).</summary>
    public bool HasChildren { get; init; }

    /// <summary>True when this group is currently collapsed.</summary>
    public bool Collapsed { get; init; }
}
