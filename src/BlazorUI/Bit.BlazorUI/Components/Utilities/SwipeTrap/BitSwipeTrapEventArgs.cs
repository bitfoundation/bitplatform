namespace Bit.BlazorUI;

/// <summary>
/// The event arguments of the SwipeTrap events.
/// </summary>
public class BitSwipeTrapEventArgs(
    decimal startX,
    decimal startY,
    decimal diffX,
    decimal diffY,
    decimal velocityX = 0,
    decimal velocityY = 0,
    string? pointerType = null,
    bool isCanceled = false)
{
    /// <summary>
    /// The horizontal start point of the swipe action in pixels, relative to the viewport.
    /// </summary>
    public decimal StartX { get; set; } = startX;

    /// <summary>
    /// The vertical start point of the swipe action in pixels, relative to the viewport.
    /// </summary>
    public decimal StartY { get; set; } = startY;

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

    /// <summary>
    /// Whether the swipe action ended by being canceled (e.g. the browser took the gesture over)
    /// instead of a normal release. Only meaningful in the OnEnd event.
    /// </summary>
    public bool IsCanceled { get; set; } = isCanceled;
}
