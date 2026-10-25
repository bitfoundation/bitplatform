namespace Bit.Bdnd;

/// <summary>
/// Designates a sub-region of a <see cref="BdndDraggable{TItem}"/> configured with
/// <c>UseHandle="true"</c> as its grab handle. The handle is the native draggable element, so only
/// drags that start on it move the item; the whole draggable is still used as the drag image, and the
/// rest of it stays interactive.
/// </summary>
public partial class BdndDragHandle
{
}
