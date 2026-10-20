using System.Net;
using System.Net.WebSockets;

namespace Bit.Bcrdt;

/// <summary>
/// Carries the synchronization protocol over a raw binary WebSocket.
/// </summary>
/// <remarks>
/// <para>
/// Where the SignalR transport buys negotiation, fallbacks and a hub programming model, this one
/// buys the absence of all three: a frame on the wire is the protocol message and nothing else, with
/// no envelope, no base64 and no dependency beyond the framework. That makes it the right choice for
/// a server that is not ASP.NET Core, for a non-.NET peer on the other end, and for high-frequency
/// traffic where a third of every packet spent on encoding overhead is worth eliminating.
/// </para>
/// <para>
/// The price is that everything SignalR was doing must be done here: message framing, send
/// serialization, liveness, and honest reporting of a socket that has died. There is deliberately no
/// reconnection logic - the <c>BcrdtSyncProvider</c> above owns that, and it repairs the resulting gap by
/// re-running the handshake, so this type's only obligation is to say clearly when it is no longer
/// connected.
/// </para>
/// <para>
/// Outside the browser, the socket is given a keep-alive with a timeout by default
/// (<see cref="DefaultKeepAliveInterval"/>, <see cref="DefaultKeepAliveTimeout"/>), so a connection
/// whose peer has silently vanished - a laptop lid, a NAT entry expiring - is detected and reported
/// rather than left looking healthy while it delivers nothing. The configure callback can override
/// both.
/// </para>
/// </remarks>
public sealed class BcrdtWebSocketTransport : IBcrdtTransport
{
    /// <summary>How often a keep-alive ping is sent, unless the configure callback says otherwise.</summary>
    public static readonly TimeSpan DefaultKeepAliveInterval = TimeSpan.FromSeconds(15);

    /// <summary>How long a keep-alive ping may go unanswered before the connection is declared dead.</summary>
    public static readonly TimeSpan DefaultKeepAliveTimeout = TimeSpan.FromSeconds(30);

    /// <summary>How long a deliberate close waits for its close frame to go out before giving up.</summary>
    private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(2);

    private readonly Uri _uri;
    private readonly Action<ClientWebSocketOptions>? _configure;
    private readonly int _receiveBufferSize;

    // Never disposed: SemaphoreSlim holds nothing unmanaged unless its wait handle is requested, and
    // disposing it while a send or connect is waiting would strand that caller.
    private readonly SemaphoreSlim _sendGate = new(1, 1);
    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private readonly object _stateGate = new();

    private ClientWebSocket? _socket;
    private CancellationTokenSource? _receiveShutdown;
    private Task? _receiveLoop;
    private int _state = (int)BcrdtTransportState.Disconnected;
    private volatile bool _disposed;

    /// <summary>Creates a transport that will connect to a WebSocket endpoint.</summary>
    /// <param name="uri">
    /// The endpoint, using the <c>ws</c> or <c>wss</c> scheme. Any document identifier the server
    /// needs is part of this address - as a path segment or a query parameter - because a WebSocket
    /// carries no metadata of its own and the upgrade request is the only chance to state it.
    /// </param>
    /// <param name="configure">
    /// Applied to the socket's options before connecting, for authentication headers, subprotocols or
    /// a different keep-alive.
    /// </param>
    /// <param name="receiveBufferSize">
    /// The size of the single buffer the receive loop reads into. This is not a message size limit:
    /// larger messages simply arrive as several fragments and are reassembled, up to
    /// <see cref="MaxMessageSize"/>.
    /// </param>
    public BcrdtWebSocketTransport(
        Uri uri,
        Action<ClientWebSocketOptions>? configure = null,
        int receiveBufferSize = BcrdtWebSocketFraming.DefaultBufferSize)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentOutOfRangeException.ThrowIfLessThan(receiveBufferSize, 256);

