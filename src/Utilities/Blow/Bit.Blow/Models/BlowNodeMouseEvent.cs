using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow;

/// <summary>
/// Payload for node-related mouse events (click, double-click, context menu,
/// enter/leave). Carries both the node and the originating DOM event so callers
/// can read modifier keys and pointer coordinates (e.g. to position a context menu).
/// </summary>
public readonly record struct BlowNodeMouseEvent(BlowNode Node, MouseEventArgs Event);
