namespace Bit.BlazorUI;

/// <summary>
/// Parameters for data to be supplied by a <see cref="BitQuickGrid{TGridItem}"/>'s <see cref="BitQuickGrid{TGridItem}.ItemsProvider"/>.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
public struct BitQuickGridItemsProviderRequest<TGridItem>
{
    /// <summary>
    /// The zero-based index of the first item to be supplied.
    /// </summary>
    public int StartIndex { get; }

    /// <summary>
    /// If set, the maximum number of items to be supplied. If not set, the maximum number is unlimited.
    /// </summary>
    public int? Count { get; }

    /// <summary>
    /// Specifies which column represents the sort order.
    ///
    /// Rather than inferring the sort rules manually, you should normally call either <see cref="ApplySorting(IQueryable{TGridItem})"/>
    /// or <see cref="GetSortByProperties"/>, since they also account for <see cref="SortByColumn" /> and <see cref="SortByAscending" /> automatically.
    /// </summary>
    public BitQuickGridColumnBase<TGridItem>? SortByColumn { get; }

    /// <summary>
    /// Specifies the current sort direction.
    ///
    /// Rather than inferring the sort rules manually, you should normally call either <see cref="ApplySorting(IQueryable{TGridItem})"/>
    /// or <see cref="GetSortByProperties"/>, since they also account for <see cref="SortByColumn" /> and <see cref="SortByAscending" /> automatically.
    /// </summary>
    public bool SortByAscending { get; }

    /// <summary>
    /// A token that indicates if the request should be cancelled.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    internal BitQuickGridItemsProviderRequest(
        int startIndex, int? count, BitQuickGridColumnBase<TGridItem>? sortByColumn, bool sortByAscending,
        CancellationToken cancellationToken)
    {
        StartIndex = startIndex;
        Count = count;
        SortByColumn = sortByColumn;
        SortByAscending = sortByAscending;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Applies the request's sorting rules to the supplied <see cref="IQueryable{TGridItem}"/>.
    ///
    /// Note that this only works if the current <see cref="SortByColumn"/> implements <see cref="IBitQuickGridSortBuilderColumn{TGridItem}"/>,
    /// otherwise it will throw.
    /// </summary>
    /// <param name="source">An <see cref="IQueryable{TGridItem}"/>.</param>
    /// <returns>A new <see cref="IQueryable{TGridItem}"/> representing the <paramref name="source"/> with sorting rules applied.</returns>
    public IQueryable<TGridItem> ApplySorting(IQueryable<TGridItem> source) => SortByColumn switch
    {
        IBitQuickGridSortBuilderColumn<TGridItem> sbc => sbc.SortBuilder?.Apply(source, SortByAscending) ?? source,
        null => source,
        _ => throw new NotSupportedException(ColumnNotSortableMessage(SortByColumn)),
    };

    /// <summary>
    /// Produces a collection of (property name, direction) pairs representing the sorting rules.
    ///
    /// Note that this only works if the current <see cref="SortByColumn"/> implements <see cref="IBitQuickGridSortBuilderColumn{TGridItem}"/>,
    /// otherwise it will throw.
    /// </summary>
    /// <returns>A collection of (property name, direction) pairs representing the sorting rules</returns>
    public IReadOnlyCollection<(string PropertyName, BitQuickGridSortDirection Direction)> GetSortByProperties() => SortByColumn switch
    {
        IBitQuickGridSortBuilderColumn<TGridItem> sbc => sbc.SortBuilder?.ToPropertyList(SortByAscending) ?? Array.Empty<(string, BitQuickGridSortDirection)>(),
        null => Array.Empty<(string, BitQuickGridSortDirection)>(),
        _ => throw new NotSupportedException(ColumnNotSortableMessage(SortByColumn)),
    };

    private static string ColumnNotSortableMessage<T>(BitQuickGridColumnBase<T> col)
        => $"The current sort column is of type '{col.GetType().FullName}', which does not implement {nameof(IBitQuickGridSortBuilderColumn<TGridItem>)}, so its sorting rules cannot be applied automatically.";
}
