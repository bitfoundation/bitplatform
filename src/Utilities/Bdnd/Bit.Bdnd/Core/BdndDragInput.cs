namespace Bit.Bdnd;

/// <summary>How the drag in progress is being made, as <see cref="IBdndMonitor.GetDragInput"/> reports it.</summary>
public enum BdndDragInput
{
    /// <summary>
    /// The browser's own HTML5 drag-and-drop: a mouse, a trackpad, or a pen or touch screen where the platform turns
    /// them into native drags.
    /// </summary>
    Native,

    /// <summary>The keyboard: the source was picked up with Space or Enter and is moved between targets with the arrow keys.</summary>
    Keyboard,

    /// <summary>
    /// Bdnd's pointer-driven drag: a touch or a pen (or, with <see cref="BdndPointerDrag.All"/>, the mouse), followed
    /// through pointer events.
    /// </summary>
    Pointer,
}
