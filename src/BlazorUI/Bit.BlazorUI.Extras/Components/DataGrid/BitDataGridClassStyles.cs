namespace Bit.BlazorUI;

/// <summary>
/// Defines per-part CSS class/style values for <see cref="BitDataGrid{TItem}"/>.
/// </summary>
public class BitDataGridClassStyles
{
    /// <summary>
    /// Custom class or style applied to the root element.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom class or style applied to the toolbar above the grid.
    /// </summary>
    public string? Toolbar { get; set; }

    /// <summary>
    /// Custom class or style applied to the column chooser panel.
    /// </summary>
    public string? ColumnChooser { get; set; }

    /// <summary>
    /// Custom class or style applied to the scrolling viewport that holds the rows.
    /// </summary>
    public string? Viewport { get; set; }

    /// <summary>
    /// Custom class or style applied to the row of column titles.
    /// </summary>
    public string? HeaderRow { get; set; }

    /// <summary>
    /// Custom class or style applied to each column header cell (the column's own <c>HeaderClass</c> is added after it).
    /// </summary>
    public string? HeaderCell { get; set; }

    /// <summary>
    /// Custom class or style applied to the row of column filters.
    /// </summary>
    public string? FilterRow { get; set; }

    /// <summary>
    /// Custom class or style applied to each data row (the grid's <c>RowClass</c> / <c>RowStyle</c> are added after it).
    /// </summary>
    public string? Row { get; set; }

    /// <summary>
    /// Custom class or style applied to each selected data row, after <see cref="Row"/>.
    /// </summary>
    public string? SelectedRow { get; set; }

    /// <summary>
    /// Custom class or style applied to each data cell (the column's own <c>CellClass</c> is added after it).
    /// </summary>
    public string? Cell { get; set; }

    /// <summary>
    /// Custom class or style applied to the cell of each group header row.
    /// </summary>
    public string? GroupRow { get; set; }

    /// <summary>
    /// Custom class or style applied to the content of each expanded detail row.
    /// </summary>
    public string? DetailRow { get; set; }

    /// <summary>
    /// Custom class or style applied to the footer row of aggregates.
    /// </summary>
    public string? FooterRow { get; set; }

    /// <summary>
    /// Custom class or style applied to the pager.
    /// </summary>
    public string? Pager { get; set; }

    /// <summary>
    /// Custom class or style applied to the cell that shows the empty message.
    /// </summary>
    public string? Empty { get; set; }

    /// <summary>
    /// Custom class or style applied to the cell that shows the loading indicator.
    /// </summary>
    public string? Loading { get; set; }
}
