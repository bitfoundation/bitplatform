namespace Bit.BlazorUI;

/// <summary>
/// Defines how the item template of a nav is rendered.
/// </summary>
public enum BitNavItemTemplateRenderMode
{
    /// <summary>
    /// Renders the template inside the button/anchor root element of the item.
    /// </summary>
    Normal,

    /// <summary>
    /// Replaces the button/anchor root element of the item.
    /// </summary>
    Replace
}
