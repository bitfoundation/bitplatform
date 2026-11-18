namespace Bit.Blow;

/// <summary>
/// Payload for <c>BlowCanvas.OnReconnectStart</c>. <see cref="MovingEnd"/> is the endpoint the
/// user grabbed; the opposite end stays anchored for the duration of the gesture.
/// </summary>
public readonly record struct BlowEdgeReconnectStart(BlowEdge Edge, BlowHandleType MovingEnd);
