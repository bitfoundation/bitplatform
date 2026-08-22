namespace Bit.BlazorUI;

/// <summary>
/// Defines the mode in which navigation is handled by the nav component.
/// </summary>
public enum BitNavMode
{
    /// <summary>
    /// The component follows the browser: it selects the item whose URL points at the page the app
    /// currently sits on, and it re-selects on every navigation.
    /// </summary>
    Automatic,

    /// <summary>
    /// The selection is driven by clicks and by the SelectedItem binding instead of by the current URL,
    /// which is what a component that switches between the panels of a single page needs.
    /// </summary>
    Manual
}
