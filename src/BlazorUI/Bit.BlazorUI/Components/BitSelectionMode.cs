namespace Bit.BlazorUI;

/// <summary>
/// How many of a component's items can be selected at the same time.
/// </summary>
public enum BitSelectionMode
{
    /// <summary>
    /// Nothing can be selected: the items act as plain content or as plain action buttons.
    /// </summary>
    None,

    /// <summary>
    /// At most one item can be selected at a time, so selecting one clears the one before it.
    /// </summary>
    Single,

    /// <summary>
    /// Any number of items can be selected at the same time.
    /// </summary>
    Multiple
}
