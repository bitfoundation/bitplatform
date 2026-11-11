using System.Buffers;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.BlazorUI;

/// <summary>
/// The settings of an endpoint mapped with <see cref="BitAiWebSocketEndpointRouteBuilderExtensions.MapBitAiWebSocket"/>.
/// </summary>
public sealed record BitAiWebSocketEndpointOptions
{
    /// <summary>The default largest request a client may send: 32 MB, the attachments it carries inline included.</summary>
    public const int DefaultMaxRequestSize = 32 * 1024 * 1024;

    /// <summary>Serializer hooks, for custom parts, and the JSON settings, for the app's own types.</summary>
    public BitAiConversationSerializerOptions? Serializer { get; init; }

    /// <summary>
    /// The largest request a client may send, in bytes. A larger one closes the socket with
    /// <see cref="WebSocketCloseStatus.MessageTooBig"/> rather than being buffered without bound.
    /// </summary>
    public int MaxRequestSize { get; init; } = DefaultMaxRequestSize;

    /// <summary>
    /// How long a client has, once its socket is accepted, to send the request. One that sends nothing is closed rather
    /// than left holding a connection.
    /// </summary>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Acts on a decision the client sends on a tool call this reply asked about; returns whether a held call was waiting
    /// for it. By default, hands it to the app's <see cref="BitAiApprovalBroker"/>, when one is registered.
    /// </summary>
    /// <remarks>
    /// Called only for a call the reply streamed a <see cref="BitAiToolApprovalRequestedEvent"/> for on this very socket,
    /// and once per call: a decision on any other call is dropped, so a client can never decide a call of someone else's
    /// reply through it.
    /// </remarks>
    public Func<BitAiApprovalResponse, HttpContext, CancellationToken, ValueTask<bool>>? OnApproval { get; init; }

    /// <summary>
    /// The WebSocket settings of the endpoint, its keep-alive and <see cref="WebSocketOptions.AllowedOrigins"/> among them.
    /// </summary>
    /// <remarks>
    /// A browser sends the cookies of the endpoint's site with a WebSocket opened from any page, and a WebSocket is not
    /// subject to CORS: an endpoint authenticated by cookie lists the origins it serves in
    /// <see cref="WebSocketOptions.AllowedOrigins"/>, or a page of another site can open it as the signed-in user.
    /// </remarks>
    public WebSocketOptions? WebSockets { get; init; }
}

/// <summary>
/// The server end of <see cref="BitAiWebSocketTransport"/>: an endpoint that accepts a WebSocket per request, reads the
/// <see cref="BitAiRequest"/> from it and streams the reply's events back, taking the decisions on the tool calls it holds
/// over the same socket.
/// </summary>
/// <remarks>
/// <para>
/// The endpoint is a plain <see cref="RequestDelegate"/>, so it is safe to trim and to compile ahead of time, and it returns
/// the <see cref="IEndpointConventionBuilder"/> of the route for the app's own conventions (<c>.RequireAuthorization()</c>,
/// rate limits). It runs the WebSocket middleware itself, with <see cref="BitAiWebSocketEndpointOptions.WebSockets"/>, as
/// a SignalR hub does, so it needs no <c>app.UseWebSockets()</c>; a request that is not a WebSocket one is answered
/// <c>400</c>.
/// </para>
/// <para>
/// A browser sends the cookies of the endpoint's site with a WebSocket from any page, and a WebSocket is not subject to
/// CORS: an endpoint authenticated by cookie lists the origins it serves in the
/// <see cref="WebSocketOptions.AllowedOrigins"/> of <see cref="BitAiWebSocketEndpointOptions.WebSockets"/>, or a page of
/// another site can open it as the signed-in user.
/// </para>
/// </remarks>
public static class BitAiWebSocketEndpointRouteBuilderExtensions
{
    private const int ReceiveChunkSize = 16 * 1024;

    // A decision is a few hundred bytes; anything far larger is not one.
    private const int MaxDecisionSize = 64 * 1024;

