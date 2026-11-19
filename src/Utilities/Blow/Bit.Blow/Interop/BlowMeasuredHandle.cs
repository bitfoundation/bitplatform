namespace Bit.Blow;

/// <summary>One handle as measured in the DOM, positioned relative to its node's top-left.</summary>
/// <param name="HandleId">The handle's own id, or an empty string when it has none.</param>
/// <param name="Type">Either <c>"source"</c> or <c>"target"</c>.</param>
/// <param name="Position">The side the handle is anchored to: <c>"top"</c>, <c>"right"</c>, <c>"bottom"</c> or <c>"left"</c>.</param>
/// <param name="OffsetX">Horizontal offset of the handle's centre from the node's left edge, in flow units.</param>
/// <param name="OffsetY">Vertical offset of the handle's centre from the node's top edge, in flow units.</param>
/// <param name="ConnectableEnd">False when the handle refuses dropped connections; null (not reported) means it accepts them.</param>
public sealed record BlowMeasuredHandle(
    string HandleId, string Type, string Position, double OffsetX, double OffsetY, bool? ConnectableEnd = null);
