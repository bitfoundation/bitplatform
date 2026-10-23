using System.Collections.Concurrent;
using System.Numerics;
using System.Text.Json;

namespace Bit.Bink;

/// <summary>Options for a <see cref="BinkCollaborationSession"/>.</summary>
public sealed record BinkCollaborationOptions
{
    /// <summary>How long a participant's presence is kept after their last update.</summary>
    public TimeSpan PresenceTimeout { get; init; } = TimeSpan.FromSeconds(20);

    /// <summary>Minimum interval between outgoing presence updates. Presence is throttled, operations are not.</summary>
    public TimeSpan PresenceInterval { get; init; } = TimeSpan.FromMilliseconds(50);

    /// <summary>Maximum number of operations queued while disconnected. Older ones are dropped past this point.</summary>
    public int MaxOfflineQueue { get; init; } = 5000;

    /// <summary>Limits applied to incoming operations, which are untrusted.</summary>
    public BinkDocumentLimits Limits { get; init; } = BinkDocumentLimits.Default;

    /// <summary>Serialization options for outgoing operations.</summary>
    public BinkSerializerOptions SerializerOptions { get; init; } = BinkSerializerOptions.Default;

    /// <summary>Whether the session sends a snapshot when another participant asks for one.</summary>
    public bool ServeSnapshots { get; init; } = true;

    /// <summary>
    /// Whether messages that name another document are ignored. Every message carries the id of the document its
    /// sender edits, and a transport shared by several documents (one hub with no per-document group) delivers all
    /// of them to every session; without this, another document's strokes would be applied to this one. Off by
    /// default because a participant may join with a fresh document of its own id and take the content from a
    /// snapshot. Turn it on when every participant constructs its document with the shared id before joining.
    /// </summary>
    public bool IgnoreOtherDocuments { get; init; }
}

/// <summary>
/// Synchronizes an <see cref="BinkDocument"/> with other participants over an <see cref="IBinkCollaborationTransport"/>.
/// </summary>
/// <remarks>
/// <para>
/// The design deliberately stops short of a CRDT. Ink is append-mostly and elements are identified by id, so the
/// operations that matter commute naturally: two people drawing at once produce two additions that can be applied
/// in any order. The only genuine conflict is two people editing the same element, which is resolved
/// last-writer-wins on the element's version, with the participant id breaking ties so every replica picks the
/// same winner.
/// </para>
/// <para>
/// Applications that need stronger guarantees can layer a CRDT on top of these operations; nobody pays for that
/// complexity by default.
/// </para>
/// <para>
/// While drawing, the in-progress stroke travels as presence, so other participants see ink appear live. The
/// finished stroke is committed once as a single operation.
/// </para>
/// </remarks>
public sealed class BinkCollaborationSession : IAsyncDisposable
{
    /// <summary>How many removed element and layer ids are remembered to tell a stale operation from an early one.</summary>
    private const int MaxRemovedIds = 10_000;

    /// <summary>How many remote operations may wait for an element or layer that has not arrived yet.</summary>
    private const int MaxDeferredOperations = 1_000;

    /// <summary>How long a remote operation waits for what it depends on before it is dropped as stale after all.</summary>
    private static readonly TimeSpan DeferredLifetime = TimeSpan.FromSeconds(30);

    /// <summary>How many operations are kept, while a requested snapshot is awaited, to replay on top of it.</summary>
    private const int MaxReplayedOperations = 5_000;

    /// <summary>The most points of another participant's in-progress stroke that are kept.</summary>
    private const int MaxLiveStrokePoints = 10_000;

    /// <summary>
    /// What this session understands, sent with every message: 1, images referred to by content id. See
    /// <see cref="BinkCollaborationMessage.Protocol"/>.
    /// </summary>
    private const int CurrentProtocol = 1;

    /// <summary>How many image ids are remembered as already shared before the memory starts over.</summary>
    private const int MaxSharedImageIds = 10_000;

    /// <summary>
    /// How much image data, in characters of data URI, is kept for references to images no longer in the document,
    /// such as one deleted and then brought back by undo.
    /// </summary>
    private const long MaxCachedImageChars = 32L * 1024 * 1024;

    /// <summary>How many changes may wait for an image that is being fetched with a snapshot.</summary>
    private const int MaxHeldForImages = 100;

    /// <summary>
    /// The least time between snapshots asked for to fetch a missing image. Every participant that serves snapshots
    /// answers with the whole document, so a peer sending references to pictures that do not exist must not be able
    /// to make this replica ask, and everyone answer, over and over.
    /// </summary>
    private static readonly TimeSpan ImageFetchInterval = TimeSpan.FromSeconds(10);

    private readonly BinkDocument _document;
    private readonly IBinkCollaborationTransport _transport;
    private readonly BinkCollaborationOptions _options;
    private readonly ConcurrentDictionary<string, BinkPresenceState> _participants = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, long> _lastSequence = new(StringComparer.Ordinal);
    private readonly Queue<BinkCollaborationMessage> _pending = new();
    private readonly object _queueGate = new();
    private readonly SemaphoreSlim _flushGate = new(1, 1);
    // Who wrote the version of each element this replica holds, which is what a version tie is settled on.
    private readonly Dictionary<Guid, string> _writers = new();
    // Elements and layers this replica has removed, oldest first. An operation on something missing is stale when
    // the id is here (the removal is the newer intent) and early when it is not: it depends on an addition another
    // participant made that has not reached this replica yet.
    private readonly HashSet<Guid> _removed = new();
    private readonly Queue<Guid> _removedOrder = new();
    // Early remote operations, in the order they arrived, retried whenever a remote operation lands.
    private readonly List<PendingOperation> _deferred = new();
    // While a requested snapshot is awaited: every operation sent and received, replayed on top of the answer.
    private readonly Queue<PendingOperation> _replay = new();
    // What each participant heard from understands (BinkCollaborationMessage.Protocol).
    private readonly ConcurrentDictionary<string, int> _protocols = new(StringComparer.Ordinal);
    // Content ids of images whose data has crossed the wire in an operation, sent or received. Operations reach every
    // participant, so each of them holds these, and a later change can refer to them instead of sending them again.
    private readonly HashSet<string> _sharedImages = new(StringComparer.Ordinal);
    // Images seen in operations, by content id, oldest first, for references to one no longer in the document.
    private readonly Dictionary<string, BinkImageSource> _imageCache = new(StringComparer.Ordinal);
    private readonly Queue<string> _imageCacheOrder = new();
    private long _imageCacheChars;
    // Changes that referred to an image this replica does not hold, waiting for the snapshot that brings it.
    private readonly List<(BinkCollaborationMessage Message, DateTimeOffset Received)> _heldForImages = new();
    private DateTimeOffset _lastImageFetch = DateTimeOffset.MinValue;
    // Message numbers, seeded from the clock rather than starting at zero. A participant that reloads the page or
    // crashes rejoins under the same id without having sent Leave, and the others still hold its old high-water
    // mark, so a counter that started over had everything the new session sent discarded as retransmissions. The
    // seed is in microseconds since 1970, which stays below 2^53 so a JSON or JavaScript transport carries it
    // exactly, and leaves room for a thousand messages a millisecond before a later session could start below an
    // earlier one. The counter is shared by every session in the process, so a session that replaces another
    // there, as a Blazor Server circuit that reconnects does, always continues above it whatever the clock says.
    private static long s_sequence = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
    private DateTimeOffset _lastExpiryCheck = DateTimeOffset.MinValue;
    private DateTimeOffset _lastPresenceSent = DateTimeOffset.MinValue;
    private BinkPresenceState? _pendingPresence;
    private bool _applyingRemote;
    private bool _awaitingSnapshot;
    private bool _disposed;

