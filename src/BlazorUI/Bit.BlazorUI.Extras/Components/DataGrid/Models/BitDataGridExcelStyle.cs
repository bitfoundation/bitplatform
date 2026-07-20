namespace Bit.BlazorUI;

/// <summary>
/// The visual theme a styled Excel export bakes into the workbook (see
/// <c>BitDataGrid.ExcelExportStyled</c>). When the option is enabled the grid samples these values
/// from its rendered cells via JS interop, so the export follows the active theme (including
/// dark mode and custom CSS). Colors are CSS hex strings (<c>#rrggbb</c>); a null color falls back
/// to Excel's default.
/// </summary>
public class BitDataGridExcelStyle
{
    /// <summary>Header row cell background color.</summary>
    public string? HeaderBackground { get; set; }

    /// <summary>Header row text color.</summary>
    public string? HeaderForeground { get; set; }

    /// <summary>Whether the header text renders bold. Defaults to true (matching the grid header).</summary>
    public bool HeaderBold { get; set; } = true;

    /// <summary>Whether the header text renders italic.</summary>
    public bool HeaderItalic { get; set; }

    /// <summary>Data row cell background color.</summary>
    public string? RowBackground { get; set; }

    /// <summary>Data row text color.</summary>
    public string? RowForeground { get; set; }

    /// <summary>Whether the data row text renders bold.</summary>
    public bool RowBold { get; set; }

    /// <summary>Whether the data row text renders italic.</summary>
    public bool RowItalic { get; set; }

    /// <summary>Background color of the alternating (even) data rows; null when the grid isn't striped.</summary>
    public string? StripeBackground { get; set; }

    /// <summary>Cell border color; null exports without cell borders.</summary>
    public string? BorderColor { get; set; }

    /// <summary>Whether cells also get vertical (left/right) borders, mirroring the grid's
    /// <c>Bordered</c> mode; otherwise only the row-separator bottom border is written.</summary>
    public bool VerticalBorders { get; set; }
}
