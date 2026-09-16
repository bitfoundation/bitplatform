namespace Bit.BlazorUI;

/// <summary>
/// Describes the data the grid needs from an external source. The same payload is passed to both
/// the grid's <c>OnRead</c> callback (server-side mode) and its <c>OnLoadMore</c> callback
/// (infinite-scrolling mode), so callers can perform their own sorting, filtering and paging
/// (e.g. against a database) in either flow.
/// </summary>
public sealed class BitDataGridReadRequest
{
    /// <summary>Zero-based number of items to skip (for paging/virtualization).</summary>
    public int Skip { get; init; }

    /// <summary>Maximum number of items to return. <c>null</c> means "all".</summary>
    public int? Take { get; init; }

    public IReadOnlyList<BitDataGridSortDescriptor> Sorts { get; init; } = Array.Empty<BitDataGridSortDescriptor>();

    public IReadOnlyList<BitDataGridFilterDescriptor> Filters { get; init; } = Array.Empty<BitDataGridFilterDescriptor>();

    /// <summary>
    /// The active group descriptors, in nesting order. Lets a server-side <c>OnRead</c> handler
    /// reconstruct the grouping the grid is displaying. Empty when no grouping is active.
    /// </summary>
    public IReadOnlyList<BitDataGridGroupDescriptor> Groups { get; init; } = Array.Empty<BitDataGridGroupDescriptor>();

    /// <summary>
    /// The grid-wide quick-search term (the grid's search box, or its <c>SearchText</c> parameter), or
    /// <c>null</c> when no search is active. It is a free-text term the handler should match across the
    /// columns it considers searchable, in addition to - not instead of - the per-column
    /// <see cref="Filters"/>.
    /// </summary>
    public string? Search { get; init; }

    public CancellationToken CancellationToken { get; init; }
}
