namespace Bit.Bdnd;

/// <summary>
/// Which pointers drag a <see cref="BdndDraggable{TItem}"/> through Bdnd's own pointer-driven drag rather than the
/// browser's native HTML5 drag and drop, which most touch browsers do not raise.
/// </summary>
public enum BdndPointerDrag
{
    /// <summary>
    /// Touch and pen drag through pointer events - after a short press (<c>TouchDelay</c>), so a swipe still
    /// scrolls - and the mouse through the native drag and drop. The default.
    /// </summary>
    Touch,

    /// <summary>
    /// Every pointer, the mouse included, drags through pointer events; the native drag and drop is off for the
    /// source. A drag then never leaves the page (no drop on another window or application) and is the same on every
    /// browser: the preview, the hit testing and the auto-scrolling are Bdnd's.
    /// </summary>
    All,

    /// <summary>Only the native drag and drop: touch input does whatever the platform's browser does with it.</summary>
    None,
}
