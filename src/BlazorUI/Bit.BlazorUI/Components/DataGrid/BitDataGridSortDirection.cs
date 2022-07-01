namespace Bit.BlazorUI;

/// <summary>
/// Describes the direction in which a <see cref="BitDataGrid{TGridItem}"/> column is sorted.
/// </summary>
public enum BitDataGridSortDirection
{
    /// <summary>
    /// Ascending order.
    /// </summary>
    Ascending,

    /// <summary>
    /// Descending order.
    /// </summary>
    Descending,

    /// <summary>
    /// Automatic sort order. When used with <see cref="BitDataGrid{TGridItem}.SortByColumnAsync(BitDataGridColumnBase{TGridItem}, BitDataGridSortDirection)"/>,
    /// the sort order will automatically toggle between <see cref="Ascending"/> and <see cref="Descending"/> on successive calls, and
    /// resets to <see cref="Ascending"/> whenever the specified column is changed.
    /// </summary>
    Auto,
}
