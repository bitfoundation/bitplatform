namespace Bit.BlazorUI;

/// <summary>
/// Determines how the nav items are rendered visually.
/// </summary>
public enum BitNavRenderType
{
    /// <summary>
    /// All items will be rendered normally only based on their own properties.
    /// </summary>
    Normal,

    /// <summary>
    /// Root elements are rendered in a specific way that resembles a grouped list of items.
    /// </summary>
    Grouped
}
