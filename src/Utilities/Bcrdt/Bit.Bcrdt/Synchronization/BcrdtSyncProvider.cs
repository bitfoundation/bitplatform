using System.Diagnostics;

namespace Bit.Bcrdt;

/// <summary>The connection lifecycle as an application sees it.</summary>
public enum BcrdtConnectionStatus
{
    /// <summary>Not connected and not trying.</summary>
    Disconnected,
    /// <summary>Establishing a connection.</summary>
    Connecting,
    /// <summary>Connected, exchanging state with the peer.</summary>
    Handshaking,
    /// <summary>Connected and up to date.</summary>
    Synced,
    /// <summary>Connection lost; waiting to retry.</summary>
    Reconnecting,
    /// <summary>Gave up, or was refused.</summary>
    Faulted,
}

/// <summary>Settings for a <see cref="BcrdtSyncProvider"/>.</summary>
public sealed class BcrdtSyncProviderOptions
{
    /// <summary>
    /// How often to refresh this replica's presence and expire participants that have gone quiet.
    /// </summary>
    /// <remarks>
    /// This is a presence heartbeat, not a connection probe: detecting a dead connection is the
    /// transport's job (SignalR's server timeout, the WebSocket transport's keep-alive). Probing at
    /// this layer would be broadcast through any relay to every peer in the room, and every peer's
    /// answer broadcast back again - traffic that grows with the square of the room's size.
    /// </remarks>
    public TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How often to re-run the handshake even while apparently connected.
    /// </summary>
    /// <remarks>
    /// Off by default, because the protocol repairs itself on reconnect. Worth enabling over a
    /// transport that can silently drop a message without dropping the connection, where a lost
    /// update would otherwise go unnoticed until the next disconnection. Runs on its own schedule,
    /// whether or not <see cref="HeartbeatInterval"/> is enabled and whichever of the two is shorter.
    /// </remarks>
    public TimeSpan ResyncInterval { get; init; } = TimeSpan.Zero;

    /// <summary>
    /// How long to wait for any peer to answer the opening handshake before concluding that none is
    /// present and treating this replica as synced.
    /// </summary>
    /// <remarks>
    /// A replica that is first into a room behind a relay - the SignalR hub and the WebSocket endpoint
    /// both relay without holding state - has nobody to answer it, and without this would stay in
    /// <see cref="BcrdtConnectionStatus.Handshaking"/> until someone else arrived. If a peer does answer
    /// but its state has not been fully received by then, the handshake is repeated instead.
    /// <see cref="TimeSpan.Zero"/> disables the fallback.
    /// </remarks>
    public TimeSpan HandshakeTimeout { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>How reconnection is paced.</summary>
    public BcrdtReconnectPolicy Reconnect { get; init; } = new();

    /// <summary>Whether presence is exchanged over the same connection, in both directions.</summary>
    public bool SyncAwareness { get; init; } = true;

    /// <summary>
    /// The largest message the provider sends, in bytes; a larger change or handshake answer goes out as
    /// several. Zero sends every message whole.
    /// </summary>
    /// <remarks>
    /// Relays limit what a client may send - a message over the limit gets the client disconnected - and
    /// a document's full state, or one large paste, can exceed any limit. Keep this below the relay's.
    /// Defaults to <see cref="BcrdtSyncProtocol.DefaultMaxMessageSize"/>.
    /// </remarks>
    public int MaxMessageSize { get; init; } = BcrdtSyncProtocol.DefaultMaxMessageSize;
}

/// <summary>
/// Keeps a document, and optionally its presence state, in sync with a peer over a transport.
/// </summary>
/// <remarks>
/// <para>
/// This is where the pieces meet: it listens to the document for local changes, forwards them, feeds
/// incoming updates back in, and runs the handshake whenever a connection is established. It also
/// owns the connection lifecycle, reconnecting with backoff when the transport drops.
/// </para>
/// <para>
/// There is no outbound message queue, and that is deliberate rather than an omission. Changes made
/// while offline accumulate in the document itself, so an eight-hour offline session reconnects with
/// one delta describing the net result - not eight hours of replayed packets. The document <i>is</i>
/// the queue, and it is a self-compacting one.
/// </para>
/// <para>
/// Updates that arrive from the peer are applied with this provider as their origin, which is what
/// stops them being echoed straight back and what keeps them out of the local undo stack.
/// </para>
/// <para>
/// <b>Threading.</b> A <see cref="BcrdtDocument"/> and a <see cref="BcrdtAwarenessStore"/> are not
/// thread-safe, while transports raise their events on whatever thread their I/O completes on. The
/// provider therefore captures <see cref="SynchronizationContext.Current"/> when it is constructed -
/// in Blazor, the renderer's context - and runs every transport callback, heartbeat tick, resync and
/// handshake on it: inline when already there, posted otherwise. Construct it on the thread that
/// edits the document. When there is no context to capture, the provider serializes its own access
/// to the document under a private lock; the application must then not mutate the document
/// concurrently with the provider.
/// </para>
/// <para>
/// Transport sends and the provider's own <see cref="StatusChanged"/>, <see cref="Synced"/> and
/// <see cref="AuthorizationDenied"/> events are queued and run once that lock is released, on the
/// context when there is one. Two things do run under it: the document's and the awareness store's
/// own observers, which applying an update or publishing presence raises synchronously - so an
/// observer must not block waiting on another thread that needs this provider - and nothing else.
/// <see cref="Error"/> may additionally be raised from a background continuation, such as a failed
/// send, so its handlers must not assume they are on the context.
/// </para>
/// </remarks>
public sealed class BcrdtSyncProvider : IAsyncDisposable
{
    /// <summary>How long disposal waits for the context to run the departure announcement.</summary>
    private static readonly TimeSpan DisposeGracePeriod = TimeSpan.FromSeconds(2);

