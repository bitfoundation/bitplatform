using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Bit.Butil.Demo.Server.Endpoints;

/// <summary>
/// The WebSocket endpoint the demo page and the E2E harness both talk to.
/// </summary>
/// <remarks>
/// One implementation rather than two, because the E2E suite asserts on this protocol while the demo
/// page is what ships it: the text prefix, the incremented bytes and the 4001 close code are a
/// contract, and two copies of a contract drift. The suite cannot simply use the demo server - the
/// app it drives is <c>Bit.Butil.Samples.Web</c>, a standalone WebAssembly host with no server side -
/// so it hosts this same handler on a loopback port of its own and is handed the URL through the
/// query string.
/// </remarks>
public static class WebSocketEcho
{
    /// <summary>The sub-protocol this endpoint accepts, when the client offers it.</summary>
    public const string Protocol = "butil-echo";

    /// <summary>The code the <c>close</c> command closes with.</summary>
    public const int ServerCloseCode = 4001;

    /// <summary>The largest message this endpoint will reassemble, in bytes.</summary>
    /// <remarks>
    /// A message arrives as however many frames the peer chose to send, so without a ceiling the
    /// buffer holding them grows to whatever a client cares to send - one connection is enough to
    /// exhaust the server. Well over anything the demo page or the E2E suite sends.
    /// </remarks>
    public const int MaxMessageBytes = 4 * 1024 * 1024;

    /// <summary>
    /// Echoes text and binary frames back, offers a sub-protocol so negotiation can be seen doing
    /// something, and understands two commands the page uses to exercise the parts of the API a
    /// plain echo cannot reach: <c>close</c> makes the server start the closing handshake with an
    /// application-defined code, and <c>burst</c> floods the connection so <c>bufferedAmount</c>
    /// becomes a number worth reading.
    /// </summary>
    public static async Task Handle(HttpContext context, CancellationToken cancellationToken)
    {
        if (context.WebSockets.IsWebSocketRequest is false)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("This endpoint only answers WebSocket upgrade requests.", cancellationToken);
            return;
        }

        // Accept the client's protocol only when it actually offered it; accepting one that was not
        // offered fails the handshake in the browser rather than degrading to none.
        var requested = context.WebSockets.WebSocketRequestedProtocols;
        var protocol = requested.Contains(Protocol) ? Protocol : null;

        using var socket = protocol is null
            ? await context.WebSockets.AcceptWebSocketAsync()
            : await context.WebSockets.AcceptWebSocketAsync(protocol);

        var buffer = new byte[8 * 1024];
        using var message = new MemoryStream();

        try
        {
            // Written out because Bit.Butil has a WebSocketState of its own - the browser-side one -
            // and this file sits inside the Bit.Butil namespace, where an enclosing namespace's type
            // beats the using above (and beats an alias for it too).
            while (socket.State == global::System.Net.WebSockets.WebSocketState.Open
                   && cancellationToken.IsCancellationRequested is false)
            {
                // A receive is a frame, not a message: anything bigger than the buffer, or sent
                // fragmented, arrives in several of them and is only whole when EndOfMessage says so.
                // Decoding a piece of a UTF-8 message would cut a character in half.
                message.SetLength(0);
                WebSocketReceiveResult result;
                var tooBig = false;
                do
                {
                    result = await socket.ReceiveAsync(buffer, cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close) break;

                    // Checked before the write rather than after it: the point of the ceiling is that
                    // the buffer never holds more than it, so a frame that would cross it must not be
                    // appended first.
                    if (message.Length + result.Count > MaxMessageBytes) { tooBig = true; break; }

                    message.Write(buffer, 0, result.Count);
                }
                while (result.EndOfMessage is false);

                if (tooBig)
                {
                    // 1009 is the code the protocol reserves for exactly this, so a client can tell
                    // "too large" apart from the server having simply given up on the connection.
                    await socket.CloseAsync(WebSocketCloseStatus.MessageTooBig,
                        $"messages are limited to {MaxMessageBytes} bytes", cancellationToken);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    // Echo the peer's own code back. The browser's close event reports the code in the
                    // frame it receives, so answering everything with 1000 would make every
                    // client-initiated close look like a normal one whatever code it sent.
                    await socket.CloseAsync(result.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                        result.CloseStatusDescription ?? string.Empty, cancellationToken);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    // Echo the bytes back with each one incremented, so the page can tell a real round
                    // trip apart from having simply kept its own array.
                    var bytes = message.ToArray();
                    for (var i = 0; i < bytes.Length; i++) bytes[i] = unchecked((byte)(bytes[i] + 1));
                    await socket.SendAsync(bytes, WebSocketMessageType.Binary, true, cancellationToken);
                    continue;
                }

                var text = Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);

                if (text == "close")
                {
                    await socket.CloseAsync((WebSocketCloseStatus)ServerCloseCode, "closed by the server", cancellationToken);
                    break;
                }

                if (text == "burst")
                {
                    var chunk = Encoding.UTF8.GetBytes(new string('x', 64 * 1024));
                    for (var i = 0; i < 32; i++)
                        await socket.SendAsync(chunk, WebSocketMessageType.Text, true, cancellationToken);
                    continue;
                }

                await socket.SendAsync(Encoding.UTF8.GetBytes($"echo: {text}"),
                    WebSocketMessageType.Text, true, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // The client went away, or the host is shutting down - the normal way this ends.
        }
        catch (WebSocketException)
        {
            // The connection dropped without a closing handshake. Nothing to report to a peer that is
            // already gone.
        }
    }
}
