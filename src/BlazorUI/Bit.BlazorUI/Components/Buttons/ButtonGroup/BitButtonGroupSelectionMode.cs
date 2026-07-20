namespace Bit.BlazorUI;

/// <summary>
/// Determines how many items of a BitButtonGroup can be toggled at the same time.
/// </summary>
public enum BitButtonGroupSelectionMode
{
    /// <summary>
    /// The items act as plain action buttons and cannot be toggled.
    /// </summary>
    None,

    /// <summary>
    /// At most one item can be toggled at a time (rendered with the radiogroup accessibility pattern).
    /// </summary>
    Single,

    /// <summary>
    /// Any number of items can be toggled at the same time (rendered with the toolbar accessibility pattern).
    /// </summary>
    Multiple
}