    private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Maps an endpoint that accepts a WebSocket, reads a request in the wire format (<see cref="BitAiWireSerializer"/>)
    /// from it and streams the events <paramref name="handler"/> produces for it, one text message each.
    /// </summary>
    /// <param name="endpoints">The route builder.</param>
    /// <param name="pattern">The route, such as <c>/api/ai/ws</c>.</param>
    /// <param name="handler">
    /// Produces the reply, typically <c>StreamBitAiEventsAsync</c> (Bit.BlazorUI.AI.ChatClient) of the app's chat client.
    /// The request carries the endpoint's <see cref="HttpContext.RequestServices"/> as its
    /// <see cref="BitAiRequest.Services"/>, and the token is cancelled when the client stops the reply or goes away.
    /// </param>
    /// <param name="options">The limits, the serializer settings and where the decisions on tool calls go.</param>
    /// <returns>The endpoint's convention builder.</returns>
    /// <remarks>
    /// <para>
    /// The socket is closed normally after the last event. A first message that is not a request closes it with
    /// <see cref="WebSocketCloseStatus.InvalidPayloadData"/>, and one too large with
    /// <see cref="WebSocketCloseStatus.MessageTooBig"/>. An exception of the handler, or an event the wire format cannot
    /// write, ends the stream with a terminal <see cref="BitAiErrorEvent"/> holding a generic message, and is logged; its
    /// own text never reaches the client.
    /// </para>
    /// <para>
    /// A decision the client sends on a call this reply asked about goes to <see cref="BitAiWebSocketEndpointOptions.OnApproval"/>
    /// (the app's <see cref="BitAiApprovalBroker"/> by default), which the handler is typically awaiting
    /// (<see cref="BitAiApprovalBroker.WaitAsync"/>). The decision arrives on the connection streaming the call, so the
    /// broker of the instance holding the call takes it, with no sticky session; and the broker is told about the call
    /// before its <see cref="BitAiToolApprovalRequestedEvent"/> is sent, so a decision arriving before the handler waits
    /// for it is kept for it.
    /// </para>
    /// <para>
    /// Everything in the request comes from the client: <c>ToChatOptions</c> (Bit.BlazorUI.AI.ChatClient) lets the
    /// server's options win over what it asks for, and <see cref="BitAiWireSerializer.ToRequest"/> hands the handler no
    /// message with a role above the user's, no system prompt and no choice forcing a tool the client did not declare.
    /// </para>
    /// </remarks>
    public static IEndpointConventionBuilder MapBitAiWebSocket(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Func<BitAiRequest, HttpContext, CancellationToken, IAsyncEnumerable<BitAiEvent>> handler,
        BitAiWebSocketEndpointOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentNullException.ThrowIfNull(handler);

        options ??= new BitAiWebSocketEndpointOptions();
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxRequestSize, 1, nameof(options));

        // The WebSocket middleware in the endpoint's own pipeline, the way MapHub has it: the app needs no UseWebSockets,
        // and the settings (the allowed origins) are this endpoint's.
        var pipeline = endpoints.CreateApplicationBuilder();
        if (options.WebSockets is { } webSockets) pipeline.UseWebSockets(webSockets);
        else pipeline.UseWebSockets();
        pipeline.Run(http => HandleAsync(http, handler, options));

