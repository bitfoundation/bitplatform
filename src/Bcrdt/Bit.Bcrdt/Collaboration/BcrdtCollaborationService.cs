namespace Bit.Bcrdt;

/// <summary>
/// Creates a transport for a document.
/// </summary>
/// <remarks>
/// A factory rather than a single shared transport, because documents are usually separate
/// conversations: one connection, room or topic per document, so that a client subscribed to one
/// document does not receive another's traffic.
/// </remarks>
public interface IBcrdtTransportFactory
{
    /// <summary>Creates a transport for a document.</summary>
    IBcrdtTransport Create(string documentId);
}

/// <summary>Application-wide collaboration settings.</summary>
public sealed class BcrdtCollaborationOptions
{
    /// <summary>Where client session identifiers come from.</summary>
    public IBcrdtClientIdProvider ClientIdProvider { get; set; } = BcrdtRandomClientIdProvider.Instance;

    /// <summary>Whether documents reclaim deleted content. Turn off only if snapshots are needed.</summary>
    public bool GarbageCollect { get; set; } = true;

    /// <summary>How long a participant's presence survives without a refresh.</summary>
    public TimeSpan PresenceTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Settings for the synchronization providers this service creates.</summary>
    public BcrdtSyncProviderOptions Sync { get; set; } = new();

    /// <summary>Settings for the persistence bindings this service creates.</summary>
    public BcrdtDocumentPersistenceOptions Persistence { get; set; } = new();

    /// <summary>Whether to connect a document as soon as it is opened.</summary>
    public bool ConnectOnOpen { get; set; } = true;

    /// <summary>
    /// Whether each document gets a default undo stack (<see cref="BcrdtCollaborativeDocument.Undo"/>).
    /// </summary>
    public bool CreateUndoManager { get; set; } = true;
}

/// <summary>Per-document overrides applied when opening.</summary>
public sealed class BcrdtOpenDocumentOptions
{
    /// <summary>Whether to connect immediately. Falls back to the service-wide setting.</summary>
    public bool? Connect { get; init; }

    /// <summary>Whether to load from and write to the store. Falls back to the service-wide setting.</summary>
    public bool? Persist { get; init; }

    /// <summary>Who the local participant is, published as soon as the document opens.</summary>
    public BcrdtCollaboratorInfo? User { get; init; }

    /// <summary>An explicit client identifier, for tests that need reproducibility.</summary>
    public ulong? ClientId { get; init; }
}

/// <summary>Opens and tracks collaborative documents.</summary>
public interface IBcrdtCollaborationService : IAsyncDisposable
{
    /// <summary>Opens a document, or returns the already-open instance.</summary>
    /// <remarks>
    /// Every successful call takes a reference to the document; pair it with one call to
    /// <see cref="CloseAsync"/>. The document stays open while any reference is held. A call that
    /// fails or is cancelled holds no reference. Options apply only to the call that actually opens
    /// the document.
    /// </remarks>
    ValueTask<BcrdtCollaborativeDocument> OpenAsync(
        string documentId,
        BcrdtOpenDocumentOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a reference taken by <see cref="OpenAsync"/>. The document is closed, and everything
    /// it owns released, when the last reference goes.
    /// </summary>
    ValueTask CloseAsync(string documentId);

    /// <summary>The documents currently open.</summary>
    IReadOnlyCollection<BcrdtCollaborativeDocument> OpenDocuments { get; }
}

/// <summary>
/// The default collaboration service: composes engine, presence, storage and transport per document.
/// </summary>
/// <remarks>
/// <para>
/// Opening a document is deliberately a single call that does everything in the right order - load
/// from storage first so the document is usable offline and immediately, then connect and let the
/// handshake reconcile whatever changed while it was away. Doing it in the other order would show
/// the user an empty document until the network answered.
/// </para>
/// <para>
/// The same document identifier opened twice returns the same instance, so two components editing
/// the same document share one replica rather than creating two that then have to sync with each
/// other through the server. The instance is reference counted: one component closing it does not
/// close it under another that is still using it.
/// </para>
/// <para>
/// A document is built on the synchronization context of the call that opens it, and its sync
/// provider is created there, so incoming changes are applied on that context - in Blazor, the
/// renderer's. A document is not thread-safe, and this is what keeps network traffic from touching
/// it at the same time as the UI. A failed open is not remembered: the next call tries again.
/// </para>
/// </remarks>
public sealed class BcrdtCollaborationService : IBcrdtCollaborationService
{
    private readonly Dictionary<string, DocumentSlot> _slots = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    private readonly BcrdtCollaborationOptions _options;
    private readonly IBcrdtTransportFactory? _transports;
    private readonly IBcrdtDocumentStore? _store;
    private bool _disposed;

