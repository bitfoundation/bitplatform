namespace Bit.BlazorUI;

/// <summary>How the pages of a document are laid out on the scrollable surface.</summary>
public enum BitPdfScrollMode
{
    /// <summary>Pages are stacked top to bottom and the surface scrolls vertically (the default).</summary>
    Vertical,

    /// <summary>Pages are placed side by side on one row and the surface scrolls horizontally.</summary>
    Horizontal,

    /// <summary>Pages flow left to right and wrap onto the next row, filling the width of the surface.</summary>
    Wrapped,

    /// <summary>Only the current page (or spread) is shown; navigation replaces it rather than scrolling to it.</summary>
    Page,
}
