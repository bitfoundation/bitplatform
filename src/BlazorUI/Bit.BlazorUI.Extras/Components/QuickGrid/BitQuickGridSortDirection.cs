namespace Bit.BlazorUI;

/// <summary>
/// Describes the direction in which a <see cref="BitQuickGrid{TGridItem}"/> column is sorted.
/// </summary>
public enum BitQuickGridSortDirection
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
    /// Automatic sort order. When used with <see cref="BitQuickGrid{TGridItem}.SortByColumnAsync(BitQuickGridColumnBase{TGridItem}, BitQuickGridSortDirection)"/>,
    /// the sort order will automatically toggle between <see cref="Ascending"/> and <see cref="Descending"/> on successive calls, and
    /// resets to <see cref="Ascending"/> whenever the specified column is changed.
    /// </summary>
    Auto,
}
