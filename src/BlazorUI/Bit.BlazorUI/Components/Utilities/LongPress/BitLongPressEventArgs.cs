namespace Bit.BlazorUI;

/// <summary>
/// The event arguments of <see cref="BitLongPress.OnLongPress"/>.
/// </summary>
public class BitLongPressEventArgs(double clientX, double clientY, string pointerType)
{
    /// <summary>
    /// The horizontal point the press was held at, in pixels relative to the viewport: where the pointer went down, or
    /// the middle of the focused element for a press from the keyboard. It is what a menu opened at a point is given
    /// (<c>BitContextMenu.OpenAt</c>, <c>BitCallout.OpenAt</c>).
    /// </summary>
    public double ClientX { get; } = clientX;

    /// <summary>
    /// The vertical point the press was held at, in pixels relative to the viewport.
    /// </summary>
    public double ClientY { get; } = clientY;

    /// <summary>
    /// What held the press: <c>"touch"</c>, <c>"pen"</c>, <c>"mouse"</c>, or <c>"keyboard"</c> for the keys of
    /// <see cref="BitLongPress.KeyboardTrigger"/>.
    /// </summary>
    public string PointerType { get; } = pointerType;
}
