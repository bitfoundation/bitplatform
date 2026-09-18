namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/WebSocket/readyState">WebSocket.readyState</see>.
/// </summary>
public enum WebSocketState
{
    /// <summary>The connection is being established. Sending in this state fails rather than queueing.</summary>
    Connecting = 0,

    /// <summary>Open, and ready to send and receive.</summary>
    Open = 1,

    /// <summary>The closing handshake has started.</summary>
    Closing = 2,

    /// <summary>Closed, or never opened. Also what a disposed handle reports.</summary>
    Closed = 3,
}