        _uri = uri;
        _configure = configure;
        _receiveBufferSize = receiveBufferSize;
        Description = $"websocket:{uri}";
    }

    /// <inheritdoc/>
    public BcrdtTransportState State => (BcrdtTransportState)Volatile.Read(ref _state);

    /// <inheritdoc/>
    public string Description { get; }

    /// <summary>The endpoint this transport connects to.</summary>
    public Uri Uri => _uri;

    /// <summary>The socket currently in use, for tests that need to break it.</summary>
    internal ClientWebSocket? CurrentSocket => Volatile.Read(ref _socket);

    /// <summary>
    /// How long to wait for the server to accept the connection. A server that accepts the TCP
    /// connection but never answers the upgrade would otherwise hold the attempt open forever.
    /// </summary>
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The largest message accepted from the peer, in bytes. A larger one closes the connection with
    /// <see cref="WebSocketCloseStatus.MessageTooBig"/> instead of being buffered without bound.
    /// </summary>
    public int MaxMessageSize { get; init; } = BcrdtWebSocketFraming.DefaultMaxMessageSize;

    /// <inheritdoc/>
    public event Action<ReadOnlyMemory<byte>>? MessageReceived;

    /// <inheritdoc/>
    public event Action<BcrdtTransportStateChange>? StateChanged;

    /// <summary>
    /// Opens the socket and starts the receive loop.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The loop is running before <see cref="BcrdtTransportState.Connected"/> is announced, because that
    /// announcement is what makes the provider above send <c>SyncStep1</c>, and the peer's reply can
    /// arrive before this method has returned. Starting the loop afterwards would leave a window in
    /// which the answer to our own handshake went unread.
    /// </para>
    /// <para>
    /// The loop holds back any terminal transition until the announcement has been made, so a server
    /// that closes the socket immediately after accepting it is reported as a drop after the
    /// connection - never overwritten by it, which would leave the transport claiming a connection
    /// that no longer exists.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The transport has been disposed.</exception>
    /// <exception cref="BcrdtTransportAuthorizationException">The server answered the upgrade with 401 or 403.</exception>
    /// <exception cref="TimeoutException">The server did not accept the connection within <see cref="ConnectTimeout"/>.</exception>
    public async ValueTask ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (State == BcrdtTransportState.Connected) return;

        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (State == BcrdtTransportState.Connected) return;

            SetState(BcrdtTransportState.Connecting, null);
            await TeardownAsync(CancellationToken.None).ConfigureAwait(false);

            ClientWebSocket socket = CreateSocket();
            try
            {
                await OpenAsync(socket, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                socket.Dispose();
                throw;
            }

            CancellationTokenSource shutdown = new();
            TaskCompletionSource announced = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _socket = socket;
            _receiveShutdown = shutdown;
            _receiveLoop = Task.Run(() => ReceiveLoopAsync(socket, announced.Task, shutdown.Token), CancellationToken.None);

            try
            {
                SetState(BcrdtTransportState.Connected, null);
            }
            finally
            {
                announced.TrySetResult();
            }
        }
        catch
        {
            // The thrown exception is the whole failure signal. The state is corrected without
            // raising StateChanged, because a provider that both catches this and observes a
            // Disconnected transition would start two reconnection loops for one failure.
            await TeardownAsync(CancellationToken.None).ConfigureAwait(false);
            lock (_stateGate) Volatile.Write(ref _state, (int)BcrdtTransportState.Disconnected);
            throw;
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    private ClientWebSocket CreateSocket()
    {
        ClientWebSocket socket = new();
        try
        {
            if (!OperatingSystem.IsBrowser())
            {
                // The browser exposes none of these; elsewhere they are what turns a 403 into a clear
                // refusal and a vanished peer into a reported drop.
                socket.Options.CollectHttpResponseDetails = true;
                socket.Options.KeepAliveInterval = DefaultKeepAliveInterval;
#if NET9_0_OR_GREATER
                // .NET 8 has no keep-alive timeout there: a vanished peer is noticed when a send or receive fails.
                socket.Options.KeepAliveTimeout = DefaultKeepAliveTimeout;
#endif
            }

            _configure?.Invoke(socket.Options);
            return socket;
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private async Task OpenAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (ConnectTimeout > TimeSpan.Zero && ConnectTimeout != Timeout.InfiniteTimeSpan)
        {
            timeout.CancelAfter(ConnectTimeout);
        }

        try
        {
            await socket.ConnectAsync(_uri, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Not an OperationCanceledException: nobody cancelled anything, and the layer above must
            // treat this as a failed attempt to retry rather than as a shutdown.
            throw new TimeoutException($"Connecting to {Description} timed out after {ConnectTimeout}.", ex);
        }
        catch (WebSocketException ex) when (IsRefusal(socket, out HttpStatusCode status))
        {
            throw new BcrdtTransportAuthorizationException(
                $"The server refused the connection to {Description} ({(int)status} {status}).", ex);
        }
    }

    private static bool IsRefusal(ClientWebSocket socket, out HttpStatusCode status)
    {
        status = default;
        if (OperatingSystem.IsBrowser()) return false;

        status = socket.HttpStatusCode;
        return status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
    }

    /// <summary>
    /// Sends a message as a single binary frame.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sends are serialized behind a semaphore because <see cref="ClientWebSocket"/> documents
    /// support for exactly one outstanding send at a time. Overlap is the normal case here, not an
    /// edge case: document updates, presence heartbeats and handshake replies are produced
    /// independently.
    /// </para>
    /// <para>
    /// Per the <see cref="IBcrdtTransport"/> contract the message is dropped rather than thrown for when
    /// there is no connection; the next handshake repairs the gap. A send cancelled part-way through
    /// is different: the framework aborts the whole socket rather than leave half a frame on the
    /// wire, so that is reported as the lost connection it is.
    /// </para>
    /// </remarks>
    public async ValueTask SendAsync(ReadOnlyMemory<byte> message, CancellationToken cancellationToken = default)
    {
        ClientWebSocket? socket = _socket;
        if (_disposed || socket is null || State != BcrdtTransportState.Connected) return;
        if (socket.State is not WebSocketState.Open)
        {
            ReportIfDead(socket, null);
            return;
        }

        try
        {
            await _sendGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Withdrawn before anything was written; the connection is unaffected.
            return;
        }

        try
        {
            // Re-checked under the gate: the socket may have died while this send was queued behind
            // another.
            if (socket.State is not WebSocketState.Open)
            {
                ReportIfDead(socket, null);
                return;
            }

            await socket
                .SendAsync(message, WebSocketMessageType.Binary, endOfMessage: true, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            ReportIfDead(socket, ex);
        }
        catch (Exception ex) when (ex is WebSocketException or ObjectDisposedException or IOException or InvalidOperationException)
        {
            // A failed send means a dead socket, so it is reported as one. The receive loop will
            // usually reach the same conclusion; only the first terminal transition is announced.
            SetStateFrom(socket, BcrdtTransportState.Faulted, ex);
        }
        finally
        {
            _sendGate.Release();
        }
    }

    private void ReportIfDead(ClientWebSocket socket, Exception? error)
    {
        if (socket.State is WebSocketState.Aborted)
        {
            SetStateFrom(socket, BcrdtTransportState.Faulted, error ?? new WebSocketException(WebSocketError.ConnectionClosedPrematurely));
        }
    }

    /// <summary>Closes the socket, keeping the transport reusable.</summary>
    /// <remarks>
    /// The receive loop is cancelled but not awaited here. A message handler is entitled to decide
    /// that the session is over - a refused authorization being the obvious case - and awaiting the
    /// loop from inside its own callback would deadlock. Disposal is where the loop is awaited, and
    /// until then a loop whose socket has been replaced is silenced by
    /// <see cref="SetStateFrom(ClientWebSocket, BcrdtTransportState, Exception?)"/>.
    /// </remarks>
    public async ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (State == BcrdtTransportState.Disconnected && _socket is null) return;

        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await TeardownAsync(cancellationToken).ConfigureAwait(false);
            SetState(BcrdtTransportState.Disconnected, null);
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    /// <summary>
    /// Reads whole messages until the socket closes or the transport is torn down.
    /// </summary>
    private async Task ReceiveLoopAsync(ClientWebSocket socket, Task announced, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[_receiveBufferSize];

        // One accumulator for the life of the loop rather than one per message: it keeps its capacity
        // between ordinary messages, so a steady stream of updates settles into no allocation beyond
        // the payload each receiver is handed.
        using MemoryStream accumulator = new(_receiveBufferSize);

        BcrdtTransportState outcome = BcrdtTransportState.Disconnected;
        Exception? error = null;

        try
        {
            while (!cancellationToken.IsCancellationRequested && socket.State is WebSocketState.Open)
            {
                byte[]? message = await BcrdtWebSocketFraming
                    .ReceiveMessageAsync(socket, buffer, accumulator, MaxMessageSize, cancellationToken)
                    .ConfigureAwait(false);

                if (message is null)
                {
                    // The peer sent a close frame; answering it is what makes the shutdown orderly
                    // rather than an abort on its side.
                    await BcrdtWebSocketFraming.AcknowledgeCloseAsync(socket).ConfigureAwait(false);
                    break;
                }

                if (message.Length == 0) continue;

                MessageReceived?.Invoke(message);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Torn down deliberately; DisconnectAsync and DisposeAsync own the state transition.
            return;
        }
        catch (Exception ex)
        {
            // Including an OperationCanceledException nobody asked for: the framework reports an
            // aborted socket that way, and an aborted socket is a lost connection.
            outcome = BcrdtTransportState.Faulted;
            error = ex;
        }

        // A socket aborted between two reads - or before the first one - ends the loop through its
        // condition rather than through an exception, and is just as much a lost connection.
        if (outcome == BcrdtTransportState.Disconnected && socket.State is WebSocketState.Aborted)
        {
            outcome = BcrdtTransportState.Faulted;
            error = new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
        }

        // Never report the end of a connection before its beginning has been announced.
        await announced.ConfigureAwait(false);
        SetStateFrom(socket, outcome, error);
    }

    /// <summary>
    /// Stops the receive loop and closes the current socket.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Always called while holding the lifecycle gate, so connect and disconnect cannot interleave
    /// their sockets.
    /// </para>
    /// <para>
    /// The close frame goes out <i>before</i> the receive loop is cancelled: cancelling a pending
    /// receive aborts the socket, after which no close frame can be sent and the peer sees an abrupt
    /// disconnection. <c>CloseOutputAsync</c> rather than <c>CloseAsync</c>, because the latter
    /// reads the peer's answer from the socket and would race the receive loop for it; and bounded,
    /// because it can queue behind a send stuck on a peer that has stopped reading.
    /// </para>
    /// </remarks>
    private async Task TeardownAsync(CancellationToken cancellationToken)
    {
        ClientWebSocket? socket = Interlocked.Exchange(ref _socket, null);
        CancellationTokenSource? shutdown = Interlocked.Exchange(ref _receiveShutdown, null);

        if (socket is not null && socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try
            {
                using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(CloseTimeout);
                await socket
                    .CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing", timeout.Token)
                    .ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Closing a socket that is already broken is expected to fail, and there is nothing
                // left to salvage.
            }
        }

        if (shutdown is not null)
        {
            // Cancelled but not disposed: the receive loop still holds this token, and a source with
            // no timer registration is reclaimed along with the loop that was watching it.
            await shutdown.CancelAsync().ConfigureAwait(false);
        }

        socket?.Dispose();
    }

    /// <summary>
    /// Reports a transition on behalf of a specific socket, ignoring it if that socket has since been
    /// replaced, or if a terminal transition has already been reported for it.
    /// </summary>
    /// <remarks>
    /// Without the first check, a receive loop unwinding from the previous connection could fault a
    /// connection that has already been re-established. Without the second, a failed send and the
    /// receive loop reaching the same conclusion would report one loss twice.
    /// </remarks>
    private void SetStateFrom(ClientWebSocket socket, BcrdtTransportState state, Exception? error)
    {
        lock (_stateGate)
        {
            if (!ReferenceEquals(Volatile.Read(ref _socket), socket)) return;

            BcrdtTransportState current = State;
            if (current == state) return;
            if (IsTerminal(state) && IsTerminal(current)) return;

            Volatile.Write(ref _state, (int)state);
        }

        StateChanged?.Invoke(new BcrdtTransportStateChange(state, error));
    }

    private static bool IsTerminal(BcrdtTransportState state) =>
        state is BcrdtTransportState.Disconnected or BcrdtTransportState.Faulted;

    private void SetState(BcrdtTransportState state, Exception? error)
    {
        // The receive loop runs on its own thread, so a failure can land while ConnectAsync is still
        // running. The lock keeps the comparison and the assignment together so that one transition
        // is announced exactly once, and the handler runs outside it because it re-enters this
        // transport to start the handshake.
        lock (_stateGate)
        {
            if (State == state) return;
            Volatile.Write(ref _state, (int)state);
        }

        StateChanged?.Invoke(new BcrdtTransportStateChange(state, error));
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await DisconnectAsync().ConfigureAwait(false);

        // Taken after disconnecting, so a loop started by a connect that was in flight is included.
        Task? receiveLoop = Interlocked.Exchange(ref _receiveLoop, null);
        if (receiveLoop is not null)
        {
            try
            {
                await receiveLoop.ConfigureAwait(false);
            }
            catch (Exception)
            {
                // The loop's failure, if any, was already reported through StateChanged.
            }
        }

        MessageReceived = null;
        StateChanged = null;
    }
}

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
