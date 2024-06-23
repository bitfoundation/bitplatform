namespace Bit.BlazorUI;

public enum BitVisibility
{
    /// <summary>
    /// The content of the component is visible.
    /// </summary>
    Visible = 0,

    /// <summary>
    /// The content of the component is hidden, but the space it takes on the page remains (visibility:hidden).
    /// </summary>
    Hidden = 1,

    /// <summary>
    /// The component is hidden (display:none).
    /// </summary>
    Collapsed = 2
}
