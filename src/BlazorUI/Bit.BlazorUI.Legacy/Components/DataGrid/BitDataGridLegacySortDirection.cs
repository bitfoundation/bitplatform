namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Describes the direction in which a <see cref="BitDataGridLegacy{TGridItem}"/> column is sorted.
/// </summary>
public enum BitDataGridLegacySortDirection
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
    /// Automatic sort order. When used with <see cref="BitDataGridLegacy{TGridItem}.SortByColumnAsync(BitDataGridLegacyColumnBase{TGridItem}, BitDataGridLegacySortDirection)"/>,
    /// the sort order will automatically toggle between <see cref="Ascending"/> and <see cref="Descending"/> on successive calls, and
    /// resets to <see cref="Ascending"/> whenever the specified column is changed.
    /// </summary>
    Auto,
}