    /// <summary>Creates a service.</summary>
    /// <param name="options">Application-wide settings.</param>
    /// <param name="transports">
    /// Where connections come from. Omit for a purely local, offline-first application - everything
    /// still works, it simply has no peer.
    /// </param>
    /// <param name="store">
    /// Where documents are kept. Omit for documents that live only as long as the session.
    /// </param>
    public BcrdtCollaborationService(
        BcrdtCollaborationOptions? options = null,
        IBcrdtTransportFactory? transports = null,
        IBcrdtDocumentStore? store = null)
    {
        _options = options ?? new BcrdtCollaborationOptions();
        _transports = transports;
        _store = store;
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<BcrdtCollaborativeDocument> OpenDocuments
    {
        get
        {
            lock (_gate)
            {
                return [.. _slots.Values
                    .Where(slot => slot.Creation.IsValueCreated && slot.Creation.Value.IsCompletedSuccessfully)
                    .Select(slot => slot.Creation.Value.Result)];
            }
        }
    }

    /// <inheritdoc/>
    public ValueTask<BcrdtCollaborativeDocument> OpenAsync(
        string documentId,
        BcrdtOpenDocumentOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentId);

        DocumentSlot slot;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_slots.TryGetValue(documentId, out DocumentSlot? existing))
            {
                // Lazy rather than a bare task, so that two concurrent opens of the same document
                // build one replica instead of racing to build two and discarding one mid-flight.
                existing = new DocumentSlot();
                DocumentSlot created = existing;
                BcrdtOpenDocumentOptions effective = options ?? new BcrdtOpenDocumentOptions();
                existing.Creation = new Lazy<Task<BcrdtCollaborativeDocument>>(
                    () => CreateOrEvictAsync(documentId, effective, created),
                    LazyThreadSafetyMode.ExecutionAndPublication);
                _slots[documentId] = existing;
            }

            slot = existing;
            slot.References++;
        }