        return endpoints.Map(pattern, pipeline.Build());
    }

    private static async Task HandleAsync(
        HttpContext http,
        Func<BitAiRequest, HttpContext, CancellationToken, IAsyncEnumerable<BitAiEvent>> handler,
        BitAiWebSocketEndpointOptions options)
    {
        if (!http.WebSockets.IsWebSocketRequest)
        {
            http.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var subProtocol = http.WebSockets.WebSocketRequestedProtocols.Contains(BitAiWebSocketTransport.SubProtocol, StringComparer.Ordinal)
            ? BitAiWebSocketTransport.SubProtocol
            : null;

        using var socket = await http.WebSockets.AcceptWebSocketAsync(subProtocol).ConfigureAwait(false);
        var logger = http.RequestServices.GetService<ILoggerFactory>()?.CreateLogger(typeof(BitAiWebSocketEndpointRouteBuilderExtensions).FullName!);
        var buffer = ArrayPool<byte>.Shared.Rent(ReceiveChunkSize);
        try
        {
            if (await ReadRequestAsync(socket, buffer, http, options).ConfigureAwait(false) is not { } request) return;

            await StreamAsync(socket, request, http, handler, options, logger).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    // The first message, or null once the socket has been closed for the reason it is not one.
    private static async Task<BitAiRequest?> ReadRequestAsync(WebSocket socket, byte[] buffer, HttpContext http, BitAiWebSocketEndpointOptions options)
    {
        var aborted = http.RequestAborted;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(aborted);
        if (options.RequestTimeout > TimeSpan.Zero && options.RequestTimeout != Timeout.InfiniteTimeSpan)
        {
            timeout.CancelAfter(options.RequestTimeout);
        }

        try
        {
            var json = await ReceiveTextAsync(socket, buffer, options.MaxRequestSize, timeout.Token).ConfigureAwait(false);
            if (json is null)
            {
                // Closed before it asked for anything: the close is answered, and that is all.
                await CloseAsync(socket, WebSocketCloseStatus.NormalClosure, null).ConfigureAwait(false);
                return null;
            }

            return BitAiWireSerializer.ToRequest(BitAiWireSerializer.DeserializeRequest(json, options.Serializer), options.Serializer, http.RequestServices);
        }
        catch (JsonException)
        {
            await CloseAsync(socket, WebSocketCloseStatus.InvalidPayloadData, "The first message is not a BitAi request.").ConfigureAwait(false);
        }
        catch (MessageTooBigException)
        {
            await CloseAsync(socket, WebSocketCloseStatus.MessageTooBig, $"The request is larger than {options.MaxRequestSize} bytes.").ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!aborted.IsCancellationRequested)
        {
            // A cancelled receive aborts the socket, so there is no close frame left to send: the client sees the
            // connection end, which is all a client that sent nothing in time is owed.
        }
        catch (Exception exception) when (exception is OperationCanceledException or WebSocketException)
        {
            // The client went away before it sent the request.
        }

        return null;
    }

    private static async Task StreamAsync(
        WebSocket socket,
        BitAiRequest request,
        HttpContext http,
        Func<BitAiRequest, HttpContext, CancellationToken, IAsyncEnumerable<BitAiEvent>> handler,
        BitAiWebSocketEndpointOptions options,
        ILogger? logger)
    {
        // Cancelled when the client goes away, closes the socket (its stop) or sends what breaks the protocol.
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(http.RequestAborted);

        var asked = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
        var onApproval = options.OnApproval ?? SubmitToBroker;
        var receiving = ReceiveDecisionsAsync(socket, http, asked, onApproval, options.Serializer, logger, stop);

        // Told to the broker even when an app's own OnApproval takes the decisions, which typically hands them to it too.
        var expected = new BitAiAskedCalls(http.RequestServices.GetService<BitAiApprovalBroker>());

        try
        {
            await foreach (var evt in BitAiServerStreams.Guard(() => handler(request, http, stop.Token), logger, stop.Token).ConfigureAwait(false))
            {
                // Recorded before it is sent, so a decision made the moment it arrives is already expected - here and by
                // the broker, which keeps it until the handler waits for it.
                if (evt is BitAiToolApprovalRequestedEvent approval) asked.TryAdd(approval.ToolCallId, 0);
                expected.Record(evt);

                var written = BitAiServerStreams.TryWrite(evt, e => Encoding.UTF8.GetBytes(BitAiWireSerializer.Serialize(e, options.Serializer)), logger, out var json);
                await socket.SendAsync(json, WebSocketMessageType.Text, endOfMessage: true, stop.Token).ConfigureAwait(false);
                if (!written) break;
            }

            await CloseAsync(socket, WebSocketCloseStatus.NormalClosure, null).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is OperationCanceledException or WebSocketException)
        {
            // The client stopped the reply, or went away.
        }
        finally
        {
            // The client answers the close (or has closed already, which ended the receiving); after that, or after the
            // grace period, the receiving is stopped for good.
            await Task.WhenAny(receiving, Task.Delay(CloseTimeout, CancellationToken.None)).ConfigureAwait(false);
            await stop.CancelAsync().ConfigureAwait(false);
            await receiving.ConfigureAwait(false);
            expected.Forget();
        }
    }

    // Reads the decisions the client sends until it closes the socket, then stops the reply (if it is still running, that
    // close was the client's stop). Never throws: what it cannot read is the client's problem, logged at most.
    private static async Task ReceiveDecisionsAsync(
        WebSocket socket,
        HttpContext http,
        ConcurrentDictionary<string, byte> asked,
        Func<BitAiApprovalResponse, HttpContext, CancellationToken, ValueTask<bool>> onApproval,
        BitAiConversationSerializerOptions? serializer,
        ILogger? logger,
        CancellationTokenSource stop)
    {
        // Runs beside the stream, so it neither starts on the caller's stack nor shares the request's buffer.
        await Task.Yield();
        var buffer = ArrayPool<byte>.Shared.Rent(ReceiveChunkSize);
        try
        {
            while (await ReceiveTextAsync(socket, buffer, MaxDecisionSize, stop.Token).ConfigureAwait(false) is { } json)
            {
                BitAiApprovalResponse response;
                try
                {
                    response = BitAiWireSerializer.DeserializeApproval(json, serializer);
                }
                catch (JsonException)
                {
                    // Not a decision: a message of a newer client this version does not know. Skipped, as an unknown
                    // event is on the other side.
                    continue;
                }

                // Only a call this reply asked about on this socket, and only once.
                if (!asked.TryRemove(response.ToolCallId, out _)) continue;

                try
                {
                    await onApproval(response, http, stop.Token).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    // The app's handler failed on this one decision: the reply goes on (the call it held is the
                    // handler's to settle), as an approval endpoint answering 500 would leave the stream alone.
                    logger?.LogError(exception, "A decision on an AI tool call failed on the server.");
                }
            }
        }
        catch (MessageTooBigException)
        {
            await CloseAsync(socket, WebSocketCloseStatus.MessageTooBig, "A decision on a tool call is never that large.").ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is OperationCanceledException or WebSocketException)
        {
            // The reply is over, or the client went away.
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            await stop.CancelAsync().ConfigureAwait(false);
        }
    }

    private static ValueTask<bool> SubmitToBroker(BitAiApprovalResponse response, HttpContext http, CancellationToken cancellationToken) =>
        ValueTask.FromResult(http.RequestServices.GetService<BitAiApprovalBroker>()?.TrySubmit(response) ?? false);

    // One whole text message, or null once the client has closed the socket. A binary message is skipped.
    private static async Task<string?> ReceiveTextAsync(WebSocket socket, byte[] buffer, int maxSize, CancellationToken cancellationToken)
    {
        ArrayBufferWriter<byte>? message = null;
        while (true)
        {
            var result = await socket.ReceiveAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (result.MessageType is WebSocketMessageType.Close) return null;

            if (message is null && result.EndOfMessage)
            {
                if (result.Count > maxSize) throw new MessageTooBigException();
                if (result.MessageType is WebSocketMessageType.Text) return Encoding.UTF8.GetString(buffer, 0, result.Count);
                continue;
            }

            message ??= new ArrayBufferWriter<byte>(ReceiveChunkSize * 2);
            if (message.WrittenCount + result.Count > maxSize) throw new MessageTooBigException();

            message.Write(buffer.AsSpan(0, result.Count));
            if (!result.EndOfMessage) continue;

            var text = result.MessageType is WebSocketMessageType.Text ? Encoding.UTF8.GetString(message.WrittenSpan) : null;
            message = null;
            if (text is not null) return text;
        }
    }

    // Sends the close frame and leaves the socket to the receiving to see the client's answer.
    private static async Task CloseAsync(WebSocket socket, WebSocketCloseStatus status, string? description)
    {
        if (socket.State is not (WebSocketState.Open or WebSocketState.CloseReceived)) return;

        using var timeout = new CancellationTokenSource(CloseTimeout);
        try
        {
            await socket.CloseOutputAsync(status, description, timeout.Token).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is WebSocketException or OperationCanceledException or ObjectDisposedException)
        {
            socket.Abort();
        }
    }

    private sealed class MessageTooBigException : Exception;
}
