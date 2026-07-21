namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Parameters for data to be supplied by a <see cref="BitDataGridLegacy{TGridItem}"/>'s <see cref="BitDataGridLegacy{TGridItem}.ItemsProvider"/>.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
public struct BitDataGridLegacyItemsProviderRequest<TGridItem>
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
    public BitDataGridLegacyColumnBase<TGridItem>? SortByColumn { get; }

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

    internal BitDataGridLegacyItemsProviderRequest(
        int startIndex, int? count, BitDataGridLegacyColumnBase<TGridItem>? sortByColumn, bool sortByAscending,
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
    /// Note that this only works if the current <see cref="SortByColumn"/> implements <see cref="IBitDataGridLegacySortBuilderColumn{TGridItem}"/>
    /// and exposes a non-null sort builder. If the column does not implement that interface, or implements it
    /// but its sort builder is null (as with <see cref="BitDataGridLegacyTemplateColumn{TGridItem}"/>), it will throw.
    /// </summary>
    /// <param name="source">An <see cref="IQueryable{TGridItem}"/>.</param>
    /// <returns>A new <see cref="IQueryable{TGridItem}"/> representing the <paramref name="source"/> with sorting rules applied.</returns>
    public IQueryable<TGridItem> ApplySorting(IQueryable<TGridItem> source) => SortByColumn switch
    {
        // A sort-builder column with a null SortBuilder cannot apply its sort; treat it as unsupported
        // (like a non-sort-builder column) rather than silently returning the unsorted source, which
        // would hide an active sort on e.g. BitDataGridLegacyTemplateColumn.
        IBitDataGridLegacySortBuilderColumn<TGridItem> { SortBuilder: { } sortBuilder } => sortBuilder.Apply(source, SortByAscending),
        null => source,
        _ => throw new NotSupportedException(ColumnNotSortableMessage(SortByColumn)),
    };

    /// <summary>
    /// Produces a collection of (property name, direction) pairs representing the sorting rules.
    ///
    /// Note that this only works if the current <see cref="SortByColumn"/> implements <see cref="IBitDataGridLegacySortBuilderColumn{TGridItem}"/>
    /// and exposes a non-null sort builder. If the column does not implement that interface, or implements it
    /// but its sort builder is null (as with <see cref="BitDataGridLegacyTemplateColumn{TGridItem}"/>), it will throw.
    /// </summary>
    /// <returns>A collection of (property name, direction) pairs representing the sorting rules</returns>
    public IReadOnlyCollection<(string PropertyName, BitDataGridLegacySortDirection Direction)> GetSortByProperties() => SortByColumn switch
    {
        // Mirror ApplySorting: a null SortBuilder on a sort-builder column is unsupported rather than
        // an empty (no-op) sort, so the caller isn't misled into thinking there is no active sort.
        IBitDataGridLegacySortBuilderColumn<TGridItem> { SortBuilder: { } sortBuilder } => sortBuilder.ToPropertyList(SortByAscending),
        null => Array.Empty<(string, BitDataGridLegacySortDirection)>(),
        _ => throw new NotSupportedException(ColumnNotSortableMessage(SortByColumn)),
    };

    private static string ColumnNotSortableMessage<T>(BitDataGridLegacyColumnBase<T> col)
        => col is IBitDataGridLegacySortBuilderColumn<T>
            ? $"The current sort column '{col.GetType().FullName}' implements {nameof(IBitDataGridLegacySortBuilderColumn<TGridItem>)} but its {nameof(IBitDataGridLegacySortBuilderColumn<TGridItem>.SortBuilder)} is null, so its sorting rules cannot be applied automatically."
            : $"The current sort column is of type '{col.GetType().FullName}', which does not implement {nameof(IBitDataGridLegacySortBuilderColumn<TGridItem>)}, so its sorting rules cannot be applied automatically.";
}
