using System.Net.WebSockets;

namespace Bit.Bcrdt;

/// <summary>
/// The message framing rules shared by both halves of this transport.
/// </summary>
/// <remarks>
/// A WebSocket delivers fragments, not messages: a large update arrives as several frames, and the
/// framework hands each one over as it lands. The protocol decoder reads a version byte and a kind
/// byte and then treats the rest as a complete payload, so surfacing a fragment would not merely
/// truncate an update - it would raise a format error on a connection that is working perfectly.
/// Both the client transport and the server relay therefore reassemble here, in one place, rather
/// than each keeping its own copy of a rule that must not drift.
/// </remarks>
internal static class BcrdtWebSocketFraming
{
    /// <summary>The default receive buffer size, comfortably above a typical keystroke update.</summary>
    internal const int DefaultBufferSize = 8 * 1024;

    /// <summary>The default largest message accepted, comfortably above a large document's full state.</summary>
    internal const int DefaultMaxMessageSize = 16 * 1024 * 1024;

    /// <summary>How long the close frame for an oversized message may take before the socket is abandoned.</summary>
    private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Reads fragments until a message is complete.
    /// </summary>
    /// <param name="socket">The socket to read from.</param>
    /// <param name="buffer">Scratch space for a single fragment, reused across calls.</param>
    /// <param name="accumulator">
    /// Where fragments are joined; reset on entry and reused across calls. Its capacity is given back
    /// after an unusually large message, so one burst does not pin memory for the life of the socket.
    /// </param>
    /// <param name="maxMessageSize">The largest message accepted, in bytes.</param>
    /// <param name="cancellationToken">Stops the read.</param>
    /// <returns>The complete message, or <see langword="null"/> when the peer sent a close frame.</returns>
    /// <exception cref="WebSocketException">
    /// The message exceeded <paramref name="maxMessageSize"/>; the socket has been closed with
    /// <see cref="WebSocketCloseStatus.MessageTooBig"/>.
    /// </exception>
    internal static async ValueTask<byte[]?> ReceiveMessageAsync(
        WebSocket socket,
        byte[] buffer,
        MemoryStream accumulator,
        int maxMessageSize,
        CancellationToken cancellationToken)
    {
        accumulator.SetLength(0);
        if (accumulator.Capacity > 4 * buffer.Length) accumulator.Capacity = buffer.Length;

        while (true)
        {
            ValueWebSocketReceiveResult result = await socket
                .ReceiveAsync(buffer.AsMemory(), cancellationToken)
                .ConfigureAwait(false);

            if (result.MessageType is WebSocketMessageType.Close) return null;

            if (accumulator.Length + result.Count > maxMessageSize)
            {
                try
                {
                    using CancellationTokenSource timeout = new(CloseTimeout);
                    await socket
                        .CloseOutputAsync(WebSocketCloseStatus.MessageTooBig, "Message too large", timeout.Token)
                        .ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // The connection is being abandoned either way.
                }

                throw new WebSocketException(
                    WebSocketError.Faulted,
                    $"A message exceeded the maximum size of {maxMessageSize} bytes.");
            }

            accumulator.Write(buffer, 0, result.Count);

            // The copy is not avoidable: receivers may hold the message beyond the call, while the
            // buffer is needed for the next fragment.
            if (result.EndOfMessage) return accumulator.ToArray();
        }
    }

    /// <summary>
    /// Answers a peer's close frame, on a best-effort basis.
    /// </summary>
    /// <remarks>
    /// Silent by design, and bounded rather than cancellable: this is the last thing said on a socket
    /// that is already closing, and a failure here has no consequence worth propagating.
    /// </remarks>
    /// <param name="socket">The socket that received the close frame.</param>
    internal static async Task AcknowledgeCloseAsync(WebSocket socket)
    {
        try
        {
            if (socket.State is WebSocketState.CloseReceived)
            {
                using CancellationTokenSource timeout = new(CloseTimeout);
                await socket
                    .CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, timeout.Token)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception)
        {
            // Nothing left to close, and nobody left to tell.
        }
    }
}
