using System.Net;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Bit.Butil.Tests.E2E.Infrastructure;

/// <summary>
/// A WebSocket echo endpoint for the duration of the test session, hosted in the test process.
/// </summary>
/// <remarks>
/// The app under test is <c>Bit.Butil.Samples.Web</c> - a standalone WebAssembly host with no server
/// side to add an endpoint to - and pointing the suite at a public echo service would make it need
/// the internet and someone else's uptime. So the suite brings its own: a dozen lines of ASP.NET
/// Core on a loopback port, whose URL the harness page is handed through the query string.
/// <br/>
/// It answers the same three things the demo server's endpoint does, because the harness asserts on
/// them: text comes back prefixed, binary comes back with every byte incremented (so a round trip is
/// distinguishable from the page having kept its own array), and the literal message <c>close</c>
/// makes the server close with code 4001.
/// </remarks>
public static class WebSocketEchoFixture
{
    private static WebApplication? _app;

    /// <summary>The <c>ws://</c> URL of the echo endpoint. Empty until <see cref="Start"/> has run.</summary>
    public static string Url { get; private set; } = string.Empty;

    public static async Task Start()
    {
        var port = GetFreePort();

        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

        var app = builder.Build();
        app.UseWebSockets();
        app.MapGet("/echo", Echo);

        await app.StartAsync();

        _app = app;
        Url = $"ws://127.0.0.1:{port}/echo";
    }

    public static async Task Stop()
    {
        if (_app is null) return;
        try
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _app.StopAsync(deadline.Token);
        }
        catch { /* best-effort cleanup */ }
        finally
        {
            await _app.DisposeAsync();
            _app = null;
        }
    }

    private static async Task Echo(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest is false)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        // Accept the harness's protocol only when it was actually offered: accepting one that was
        // not offered fails the handshake in the browser rather than degrading to no protocol.
        var protocol = context.WebSockets.WebSocketRequestedProtocols.Contains("butil-echo") ? "butil-echo" : null;

        using var socket = protocol is null
            ? await context.WebSockets.AcceptWebSocketAsync()
            : await context.WebSockets.AcceptWebSocketAsync(protocol);

        var buffer = new byte[4 * 1024];
        using var message = new MemoryStream();

        try
        {
            while (socket.State == System.Net.WebSockets.WebSocketState.Open)
            {
                // A receive is a frame, not a message: anything bigger than the buffer, or sent
                // fragmented, arrives in several of them and is only whole when EndOfMessage says
                // so. Decoding a piece of a UTF-8 message would cut a character in half.
                message.SetLength(0);
                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close) break;
                    message.Write(buffer, 0, result.Count);
                }
                while (result.EndOfMessage is false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    // Echo the peer's own code back. The browser's close event reports the code in
                    // the frame it receives, so answering everything with 1000 would make every
                    // client-initiated close look like a normal one whatever code it sent.
                    await socket.CloseAsync(result.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                        result.CloseStatusDescription ?? string.Empty, CancellationToken.None);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    var bytes = message.ToArray();
                    for (var i = 0; i < bytes.Length; i++) bytes[i] = unchecked((byte)(bytes[i] + 1));
                    await socket.SendAsync(bytes, WebSocketMessageType.Binary, true, CancellationToken.None);
                    continue;
                }

                var text = Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);

                if (text == "close")
                {
                    await socket.CloseAsync((WebSocketCloseStatus)4001, "closed by the server", CancellationToken.None);
                    break;
                }

                await socket.SendAsync(Encoding.UTF8.GetBytes($"echo: {text}"), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
        catch (OperationCanceledException) { /* the run ended */ }
        catch (WebSocketException) { /* the client went away without a closing handshake */ }
    }

    private static int GetFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
