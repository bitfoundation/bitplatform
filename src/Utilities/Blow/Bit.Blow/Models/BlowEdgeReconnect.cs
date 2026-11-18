namespace Bit.Blow;

/// <summary>
/// Payload for <c>BlowCanvas.OnReconnect</c>, raised when an existing edge's
/// endpoint is dragged to a new handle. <see cref="Edge"/> already reflects the
/// updated source/target; <see cref="Connection"/> describes the new wiring.
/// </summary>
public readonly record struct BlowEdgeReconnect(BlowEdge Edge, BlowConnection Connection);
