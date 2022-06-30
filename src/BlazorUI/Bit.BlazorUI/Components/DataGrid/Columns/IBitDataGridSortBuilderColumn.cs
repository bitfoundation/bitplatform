namespace Bit.BlazorUI;

/// <summary>
/// An interface that, if implemented by a <see cref="BitDataGridColumnBase{TGridItem}"/> subclass, allows a <see cref="BitDataGrid{TGridItem}"/>
/// to understand the sorting rules associated with that column.
///
/// If a <see cref="BitDataGridColumnBase{TGridItem}"/> subclass does not implement this, that column can still be marked as sortable and can
/// be the current sort column, but its sorting logic cannot be applied to the data queries automatically. The developer would be
/// responsible for implementing that sorting logic separately inside their <see cref="BitDataGridItemsProvider{TGridItem}"/>.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
public interface IBitDataGridSortBuilderColumn<TGridItem>
{
    /// <summary>
    /// Gets the sorting rules associated with the column.
    /// </summary>
    public BitDataGridSort<TGridItem>? SortBuilder { get; }
}
