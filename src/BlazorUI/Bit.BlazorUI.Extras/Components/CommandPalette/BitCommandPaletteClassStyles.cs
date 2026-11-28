namespace Bit.BlazorUI;

public class BitCommandPaletteClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitCommandPalette: the modal dialog, or the section of an
    /// inline palette.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the header of the BitCommandPalette, which holds the breadcrumbs and the search box.
    /// </summary>
    public string? Header { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the search box of the BitCommandPalette.
    /// </summary>
    public string? Input { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the list of the BitCommandPalette.
    /// </summary>
    public string? List { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each group heading of the BitCommandPalette.
    /// </summary>
    public string? GroupHeading { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each row of the BitCommandPalette.
    /// </summary>
    public string? Item { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the active row of the BitCommandPalette.
    /// </summary>
    public string? ActiveItem { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for what a page of the BitCommandPalette with no commands at all shows.
    /// </summary>
    public string? Empty { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for what a search of the BitCommandPalette that matched nothing shows.
    /// </summary>
    public string? NoResults { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the footer of the BitCommandPalette, with its keyboard hints and toasts.
    /// </summary>
    public string? Footer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the detail pane of the BitCommandPalette.
    /// </summary>
    public string? Detail { get; set; }
}