    /// <summary>Creates a session for a document.</summary>
    /// <param name="document">The document to synchronize.</param>
    /// <param name="transport">How messages reach other participants.</param>
    /// <param name="participantId">This participant's stable identifier.</param>
    /// <param name="options">Session options.</param>
    public BinkCollaborationSession(BinkDocument document, IBinkCollaborationTransport transport, string participantId, BinkCollaborationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentException.ThrowIfNullOrWhiteSpace(participantId);

        _document = document;
        _transport = transport;
        ParticipantId = participantId;
        _options = options ?? new BinkCollaborationOptions();

        _document.Changed += OnDocumentChanged;
        _transport.MessageReceived += OnMessageReceivedAsync;
        _transport.ConnectionChanged += OnConnectionChanged;
    }

    /// <summary>This participant's identifier.</summary>
    public string ParticipantId { get; }

    /// <summary>
    /// How incoming operations and snapshots are read: the session's limits, and the same brush and element registries
    /// that write outgoing ones, so a custom element or brush that can be sent can also be received.
    /// </summary>
    private BinkDeserializerOptions ReadOptions => new()
    {
        Limits = _options.Limits,
        BrushRegistry = _options.SerializerOptions.BrushRegistry,
        ElementRegistry = _options.SerializerOptions.ElementRegistry,
    };

    /// <summary>The clock presence expiry and waiting operations are measured against. Replaced by tests.</summary>
    internal TimeProvider Clock { get; set; } = TimeProvider.System;

    private DateTimeOffset UtcNow => Clock.GetUtcNow();

    private static long NextSequence() => Interlocked.Increment(ref s_sequence);

    /// <summary>The other participants currently present.</summary>
    public IReadOnlyCollection<BinkPresenceState> Participants
    {
        get
        {
            ExpireParticipants();
            return _participants.Values.ToArray();
        }
    }

    /// <summary>
    /// Number of operations waiting to be sent because the transport is disconnected, together with any snapshot or
    /// snapshot request waiting behind them so it does not overtake them.
    /// </summary>
    public int PendingCount
    {
        get { lock (_queueGate) return _pending.Count; }
    }

    /// <summary>True when connected and nothing is queued.</summary>
    public bool IsSynchronized => _transport.IsConnected && PendingCount == 0;

    /// <summary>Raised when another participant's presence changes, so the canvas can redraw their cursor.</summary>
    public event Action<BinkPresenceState>? PresenceChanged;

    /// <summary>Raised when a participant is removed after their presence expired or they left.</summary>
    public event Action<string>? ParticipantLeft;

    /// <summary>Raised when a remote operation could not be applied, with the reason.</summary>
    public event Action<string>? RemoteOperationRejected;

    /// <summary>
    /// Asks the other participants for a full document, used when joining an existing session. Every participant
    /// that serves snapshots answers, addressed to this one alone; the first answer is applied and the rest are
    /// ignored, so edits made after it are not overwritten by a slower, older copy. Changes made here or received
    /// while the answer is on its way are applied again on top of it, because it may have been written before they
    /// reached whoever answered.
    /// </summary>
    public Task RequestSnapshotAsync(CancellationToken cancellationToken = default)
    {
        // Set before sending: over an in-process transport the answer can arrive before the send returns. The
        // replay log starts with the first request; a repeated one keeps what the first has collected, because the
        // answer it gets may be just as old.
        if (!_awaitingSnapshot) _replay.Clear();
        _awaitingSnapshot = true;
        return SendAsync(new BinkCollaborationMessage
        {
            Kind = BinkCollaborationMessageKind.SnapshotRequest,
            SenderId = ParticipantId,
            Protocol = CurrentProtocol,
            DocumentId = _document.Id,
            Sequence = NextSequence(),
        }, cancellationToken);
    }

    /// <summary>
    /// Sends the current document to every other participant, each of whom replaces their own copy with it. Use it
    /// to reset everyone to one version, for example after restoring a saved document. A document that cannot be
    /// serialized is reported through <see cref="RemoteOperationRejected"/> and nothing is sent.
    /// </summary>
    public Task SendSnapshotAsync(CancellationToken cancellationToken = default) => SendSnapshotAsync(null, cancellationToken);

