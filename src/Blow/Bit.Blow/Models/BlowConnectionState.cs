namespace Bit.Blow;

/// <summary>
/// Live state of an in-progress connection, passed to a custom connection-line template
/// and raised by <c>BlowCanvas.OnConnectionChange</c>. Mirrors the data React Flow exposes
/// through its connection line component / <c>useConnection</c> hook.
/// </summary>
public sealed class BlowConnectionLineContext
{
    /// <summary>The node the connection started from.</summary>
    public required string FromNode { get; init; }

    /// <summary>The handle id the connection started from (may be null).</summary>
    public string? FromHandle { get; init; }

    /// <summary>Whether the originating handle is a source or target.</summary>
    public BlowHandleType FromHandleType { get; init; }

    /// <summary>The side the originating handle is anchored to.</summary>
    public BlowPosition FromPosition { get; init; }

    /// <summary>Start point in flow coordinates (the originating handle anchor).</summary>
    public BlowXYPosition From { get; init; }

    /// <summary>Current pointer point in flow coordinates.</summary>
    public BlowXYPosition To { get; init; }

    /// <summary>Computed end side used for path direction.</summary>
    public BlowPosition ToPosition { get; init; }

    /// <summary>True when the pointer is currently over a valid drop target.</summary>
    public bool IsValid { get; init; }
}

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
}
