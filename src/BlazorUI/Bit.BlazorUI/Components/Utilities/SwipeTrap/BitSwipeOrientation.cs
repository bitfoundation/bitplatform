namespace Bit.BlazorUI;

/// <summary>
/// The lock orientation of the swipe trap component.
/// </summary>
public enum BitSwipeOrientation
{
    /// <summary>
    /// No orientation lock for the swipe trap.
    /// </summary>
    None,

    /// <summary>
    /// Horizontal orientation lock of trapping the swipe action.
    /// </summary>
    Horizontal,

    /// <summary>
    /// Vertical orientation lock of trapping the swipe action.
    /// </summary>
    Vertical,

    /// <summary>
    /// Locks the trap to the first orientation the gesture moves along, trapping that axis and zeroing the other.
    /// </summary>
    Auto
}
