using Microsoft.AspNetCore.SignalR.Client;

namespace Bit.Bcrdt.SignalR;

/// <summary>Builds the hub connection a <see cref="BcrdtSignalRTransport"/> will run on.</summary>
/// <remarks>
/// A factory rather than a ready-made connection, because the transport replaces its connection on
/// every reconnect: handing it a single instance would leave it holding a closed object after the
/// first drop, with no way to obtain a fresh one.
/// </remarks>
/// <returns>A connection that has not been started.</returns>
public delegate HubConnection BcrdtHubConnectionFactory();

/// <summary>
/// Carries the synchronization protocol over an ASP.NET Core SignalR hub connection.
/// </summary>
/// <remarks>
/// <para>
/// SignalR is the pragmatic default for a Blazor application: it negotiates the best available
/// transport, works through proxies that block raw WebSockets, and falls back to server-sent events
/// or long polling on networks that need it. The cost is a framing layer this transport has to keep
/// out of the protocol's way, which is what the hub method names below are for - the wire format is
/// still exactly the bytes <see cref="BcrdtSyncProtocol"/> produced.
/// </para>
/// <para>
/// The default hub protocol is JSON, which base64-encodes a <see cref="byte"/> array and so inflates
/// every update by a third. Register the MessagePack protocol in the configure callback for a binary
/// frame when throughput matters. Whichever protocol is used, the server's
/// <c>HubOptions.MaximumReceiveMessageSize</c> applies to every message a client sends, and its
/// ASP.NET Core default of 32 KB is smaller than the handshake reply for any sizeable document: a
/// client sending a larger message is disconnected by the server. Register the hub with
/// <c>AddBitBcrdtHub</c> from <c>Bit.Bcrdt.Server</c>, which raises
/// the limit.
/// </para>
/// <para>
/// <b>Reconnection is deliberately left to SignalR's caller, not to SignalR.</b> The connection is
/// built without <c>WithAutomaticReconnect</c> so that a drop surfaces as a
/// <see cref="BcrdtTransportState.Disconnected"/> transition and the <c>BcrdtSyncProvider</c> state machine
/// above re-runs <see cref="ConnectAsync"/> and then the handshake. Enabling SignalR's own
/// reconnection as well would produce two competing retry loops with two backoff schedules, and -
/// worse - SignalR would restore the connection silently, so the provider would never see the drop
/// and would never re-issue <c>SyncStep1</c>. The replica would sit on a live connection believing
/// it was in sync while missing every update sent during the outage. Callers who want SignalR to own
/// reconnection can still enable it through the configure callback; the
/// <see cref="HubConnection.Reconnected"/> mapping below keeps that case correct.
/// </para>
/// </remarks>
public sealed class BcrdtSignalRTransport : IBcrdtTransport
{
    /// <summary>The hub method a client calls to subscribe to a document's messages.</summary>
    public const string JoinMethod = "Join";

    /// <summary>The hub method a client calls to stop receiving a document's messages.</summary>
    public const string LeaveMethod = "Leave";

    /// <summary>The hub method a client calls to relay a message to the document's other clients.</summary>
    public const string BroadcastMethod = "Broadcast";

    /// <summary>The client method the hub calls to deliver a message.</summary>
    public const string ReceiveMethod = "Receive";

    private readonly BcrdtHubConnectionFactory _connectionFactory;
    private readonly string _documentId;

    // Never disposed: SemaphoreSlim holds nothing unmanaged unless its wait handle is requested, and
    // disposing it would strand any caller still waiting on it.
    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private readonly object _stateGate = new();

    private HubConnection? _connection;
    private ConnectionHandlers? _handlers;
    private int _state = (int)BcrdtTransportState.Disconnected;
    private volatile bool _disposed;

    /// <summary>Creates a transport over a caller-built hub connection.</summary>
    /// <param name="connectionFactory">
    /// Produces an unstarted connection each time one is needed. This is the hook for access tokens,
    /// a custom hub protocol, or any other option this transport does not model.
    /// </param>
    /// <param name="documentId">The document whose messages this transport carries.</param>
    /// <param name="description">
    /// How the peer appears in logs. Defaults to the document identifier, since a caller-built
    /// connection does not expose a URL to name it by.
    /// </param>
    public BcrdtSignalRTransport(BcrdtHubConnectionFactory connectionFactory, string documentId, string? description = null)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentException.ThrowIfNullOrEmpty(documentId);

