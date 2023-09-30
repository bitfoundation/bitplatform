namespace Bit.BlazorUI;

public enum BitScrollbarVisibility
{
    /// <summary>
    /// Scrollbars are displayed automatically when needed based on the content size, and hidden when not needed.
    /// </summary>
    Auto,

    /// <summary>
    /// Scrollbars are always hidden, even if the content overflows the visible area.
    /// </summary>
    Hidden,

    /// <summary>
    /// Scrollbars are always visible, allowing users to scroll through the content even if it doesn't overflow the visible area.
    /// </summary>
    Scroll
}
