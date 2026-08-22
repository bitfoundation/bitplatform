namespace Bit.BlazorUI;

/// <summary>
/// The shape of the indicator that marks the selected item of a BitNavBar.
/// </summary>
public enum BitNavBarIndicator
{
    /// <summary>
    /// No indicator of its own: the selection is conveyed by the color of the item and, while an Accent is
    /// set, by the fill of the item.
    /// </summary>
    None,

    /// <summary>
    /// A line drawn along the edge of the selected item: its bottom edge in a horizontal navbar and its
    /// leading edge in a vertical rail, the way a tab strip marks its current tab.
    /// </summary>
    Line,

    /// <summary>
    /// A pill drawn behind the icon of the selected item, which is how a Material navigation bar marks its
    /// current destination. It takes the fill off the item itself, so the pill is the only filled part.
    /// </summary>
    Pill
}
