namespace Bit.BlazorUI.Legacy;

/// <summary>
/// An interface that, if implemented by a <see cref="BitDataGridLegacyColumnBase{TGridItem}"/> subclass, allows a <see cref="BitDataGridLegacy{TGridItem}"/>
/// to understand the sorting rules associated with that column.
///
/// If a <see cref="BitDataGridLegacyColumnBase{TGridItem}"/> subclass does not implement this, that column can still be marked as sortable and can
/// be the current sort column, but its sorting logic cannot be applied to the data queries automatically. The developer would be
/// responsible for implementing that sorting logic separately inside their <see cref="BitDataGridItemsProvider{TGridItem}"/>.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
public interface IBitDataGridLegacySortBuilderColumn<TGridItem>
{
    /// <summary>
    /// Gets the sorting rules associated with the column.
    /// </summary>
    public BitDataGridLegacySort<TGridItem>? SortBuilder { get; }
}