    private Task SendSnapshotAsync(string? recipientId, CancellationToken cancellationToken)
    {
        string snapshot;
        try
        {
            // A snapshot with an asset table is format version 2, which a participant from before the table refuses;
            // it would then be unable to join at all.
            BinkSerializerOptions options = EveryoneResolvesImages() ? _options.SerializerOptions : _options.SerializerOptions with { ShareImageData = false };
            snapshot = BinkSerializer.Serialize(_document, options);
        }
        catch (Exception ex) when (IsSerializationFailure(ex))
        {
            // This runs inside the receive callback when answering a request, where an exception would take the
            // transport down with it; on Blazor Server, the circuit.
            RemoteOperationRejected?.Invoke("The document could not be shared: " + ex.Message);
            return Task.CompletedTask;
        }
        return SendAsync(new BinkCollaborationMessage
        {
            Kind = BinkCollaborationMessageKind.Snapshot,
            SenderId = ParticipantId,
            Protocol = CurrentProtocol,
            RecipientId = recipientId,
            DocumentId = _document.Id,
            Sequence = NextSequence(),
            Snapshot = snapshot,
        }, cancellationToken);
    }

    /// <summary>
    /// The exceptions writing a document or an operation reports problems with: an element type that cannot be
    /// written, or a value the format cannot carry.
    /// </summary>
    private static bool IsSerializationFailure(Exception ex)
        => ex is InvalidOperationException or ArgumentException or FormatException or OverflowException or JsonException;