        return AwaitOpenAsync(documentId, slot, cancellationToken);
    }

    private async ValueTask<BcrdtCollaborativeDocument> AwaitOpenAsync(
        string documentId,
        DocumentSlot slot,
        CancellationToken cancellationToken)
    {
        try
        {
            // Reading Value starts creation synchronously, on the caller's context - see CreateAsync.
            // Each caller waits with its own token: one caller giving up must not fail the others.
            Task<BcrdtCollaborativeDocument> creation = slot.Creation.Value;
            return await creation.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Do not make a caller that gave up wait for the creation it abandoned: if this was the
            // last reference, the creation is cancelled and its result disposed in the background.
            await ReleaseAsync(documentId, slot, awaitDisposal: false).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<BcrdtCollaborativeDocument> CreateOrEvictAsync(
        string documentId,
        BcrdtOpenDocumentOptions options,
        DocumentSlot slot)
    {
        try
        {
            return await CreateAsync(documentId, options, slot.Abandoned);
        }
        catch
        {
            // Forget the failure, so the next open tries again rather than rethrowing this forever.
            lock (_gate)
            {
                if (_slots.TryGetValue(documentId, out DocumentSlot? current) && ReferenceEquals(current, slot))
                {
                    _slots.Remove(documentId);
                }
            }
            throw;
        }
    }

    /// <remarks>
    /// The awaits here deliberately keep the caller's synchronization context: the sync provider is
    /// constructed after them and captures the current context, which is where it will apply every
    /// incoming change. Resuming on a thread-pool thread would make it apply them there instead.
    /// </remarks>
    private async Task<BcrdtCollaborativeDocument> CreateAsync(
        string documentId,
        BcrdtOpenDocumentOptions options,
        CancellationToken abandoned)
    {
        var crdt = new BcrdtDocument(new BcrdtDocumentOptions
        {
            Id = documentId,
            ClientId = options.ClientId,
            ClientIdProvider = _options.ClientIdProvider,
            GarbageCollect = _options.GarbageCollect,
        });

        var awareness = new BcrdtAwarenessStore(crdt.ClientId, _options.PresenceTimeout);
        var document = new BcrdtCollaborativeDocument(documentId, crdt, awareness);

        try
        {
            // Storage first: the document should be readable and editable before the network is
            // consulted, which is what makes an offline start indistinguishable from an online one.
            bool persist = options.Persist ?? _store is not null;
            if (persist && _store is not null)
            {
                document.Persistence = await BcrdtDocumentPersistence
                    .AttachAsync(crdt, _store, documentId, _options.Persistence, abandoned);
            }

            if (_options.CreateUndoManager)
            {
                document.Undo = new BcrdtUndoManager(crdt, [crdt.GetMap("")], new BcrdtUndoManagerOptions());
            }

            if (options.User is not null) document.Presence.SetUser(options.User);

            if (_transports is not null)
            {
                IBcrdtTransport transport = _transports.Create(documentId);
                document.Own(transport);
                document.Sync = new BcrdtSyncProvider(crdt, transport, awareness, _options.Sync);

                if (options.Connect ?? _options.ConnectOnOpen)
                {
                    await document.Sync.ConnectAsync(abandoned);
                }
            }

            // Everyone who asked for the document gave up while it was being built.
            abandoned.ThrowIfCancellationRequested();
            return document;
        }
        catch
        {
            // Release what was built - a persistence writer, a transport - rather than leaking it.
            try
            {
                await document.DisposeAsync();
            }
            catch (Exception)
            {
                // The original failure is the one worth reporting.
            }
            throw;
        }
    }

    /// <inheritdoc/>
    public ValueTask CloseAsync(string documentId)
    {
        ArgumentNullException.ThrowIfNull(documentId);

        DocumentSlot? slot;
        lock (_gate)
        {
            if (!_slots.TryGetValue(documentId, out slot)) return ValueTask.CompletedTask;
        }

        return ReleaseAsync(documentId, slot, awaitDisposal: true);
    }

    private async ValueTask ReleaseAsync(string documentId, DocumentSlot slot, bool awaitDisposal)
    {
        lock (_gate)
        {
            if (slot.References > 0) slot.References--;
            if (slot.References > 0) return;

            if (_slots.TryGetValue(documentId, out DocumentSlot? current) && ReferenceEquals(current, slot))
            {
                _slots.Remove(documentId);
            }
        }

        // Nobody wants the document any more: stop building it if it is still being built - loading,
        // connecting - instead of finishing only to throw the result away.
        slot.Abandon();

        if (awaitDisposal)
        {
            await DisposeSlotAsync(slot).ConfigureAwait(false);
        }
        else
        {
            _ = DisposeInBackgroundAsync(slot);
        }
    }

    private static async Task DisposeInBackgroundAsync(DocumentSlot slot)
    {
        try
        {
            await DisposeSlotAsync(slot).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Nobody is left to report to: the only caller gave up on the document.
        }
    }

    private static async ValueTask DisposeSlotAsync(DocumentSlot slot)
    {
        if (!slot.Creation.IsValueCreated) return;

        BcrdtCollaborativeDocument document;
        try
        {
            document = await slot.Creation.Value.ConfigureAwait(false);
        }
        catch (Exception)
        {
            // The open failed or was abandoned, and CreateAsync already released what it had built.
            return;
        }

        await document.DisposeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        DocumentSlot[] slots;
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;

            slots = [.. _slots.Values];
            _slots.Clear();
        }

        foreach (DocumentSlot slot in slots) slot.Abandon();

        List<Exception>? failures = null;
        foreach (DocumentSlot slot in slots)
        {
            try
            {
                await DisposeSlotAsync(slot).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                (failures ??= []).Add(ex);
            }
        }

        if (failures is not null)
        {
            throw new AggregateException("One or more documents failed to close cleanly.", failures);
        }
    }

    private sealed class DocumentSlot
    {
        private readonly CancellationTokenSource _abandoned = new();

        public Lazy<Task<BcrdtCollaborativeDocument>> Creation { get; set; } = null!;

        public int References { get; set; }

        /// <summary>Cancelled when the last reference is released, so an unfinished creation stops.</summary>
        public CancellationToken Abandoned => _abandoned.Token;

        public void Abandon()
        {
            try
            {
                _abandoned.Cancel();
            }
            catch (AggregateException)
            {
                // A cancellation callback threw; the creation it was attached to still fails.
            }
        }
    }
}
