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
