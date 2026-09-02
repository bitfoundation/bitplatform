namespace Bit.BlazorUI;

/// <summary>
/// The event arguments of the SwipeTrap trigger event.
/// </summary>
public class BitSwipeTrapTriggerArgs(
    BitSwipeDirection direction,
    decimal diffX,
    decimal diffY,
    decimal velocityX = 0,
    decimal velocityY = 0,
    string? pointerType = null)
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

    /// <summary>
    /// The horizontal velocity of the swipe action in pixels per millisecond.
    /// </summary>
    public decimal VelocityX { get; set; } = velocityX;

    /// <summary>
    /// The vertical velocity of the swipe action in pixels per millisecond.
    /// </summary>
    public decimal VelocityY { get; set; } = velocityY;

    /// <summary>
    /// The type of the pointer that performed the swipe action: "mouse", "touch" or "pen".
    /// </summary>
    public string? PointerType { get; set; } = pointerType;
}
