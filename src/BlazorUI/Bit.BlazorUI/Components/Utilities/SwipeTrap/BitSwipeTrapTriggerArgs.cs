namespace Bit.BlazorUI;

/// <summary>
/// The event arguments of the SwipeTrap trigger event.
/// </summary>
public class BitSwipeTrapTriggerArgs(BitSwipeDirection direction, decimal diffX, decimal diffY)
{
    /// <summary>
    /// The swipe direction in which the action triggered.
    /// </summary>
    public BitSwipeDirection Direction { get; set; } = direction;

    /// <summary>
    /// The horizontal difference of swipe action in pixels.
    /// </summary>
    public decimal DiffX { get; set; } = diffX;

    /// <summary>
    /// The vertical difference of swipe action in pixels.
    /// </summary>
    public decimal DiffY { get; set; } = diffY;
}
