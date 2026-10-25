namespace Bit.Bdnd;

/// <summary>Non-generic seam so <see cref="BdndDragHandle"/> can control any draggable.</summary>
public interface IBdndDragHandleController
{
    /// <summary>True when the owning draggable only starts drags from a <see cref="BdndDragHandle"/>.</summary>
    bool RequiresHandle { get; }

    /// <summary>True when the owning draggable currently allows dragging.</summary>
    bool CanDrag { get; }

    /// <summary>True when the keyboard can pick the owning draggable up (from the handle, when it requires one).</summary>
    bool KeyboardEnabled => false;

    /// <summary>The id of the element holding the keyboard instructions the handle is described by.</summary>
    string? InstructionsId => null;

    /// <summary>The owning draggable's name for screen readers, which labels a handle that has no <c>aria-label</c>.</summary>
    string? Label => null;

    /// <summary>True while the owning draggable is being dragged.</summary>
    bool IsDragging => false;

    /// <summary>True when the mouse drags the owning draggable through the browser's native drag and drop.</summary>
    bool NativeDrag => true;

    /// <summary>True when touch or pen (or every pointer) drag the owning draggable through pointer events.</summary>
    bool PointerDragEnabled => false;
}
