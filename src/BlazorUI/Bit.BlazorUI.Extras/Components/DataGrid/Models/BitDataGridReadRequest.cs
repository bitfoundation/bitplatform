namespace Bit.BlazorUI;

/// <summary>
/// Describes the data the grid needs from an external/server-side source.
/// Passed to the grid's <c>OnRead</c> callback so callers can perform their own
/// sorting, filtering and paging (e.g. against a database).
/// </summary>
public sealed class BitDataGridReadRequest
{
    /// <summary>Zero-based number of items to skip (for paging/virtualization).</summary>
    public int Skip { get; init; }

    /// <summary>Maximum number of items to return. <c>null</c> means "all".</summary>
    public int? Take { get; init; }

    public IReadOnlyList<BitDataGridSortDescriptor> Sorts { get; init; } = Array.Empty<BitDataGridSortDescriptor>();

    public IReadOnlyList<BitDataGridFilterDescriptor> Filters { get; init; } = Array.Empty<BitDataGridFilterDescriptor>();

    public CancellationToken CancellationToken { get; init; }
}
