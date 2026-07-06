namespace Bit.BlazorUI;

/// <summary>
/// A serializable snapshot of a <see cref="BitDataGrid{TItem}"/>'s user-adjustable state
/// (page, page size, sorts, filters, groups and per-column layout), obtained via
/// <c>GetState()</c> and restored via <c>ApplyStateAsync()</c>. Enables persisting grid state
/// across sessions (e.g. to local storage or a user-preferences store).
/// </summary>
/// <remarks>
/// Filter <see cref="BitDataGridFilterDescriptor.Value"/> is typed <see cref="object"/>; when a state
/// snapshot is round-tripped through JSON, deserialize those values back to their intended CLR types
/// (number/date/bool/enum) before applying, otherwise the filters fail closed and match no rows.
/// </remarks>
public sealed class BitDataGridState
{
    /// <summary>The 1-based current page. Only meaningful while paging is active.</summary>
    public int CurrentPage { get; set; } = 1;

    /// <summary>The user-selected page size, or <c>null</c> when the grid's <c>PageSize</c> parameter applies.</summary>
    public int? PageSize { get; set; }

    public List<BitDataGridSortDescriptor> Sorts { get; set; } = new();

    public List<BitDataGridFilterDescriptor> Filters { get; set; } = new();

    public List<BitDataGridGroupDescriptor> Groups { get; set; } = new();

    /// <summary>Per-column layout state (visibility, resized width, display order).</summary>
    public List<BitDataGridColumnState> Columns { get; set; } = new();
}

/// <summary>Layout state of a single column inside a <see cref="BitDataGridState"/> snapshot.</summary>
public sealed class BitDataGridColumnState
{
    /// <summary>The column's resolved id (<c>ColumnId</c> or <c>Field</c>).</summary>
    public string ColumnId { get; set; } = string.Empty;

    public bool Visible { get; set; } = true;

    /// <summary>The user-resized width in pixels, or <c>null</c> when the column keeps its declared width.</summary>
    public double? Width { get; set; }

    /// <summary>Zero-based display order of the column.</summary>
    public int Order { get; set; }
}
