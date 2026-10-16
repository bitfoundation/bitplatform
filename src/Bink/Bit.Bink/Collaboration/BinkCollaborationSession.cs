using System.Collections.Concurrent;

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
    private DateTimeOffset _lastExpiryCheck = DateTimeOffset.MinValue;
    private long _sequence;
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

    /// <summary>The other participants currently present.</summary>
    public IReadOnlyCollection<BinkPresenceState> Participants
    {
        get
        {
            ExpireParticipants();
            return _participants.Values.ToArray();
        }
    }

    /// <summary>Number of operations waiting to be sent because the transport is disconnected.</summary>
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
    /// ignored, so edits made after it are not overwritten by a slower, older copy.
    /// </summary>
    public Task RequestSnapshotAsync(CancellationToken cancellationToken = default)
    {
        // Set before sending: over an in-process transport the answer can arrive before the send returns.
        _awaitingSnapshot = true;
        return SendAsync(new BinkCollaborationMessage
        {
            Kind = BinkCollaborationMessageKind.SnapshotRequest,
            SenderId = ParticipantId,
            DocumentId = _document.Id,
            Sequence = Interlocked.Increment(ref _sequence),
        }, cancellationToken);
    }

    /// <summary>
    /// Sends the current document to every other participant, each of whom replaces their own copy with it. Use it
    /// to reset everyone to one version, for example after restoring a saved document.
    /// </summary>
    public Task SendSnapshotAsync(CancellationToken cancellationToken = default) => SendSnapshotAsync(null, cancellationToken);

    private Task SendSnapshotAsync(string? recipientId, CancellationToken cancellationToken)
        => SendAsync(new BinkCollaborationMessage
        {
            Kind = BinkCollaborationMessageKind.Snapshot,
            SenderId = ParticipantId,
            RecipientId = recipientId,
            DocumentId = _document.Id,
            Sequence = Interlocked.Increment(ref _sequence),
            Snapshot = BinkSerializer.Serialize(_document, _options.SerializerOptions),
        }, cancellationToken);

    /// <summary>
    /// Publishes this participant's presence. Calls are throttled to <see cref="BinkCollaborationOptions.PresenceInterval"/>;
    /// the most recent state is always sent eventually.
    /// </summary>
    public async Task UpdatePresenceAsync(BinkPresenceState presence, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(presence);
        _pendingPresence = presence with { ParticipantId = ParticipantId, Timestamp = DateTimeOffset.UtcNow };

        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (now - _lastPresenceSent < _options.PresenceInterval) return;
        _lastPresenceSent = now;

        BinkPresenceState? state = _pendingPresence;
        _pendingPresence = null;
        if (state is null) return;

        await SendAsync(new BinkCollaborationMessage
        {
            Kind = BinkCollaborationMessageKind.Presence,
            SenderId = ParticipantId,
            DocumentId = _document.Id,
            Sequence = Interlocked.Increment(ref _sequence),
            Presence = state,
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends any presence update the throttle held back. Call this on a timer or animation frame.</summary>
    public async Task FlushPresenceAsync(CancellationToken cancellationToken = default)
    {
        if (_pendingPresence is null) return;
        if (DateTimeOffset.UtcNow - _lastPresenceSent < _options.PresenceInterval) return;
        BinkPresenceState state = _pendingPresence;
        _pendingPresence = null;
        _lastPresenceSent = DateTimeOffset.UtcNow;
        await SendAsync(new BinkCollaborationMessage
        {
            Kind = BinkCollaborationMessageKind.Presence,
            SenderId = ParticipantId,
            DocumentId = _document.Id,
            Sequence = Interlocked.Increment(ref _sequence),
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
            _flushGate.Release();
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
                if (_pending.Count > 0) _pending.Dequeue();
            }
        }
    }

    private void OnDocumentChanged(object? sender, BinkDocumentChangedEventArgs e)
    {
        // Remote changes are not echoed back, but local undo and redo are: to everyone else they are ordinary edits.
        if (_applyingRemote || _disposed) return;
        if (e.Origin is BinkChangeOrigin.Remote or BinkChangeOrigin.Load) return;
        RecordWriter(e.Operation, ParticipantId);

        string payload;
        try
        {
            payload = BinkOperationCodec.Serialize(e.Operation, _options.SerializerOptions);
        }
        catch (InvalidOperationException ex)
        {
            RemoteOperationRejected?.Invoke("A local change could not be shared: " + ex.Message);
            return;
        }

        _ = SendAsync(new BinkCollaborationMessage
        {
            Kind = BinkCollaborationMessageKind.Operation,
            SenderId = ParticipantId,
            DocumentId = _document.Id,
            Sequence = Interlocked.Increment(ref _sequence),
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
        if (message.Kind == BinkCollaborationMessageKind.Operation)
        {
            lock (_queueGate)
            {
                if (!_transport.IsConnected || _pending.Count > 0)
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

        // A host that renders presence from the event rather than polling Participants would otherwise never
        // expire anyone, so expiry is also driven by traffic. The check is throttled to stay off the hot path.
        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (now - _lastExpiryCheck >= _options.PresenceInterval)
        {
            _lastExpiryCheck = now;
            ExpireParticipants();
        }

        // Duplicate delivery is harmless to ignore, and a lower sequence means a retransmission we already saw.
        if (message.Kind == BinkCollaborationMessageKind.Operation || message.Kind == BinkCollaborationMessageKind.Presence)
        {
            long previous = _lastSequence.GetValueOrDefault(message.SenderId, -1);
            if (message.Sequence <= previous && message.Sequence != 0) return;
            _lastSequence[message.SenderId] = message.Sequence;
        }

        switch (message.Kind)
        {
            case BinkCollaborationMessageKind.Operation:
                ApplyRemoteOperation(message);
                break;
            case BinkCollaborationMessageKind.Presence:
                if (message.Presence is { } presence)
                {
                    _participants[message.SenderId] = presence;
                    PresenceChanged?.Invoke(presence);
                }
                break;
            case BinkCollaborationMessageKind.Snapshot:
                // An answer to a request is applied once, by the participant that asked: replacing the document
                // with every answer, or answers meant for someone else joining, would throw away whatever was
                // drawn since the first one. A snapshot sent to everyone is a deliberate reset and always applies.
                if (message.RecipientId is null) ApplySnapshot(message);
                else if (_awaitingSnapshot && ApplySnapshot(message)) _awaitingSnapshot = false;
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
        if (message.Operation is not { Length: > 0 } payload) return;
        BinkDrawingOperation operation;
        try
        {
            operation = BinkOperationCodec.Deserialize(payload, new BinkDeserializerOptions { Limits = _options.Limits });
        }
        catch (BinkFormatException ex)
        {
            RemoteOperationRejected?.Invoke($"A change from {message.SenderId} was rejected: {ex.Message}");
            return;
        }

        BinkDrawingOperation? resolved = ResolveConflicts(operation, message.SenderId);
        if (resolved is null) return;
        operation = resolved;
        if (!operation.CanApply(_document))
        {
            // A stale operation, for example an edit to something already deleted. Dropping it is correct:
            // the deletion is the newer intent.
            return;
        }

        _applyingRemote = true;
        try
        {
            _document.Apply(operation, BinkChangeOrigin.Remote);
            RecordWriter(operation, message.SenderId);
        }
        catch (InvalidOperationException ex)
        {
            RemoteOperationRejected?.Invoke($"A change from {message.SenderId} could not be applied: {ex.Message}");
        }
        finally
        {
            _applyingRemote = false;
        }
    }

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
            case BinkBatchOperation batch:
            {
                var resolved = new List<BinkDrawingOperation>(batch.Operations.Count);
                foreach (BinkDrawingOperation inner in batch.Operations)
                {
                    BinkDrawingOperation? r = ResolveConflicts(inner, senderId);
                    if (r is not null) resolved.Add(r);
                }
                return resolved.Count == 0 ? null : new BinkBatchOperation(resolved, batch.Label);
            }
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
            incoming = BinkSerializer.Deserialize(payload, new BinkDeserializerOptions { Limits = _options.Limits });
        }
        catch (BinkFormatException ex)
        {
            RemoteOperationRejected?.Invoke("The received document could not be read: " + ex.Message);
            return false;
        }

        // Build the whole change first, then apply it. Removing the local content before discovering that the
        // incoming document does not fit would destroy the user's work and leave nothing in its place.
        var operations = new List<BinkDrawingOperation>();
        foreach (BinkLayer layer in _document.Layers)
        {
            foreach (BinkElement element in layer.Elements) operations.Add(new BinkRemoveElementOperation(element.Id));
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
        DateTimeOffset cutoff = DateTimeOffset.UtcNow - _options.PresenceTimeout;
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
                    DocumentId = _document.Id,
                    Sequence = Interlocked.Increment(ref _sequence),
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
        _flushGate.Dispose();
    }
}
