using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow;

/// <summary>
/// Payload for events that act on a whole selection rather than one element: dragging several
/// nodes at once, or right-clicking one of them. <see cref="Nodes"/> is the set the gesture
/// affects, in the order the canvas is moving them.
/// </summary>
public readonly record struct BlowSelectionMouseEvent(IReadOnlyList<BlowNode> Nodes, MouseEventArgs Event);
