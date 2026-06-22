using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI;

/// <summary>
/// Arguments passed to cell-level event callbacks (<c>OnCellClick</c>, <c>OnCellDoubleClick</c>,
/// <c>OnCellContextMenu</c>). Mirrors react-data-grid's <c>CellMouseArgs</c>.
/// </summary>
/// <typeparam name="TItem">The row item type.</typeparam>
public sealed class BitDataGridCellEventArgs<TItem>
{
    public required TItem Item { get; init; }
    // Note: this holds a live reference to the column rather than an immutable snapshot. The grid assumes
    // column instances remain stable for its lifetime, so this is safe today. If columns ever become
    // dynamically mutated, capture immutable metadata (e.g. Id and DisplayTitle) here instead of the whole column.
    public required BitDataGridColumn<TItem> Column { get; init; }

    /// <summary>The column field/identifier for convenience.</summary>
    public string ColumnId => Column.Id;

    /// <summary>The column's display title (header text).</summary>
    public string ColumnTitle => Column.DisplayTitle;

    /// <summary>The raw value of the cell.</summary>
    public object? Value { get; init; }

    /// <summary>The underlying browser mouse event.</summary>
    public MouseEventArgs Mouse { get; init; } = new();
}