    private readonly BcrdtDocument _doc;
    private readonly IBcrdtTransport _transport;
    private readonly BcrdtAwarenessStore? _awareness;
    private readonly BcrdtSyncProviderOptions _options;
    private readonly SynchronizationContext? _context;
    private readonly CancellationTokenSource _shutdown = new();

    // Everything below is guarded by _gate. Work that calls out of the provider - transport sends,
    // the provider's own events - is queued in _deferred and run only once the gate is released. The
    // exception is the document's and awareness store's observers, which run synchronously inside the
    // ApplyUpdate or SetLocalState call that raises them; see the class remarks.
    private readonly object _gate = new();
    private readonly Queue<Action> _deferred = new();
    private bool _flushing;
    private int _status;
    private int _access = (int)BcrdtDocumentAccess.ReadWrite;
    private bool _wantConnection;
    private bool _stopRequested;
    private bool _refused;
    private int _reconnectAttempts;
    private CancellationTokenSource? _reconnectAttempt;
    private CancellationTokenSource? _connectAttempt;
    private int _handshakeGeneration;
    private int _handshakeRetries;
    private bool _handshakeAnswered;
    private bool _handshakeDeltaApplied;
    private bool _repairResyncPending;
    private long _handshakeStarted;
    private long _lastSynced;
    private BcrdtStateVector? _awaitedVector;
    private IReadOnlyDictionary<string, object?>? _stashedPresence;
    private Task? _heartbeatLoop;
    private Task? _reconnectLoop;

    private volatile bool _disposed;
    private int _disposeStarted;

    /// <summary>Creates a provider binding a document to a transport.</summary>
    /// <remarks>
    /// Captures the current <see cref="SynchronizationContext"/>; see the remarks on
    /// <see cref="BcrdtSyncProvider"/>.
    /// </remarks>
    public BcrdtSyncProvider(
        BcrdtDocument doc,
        IBcrdtTransport transport,
        BcrdtAwarenessStore? awareness = null,
        BcrdtSyncProviderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(transport);

        _doc = doc;
        _transport = transport;
        _awareness = awareness;
        _options = options ?? new BcrdtSyncProviderOptions();
        _context = SynchronizationContext.Current;

        _doc.Updated += OnDocumentUpdated;
        _transport.MessageReceived += OnMessageReceived;
        _transport.StateChanged += OnTransportStateChanged;

        if (SyncsAwareness) _awareness!.Updated += OnAwarenessUpdated;
    }

    /// <summary>Where the connection currently stands.</summary>
    public BcrdtConnectionStatus Status => (BcrdtConnectionStatus)Volatile.Read(ref _status);

    /// <summary>Whether the document is currently believed to match the peer's.</summary>
    public bool IsSynced => Status == BcrdtConnectionStatus.Synced;

    /// <summary>When the provider last completed a handshake, or <see langword="null"/> if it never has.</summary>
    /// <remarks>
    /// Useful for telling a user how fresh their copy is while offline, and for spotting a client that
    /// has been connected for a long time without ever catching up.
    /// </remarks>
    public DateTimeOffset? LastSyncedAt
    {
        get
        {
            long ticks = Interlocked.Read(ref _lastSynced);
            return ticks == 0 ? null : new DateTimeOffset(ticks, TimeSpan.Zero);
        }
    }

    /// <summary>Raised whenever <see cref="Status"/> changes.</summary>
    public event Action<BcrdtConnectionStatus>? StatusChanged;

    /// <summary>
    /// Raised the first time, and each subsequent time, the document catches up with its peers - or
    /// the handshake concludes that no peer is present.
    /// </summary>
    public event Action? Synced;

    /// <summary>Raised when something goes wrong that did not stop the provider working.</summary>
    /// <remarks>May be raised on any thread.</remarks>
    public event Action<Exception>? Error;

    /// <summary>Raised when a peer refuses the connection. The provider stops reconnecting.</summary>
    public event Action<string>? AuthorizationDenied;

    /// <summary>What the server last said this connection may do with the document.</summary>
    /// <remarks>
    /// <see cref="BcrdtDocumentAccess.ReadWrite"/> until a server says otherwise, which is also what a
    /// relay that predates access levels - or a transport with no server at all - amounts to. The grant
    /// is kept across reconnections, because a server that grants access announces it on every join.
    /// While it is <see cref="BcrdtDocumentAccess.ReadOnly"/> the provider sends nothing that would
    /// change another replica: the relay would drop it anyway. A local change made meanwhile stays in
    /// this replica only, and is reported through <see cref="Error"/> rather than lost quietly.
    /// </remarks>
    public BcrdtDocumentAccess Access => (BcrdtDocumentAccess)Volatile.Read(ref _access);

