namespace Bit.BlazorUI;

/// <summary>
/// Describes the data the grid needs from an external source. The same payload is passed to both
/// the grid's <c>OnRead</c> callback (server-side mode) and its <c>OnLoadMore</c> callback
/// (infinite-scrolling mode), so callers can perform their own sorting, filtering and paging
/// (e.g. against a database) in either flow, and their own footer aggregates in server mode.
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

    /// <summary>
    /// The aggregates the footer shows from this read: one for each column that aggregates (hidden ones included), in
    /// column order. Compute them over the whole filtered data, not just the requested page, and return them in
    /// <see cref="BitDataGridReadResult{TItem}.Aggregates"/>; the footer then shows those in place of the page's own,
    /// and a column the result leaves out shows none. Empty when no column aggregates, and for the reads whose
    /// aggregates the grid does not use: the <c>OnLoadMore</c> batches, and the reads of an export or of
    /// <c>GetViewItemsAsync</c>.
    /// <para>The grid's first read goes out before its columns register, so it asks for none. When that read's result
    /// has aggregates (not <c>null</c>) but misses ones the columns declare, the grid reads once more, asking for them;
    /// it does the same when a column changes its aggregate later. A result without aggregates is never read again
    /// for them: the footer then aggregates the rows the grid holds.</para>
    /// </summary>
    public IReadOnlyList<BitDataGridAggregateDescriptor> Aggregates { get; init; } = Array.Empty<BitDataGridAggregateDescriptor>();

    public CancellationToken CancellationToken { get; init; }
}
