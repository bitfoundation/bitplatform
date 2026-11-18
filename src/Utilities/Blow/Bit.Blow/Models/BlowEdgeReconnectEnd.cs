namespace Bit.Blow;

/// <summary>
/// Payload for <c>BlowCanvas.OnReconnectEnd</c>, raised when a reconnect gesture finishes.
/// <see cref="Rewired"/> is false when the endpoint was dropped somewhere invalid and the edge
/// was left untouched - the usual cue to delete it.
/// </summary>
public readonly record struct BlowEdgeReconnectEnd(BlowEdge Edge, BlowHandleType MovingEnd, bool Rewired);
