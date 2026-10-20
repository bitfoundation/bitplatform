using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow;

/// <summary>
/// Payload for node-related mouse events (click, double-click, context menu,
/// enter/leave). Carries both the node and the originating DOM event so callers
/// can read modifier keys and pointer coordinates (e.g. to position a context menu).
/// </summary>
public readonly record struct BlowNodeMouseEvent(BlowNode Node, MouseEventArgs Event);

/// <summary>
/// Payload for edge-related mouse events (double-click, context menu, enter/leave).
/// </summary>
public readonly record struct BlowEdgeMouseEvent(BlowEdge Edge, MouseEventArgs Event);

/// <summary>
/// Payload for events that act on a whole selection rather than one element: dragging several
/// nodes at once, or right-clicking one of them. <see cref="Nodes"/> is the set the gesture
/// affects, in the order the canvas is moving them.
/// </summary>
public readonly record struct BlowSelectionMouseEvent(IReadOnlyList<BlowNode> Nodes, MouseEventArgs Event);

/// <summary>
/// Identifies the handle a connection drag started from. Raised by
/// <c>BlowCanvas.OnConnectStart</c>.
/// </summary>
public readonly record struct BlowConnectionStartInfo(
    string NodeId,
    string? HandleId,
    BlowHandleType HandleType);

/// <summary>
/// Payload for <c>BlowCanvas.OnReconnect</c>, raised when an existing edge's
/// endpoint is dragged to a new handle. <see cref="Edge"/> already reflects the
/// updated source/target; <see cref="Connection"/> describes the new wiring.
/// </summary>
public readonly record struct BlowEdgeReconnect(BlowEdge Edge, BlowConnection Connection);

/// <summary>
/// Payload for <c>BlowCanvas.OnReconnectStart</c>. <see cref="MovingEnd"/> is the endpoint the
/// user grabbed; the opposite end stays anchored for the duration of the gesture.
/// </summary>
public readonly record struct BlowEdgeReconnectStart(BlowEdge Edge, BlowHandleType MovingEnd);

/// <summary>
/// Payload for <c>BlowCanvas.OnReconnectEnd</c>, raised when a reconnect gesture finishes.
/// <see cref="Rewired"/> is false when the endpoint was dropped somewhere invalid and the edge
/// was left untouched - the usual cue to delete it.
/// </summary>
public readonly record struct BlowEdgeReconnectEnd(BlowEdge Edge, BlowHandleType MovingEnd, bool Rewired);
