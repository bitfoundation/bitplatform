namespace Bit.Bdnd;

/// <summary>Non-generic seam so <see cref="BdndDragHandle"/> can control any draggable.</summary>
public interface IBdndDragHandleController
{
    /// <summary>True when the owning draggable only starts drags from a <see cref="BdndDragHandle"/>.</summary>
    bool RequiresHandle { get; }

    /// <summary>True when the owning draggable currently allows dragging.</summary>
    bool CanDrag { get; }
}
