namespace Bit.Butil;

/// <summary>One frame received on a <see cref="Bit.Butil.WebSocket"/>.</summary>
/// <param name="IsBinary">
/// Which of the two payloads carries this frame. A WebSocket frame is text or binary, never both,
/// and the sender decides - so this is the flag to branch on rather than a null check.
/// </param>
/// <param name="Text">The payload of a text frame, or null for a binary one.</param>
/// <param name="Data">
/// The payload of a binary frame, or null for a text one. The socket is opened in
/// <c>arraybuffer</c> mode, so these bytes arrive without a round trip through a Blob.
/// </param>
public record WebSocketMessage(bool IsBinary, string? Text, byte[]? Data);