    /// <summary>Raised when <see cref="Access"/> changes.</summary>
    public event Action<BcrdtDocumentAccess>? AccessChanged;

    private bool IsReadOnly => Access == BcrdtDocumentAccess.ReadOnly;

    private bool SyncsAwareness => _awareness is not null && _options.SyncAwareness;

    private BcrdtConnectionStatus CurrentStatus => (BcrdtConnectionStatus)_status;

    private CancellationToken ShutdownToken
    {
        get
        {
            try
            {
                return _shutdown.Token;
            }
            catch (ObjectDisposedException)
            {
                return new CancellationToken(canceled: true);
            }
        }
    }

    /// <summary>Connects, and keeps reconnecting until told to stop.</summary>
    /// <remarks>
    /// A failed attempt does not throw: it is reported through <see cref="Error"/> and retried on the
    /// reconnection schedule. Cancelling <paramref name="cancellationToken"/> is the exception - it
    /// abandons the connection, leaves the provider disconnected, and throws. A
    /// <see cref="DisconnectAsync"/> while the attempt is still in flight abandons it too, and this
    /// method then returns normally.
    /// </remarks>
    public async ValueTask ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using CancellationTokenSource attempt = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ShutdownToken);

        bool alreadyConnected = false;
        await RunAsync(() =>
        {
            _wantConnection = true;
            _stopRequested = false;
            _refused = false;
            _reconnectAttempts = 0;
            StartHeartbeat();

            if (_transport.State == BcrdtTransportState.Connected)
            {
                // Connected by an earlier call, or before this provider existed. Nothing will announce
                // the connection again, so the handshake runs now unless one is already under way.
                alreadyConnected = true;
                if (CurrentStatus is not (BcrdtConnectionStatus.Handshaking or BcrdtConnectionStatus.Synced))
                {
                    SetStatus(BcrdtConnectionStatus.Handshaking);
                    BeginHandshake();
                }
                return;
            }

            // Registered so that a disconnection can abandon it, as it can a reconnect attempt.
            _connectAttempt = attempt;
            SetStatus(BcrdtConnectionStatus.Connecting);
        }, cancellationToken).ConfigureAwait(false);

        if (alreadyConnected) return;

