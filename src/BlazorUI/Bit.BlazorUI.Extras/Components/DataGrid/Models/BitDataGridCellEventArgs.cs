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

    // Holds a live reference to the column so handlers can reach the full column instance. The
    // identity-level metadata (ColumnId/ColumnTitle) is snapshotted at event-build time below so an
    // async handler always observes a consistent payload even if the column's title changes after dispatch.
    private readonly BitDataGridColumn<TItem> _column = default!;
    public required BitDataGridColumn<TItem> Column
    {
        get => _column;
        init
        {
            _column = value;
            ColumnId = value.Id;
            ColumnTitle = value.DisplayTitle;
        }
    }

    /// <summary>The column field/identifier for convenience, snapshotted when the event was raised.</summary>
    public string ColumnId { get; private init; } = string.Empty;

    /// <summary>The column's display title (header text), snapshotted when the event was raised.</summary>
    public string ColumnTitle { get; private init; } = string.Empty;

    /// <summary>The raw value of the cell.</summary>
    public required object? Value { get; init; }

    /// <summary>The underlying browser mouse event.</summary>
    public required MouseEventArgs Mouse { get; init; }
}