    /// <summary>
    /// Publishes this participant's presence. Calls are throttled to <see cref="BinkCollaborationOptions.PresenceInterval"/>;
    /// the most recent state is always sent eventually.
    /// </summary>
    public async Task UpdatePresenceAsync(BinkPresenceState presence, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(presence);
        DateTimeOffset now = UtcNow;
        _pendingPresence = presence with { ParticipantId = ParticipantId, Timestamp = now };

        if (now - _lastPresenceSent < _options.PresenceInterval) return;
        _lastPresenceSent = now;

        BinkPresenceState? state = _pendingPresence;
        _pendingPresence = null;
        if (state is null) return;

        await SendAsync(new BinkCollaborationMessage
        {
            Kind = BinkCollaborationMessageKind.Presence,
            SenderId = ParticipantId,
            Protocol = CurrentProtocol,
            DocumentId = _document.Id,
            Sequence = NextSequence(),
            Presence = state,
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends any presence update the throttle held back. Call this on a timer or animation frame.</summary>
    public async Task FlushPresenceAsync(CancellationToken cancellationToken = default)
    {
        if (_pendingPresence is null) return;
        DateTimeOffset now = UtcNow;
        if (now - _lastPresenceSent < _options.PresenceInterval) return;
        BinkPresenceState state = _pendingPresence;
        _pendingPresence = null;
        _lastPresenceSent = now;
        await SendAsync(new BinkCollaborationMessage
        {
            Kind = BinkCollaborationMessageKind.Presence,
            SenderId = ParticipantId,
            Protocol = CurrentProtocol,
            DocumentId = _document.Id,
            Sequence = NextSequence(),
            Presence = state,
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops everything queued while disconnected. Use it when the local changes have been superseded, for
    /// example after accepting a snapshot, so stale operations are not replayed on top of it.
    /// </summary>
    public void ClearPending()
    {
        lock (_queueGate) _pending.Clear();
    }

    /// <summary>Sends everything queued while the transport was disconnected.</summary>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        // Two overlapping flushes would peek the same message, send it twice and then dequeue twice, silently
        // dropping the message behind it. Only one flush runs at a time.
        try
        {
            await _flushGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (ObjectDisposedException)
        {
            return;
        }
        try
        {
            await FlushCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // A reconnect starts a flush nobody awaits, and the session can be disposed while that flush is still
            // waiting on the transport. Releasing the gate after it was disposed would throw from that task.
            try
            {
                _flushGate.Release();
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    private async Task FlushCoreAsync(CancellationToken cancellationToken)
    {
        while (_transport.IsConnected && !_disposed)
        {
            BinkCollaborationMessage? message;
            lock (_queueGate)
            {
                if (_pending.Count == 0) return;
                message = _pending.Peek();
            }
            try
            {
                await _transport.SendAsync(message, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
#pragma warning disable CA1031 // A transport failure must not lose the queue; it stays for the next flush.
            catch
            {
                return;
            }
#pragma warning restore CA1031
            lock (_queueGate)
            {
                // Only the message that was sent leaves the queue. While the send was awaited, a change made past
                // MaxOfflineQueue could have trimmed it from the front, or ClearPending emptied the queue and new
                // messages arrived; dequeuing the head regardless dropped a message that was never sent.
                if (_pending.Count > 0 && ReferenceEquals(_pending.Peek(), message)) _pending.Dequeue();
            }
        }
    }

    private void OnDocumentChanged(object? sender, BinkDocumentChangedEventArgs e)
    {
        if (_disposed) return;
        // Whatever caused it, a removal is remembered, so a late edit of what it removed is recognised as stale.
        RecordRemovals(e.Inverse);

        // Remote changes are not echoed back, but local undo and redo are: to everyone else they are ordinary edits.
        if (_applyingRemote) return;
        if (e.Origin is BinkChangeOrigin.Remote or BinkChangeOrigin.Load) return;
        RecordWriter(e.Operation, ParticipantId);

        string payload;
        // An image goes in full the first time; after that, and again within this one change (pasting three copies of
        // a photo), it is referred to by its content id, once everyone can resolve one. Dragging an embedded photo
        // otherwise sent the whole picture again with every frame of the drag.
        var written = new Dictionary<string, BinkImageSource>(StringComparer.Ordinal);
        bool refer = EveryoneResolvesImages();
        var assets = new BinkImageAssets
        {
            CanRefer = refer ? id => written.ContainsKey(id) || _sharedImages.Contains(id) : null,
            Written = (id, image) => written[id] = image,
        };
        try
        {
            payload = BinkOperationCodec.Serialize(e.Operation, _options.SerializerOptions, assets);
        }
        catch (Exception ex) when (IsSerializationFailure(ex))
        {
            // This runs inside the document's Changed event, so anything thrown here would surface in whatever
            // tool or API call made the change, after the change was already applied.
            RemoteOperationRejected?.Invoke("A local change could not be shared: " + ex.Message);
            return;
        }
        foreach ((string id, BinkImageSource image) in written) RememberImage(id, image);

        // The answer to a pending snapshot request may have been serialized before this change reached whoever
        // answered; it is applied again on top of the answer rather than wiped by it.
        if (_awaitingSnapshot) Log(e.Operation, ParticipantId);

        _ = SendAsync(new BinkCollaborationMessage
        {
            Kind = BinkCollaborationMessageKind.Operation,
            SenderId = ParticipantId,
            Protocol = CurrentProtocol,
            DocumentId = _document.Id,
            Sequence = NextSequence(),
            Operation = payload,
        }, CancellationToken.None);
    }

    private async Task SendAsync(BinkCollaborationMessage message, CancellationToken cancellationToken)
    {
        if (_disposed) return;

        // A queue that is not yet drained must not be overtaken. The transport contract is that one sender's
        // messages arrive in order, and a receiver discards anything that arrives behind a later sequence.
        bool queued = false;
        bool heldBack = false;
        if (message.Kind is BinkCollaborationMessageKind.Operation or BinkCollaborationMessageKind.Snapshot or BinkCollaborationMessageKind.SnapshotRequest)
        {
            // Operations are queued while offline. Snapshots and requests are not, because a stale answer is worse
            // than none, but every kind shares the sequence and the receiver discards whatever arrives behind a
            // later one, so while the queue drains they wait behind it too: a request sent past queued operations,
            // which is what reconnecting and then asking for a snapshot does, would make everyone drop them.
            lock (_queueGate)
            {
                bool connected = _transport.IsConnected;
                bool queue = message.Kind == BinkCollaborationMessageKind.Operation
                    ? !connected || _pending.Count > 0
                    : connected && _pending.Count > 0;
                if (queue)
                {
                    _pending.Enqueue(message);
                    while (_pending.Count > _options.MaxOfflineQueue) _pending.Dequeue();
                    queued = true;
                }
            }
        }
        else if (message.Kind == BinkCollaborationMessageKind.Presence)
        {
            // Presence shares the sequence counter with operations, so sending it past queued operations would
            // make every receiver discard those operations as late arrivals once the queue drains. It waits for
            // the next FlushPresenceAsync instead, unless a newer state is already waiting.
            lock (_queueGate) heldBack = _pending.Count > 0;
        }
        if (heldBack)
        {
            _pendingPresence ??= message.Presence;
            return;
        }
        if (queued)
        {
            if (_transport.IsConnected) await FlushAsync(cancellationToken).ConfigureAwait(false);
            return;
        }
        // Presence is worthless once stale, so it is dropped rather than queued.
        if (!_transport.IsConnected) return;

        try
        {
            await _transport.SendAsync(message, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
#pragma warning disable CA1031 // A send failure must not tear down the editor; the change is queued for retry.
        catch
        {
            if (message.Kind == BinkCollaborationMessageKind.Operation)
            {
                lock (_queueGate)
                {
                    _pending.Enqueue(message);
                    while (_pending.Count > _options.MaxOfflineQueue) _pending.Dequeue();
                }
            }
        }
#pragma warning restore CA1031
    }

    private async Task OnMessageReceivedAsync(BinkCollaborationMessage message)
    {
        if (_disposed || message is null) return;
        if (string.Equals(message.SenderId, ParticipantId, StringComparison.Ordinal)) return;
        // Addressed to someone else: a transport that broadcasts everything delivers it here too.
        if (message.RecipientId is { } recipient && !string.Equals(recipient, ParticipantId, StringComparison.Ordinal)) return;
        // Traffic for another document, from a transport shared by several of them.
        if (_options.IgnoreOtherDocuments && message.DocumentId != _document.Id) return;

        // A host that renders presence from the event rather than polling Participants would otherwise never
        // expire anyone, so expiry is also driven by traffic. The check is throttled to stay off the hot path.
        DateTimeOffset now = UtcNow;
        if (now - _lastExpiryCheck >= _options.PresenceInterval)
        {
            _lastExpiryCheck = now;
            ExpireParticipants();
            ExpireDeferred();
            ExpireHeldForImages();
        }

        // Duplicate delivery is harmless to ignore, and a lower sequence means a retransmission we already saw.
        // Every kind is checked, because they all share the sender's counter: a snapshot that skipped the check
        // and arrived twice, the second time after a later edit, replaced the document again and lost that edit.
        long previous = _lastSequence.GetValueOrDefault(message.SenderId, -1);
        if (message.Sequence <= previous && message.Sequence != 0)
        {
            // A snapshot request is the first thing a participant sends when it joins. One that is not above the
            // cursor comes from a new session under the same id whose clock is behind the old one's, for example
            // the same user on another device. Discarding it, and everything sent after it, would freeze them.
            if (message.Kind != BinkCollaborationMessageKind.SnapshotRequest) return;
        }
        _lastSequence[message.SenderId] = message.Sequence;
        _protocols[message.SenderId] = message.Protocol;

        switch (message.Kind)
        {
            case BinkCollaborationMessageKind.Operation:
                ApplyRemoteOperation(message);
                break;
            case BinkCollaborationMessageKind.Presence:
                if (message.Presence is { } presence)
                {
                    BinkPresenceState state = Sanitize(presence, message.SenderId, now);
                    _participants[message.SenderId] = state;
                    PresenceChanged?.Invoke(state);
                }
                break;
            case BinkCollaborationMessageKind.Snapshot:
                // An answer to a request is applied once, by the participant that asked: replacing the document
                // with every answer, or answers meant for someone else joining, would throw away whatever was
                // drawn since the first one. A snapshot sent to everyone is a deliberate reset and always applies.
                if (message.RecipientId is null)
                {
                    if (ApplySnapshot(message)) RetryHeldForImages();
                }
                else if (_awaitingSnapshot && ApplySnapshot(message))
                {
                    _awaitingSnapshot = false;
                    ReplayOnSnapshot();
                    RetryHeldForImages();
                }
                break;
            case BinkCollaborationMessageKind.SnapshotRequest:
                if (_options.ServeSnapshots) await SendSnapshotAsync(message.SenderId, CancellationToken.None).ConfigureAwait(false);
                break;
            case BinkCollaborationMessageKind.Leave:
                Forget(message.SenderId);
                break;
        }
    }

    private void ApplyRemoteOperation(BinkCollaborationMessage message)
    {
        if (message.Operation is not { Length: > 0 }) return;
        BinkDrawingOperation operation;
        try
        {
            operation = ReadOperation(message, fetchMissing: true);
        }
        catch (BinkMissingAssetException)
        {
            // Not malformed: the picture crossed the wire before this replica could keep it. A snapshot brings it,
            // and the change is applied on top of that.
            HoldForImages(message);
            return;
        }
        catch (BinkFormatException ex)
        {
            RemoteOperationRejected?.Invoke($"A change from {message.SenderId} was rejected: {ex.Message}");
            return;
        }
        ApplyReceived(operation, message.SenderId);
    }

    /// <summary>
    /// Reads a remote operation. An image in it is either carried in full, and remembered for later references, or
    /// referred to by content id: one written earlier in the same operation, one this replica's document shows, or
    /// one kept from an earlier operation.
    /// </summary>
    /// <param name="message">The operation message.</param>
    /// <param name="fetchMissing">Whether an image that cannot be resolved throws <see cref="BinkMissingAssetException"/>, rather than failing the read as malformed.</param>
    private BinkDrawingOperation ReadOperation(BinkCollaborationMessage message, bool fetchMissing)
    {
        var inOperation = new Dictionary<string, BinkImageSource>(StringComparer.Ordinal);
        var assets = new BinkImageAssets
        {
            Resolve = id => inOperation.GetValueOrDefault(id) ?? ResolveImage(id),
            Read = image =>
            {
                string id = BinkImageAssets.IdOf(image.Uri);
                inOperation[id] = image;
                RememberImage(id, image);
            },
            ThrowWhenMissing = true,
        };
        try
        {
            return BinkOperationCodec.Deserialize(message.Operation!, ReadOptions, assets);
        }
        catch (BinkMissingAssetException ex) when (!fetchMissing)
        {
            throw new BinkFormatException(ex.Message, ex);
        }
    }

    private void ApplyReceived(BinkDrawingOperation operation, string senderId)
    {
        var received = new PendingOperation(operation, senderId, UtcNow);
        // The answer to a pending snapshot request may have been serialized before this change reached whoever
        // answered, so the change is applied again on top of the answer rather than wiped by it.
        if (_awaitingSnapshot) Log(received);

        _applyingRemote = true;
        try
        {
            if (ApplyRemote(received, defer: true, report: true) > 0) RetryDeferred();
        }
        finally
        {
            _applyingRemote = false;
        }
    }

    /// <summary>
    /// Applies a remote operation as far as it still applies, and returns how many operations were applied.
    /// </summary>
    /// <remarks>
    /// A batch is applied one operation at a time inside one batch scope, so it still reports a single change, and
    /// an operation in it that no longer applies is skipped rather than failing the whole batch. Applied whole, one
    /// stale operation inside a batch (an element a collaborator deleted meanwhile) made it roll back, and the
    /// replicas diverged for good: clearing the page while someone else deleted one stroke left every other stroke
    /// behind on the side that had deleted it.
    /// </remarks>
    /// <param name="received">The operation, its sender and when it arrived.</param>
    /// <param name="defer">Whether an operation that depends on something not yet here waits for it.</param>
    /// <param name="report">Whether an operation that fails to apply is reported.</param>
    private int ApplyRemote(PendingOperation received, bool defer, bool report)
    {
        if (received.Operation is not BinkBatchOperation batch) return ApplyOne(received, defer, report) ? 1 : 0;

        int applied = 0;
        using (_document.BeginBatch(batch.Label, BinkChangeOrigin.Remote))
        {
            foreach (BinkDrawingOperation inner in batch.Operations) applied += ApplyRemote(received with { Operation = inner }, defer, report);
        }
        return applied;
    }

    /// <summary>Applies one operation that is not a batch. Returns true when it was applied.</summary>
    private bool ApplyOne(PendingOperation received, bool defer, bool report)
    {
        BinkDrawingOperation? operation = ResolveConflicts(received.Operation, received.SenderId);
        if (operation is null) return false;
        if (!operation.CanApply(_document))
        {
            // Either stale, an edit of something already deleted, which is dropped because the deletion is the
            // newer intent; or early. Each sender's messages arrive in order, but not messages from different
            // senders, so over a mesh an edit of what another participant just added can arrive before the
            // addition. Dropping that one lost the edit on this replica alone, so it waits for the addition.
            if (defer && AwaitsDependency(operation)) Defer(received);
            return false;
        }

        try
        {
            _document.Apply(operation, BinkChangeOrigin.Remote);
            RecordWriter(operation, received.SenderId);
            return true;
        }
        catch (InvalidOperationException ex)
        {
            if (report) RemoteOperationRejected?.Invoke($"A change from {received.SenderId} could not be applied: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Whether an operation that cannot be applied refers to an element or layer this replica has never had, as
    /// opposed to one it has removed.
    /// </summary>
    private bool AwaitsDependency(BinkDrawingOperation operation) => operation switch
    {
        BinkAddElementOperation add => !_document.ContainsElement(add.Element.Id) && IsUnknownLayer(add.LayerId),
        BinkRemoveElementOperation remove => IsUnknownElement(remove.ElementId),
        BinkReplaceElementOperation replace => IsUnknownElement(replace.Element.Id),
        BinkReorderElementOperation reorder => _document.ContainsElement(reorder.ElementId)
            ? IsUnknownLayer(reorder.TargetLayerId)
            : IsUnknownElement(reorder.ElementId) && !_removed.Contains(reorder.TargetLayerId),
        BinkRemoveLayerOperation removeLayer => IsUnknownLayer(removeLayer.LayerId),
        BinkUpdateLayerOperation updateLayer => IsUnknownLayer(updateLayer.LayerId),
        BinkReorderLayerOperation reorderLayer => IsUnknownLayer(reorderLayer.LayerId),
        _ => false,
    };

    private bool IsUnknownElement(Guid id) => !_document.ContainsElement(id) && !_removed.Contains(id);

    private bool IsUnknownLayer(Guid id) => !_document.TryGetLayer(id, out _) && !_removed.Contains(id);

    /// <summary>Holds an early operation until what it depends on arrives. The oldest makes room when full.</summary>
    private void Defer(PendingOperation received)
    {
        ExpireDeferred();
        if (_deferred.Count >= MaxDeferredOperations) _deferred.RemoveAt(0);
        _deferred.Add(received);
    }

    /// <summary>
    /// Applies whatever early operations have become applicable, until none is left that can be. Runs after every
    /// remote operation that landed, because each one can be what another was waiting for.
    /// </summary>
    private void RetryDeferred()
    {
        bool progress = true;
        while (progress && _deferred.Count > 0)
        {
            progress = false;
            ExpireDeferred();
            for (int i = 0; i < _deferred.Count; i++)
            {
                PendingOperation entry = _deferred[i];
                if (!entry.Operation.CanApply(_document))
                {
                    // Still early, or stale by now: what it refers to arrived and was removed again.
                    if (!AwaitsDependency(entry.Operation)) _deferred.RemoveAt(i--);
                    continue;
                }
                _deferred.RemoveAt(i--);
                if (ApplyOne(entry, defer: false, report: true)) progress = true;
            }
        }
    }

    /// <summary>
    /// Drops early operations that have waited too long. What they refer to is not coming, so they were stale after
    /// all, for example an edit of an element that was removed before this participant joined; stale operations
    /// have always been dropped silently.
    /// </summary>
    private void ExpireDeferred()
    {
        if (_deferred.Count == 0) return;
        DateTimeOffset cutoff = UtcNow - DeferredLifetime;
        _deferred.RemoveAll(entry => entry.Received < cutoff);
    }

    /// <summary>
    /// Whether an image may be referred to by content id: only when everyone this session has heard from resolves
    /// such references. A participant from before them would otherwise read the change as malformed, and with nobody
    /// heard from yet there is nobody to be sure of.
    /// </summary>
    private bool EveryoneResolvesImages()
    {
        if (_protocols.IsEmpty) return false;
        foreach (int protocol in _protocols.Values)
        {
            if (protocol < 1) return false;
        }
        return true;
    }

    /// <summary>Remembers an image whose data crossed the wire, so later changes can refer to it and be resolved here.</summary>
    private void RememberImage(string id, BinkImageSource image)
    {
        // Past the limit the memory starts over, which only means a few images go in full once more.
        if (_sharedImages.Count >= MaxSharedImageIds) _sharedImages.Clear();
        _sharedImages.Add(id);

        if (_imageCache.ContainsKey(id) || image.Uri.Length > MaxCachedImageChars) return;
        _imageCache[id] = image;
        _imageCacheOrder.Enqueue(id);
        _imageCacheChars += image.Uri.Length;
        while (_imageCacheChars > MaxCachedImageChars && _imageCacheOrder.TryDequeue(out string? oldest))
        {
            if (_imageCache.Remove(oldest, out BinkImageSource? dropped)) _imageCacheChars -= dropped.Uri.Length;
        }
    }

    /// <summary>The image with a content id: one kept from an earlier operation, or one the document shows.</summary>
    private BinkImageSource? ResolveImage(string id)
    {
        // The cache first: each frame of a collaborator's drag refers to the same picture, and looking it up there
        // costs nothing, while the document is searched element by element.
        if (_imageCache.TryGetValue(id, out BinkImageSource? cached)) return cached;
        foreach (BinkElement element in _document.AllElements)
        {
            if (element is not BinkImageElement { Source: { IsEmbedded: true } source } || BinkImageAssets.IdOf(source.Uri) != id) continue;
            // Kept, so the next frame of the same drag finds it at once; a picture that came with a snapshot is not
            // in the cache until then.
            RememberImage(id, source);
            return source;
        }
        return null;
    }

    /// <summary>Holds a change that refers to an image this replica does not have, and asks for a snapshot, which brings it.</summary>
    private void HoldForImages(BinkCollaborationMessage message)
    {
        DateTimeOffset now = UtcNow;
        if (_heldForImages.Count >= MaxHeldForImages) _heldForImages.RemoveAt(0);
        _heldForImages.Add((message, now));
        // A change that arrives while one fetch is under way, or soon after, waits for that snapshot or the next.
        if (_awaitingSnapshot || now - _lastImageFetch < ImageFetchInterval) return;
        _lastImageFetch = now;
        _ = RequestSnapshotAsync();
    }

    /// <summary>
    /// Applies, on top of a snapshot that has just landed, the changes that were waiting for an image. The snapshot
    /// shows the images its sender's document did; a change whose image is still missing was about something already
    /// gone, and is reported and dropped.
    /// </summary>
    private void RetryHeldForImages()
    {
        if (_heldForImages.Count == 0) return;
        (BinkCollaborationMessage Message, DateTimeOffset Received)[] held = [.. _heldForImages];
        _heldForImages.Clear();
        foreach ((BinkCollaborationMessage message, _) in held)
        {
            BinkDrawingOperation operation;
            try
            {
                operation = ReadOperation(message, fetchMissing: false);
            }
            catch (BinkFormatException ex)
            {
                RemoteOperationRejected?.Invoke($"A change from {message.SenderId} was rejected: {ex.Message}");
                continue;
            }
            ApplyReceived(operation, message.SenderId);
        }
    }

    /// <summary>
    /// Drops changes that have waited too long for an image, reporting each: no snapshot is coming. Those still
    /// waiting get a snapshot asked for once the interval allows, if the one they arrived after was held back.
    /// </summary>
    private void ExpireHeldForImages()
    {
        if (_heldForImages.Count == 0) return;
        DateTimeOffset now = UtcNow;
        DateTimeOffset cutoff = now - DeferredLifetime;
        for (int i = 0; i < _heldForImages.Count; i++)
        {
            if (_heldForImages[i].Received >= cutoff) continue;
            RemoteOperationRejected?.Invoke($"A change from {_heldForImages[i].Message.SenderId} was dropped: the image it shows never arrived.");
            _heldForImages.RemoveAt(i--);
        }
        if (_heldForImages.Count == 0 || _awaitingSnapshot || now - _lastImageFetch < ImageFetchInterval) return;
        _lastImageFetch = now;
        _ = RequestSnapshotAsync();
    }

    /// <summary>Remembers an operation to replay on top of the snapshot that answers a pending request.</summary>
    private void Log(BinkDrawingOperation operation, string senderId) => Log(new PendingOperation(operation, senderId, UtcNow));

    private void Log(PendingOperation entry)
    {
        _replay.Enqueue(entry);
        // Bounded like the offline queue: past this a joiner has been waiting for an answer for a very long time,
        // and the oldest changes are the likeliest to be in it already.
        while (_replay.Count > MaxReplayedOperations) _replay.Dequeue();
    }

    /// <summary>
    /// Applies again, on top of a snapshot that answered this participant's request, what was sent and received
    /// while waiting for it. The answer was serialized when the request arrived, so a stroke drawn here, or one from
    /// a third participant that reached this replica first, would otherwise be on everyone's copy but this one.
    /// The replay is lenient: what the snapshot already holds, or holds a newer version of, is skipped. It is applied
    /// as a remote change, so it is neither sent again nor recorded in the undo history.
    /// </summary>
    private void ReplayOnSnapshot()
    {
        if (_replay.Count == 0) return;
        PendingOperation[] log = _replay.ToArray();
        _replay.Clear();
        _applyingRemote = true;
        try
        {
            int applied = 0;
            using (_document.BeginBatch("Synchronize", BinkChangeOrigin.Remote))
            {
                foreach (PendingOperation entry in log) applied += ApplyRemote(entry, defer: true, report: false);
            }
            if (applied > 0) RetryDeferred();
        }
        finally
        {
            _applyingRemote = false;
        }
    }

    /// <summary>
    /// Remembers what a change removed, read from the additions that would undo it, so a late operation on it is
    /// recognised as stale rather than held as early.
    /// </summary>
    private void RecordRemovals(BinkDrawingOperation inverse)
    {
        switch (inverse)
        {
            case BinkAddElementOperation add:
                Tombstone(add.Element.Id);
                break;
            case BinkAddLayerOperation addLayer:
                Tombstone(addLayer.LayerId);
                if (addLayer.Elements is { } elements)
                {
                    foreach (BinkElement element in elements) Tombstone(element.Id);
                }
                break;
            case BinkBatchOperation batch:
                foreach (BinkDrawingOperation inner in batch.Operations) RecordRemovals(inner);
                break;
        }
    }

    private void Tombstone(Guid id)
    {
        if (!_removed.Add(id)) return;
        _removedOrder.Enqueue(id);
        // Forgetting the oldest only means a very late edit of it waits, and then expires, instead of being dropped.
        if (_removedOrder.Count > MaxRemovedIds) _removed.Remove(_removedOrder.Dequeue());
    }

    /// <summary>
    /// Presence as this replica stores it. The identity is the sender's, because the id inside the state is
    /// whatever the sender wrote and could name someone else, and the time is this replica's, because expiry
    /// compared the sender's clock with this one: a peer whose clock ran behind expired at once, and one that sent
    /// a far-future time never did. The live stroke and the selection are bounded, and points that are not finite
    /// or lie outside the coordinate limit are dropped, since all of it is drawn on every frame.
    /// </summary>
    private BinkPresenceState Sanitize(BinkPresenceState presence, string senderId, DateTimeOffset now)
    {
        float max = _options.Limits.MaxCoordinate;
        bool Valid(Vector2 p) => float.IsFinite(p.X) && float.IsFinite(p.Y) && MathF.Abs(p.X) <= max && MathF.Abs(p.Y) <= max;

        IReadOnlyList<Vector2>? live = presence.LiveStroke;
        if (live is { Count: > 0 } && (live.Count > MaxLiveStrokePoints || !live.All(Valid)))
        {
            // The newest points are kept: they are where the pen is.
            int start = Math.Max(0, live.Count - MaxLiveStrokePoints);
            var kept = new List<Vector2>(live.Count - start);
            for (int i = start; i < live.Count; i++)
            {
                if (Valid(live[i])) kept.Add(live[i]);
            }
            live = kept;
        }

        IReadOnlyList<Guid>? selection = presence.Selection;
        if (selection is not null && selection.Count > _options.Limits.MaxElements) selection = selection.Take(_options.Limits.MaxElements).ToArray();

        float width = presence.LiveStrokeWidth;
        width = float.IsFinite(width) ? Math.Clamp(width, 0f, _options.Limits.MaxStrokeWidth) : 0f;

        return presence with
        {
            ParticipantId = senderId,
            Timestamp = now,
            Cursor = presence.Cursor is { } cursor && Valid(cursor) ? cursor : null,
            LiveStroke = live,
            LiveStrokeWidth = width,
            Selection = selection,
        };
    }

    /// <summary>An operation, who sent it and when it arrived.</summary>
    private readonly record struct PendingOperation(BinkDrawingOperation Operation, string SenderId, DateTimeOffset Received);

    /// <summary>
    /// Applies last-writer-wins to element edits. When the local element is newer the remote edit is dropped;
    /// when versions tie, the participant id decides so that every replica reaches the same result.
    /// </summary>
    /// <remarks>
    /// A tie is between the participant who wrote the version this replica holds and the sender, not between
    /// this replica and the sender. With three or more participants those differ: a bystander holding one
    /// editor's version would otherwise judge the other editor's version against its own id, and could keep a
    /// different winner from the one both editors settled on.
    /// </remarks>
    /// <returns>The operation to apply, or null when it lost the conflict and must be dropped.</returns>
    private BinkDrawingOperation? ResolveConflicts(BinkDrawingOperation operation, string senderId)
    {
        switch (operation)
        {
            case BinkReplaceElementOperation replace when _document.TryGetElement(replace.Element.Id, out BinkElement? current):
            {
                if (current.Version > replace.Element.Version) return null;
                if (current.Version == replace.Element.Version)
                {
                    // A version this session did not see being written (loaded, or from a snapshot) falls back to
                    // this participant's id, which is the rule two participants have always used.
                    string writer = _writers.GetValueOrDefault(replace.Element.Id, ParticipantId);
                    if (string.CompareOrdinal(writer, senderId) > 0) return null;
                }
                return replace;
            }
            // A batch is never resolved as a whole: ApplyRemote resolves each operation in it just before applying
            // it, against the state the earlier ones left.
            default:
                return operation;
        }
    }

    /// <summary>Remembers who wrote the version of each element an applied operation leaves behind.</summary>
    private void RecordWriter(BinkDrawingOperation operation, string writer)
    {
        switch (operation)
        {
            case BinkAddElementOperation add:
                _writers[add.Element.Id] = writer;
                break;
            case BinkReplaceElementOperation replace:
                _writers[replace.Element.Id] = writer;
                break;
            case BinkRemoveElementOperation remove:
                _writers.Remove(remove.ElementId);
                break;
            case BinkAddLayerOperation { Elements: { } elements }:
                foreach (BinkElement element in elements) _writers[element.Id] = writer;
                break;
            case BinkBatchOperation batch:
                foreach (BinkDrawingOperation inner in batch.Operations) RecordWriter(inner, writer);
                break;
        }
    }

    /// <summary>Replaces the document with the one in the message. Returns false when it could not be applied.</summary>
    private bool ApplySnapshot(BinkCollaborationMessage message)
    {
        if (message.Snapshot is not { Length: > 0 } payload) return false;
        BinkDocument incoming;
        try
        {
            incoming = BinkSerializer.Deserialize(payload, ReadOptions);
        }
        catch (BinkFormatException ex)
        {
            RemoteOperationRejected?.Invoke("The received document could not be read: " + ex.Message);
            return false;
        }

        // Build the whole change first, then apply it. Removing the local content before discovering that the
        // incoming document does not fit would destroy the user's work and leave nothing in its place.
        // Each layer is emptied from the top down. A removal looks its element up from the top of the layer and
        // closes the gap behind it, so removing bottom-first cost a scan and a shift of the whole layer per element:
        // quadratic, seconds for a few tens of thousands of strokes.
        var operations = new List<BinkDrawingOperation>();
        foreach (BinkLayer layer in _document.Layers)
        {
            for (int i = layer.Elements.Count - 1; i >= 0; i--) operations.Add(new BinkRemoveElementOperation(layer.Elements[i].Id));
        }

        // Add the incoming layers before removing the local ones that are gone: a document must always keep at
        // least one layer, and removing them first would trip that rule.
        foreach (BinkLayer layer in incoming.Layers)
        {
            if (_document.TryGetLayer(layer.Id, out _))
            {
                operations.Add(new BinkUpdateLayerOperation(layer.Id, layer.Properties));
                foreach (BinkElement element in layer.Elements) operations.Add(new BinkAddElementOperation(layer.Id, element));
            }
            else
            {
                operations.Add(new BinkAddLayerOperation(layer.Id, layer.Properties, null, layer.Elements.ToArray()));
            }
        }
        foreach (BinkLayer layer in _document.Layers)
        {
            if (!incoming.TryGetLayer(layer.Id, out _)) operations.Add(new BinkRemoveLayerOperation(layer.Id));
        }

        // The rest of the document comes along too. Without these a joining participant kept its own page size,
        // background and metadata, and layers it already had stayed wherever they were in its own stack, so the
        // two replicas drew the same layers in a different order.
        for (int i = 0; i < incoming.Layers.Count; i++) operations.Add(new BinkReorderLayerOperation(incoming.Layers[i].Id, i));
        if (_document.Settings != incoming.Settings) operations.Add(new BinkUpdateSettingsOperation(incoming.Settings));
        foreach (string key in _document.Metadata.Keys)
        {
            if (!incoming.Metadata.ContainsKey(key)) operations.Add(new BinkSetMetadataOperation(key, null));
        }
        foreach ((string key, string value) in incoming.Metadata)
        {
            if (!_document.Metadata.TryGetValue(key, out string? current) || current != value) operations.Add(new BinkSetMetadataOperation(key, value));
        }

        // Already identical: nothing to change, and the request has been answered.
        if (operations.Count == 0) return true;

        _applyingRemote = true;
        try
        {
            // A batch rolls back on failure, so a snapshot either lands completely or not at all.
            _document.Apply(new BinkBatchOperation(operations, "Synchronize"), BinkChangeOrigin.Remote);
            // Who wrote each incoming version is not part of a snapshot.
            _writers.Clear();
            // Operations still waiting for something to arrive are superseded: every other replica that applied
            // them has had its copy replaced as well. A joiner replays what it received while waiting instead.
            _deferred.Clear();
            return true;
        }
        catch (InvalidOperationException ex)
        {
            RemoteOperationRejected?.Invoke("The received document could not be applied: " + ex.Message);
            return false;
        }
        finally
        {
            _applyingRemote = false;
        }
    }

    private void OnConnectionChanged(bool connected)
    {
        if (connected) _ = FlushAsync();
    }

    private void ExpireParticipants()
    {
        DateTimeOffset cutoff = UtcNow - _options.PresenceTimeout;
        foreach ((string id, BinkPresenceState state) in _participants)
        {
            if (state.Timestamp < cutoff) Forget(id);
        }
    }

    /// <summary>
    /// Drops everything remembered about a participant. The de-duplication cursor has to go with the presence
    /// entry: keeping it would grow without bound in a long-lived session, and a participant that rejoins with
    /// the same id would have its first messages discarded as retransmissions.
    /// </summary>
    private void Forget(string participantId)
    {
        _lastSequence.TryRemove(participantId, out _);
        _protocols.TryRemove(participantId, out _);
        if (_participants.TryRemove(participantId, out _)) ParticipantLeft?.Invoke(participantId);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _document.Changed -= OnDocumentChanged;
        _transport.MessageReceived -= OnMessageReceivedAsync;
        _transport.ConnectionChanged -= OnConnectionChanged;

        if (_transport.IsConnected)
        {
            try
            {
                await _transport.SendAsync(new BinkCollaborationMessage
                {
                    Kind = BinkCollaborationMessageKind.Leave,
                    SenderId = ParticipantId,
                    Protocol = CurrentProtocol,
                    DocumentId = _document.Id,
                    Sequence = NextSequence(),
                }).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Leaving is best effort.
            catch
            {
            }
#pragma warning restore CA1031
        }
        _participants.Clear();
        _lastSequence.Clear();
        _protocols.Clear();
        _deferred.Clear();
        _replay.Clear();
        _heldForImages.Clear();
        _imageCache.Clear();
        _imageCacheOrder.Clear();
        _sharedImages.Clear();
        _flushGate.Dispose();
    }
}
