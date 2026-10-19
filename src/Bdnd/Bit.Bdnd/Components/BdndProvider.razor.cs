namespace Bit.Bdnd;

/// <summary>
/// The root of a drag-and-drop context, the equivalent of React DnD's
/// <c>&lt;DndProvider backend={HTML5Backend}&gt;</c>. It creates a <see cref="BdndDragDropManager"/> and
/// cascades it to every descendant <see cref="BdndDraggable{TItem}"/>, <see cref="BdndDropTarget{TItem}"/>
/// and <see cref="BdndDragLayer{TItem}"/>. Nest providers for isolated drag contexts.
/// </summary>
public partial class BdndProvider
{
}
