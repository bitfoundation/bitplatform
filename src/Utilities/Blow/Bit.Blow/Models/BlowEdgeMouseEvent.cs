using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow;

/// <summary>
/// Payload for edge-related mouse events (double-click, context menu, enter/leave).
/// </summary>
public readonly record struct BlowEdgeMouseEvent(BlowEdge Edge, MouseEventArgs Event);