        try
        {
            await _transport.ConnectAsync(attempt.Token).ConfigureAwait(false);
        }
        // The outcomes below are posted to the context rather than awaited on it. This continuation
        // may be on a pool thread, and waiting for a context that is blocked - by a host disposing
        // synchronously, say - would hang the caller for nothing: none of this work has a result.
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The caller withdrew. Honour that rather than quietly reconnecting in the background.
            Post(() =>
            {
                _wantConnection = false;
                _stopRequested = true;
                CancelPendingConnects();
                if (!_refused) SetStatus(BcrdtConnectionStatus.Disconnected);
            });
            throw;
        }
        catch (OperationCanceledException) when (attempt.IsCancellationRequested)
        {
            // Abandoned by DisconnectAsync or disposal, which own the status from here.
        }
        catch (BcrdtTransportAuthorizationException ex)
        {
            Post(() => Refuse(ex.Message));
        }
        catch (Exception ex)
        {
            Post(() =>
            {
                ReportError(ex);
                ScheduleReconnect();
            });
        }
        finally
        {
            lock (_gate)
            {
                if (ReferenceEquals(_connectAttempt, attempt)) _connectAttempt = null;
            }
        }
    }

    /// <summary>Disconnects and stops trying to reconnect.</summary>
    /// <remarks>
    /// Announces this replica's departure to the room first, and restores its presence when it next
    /// connects - a deliberate disconnection hides the participant without forgetting who they are.
    /// </remarks>
    public async ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) return;

        await RunAsync(() =>
        {
            _wantConnection = false;
            _stopRequested = true;
            CancelPendingConnects();
            _handshakeGeneration++;
            SayGoodbye();
        }, cancellationToken).ConfigureAwait(false);

        await _transport.DisconnectAsync(cancellationToken).ConfigureAwait(false);

        // Posted, not awaited: this continuation may be on a pool thread, and a host that disposes
        // synchronously blocks the very context the status update would wait for. A connection asked
        // for since - a ConnectAsync that did not wait for this one - owns the status by then, and a
        // late "disconnected" must not overwrite its progress.
        Post(() =>
        {
            if (!_refused && !_wantConnection) SetStatus(BcrdtConnectionStatus.Disconnected);
        });
    }

    /// <summary>Re-runs the handshake, reconciling with the room in both directions.</summary>
    /// <remarks>
    /// Completes once the announcement has been handed to the transport's send path; the replies
    /// arrive like any others. The send goes through the same ordering as every other message, so a
    /// call made from inside a document observer - while an update is being applied - is queued
    /// until that work has finished rather than re-entering it. Cancelling stops the wait for the
    /// provider's context, never a send part-way through: on some transports that tears down the
    /// whole connection.
    /// </remarks>
    public async ValueTask ResyncAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await RunAsync(() =>
        {
            if (_transport.State == BcrdtTransportState.Connected) Send(BcrdtSyncExchange.CreateAnnouncement(_doc));
        }, cancellationToken).ConfigureAwait(false);
    }

    private void OnDocumentUpdated(BcrdtUpdateEventArgs args)
    {
        // Never echo a peer's own update back to it.
        if (ReferenceEquals(args.Origin, this)) return;

        if (IsReadOnly)
        {
            // The relay drops what a reader writes, so sending it would only cost bandwidth. A local
            // edit is the application's to know about: it will not reach anybody else.
            if (args.Transaction.Local)
            {
                ReportError(new InvalidOperationException(
                    "The document was changed locally, but this connection is read-only: the change stays in this replica and is not sent to anyone."));
            }
            return;
        }

        foreach (byte[] message in BcrdtSyncProtocol.EncodeDelta(BcrdtSyncMessageKind.Update, args.Update, _options.MaxMessageSize))
        {
            Send(message);
        }
    }

    private void OnAwarenessUpdated(byte[] update) => Send(BcrdtSyncProtocol.EncodeAwarenessUpdate(update));

    private void OnTransportStateChanged(BcrdtTransportStateChange change) => Post(() => HandleTransportStateChanged(change));

    private void OnMessageReceived(ReadOnlyMemory<byte> message)
    {
        KeyValuePair<string, object?> kind = BcrdtDiagnostics.Kind(message.Span);
        BcrdtDiagnostics.MessagesReceived.Add(1, kind);
        BcrdtDiagnostics.BytesReceived.Add(message.Length, kind);
        Post(() => HandleMessage(message));
    }

    private void HandleTransportStateChanged(BcrdtTransportStateChange change)
    {
        if (_disposed) return;

        switch (change.State)
        {
            case BcrdtTransportState.Connected:
                // The attempt count is deliberately not reset here but once the handshake completes: a
                // server that accepts the connection and then drops it - an oversized message, a proxy
                // cutting it, an eviction - would otherwise be retried at the first, shortest delay
                // forever instead of backing off.

                // The attempt that produced this connection is over; a later drop needs a new one.
                _reconnectAttempt = null;

                if (_stopRequested)
                {
                    // A connect that was already in flight when the application asked to stop, or
                    // when the peer refused us.
                    Defer(() => _ = DisconnectTransportQuietlyAsync());
                    return;
                }

                SetStatus(BcrdtConnectionStatus.Handshaking);
                BeginHandshake();
                break;

            case BcrdtTransportState.Disconnected or BcrdtTransportState.Faulted:
                if (change.Error is not null) ReportError(change.Error);

                // Posted from an I/O thread, the end of a previous connection can be handled after the
                // next one has already been announced and its handshake begun. It must not cancel
                // that handshake's progress or its timeout.
                if (_transport.State == BcrdtTransportState.Connected) break;

                _handshakeGeneration++;
                _awaitedVector = null;
                ScheduleReconnect();
                break;

            case BcrdtTransportState.Connecting:
                if (!_refused && CurrentStatus is not BcrdtConnectionStatus.Reconnecting)
                {
                    SetStatus(BcrdtConnectionStatus.Connecting);
                }
                break;
        }
    }

    /// <summary>
    /// Opens the exchange by stating what this replica has and asking who else is present.
    /// </summary>
    private void BeginHandshake()
    {
        int generation = ++_handshakeGeneration;
        _handshakeStarted = Stopwatch.GetTimestamp();
        _handshakeRetries = 0;
        _handshakeAnswered = false;
        _handshakeDeltaApplied = false;
        _awaitedVector = null;

        try
        {
            Send(BcrdtSyncExchange.CreateAnnouncement(_doc));

            if (SyncsAwareness)
            {
                BcrdtAwarenessStore awareness = _awareness!;
                Send(BcrdtSyncProtocol.EncodeAwarenessQuery());

                IReadOnlyDictionary<string, object?>? stashed = _stashedPresence;
                _stashedPresence = null;

                if (stashed is not null && awareness.LocalState is null)
                {
                    // Restore what the last deliberate disconnection withdrew. Publishing goes out
                    // through the store's Updated event like any other local change.
                    awareness.SetLocalState(stashed);
                }
                else if (awareness.LocalState is not null)
                {
                    Send(BcrdtSyncProtocol.EncodeAwarenessUpdate(awareness.EncodeUpdate([awareness.LocalClientId])));
                }
            }
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }

        StartHandshakeTimer(generation);
    }

    private void StartHandshakeTimer(int generation)
    {
        TimeSpan timeout = _options.HandshakeTimeout;
        if (timeout <= TimeSpan.Zero) return;

        // Each repeat waits twice as long, up to a minute: a peer that has answered but whose data is
        // slow to arrive - a large document on a slow link - must not be asked for all of it again
        // every couple of seconds, which would only slow it down further.
        long ticks = Math.Min(timeout.Ticks << Math.Min(_handshakeRetries, 5), Math.Max(timeout.Ticks, MaxHandshakeTimeout.Ticks));
        _ = HandshakeTimeoutAsync(TimeSpan.FromTicks(ticks), generation);
    }

    /// <summary>The longest a repeated handshake waits for its answer.</summary>
    private static readonly TimeSpan MaxHandshakeTimeout = TimeSpan.FromMinutes(1);

    private async Task HandshakeTimeoutAsync(TimeSpan timeout, int generation)
    {
        try
        {
            CancellationToken token = ShutdownToken;
            await Task.Delay(timeout, token).ConfigureAwait(false);
            await RunAsync(() => OnHandshakeTimeout(generation), token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    private void OnHandshakeTimeout(int generation)
    {
        if (_disposed || generation != _handshakeGeneration || CurrentStatus != BcrdtConnectionStatus.Handshaking) return;

        if (!_handshakeAnswered)
        {
            // Nobody answered: there is no peer to be behind, so this replica's state is the shared one.
            SetStatus(BcrdtConnectionStatus.Synced);
            return;
        }

        // A peer answered, but what it holds has not all arrived - a lost message, or a slow one.
        int next = ++_handshakeGeneration;
        _handshakeRetries++;
        _handshakeAnswered = false;
        _handshakeDeltaApplied = false;
        _awaitedVector = null;
        Send(BcrdtSyncExchange.CreateAnnouncement(_doc));
        StartHandshakeTimer(next);
    }

    private void HandleMessage(ReadOnlyMemory<byte> raw)
    {
        if (_disposed) return;

        try
        {
            BcrdtSyncMessage message = BcrdtSyncProtocol.Decode(raw);

            switch (message.Kind)
            {
                case BcrdtSyncMessageKind.SyncStep1:
                case BcrdtSyncMessageKind.SyncStep1Reply:
                case BcrdtSyncMessageKind.SyncStep2:
                case BcrdtSyncMessageKind.Update:
                    HandleDocumentMessage(message);
                    break;

                case BcrdtSyncMessageKind.AwarenessUpdate:
                    if (SyncsAwareness) _awareness!.ApplyUpdate(message.Payload, origin: this);
                    break;

                case BcrdtSyncMessageKind.AwarenessQuery:
                    AnswerAwarenessQuery();
                    break;

                case BcrdtSyncMessageKind.Auth:
                {
                    (BcrdtDocumentAccess access, string reason) = BcrdtSyncProtocol.DecodeAuthAccess(message.Payload);

                    // Refusal is final: retrying would only produce the same answer.
                    if (access == BcrdtDocumentAccess.None) Refuse(reason);
                    else SetAccess(access);
                    break;
                }

                case BcrdtSyncMessageKind.Ping:
                    Send(BcrdtSyncProtocol.EncodePong());
                    break;

                case BcrdtSyncMessageKind.Pong:
                    break;
            }
        }
        catch (Exception ex)
        {
            // A malformed or hostile message must cost us this message, not the session.
            ReportError(ex);
        }
    }

    private void HandleDocumentMessage(BcrdtSyncMessage message)
    {
        List<byte[]> replies = [];
        BcrdtSyncExchangeResult result = default;
        try
        {
            result = BcrdtSyncExchange.Handle(_doc, message, this, replies, _options.MaxMessageSize);
        }
        finally
        {
            // A reader answers nobody. Its content would be dropped by the relay, and its statement of what
            // it holds would promise others content they will never receive - a local edit it made, or one
            // it made offline before it learned it was read-only - so they would wait for it for ever. It
            // still announces itself and applies what it is sent.
            if (!IsReadOnly)
            {
                foreach (byte[] reply in replies) Send(reply);
            }
            UpdateHandshakeProgress(result);
        }
    }

    /// <summary>
    /// Decides whether the handshake is complete.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only a <see cref="BcrdtSyncMessageKind.SyncStep1Reply"/> proves anything: it states what a peer
    /// holds, so once this replica's state vector covers it - and a delta carrying the peer's whole
    /// delete set has been applied - the replica holds everything that peer does. A
    /// <see cref="BcrdtSyncMessageKind.Update"/> proves nothing, and a relay delivers every peer's
    /// <see cref="BcrdtSyncMessageKind.SyncStep2"/> to everyone, computed against somebody else's state.
    /// </para>
    /// <para>
    /// A peer sends its small reply before its delta, so the reply arrives quickly even when the delta
    /// is large and the link slow - which is what stops the handshake timeout from concluding that
    /// nobody is there while the document is still on its way. Waiting for a delta as well keeps a
    /// replica that already has every block, but not every deletion, from claiming to be synced in the
    /// moment between the two.
    /// </para>
    /// </remarks>
    private void UpdateHandshakeProgress(BcrdtSyncExchangeResult result)
    {
        if (CurrentStatus != BcrdtConnectionStatus.Handshaking) return;

        if (result.AppliedDelta) _handshakeDeltaApplied = true;

        if (result.PeerStateVector is { } peer)
        {
            _handshakeAnswered = true;
            if (_awaitedVector is null) _awaitedVector = peer.Clone();
            else _awaitedVector.MergeMax(peer);
        }

        if (_handshakeAnswered &&
            _handshakeDeltaApplied &&
            _awaitedVector is not null &&
            _doc.GetStateVector().IsSupersetOf(_awaitedVector))
        {
            _awaitedVector = null;
            SetStatus(BcrdtConnectionStatus.Synced);
        }
    }

    private void AnswerAwarenessQuery()
    {
        if (!SyncsAwareness) return;

        // Only our own entry. Every live peer answers for itself, so the asker hears from everyone who
        // is actually present - and a relayed copy of a departed client can never refresh its timeout.
        BcrdtAwarenessStore awareness = _awareness!;
        if (awareness.LocalState is null) return;
        Send(BcrdtSyncProtocol.EncodeAwarenessUpdate(awareness.EncodeUpdate([awareness.LocalClientId])));
    }

    /// <summary>Tells the room this replica is leaving, remembering what it was publishing.</summary>
    private void SayGoodbye()
    {
        if (!SyncsAwareness || _transport.State != BcrdtTransportState.Connected) return;

        BcrdtAwarenessStore awareness = _awareness!;
        try
        {
            IReadOnlyDictionary<string, object?>? local = awareness.LocalState;
            if (local is null) return;

            _stashedPresence = local;
            awareness.SetLocalState(null);
        }
        catch (ObjectDisposedException)
        {
            // The store is already gone, and with it anything to announce.
        }
    }

    private void SetAccess(BcrdtDocumentAccess access)
    {
        if (Interlocked.Exchange(ref _access, (int)access) == (int)access) return;
        Defer(() => AccessChanged?.Invoke(access));
    }

    private void Refuse(string reason)
    {
        _refused = true;
        _wantConnection = false;
        _stopRequested = true;
        CancelPendingConnects();
        _handshakeGeneration++;
        _awaitedVector = null;
        SetStatus(BcrdtConnectionStatus.Faulted);
        Defer(() => AuthorizationDenied?.Invoke(reason));

        // A refusal binds even if the peer leaves the connection open.
        if (_transport.State is not BcrdtTransportState.Disconnected)
        {
            Defer(() => _ = DisconnectTransportQuietlyAsync());
        }
    }

    private void ScheduleReconnect()
    {
        if (_disposed) return;

        if (!_wantConnection)
        {
            if (!_refused) SetStatus(BcrdtConnectionStatus.Disconnected);
            return;
        }

        if (_transport.State == BcrdtTransportState.Connected) return;

        // One attempt at a time: a transport that reports a single failure twice, or a drop that
        // lands while an attempt is pending, must not start a second loop.
        if (_reconnectAttempt is not null)
        {
            SetStatus(BcrdtConnectionStatus.Reconnecting);
            return;
        }

        BcrdtReconnectPolicy policy = _options.Reconnect;
        if (!policy.Enabled)
        {
            SetStatus(BcrdtConnectionStatus.Disconnected);
            return;
        }

        if (_reconnectAttempts >= policy.MaxAttempts)
        {
            SetStatus(BcrdtConnectionStatus.Faulted);
            return;
        }

        CancellationTokenSource attempt;
        try
        {
            attempt = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token);
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        int number = ++_reconnectAttempts;
        BcrdtDiagnostics.Reconnects.Add(1);
        _reconnectAttempt = attempt;
        SetStatus(BcrdtConnectionStatus.Reconnecting);
        _reconnectLoop = Task.Run(() => ReconnectAsync(attempt, number), CancellationToken.None);
    }

    private async Task ReconnectAsync(CancellationTokenSource attempt, int number)
    {
        Exception? failure = null;
        try
        {
            await Task.Delay(_options.Reconnect.NextDelay(number), attempt.Token).ConfigureAwait(false);
            await _transport.ConnectAsync(attempt.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (attempt.IsCancellationRequested)
        {
            // Disposed, or the application disconnected while this attempt was pending.
        }
        catch (Exception ex)
        {
            // Including an OperationCanceledException the transport raised for reasons of its own -
            // a handshake or negotiation timeout - which is a failed attempt, not a shutdown.
            failure = ex;
        }

        try
        {
            bool cancelled = attempt.IsCancellationRequested;
            await RunAsync(() =>
            {
                bool current = ReferenceEquals(_reconnectAttempt, attempt);
                if (current) _reconnectAttempt = null;
                if (failure is null) return;

                // An attempt that was abandoned or superseded - by a disconnection, or by a fresh
                // ConnectAsync - may still fail on its way out. Its failure is worth reporting, but it
                // must not schedule another loop or refuse a connection that is no longer its business.
                if (!current || cancelled)
                {
                    ReportError(failure);
                    return;
                }

                if (failure is BcrdtTransportAuthorizationException denied)
                {
                    Refuse(denied.Message);
                    return;
                }

                ReportError(failure);
                ScheduleReconnect();
            }, ShutdownToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
        finally
        {
            attempt.Dispose();
        }
    }

    /// <summary>Abandons any connection attempt in flight - the first one, or a reconnection.</summary>
    /// <remarks>
    /// The cancellation itself is deferred until the gate is released, because cancelling runs the
    /// transport's own callbacks synchronously.
    /// </remarks>
    private void CancelPendingConnects()
    {
        foreach (CancellationTokenSource? pending in new[] { _reconnectAttempt, _connectAttempt })
        {
            if (pending is null) continue;
            Defer(() =>
            {
                try
                {
                    pending.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // The attempt finished on its own in the meantime.
                }
            });
        }

        _reconnectAttempt = null;
        _connectAttempt = null;
    }

    private async Task DisconnectTransportQuietlyAsync()
    {
        try
        {
            await _transport.DisconnectAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    private void StartHeartbeat()
    {
        if (_heartbeatLoop is not null) return;

        TimeSpan heartbeat = _options.HeartbeatInterval;
        TimeSpan resync = _options.ResyncInterval;

        // One timer at the shorter of the two intervals, each piece of work running when its own
        // interval has elapsed - so neither is rounded up to a multiple of the other.
        TimeSpan tick = (heartbeat > TimeSpan.Zero, resync > TimeSpan.Zero) switch
        {
            (true, true) => heartbeat < resync ? heartbeat : resync,
            (true, false) => heartbeat,
            (false, true) => resync,
            _ => TimeSpan.Zero,
        };
        if (tick <= TimeSpan.Zero) return;

        CancellationToken token = ShutdownToken;
        _heartbeatLoop = Task.Run(() => HeartbeatLoopAsync(tick, token), CancellationToken.None);
    }

    private async Task HeartbeatLoopAsync(TimeSpan tick, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(tick);
        long lastHeartbeat = Environment.TickCount64;
        long lastResync = lastHeartbeat;

        // Timer ticks land a little early or late. Half a tick of slack stops an interval that is an
        // exact multiple of the tick from being judged "not quite due" and skipped for a whole tick.
        long slack = (long)tick.TotalMilliseconds / 2;

        bool Due(TimeSpan interval, ref long last, long now)
        {
            if (interval <= TimeSpan.Zero || now - last < (long)interval.TotalMilliseconds - slack) return false;
            last = now;
            return true;
        }

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                long now = Environment.TickCount64;
                bool heartbeat = Due(_options.HeartbeatInterval, ref lastHeartbeat, now);
                bool resync = Due(_options.ResyncInterval, ref lastResync, now);
                if (!heartbeat && !resync) continue;

                try
                {
                    await RunAsync(() => Tick(heartbeat, resync), cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // One failed tick must not end the heartbeat for the rest of the session.
                    ReportError(ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
    }

    private void Tick(bool heartbeat, bool resync)
    {
        if (_disposed) return;

        if (heartbeat && SyncsAwareness)
        {
            // Expire participants we have stopped hearing from, and republish our own entry.
            try
            {
                _awareness!.Heartbeat();
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }
        }

        if (resync && _transport.State == BcrdtTransportState.Connected)
        {
            try
            {
                Send(BcrdtSyncExchange.CreateAnnouncement(_doc));
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }
        }
    }

    private void SetStatus(BcrdtConnectionStatus value)
    {
        if (CurrentStatus == value) return;
        Volatile.Write(ref _status, (int)value);

        // A connection that got as far as a completed handshake was a success; the next drop starts
        // the backoff afresh.
        if (value == BcrdtConnectionStatus.Synced)
        {
            _reconnectAttempts = 0;
            Interlocked.Exchange(ref _lastSynced, DateTimeOffset.UtcNow.UtcTicks);

            if (_handshakeStarted != 0)
            {
                BcrdtDiagnostics.HandshakeDuration.Record(Stopwatch.GetElapsedTime(_handshakeStarted).TotalMilliseconds);
                _handshakeStarted = 0;
            }
        }

        Defer(() =>
        {
            StatusChanged?.Invoke(value);
            if (value == BcrdtConnectionStatus.Synced) Synced?.Invoke();
        });
    }

    private void Send(byte[] message)
    {
        if (Monitor.IsEntered(_gate))
        {
            // Never call into the transport while holding the gate: an in-process transport delivers
            // synchronously, and the receiving provider must not take its own gate under ours.
            Defer(() => _ = SendCoreAsync(message));
            return;
        }

        _ = SendCoreAsync(message);
    }

    private async Task SendCoreAsync(byte[] message)
    {
        if (_disposed || _transport.State != BcrdtTransportState.Connected) return;

        try
        {
            await _transport.SendAsync(message).ConfigureAwait(false);

            KeyValuePair<string, object?> kind = BcrdtDiagnostics.Kind(message);
            BcrdtDiagnostics.MessagesSent.Add(1, kind);
            BcrdtDiagnostics.BytesSent.Add(message.Length, kind);
        }
        catch (Exception ex)
        {
            ReportError(ex);

            // A send that failed on a connection that stayed up is a message the peers will never ask
            // for: nothing else would make them. A handshake shortly afterwards brings it back.
            Post(ScheduleRepair);
        }
    }

    /// <summary>Re-runs the handshake soon, once, however many sends failed meanwhile.</summary>
    private void ScheduleRepair()
    {
        if (_disposed || _repairResyncPending) return;
        _repairResyncPending = true;
        _ = RepairAsync();
    }

    private async Task RepairAsync()
    {
        try
        {
            CancellationToken token = ShutdownToken;
            TimeSpan delay = _options.HandshakeTimeout > TimeSpan.Zero ? _options.HandshakeTimeout : TimeSpan.FromSeconds(2);
            await Task.Delay(delay, token).ConfigureAwait(false);
            await RunAsync(() =>
            {
                _repairResyncPending = false;
                if (!_disposed && _transport.State == BcrdtTransportState.Connected) Send(BcrdtSyncExchange.CreateAnnouncement(_doc));
            }, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    private void ReportError(Exception error)
    {
        if (Monitor.IsEntered(_gate))
        {
            Defer(() => RaiseError(error));
            return;
        }

        RaiseError(error);
    }

    private void RaiseError(Exception error)
    {
        BcrdtDiagnostics.SyncErrors.Add(1, new KeyValuePair<string, object?>("error.type", error.GetType().Name));

        try
        {
            Error?.Invoke(error);
        }
        catch (Exception)
        {
            // An error handler that fails has nowhere left to report to.
        }
    }

    /// <summary>Runs work on the provider's context, under its gate, without waiting for it.</summary>
    private void Post(Action action)
    {
        SynchronizationContext? context = _context;
        if (context is null || SynchronizationContext.Current == context)
        {
            RunGatedReportingErrors(action);
            return;
        }

        context.Post(static state =>
        {
            (BcrdtSyncProvider provider, Action work) = ((BcrdtSyncProvider, Action))state!;
            provider.RunGatedReportingErrors(work);
        }, (this, action));
    }

    /// <summary>Runs work on the provider's context, under its gate, and completes when it has run.</summary>
    private Task RunAsync(Action action, CancellationToken cancellationToken = default)
    {
        SynchronizationContext? context = _context;
        if (context is null || SynchronizationContext.Current == context)
        {
            RunGated(action);
            return Task.CompletedTask;
        }

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration registration = default;
        if (cancellationToken.CanBeCanceled)
        {
            registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        }

        context.Post(_ =>
        {
            registration.Dispose();
            if (completion.Task.IsCompleted) return;

            try
            {
                RunGated(action);
                completion.TrySetResult();
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        }, null);

        return completion.Task;
    }

    private void RunGatedReportingErrors(Action action)
    {
        try
        {
            RunGated(action);
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    private void RunGated(Action action)
    {
        try
        {
            lock (_gate) action();
        }
        finally
        {
            FlushDeferred();
        }
    }

    private void Defer(Action action)
    {
        lock (_gate) _deferred.Enqueue(action);
    }

    /// <summary>
    /// Runs deferred work in order, once no gate is held on this thread.
    /// </summary>
    /// <remarks>
    /// Only one frame drains at a time. A send can deliver synchronously into a peer whose answer
    /// re-enters this provider on the same stack; that inner frame only queues, and the outer loop -
    /// already draining - picks its work up in order.
    /// </remarks>
    private void FlushDeferred()
    {
        if (Monitor.IsEntered(_gate)) return;

        lock (_gate)
        {
            if (_flushing) return;
            _flushing = true;
        }

        while (true)
        {
            Action? next;
            lock (_gate)
            {
                if (!_deferred.TryDequeue(out next))
                {
                    _flushing = false;
                    return;
                }
            }

            try
            {
                next();
            }
            catch (Exception ex)
            {
                RaiseError(ex);
            }
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// A connected provider announces its departure to the room before it lets go of the connection,
    /// so peers do not show a ghost participant until the presence timeout. The transport itself is
    /// not disposed: whoever created it owns it.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0) return;

        try
        {
            // Bounded, because a synchronization context that has already shut down never runs it.
            using var grace = new CancellationTokenSource(DisposeGracePeriod);
            await RunAsync(() =>
            {
                _wantConnection = false;
                _stopRequested = true;
                CancelPendingConnects();
                SayGoodbye();
                _stashedPresence = null;
            }, grace.Token).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Saying goodbye is a courtesy; failing to must not stop the teardown.
        }

        _disposed = true;

        _doc.Updated -= OnDocumentUpdated;
        _transport.MessageReceived -= OnMessageReceived;
        _transport.StateChanged -= OnTransportStateChanged;
        if (_awareness is not null) _awareness.Updated -= OnAwarenessUpdated;

        await _shutdown.CancelAsync().ConfigureAwait(false);

        Task? heartbeat;
        Task? reconnect;
        lock (_gate)
        {
            heartbeat = _heartbeatLoop;
            reconnect = _reconnectLoop;
        }

        foreach (Task? loop in new[] { heartbeat, reconnect })
        {
            if (loop is null) continue;
            try
            {
                await loop.ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Expected during shutdown; failures were already reported through Error.
            }
        }

        _shutdown.Dispose();
        StatusChanged = null;
        Synced = null;
        Error = null;
        AuthorizationDenied = null;
        AccessChanged = null;
    }
}
