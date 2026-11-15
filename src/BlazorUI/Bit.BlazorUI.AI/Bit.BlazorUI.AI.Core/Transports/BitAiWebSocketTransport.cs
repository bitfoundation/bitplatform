using System.Buffers;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// The settings of a <see cref="BitAiWebSocketTransport"/>.
/// </summary>
public sealed record BitAiWebSocketTransportOptions
{
    /// <summary>The default largest event accepted from the server: 16 MB.</summary>
    public const int DefaultMaxMessageSize = 16 * 1024 * 1024;

    /// <summary>Serializer hooks, for custom parts, and the JSON settings, for the app's own types.</summary>
    public BitAiConversationSerializerOptions? Serializer { get; init; }

    /// <summary>
    /// Called with the options of every socket before it connects: to add a header or a client certificate outside the
    /// browser. In the browser a WebSocket sends no header of the app's, so a credential goes in the cookies the browser
    /// sends anyway, or in the address (the endpoint factory of <see cref="BitAiBuilder.UseWebSocketTransport"/> builds
    /// it per request).
    /// </summary>
    public Func<ClientWebSocketOptions, CancellationToken, ValueTask>? ConfigureSocket { get; init; }

    /// <summary>
    /// How long a connection may take to be accepted. A server that takes the TCP connection but never answers the
    /// upgrade would otherwise hold the turn open until it is stopped.
    /// </summary>
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The largest event accepted from the server, in bytes. A larger one fails the turn rather than being buffered
    /// without bound.
    /// </summary>
    public int MaxMessageSize { get; init; } = DefaultMaxMessageSize;
}

/// <summary>
/// A transport that opens a WebSocket per request to a server endpoint and reads the reply's events from it, in the
/// package's wire format (<see cref="BitAiWireSerializer"/>).
/// </summary>
/// <remarks>
/// <para>
/// The server end is <c>MapBitAiWebSocket</c> of the Bit.BlazorUI.AI.Server package. Register it with
/// <see cref="BitAiBuilder.UseWebSocketTransport"/>. It needs nothing beyond the framework, so it runs in Blazor
/// WebAssembly, Server and Hybrid alike.
/// </para>
/// <para>
/// What it adds over <see cref="BitAiSseTransport"/>: a decision on a tool call the server holds goes back over the very
/// socket the call was streamed on, so it reaches the server instance holding the call with no sticky session, and only
/// the client that was asked can answer it. Stopping a reply closes the socket, which cancels the server's handler.
/// </para>
/// <para>
/// The protocol is the wire format's JSON, one text message each: the request first, from the client; then the events,
/// from the server, which closes the socket after the last one; and from the client, any time before that, the decisions
/// on the calls the server asked about (<see cref="BitAiToolApprovalRequestedEvent"/>). The client offers the
/// <see cref="SubProtocol"/> subprotocol. An event of a type this version of the package does not know is skipped. A
/// connection the server refuses fails the turn with an <see cref="HttpRequestException"/> (outside the browser, which
/// does not tell why), and one it closes with an error with a <see cref="WebSocketException"/>; the chat session shows
/// either as its error.
/// </para>
/// </remarks>
public sealed class BitAiWebSocketTransport : IBitAiTransport, IBitAiApprovalTransport
{
    /// <summary>The WebSocket subprotocol of the wire format's first version.</summary>
    public const string SubProtocol = "bitai.v1";

    private const int ReceiveChunkSize = 16 * 1024;
    private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(2);

    private readonly Uri _endpoint;
    private readonly BitAiWebSocketTransportOptions _options;

    // The replies streaming right now, to route a decision to the socket whose server asked for it. A transport is
    // scoped, and one scope can run the turns of several sessions at once.
    private readonly List<Turn> _turns = [];

    /// <summary>Creates the transport.</summary>
    /// <param name="endpoint">
    /// The address of the endpoint: <c>ws</c> / <c>wss</c>, or <c>http</c> / <c>https</c> for the matching WebSocket scheme.
    /// </param>
    /// <param name="options">The socket and the serializer settings.</param>
    /// <exception cref="ArgumentException"><paramref name="endpoint"/> is not an absolute address of one of those schemes.</exception>
    public BitAiWebSocketTransport(Uri endpoint, BitAiWebSocketTransportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        _endpoint = ToWebSocketUri(endpoint);
        _options = options ?? new BitAiWebSocketTransportOptions();
        ArgumentOutOfRangeException.ThrowIfLessThan(_options.MaxMessageSize, 1, nameof(options));
    }

