namespace Bit.BlazorUI;

/// <summary>
/// The event arguments of the SwipeTrap events.
/// </summary>
public class BitSwipeTrapEventArgs(decimal startX, decimal startY, decimal diffX, decimal diffY)
{
    /// <summary>
    /// The horizontal start point of the swipe action in pixels.
    /// </summary>
    public decimal StartX { get; set; } = startX;

    /// <summary>
    /// The vertical start point of the swipe action in pixels.
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
}
