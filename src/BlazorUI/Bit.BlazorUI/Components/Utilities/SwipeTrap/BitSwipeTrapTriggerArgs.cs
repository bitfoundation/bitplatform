namespace Bit.BlazorUI;

/// <summary>
/// The event arguments of the SwipeTrap trigger event.
/// </summary>
public class BitSwipeTrapTriggerArgs(
    BitPlacement direction,
    decimal diffX,
    decimal diffY,
    decimal velocityX = 0,
    decimal velocityY = 0,
    string? pointerType = null,
    decimal duration = 0)
{
    /// <summary>
    /// The swipe direction in which the action triggered.
    /// </summary>
    /// <remarks>
    /// This is the direction the pointer travelled in, read off the screen rather than off the reading
    /// direction, so it is always one of the physical four: <see cref="BitPlacement.Top"/>,
    /// <see cref="BitPlacement.Bottom"/>, <see cref="BitPlacement.Left"/> or
    /// <see cref="BitPlacement.Right"/>. A handler that wants to know whether the swipe went with or against
    /// the reading direction compares it against <see cref="BitComponentBase.Dir"/> itself, the way
    /// BitNavPanel does.
    /// </remarks>
    public BitPlacement Direction { get; set; } = direction;

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
    /// The elapsed time of the swipe action in milliseconds, measured from the moment it started.
    /// </summary>
    public decimal Duration { get; set; } = duration;
}
