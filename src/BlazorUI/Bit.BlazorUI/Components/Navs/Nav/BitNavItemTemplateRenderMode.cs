namespace Bit.BlazorUI;

/// <summary>
/// Defines how the item template of a nav is rendered.
/// </summary>
public enum BitNavItemTemplateRenderMode
{
    /// <summary>
    /// Renders the template inside the anchor (or the button) the item is, so the item keeps its click, its focus and its
    /// place in the keyboard navigation.
    /// </summary>
    Normal,

    /// <summary>
    /// Replaces the anchor (or the button) the item is with the template, which is what an item that is a control of its
    /// own needs. The template owns its clicks, its focus and its accessible name, and the item is left out of the
    /// keyboard navigation.
    /// </summary>
    Replace
}
