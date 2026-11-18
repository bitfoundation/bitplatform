namespace Bit.Blow;

/// <summary>
/// Payload for <c>BlowCanvas.OnConnectEnd</c>, raised whenever a connection drag ends
/// regardless of whether it produced an edge. Mirrors React Flow's <c>onConnectEnd</c>
/// (event + final connection state), enabling "add node on edge drop" interactions.
/// </summary>
public readonly record struct BlowConnectEndInfo(
    BlowConnectionStartInfo Start,
    BlowXYPosition FlowPosition,
    double ClientX,
    double ClientY,
    bool IsValid)
{
    /// <summary>
    /// True when the gesture ended with the pointer being released after an actual drag - as
    /// opposed to a click on a handle, a click-to-connect completed on a second handle, Escape,
    /// or the browser cancelling the pointer. "Add a node where the connection was dropped" wants
    /// exactly the drops that are not also <see cref="IsValid"/>.
    /// </summary>
    public bool IsDropped { get; init; }

    /// <summary>True when the gesture was dragging one end of an existing edge rather than drawing a new one.</summary>
    public bool IsReconnect { get; init; }

    /// <summary>
    /// Why the gesture did not connect, when a handle refused it: the user let go on (or near) a handle the rules
    /// turned down, or <c>OnBeforeConnect</c> declined. <see cref="BlowConnectionRejection.None"/> when it
    /// connected, or ended on empty space.
    /// </summary>
    public BlowConnectionRejection Rejection { get; init; }
}
