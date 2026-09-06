namespace Bit.Butil;

/// <summary>How a <see cref="Bit.Butil.WebSocket"/> connection ended.</summary>
/// <param name="Code">
/// The close code. <c>1000</c> is a normal close; <c>1006</c> means no close frame ever arrived,
/// which is the browser's stand-in for "the connection dropped"; <c>1005</c> means it closed
/// cleanly but without a code. Anything from <c>3000</c> upwards is defined by the application.
/// </param>
/// <param name="Reason">
/// The peer's explanation, or an empty string. At most 123 UTF-8 bytes, which is the protocol's
/// own limit rather than this wrapper's.
/// </param>
/// <param name="WasClean">
/// Whether the closing handshake completed. False means the connection was lost rather than closed,
/// and is the signal to reconnect if that is what you want - nothing here reconnects by itself.
/// </param>
public record WebSocketClose(int Code, string Reason, bool WasClean);
