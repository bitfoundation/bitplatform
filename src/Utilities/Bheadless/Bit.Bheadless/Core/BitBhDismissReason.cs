namespace Bit.Bheadless;

/// <summary>Why a floating layer asked to be dismissed.</summary>
public enum BitBhDismissReason
{
    /// <summary>The Escape key was pressed.</summary>
    EscapeKey,

    /// <summary>A pointer was pressed outside the layer (and its trigger / nested layers).</summary>
    PointerDownOutside,

    /// <summary>Focus moved outside the layer.</summary>
    FocusOutside,

    /// <summary>The layer was closed from code (selection, close button, …).</summary>
    Programmatic,

    /// <summary>
    /// The surface it was opened from closed (a submenu of a closing menu, a popover inside a closing
    /// dialog). The runtime has already closed it, so cancelling the request only reopens it.
    /// </summary>
    ParentClosed,

    /// <summary>The surface was swiped away (a drawer dragged towards its edge past the threshold).</summary>
    Swipe,
}
