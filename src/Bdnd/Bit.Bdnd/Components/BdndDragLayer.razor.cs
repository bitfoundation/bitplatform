namespace Bit.Bdnd;

/// <summary>
/// Renders a custom drag preview that follows the pointer, the equivalent of React DnD's
/// <c>useDragLayer</c>. Pair it with <c>UseCustomDragLayer="true"</c> on the
/// <see cref="BdndDraggable{TItem}"/> to hide the native preview. The child content receives a
/// <see cref="BdndDragLayerState{TItem}"/> with the live offsets.
/// </summary>
/// <typeparam name="TItem">The type of the dragged item payload the preview renders.</typeparam>
public partial class BdndDragLayer<TItem>
{
}