        _connectionFactory = connectionFactory;
        _documentId = documentId;
        Description = description ?? $"signalr:{documentId}";
    }

    /// <summary>Creates a transport that connects to a hub URL.</summary>
    /// <param name="hubUrl">
    /// Where the hub is mapped, as an absolute URL. The .NET SignalR client cannot resolve a relative
    /// one; in Blazor WebAssembly, resolve it against <c>HostEnvironment.BaseAddress</c>.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="hubUrl"/> is not an absolute URL.</exception>
    /// <param name="documentId">The document whose messages this transport carries.</param>
    /// <param name="configure">
    /// Applied to the builder before the connection is created, for access tokens, a binary hub
    /// protocol or logging. Note that enabling automatic reconnection here hands reconnection to
    /// SignalR; see the remarks on <see cref="BcrdtSignalRTransport"/> for what that changes.
    /// </param>
    public BcrdtSignalRTransport(string hubUrl, string documentId, Action<IHubConnectionBuilder>? configure = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(hubUrl);
        RequireAbsolute(hubUrl);
        ArgumentException.ThrowIfNullOrEmpty(documentId);

        _documentId = documentId;
        Description = $"signalr:{documentId}@{hubUrl}";
        _connectionFactory = () =>
        {
            HubConnectionBuilder builder = new();
            builder.WithUrl(hubUrl);
            configure?.Invoke(builder);
            return builder.Build();
        };
    }

    /// <summary>
    /// Rejects a relative hub URL when it is configured, rather than when the first connection is
    /// built, where it would surface as an <see cref="UriFormatException"/> from inside SignalR.
    /// </summary>
    internal static void RequireAbsolute(string hubUrl)
    {
        // The scheme test matters as much as the absoluteness test: on Unix a path such as
        // "/hub/collaboration" parses as an absolute file URI.
        if (!Uri.TryCreate(hubUrl, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                $"The hub URL '{hubUrl}' is not an absolute http(s) URL, which the .NET SignalR client needs; " +
                "in Blazor WebAssembly, resolve it against HostEnvironment.BaseAddress.",
                nameof(hubUrl));
        }
    }

    /// <inheritdoc/>
    public BcrdtTransportState State => (BcrdtTransportState)Volatile.Read(ref _state);

    /// <inheritdoc/>
    public string Description { get; }

    /// <summary>The document this transport carries messages for.</summary>
    public string DocumentId => _documentId;

    /// <inheritdoc/>
    public event Action<ReadOnlyMemory<byte>>? MessageReceived;

    /// <inheritdoc/>
    public event Action<BcrdtTransportStateChange>? StateChanged;

    /// <summary>
    /// Builds a connection, starts it, and joins the document's group.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The join is an <c>InvokeAsync</c> rather than a fire-and-forget send, because its completion
    /// is what proves the server accepted this client for this document. Reporting
    /// <see cref="BcrdtTransportState.Connected"/> before the server had agreed would start a handshake
    /// into a group this connection was never added to, and the silence that followed would look
    /// like an idle document rather than a refusal.
    /// </para>
    /// <para>
    /// A failed attempt is reported only by the thrown exception: the connection's
    /// <see cref="HubConnection.Closed"/> event is held back until this method has decided, and
    /// discarded if it failed. A close that lands after the join but before
    /// <see cref="BcrdtTransportState.Connected"/> is announced is reported straight after the
    /// announcement, so the transport can never claim a connection that is already gone.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The transport has been disposed.</exception>
    public async ValueTask ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (State == BcrdtTransportState.Connected) return;

        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        ConnectionHandlers? handlers = null;
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (State == BcrdtTransportState.Connected) return;

            SetState(BcrdtTransportState.Connecting, null);

            // A HubConnection that has closed is discarded rather than restarted: the provider
            // reconnects by calling this method again, and a fresh instance is the only way to be
            // certain no negotiation state or handler registration survives a failed attempt.
            await DiscardConnectionAsync().ConfigureAwait(false);

            HubConnection connection = _connectionFactory();
            connection.On<byte[]>(ReceiveMethod, OnReceive);
            handlers = new ConnectionHandlers(this, connection);
            _connection = connection;
            _handlers = handlers;

            await connection.StartAsync(cancellationToken).ConfigureAwait(false);
            await connection.InvokeAsync(JoinMethod, _documentId, cancellationToken).ConfigureAwait(false);

            SetState(BcrdtTransportState.Connected, null);
            handlers.Decide(connected: true);
        }
        catch
        {
            // The thrown exception is the whole failure signal; the held-back Closed event is
            // dropped, because a provider that both catches this and observes a terminal transition
            // would start two reconnection loops for one failure.
            handlers?.Decide(connected: false);
            await DiscardConnectionAsync().ConfigureAwait(false);
            lock (_stateGate) Volatile.Write(ref _state, (int)BcrdtTransportState.Disconnected);
            throw;
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    /// <summary>
    /// Relays a message to the document's other clients.
    /// </summary>
    /// <remarks>
    /// Uses a send rather than an invoke: the hub has nothing to return, and awaiting a round trip
    /// per message would put the server's latency in front of every keystroke. Per the
    /// <see cref="IBcrdtTransport"/> contract this returns silently when there is no connection - the
    /// next handshake repairs whatever was dropped, so a disconnection stays an ordinary network
    /// event instead of becoming an exception on the editing path.
    /// </remarks>
    public async ValueTask SendAsync(ReadOnlyMemory<byte> message, CancellationToken cancellationToken = default)
    {
        HubConnection? connection = _connection;
        if (_disposed || connection is null || State != BcrdtTransportState.Connected) return;

        try
        {
            // SignalR serializes the argument after this call returns, and the caller is entitled to
            // reuse its buffer the moment Send completes, so the payload is copied.
            await connection.SendAsync(BroadcastMethod, _documentId, message.ToArray(), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception) when (State != BcrdtTransportState.Connected || connection.State != HubConnectionState.Connected)
        {
            // Raced with the connection dropping; the Closed handler reports it.
        }
    }

    /// <summary>Leaves the document's group and closes the connection, keeping the transport reusable.</summary>
    public async ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (State == BcrdtTransportState.Disconnected && _connection is null) return;

        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            HubConnection? connection = _connection;
            if (connection is not null && connection.State == HubConnectionState.Connected)
            {
                try
                {
                    await connection.InvokeAsync(LeaveMethod, _documentId, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Best effort: leaving is a courtesy to the other clients, and failing to say
                    // goodbye must not stop us from closing.
                }
            }

            await DiscardConnectionAsync().ConfigureAwait(false);
            SetState(BcrdtTransportState.Disconnected, null);
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    private void OnReceive(byte[] message)
    {
        // Not gated on Connected: the server answers a refused Join with an Auth message before this
        // transport has finished announcing the connection, and dropping it would turn a clear
        // refusal into a document that silently never syncs.
        if (message is null || message.Length == 0) return;
        MessageReceived?.Invoke(message);
    }

    private bool IsCurrent(HubConnection connection) => ReferenceEquals(Volatile.Read(ref _connection), connection);

    /// <summary>
    /// Detaches the handlers before disposing, so that a close this transport asked for cannot be
    /// mistaken for a drop and trigger a reconnection nobody wanted.
    /// </summary>
    private async Task DiscardConnectionAsync()
    {
        HubConnection? connection = Interlocked.Exchange(ref _connection, null);
        Interlocked.Exchange(ref _handlers, null)?.Detach();
        if (connection is null) return;

        try
        {
            await connection.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Tearing down a connection that is already broken is expected to fail; there is nothing
            // left to salvage and nobody to report it to.
        }
    }

    private void SetState(BcrdtTransportState state, Exception? error)
    {
        // SignalR raises its lifecycle events on its own threads, so a drop can land while
        // ConnectAsync is still running. The lock keeps the comparison and the assignment together
        // so that one transition is announced exactly once, and the handler runs outside it because
        // it may re-enter this transport.
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
        MessageReceived = null;
        StateChanged = null;
    }

    /// <summary>
    /// The lifecycle handlers of one connection, held back until its connect attempt has decided.
    /// </summary>
    private sealed class ConnectionHandlers
    {
        private readonly BcrdtSignalRTransport _owner;
        private readonly HubConnection _connection;
        private readonly TaskCompletionSource<bool> _decision = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ConnectionHandlers(BcrdtSignalRTransport owner, HubConnection connection)
        {
            _owner = owner;
            _connection = connection;
            connection.Closed += OnClosedAsync;
            connection.Reconnecting += OnReconnectingAsync;
            connection.Reconnected += OnReconnectedAsync;
        }

        /// <summary>Releases the held-back events: reported if the connect succeeded, dropped if not.</summary>
        public void Decide(bool connected) => _decision.TrySetResult(connected);

        public void Detach()
        {
            _connection.Closed -= OnClosedAsync;
            _connection.Reconnecting -= OnReconnectingAsync;
            _connection.Reconnected -= OnReconnectedAsync;
            _decision.TrySetResult(false);
        }

        /// <summary>Waits for the decision, off SignalR's own lock, and says whether to report.</summary>
        private async Task<bool> ShouldReportAsync()
        {
            await Task.Yield();
            return await _decision.Task.ConfigureAwait(false) && _owner.IsCurrent(_connection);
        }

        /// <summary>Reports an unexpected close, which is what lets the provider above start its backoff.</summary>
        private async Task OnClosedAsync(Exception? error)
        {
            if (!await ShouldReportAsync().ConfigureAwait(false)) return;
            _owner.SetState(error is null ? BcrdtTransportState.Disconnected : BcrdtTransportState.Faulted, error);
        }

        private async Task OnReconnectingAsync(Exception? error)
        {
            if (!await ShouldReportAsync().ConfigureAwait(false)) return;
            _owner.SetState(BcrdtTransportState.Connecting, error);
        }

        /// <summary>
        /// Rejoins the document's group after SignalR restored the connection on its own.
        /// </summary>
        /// <remarks>
        /// Only reachable when a caller opted into automatic reconnection through the configure
        /// callback. The rejoin is not optional: group membership is scoped to a connection and does
        /// not survive a reconnect, so a client that skipped it would hold a healthy connection, run
        /// a handshake into an empty group, and receive nothing ever again.
        /// </remarks>
        private async Task OnReconnectedAsync(string? connectionId)
        {
            if (!await ShouldReportAsync().ConfigureAwait(false)) return;

            try
            {
                await _connection.InvokeAsync(JoinMethod, _owner._documentId).ConfigureAwait(false);
                if (_owner.IsCurrent(_connection)) _owner.SetState(BcrdtTransportState.Connected, null);
            }
            catch (Exception ex)
            {
                if (_owner.IsCurrent(_connection)) _owner.SetState(BcrdtTransportState.Faulted, ex);
            }
        }
    }
}