    /// <summary>The address the sockets connect to.</summary>
    public Uri Endpoint => _endpoint;

    /// <inheritdoc />
    public async IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshot = await BitAiWireSerializer.ToSnapshotAsync(request, _options.Serializer, cancellationToken).ConfigureAwait(false);
        var payload = Encoding.UTF8.GetBytes(BitAiWireSerializer.SerializeRequest(snapshot, _options.Serializer));

        var socket = new ClientWebSocket();
        var turn = new Turn(socket);
        var buffer = ArrayPool<byte>.Shared.Rent(ReceiveChunkSize);
        try
        {
            await ConnectAsync(socket, cancellationToken).ConfigureAwait(false);
            await socket.SendAsync(payload, WebSocketMessageType.Text, endOfMessage: true, cancellationToken).ConfigureAwait(false);

            lock (_turns) _turns.Add(turn);

            while (await ReceiveAsync(socket, buffer, cancellationToken).ConfigureAwait(false) is { } json)
            {
                if (BitAiWireSerializer.Deserialize(json, _options.Serializer) is not { } evt) continue;

                if (evt is BitAiToolApprovalRequestedEvent asked) turn.Ask(asked.ToolCallId);
                yield return evt;
            }
        }
        finally
        {
            lock (_turns) _turns.Remove(turn);
            ArrayPool<byte>.Shared.Return(buffer);
            await CloseAsync(socket).ConfigureAwait(false);
            socket.Dispose();
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Sent over the socket of the reply whose server asked about the call. A decision no streaming reply asked for (its
    /// reply already ended) has nobody to reach, and is dropped.
    /// </remarks>
    public async ValueTask SendApprovalAsync(BitAiApprovalResponse response, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        Turn? turn;
        lock (_turns) turn = _turns.FindLast(t => t.IsAsked(response.ToolCallId));
        if (turn is null) return;

        var payload = Encoding.UTF8.GetBytes(BitAiWireSerializer.SerializeApproval(response, _options.Serializer));
        await turn.SendAsync(payload, response.ToolCallId, cancellationToken).ConfigureAwait(false);
    }

    private async Task ConnectAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        socket.Options.AddSubProtocol(SubProtocol);
        if (!OperatingSystem.IsBrowser())
        {
            // What turns a refused upgrade into its status code; the browser exposes none.
            socket.Options.CollectHttpResponseDetails = true;
        }

        if (_options.ConfigureSocket is { } configure) await configure(socket.Options, cancellationToken).ConfigureAwait(false);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (_options.ConnectTimeout > TimeSpan.Zero && _options.ConnectTimeout != Timeout.InfiniteTimeSpan)
        {
            timeout.CancelAfter(_options.ConnectTimeout);
        }

        try
        {
            await socket.ConnectAsync(_endpoint, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Connecting to the AI endpoint {_endpoint} timed out after {_options.ConnectTimeout}.", exception);
        }
        catch (WebSocketException exception) when (RefusedWith(socket) is { } status)
        {
            // An answer other than the upgrade (a 401, a 403, a 404) is told as the HTTP failure it is, as the SSE
            // transport tells one, so an app handles both the same way.
            throw new HttpRequestException(
                $"The AI endpoint answered {(int)status} ({status}) rather than accepting the WebSocket.",
                exception,
                status);
        }
    }

    private static HttpStatusCode? RefusedWith(ClientWebSocket socket)
    {
        if (OperatingSystem.IsBrowser()) return null;

        var status = socket.HttpStatusCode;
        return status is 0 or HttpStatusCode.SwitchingProtocols ? null : status;
    }

    // One whole text message, or null once the server has closed the socket normally.
    private async ValueTask<string?> ReceiveAsync(ClientWebSocket socket, byte[] buffer, CancellationToken cancellationToken)
    {
        ArrayBufferWriter<byte>? message = null;
        while (true)
        {
            ValueWebSocketReceiveResult result;
            try
            {
                result = await socket.ReceiveAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            }
            catch (WebSocketException exception) when (cancellationToken.IsCancellationRequested)
            {
                // A cancellation aborts the socket; one that lands as a receive completes leaves the next one to find it
                // aborted. Either way the reply was stopped, and says so.
                throw new OperationCanceledException("The reply was stopped.", exception, cancellationToken);
            }

            if (result.MessageType is WebSocketMessageType.Close)
            {
                if (socket.CloseStatus is null or WebSocketCloseStatus.NormalClosure) return null;

                throw new WebSocketException(
                    WebSocketError.Faulted,
                    $"The AI endpoint closed the connection ({(int)socket.CloseStatus} {socket.CloseStatus}): {socket.CloseStatusDescription}");
            }

            if (message is null && result.EndOfMessage)
            {
                // The common case, an event that fits one read: decoded straight from the buffer - within the limit too,
                // which can be smaller than one read.
                if (result.Count > _options.MaxMessageSize) throw TooLarge();
                if (result.MessageType is WebSocketMessageType.Text) return Encoding.UTF8.GetString(buffer, 0, result.Count);
                continue;
            }

            message ??= new ArrayBufferWriter<byte>(ReceiveChunkSize * 2);
            if (message.WrittenCount + result.Count > _options.MaxMessageSize) throw TooLarge();

            message.Write(buffer.AsSpan(0, result.Count));
            if (!result.EndOfMessage) continue;

            var text = result.MessageType is WebSocketMessageType.Text ? Encoding.UTF8.GetString(message.WrittenSpan) : null;
            message = null;
            if (text is not null) return text;
        }
    }

    private WebSocketException TooLarge() => new(
        WebSocketError.Faulted,
        $"The AI endpoint sent a message larger than {_options.MaxMessageSize} bytes (BitAiWebSocketTransportOptions.MaxMessageSize).");

    // A reply the server finished is closed back; one the app stopped (a break out of the enumeration, a disposed
    // session) is closed from here, which the server takes as the stop. A socket that cannot close in time is aborted,
    // and one a cancelled receive already aborted is left to Dispose.
    private static async ValueTask CloseAsync(ClientWebSocket socket)
    {
        if (socket.State is not (WebSocketState.Open or WebSocketState.CloseReceived)) return;

        using var timeout = new CancellationTokenSource(CloseTimeout);
        try
        {
            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, timeout.Token).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is WebSocketException or OperationCanceledException or ObjectDisposedException)
        {
            socket.Abort();
        }
    }

    private static Uri ToWebSocketUri(Uri endpoint)
    {
        if (!endpoint.IsAbsoluteUri)
        {
            throw new ArgumentException("The address of the AI endpoint must be absolute (build it on the app's base address).", nameof(endpoint));
        }

        var scheme = endpoint.Scheme.ToLowerInvariant() switch
        {
            "ws" or "http" => "ws",
            "wss" or "https" => "wss",
            _ => throw new ArgumentException($"The address of the AI endpoint must be ws, wss, http or https, not '{endpoint.Scheme}'.", nameof(endpoint)),
        };

        return scheme == endpoint.Scheme
            ? endpoint
            : new UriBuilder(endpoint) { Scheme = scheme, Port = endpoint.IsDefaultPort ? -1 : endpoint.Port }.Uri;
    }

    // One reply in flight: its socket, the calls its server asked about, and the gate of its sends - a WebSocket takes one
    // send at a time, and decisions on parallel calls can be made at once.
    private sealed class Turn(ClientWebSocket socket)
    {
        private readonly SemaphoreSlim _sendGate = new(1, 1);
        private readonly HashSet<string> _asked = new(StringComparer.Ordinal);

        public void Ask(string toolCallId)
        {
            lock (_asked) _asked.Add(toolCallId);
        }

        public bool IsAsked(string toolCallId)
        {
            lock (_asked) return _asked.Contains(toolCallId);
        }

        public async ValueTask SendAsync(byte[] payload, string toolCallId, CancellationToken cancellationToken)
        {
            await _sendGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await socket.SendAsync(payload, WebSocketMessageType.Text, endOfMessage: true, cancellationToken).ConfigureAwait(false);
                lock (_asked) _asked.Remove(toolCallId);
            }
            finally
            {
                _sendGate.Release();
            }
        }
    }
}
