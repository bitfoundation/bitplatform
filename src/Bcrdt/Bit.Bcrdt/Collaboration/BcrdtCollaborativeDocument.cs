namespace Bit.Bcrdt;

/// <summary>
/// An open collaborative document: replicated state, presence, history and connection in one object.
/// </summary>
/// <remarks>
/// <para>
/// This is the type applications work with. It composes the pieces that a collaborative feature
/// always needs together - the CRDT document, the presence registry, an undo stack, a sync provider
/// and a persistence binding - so that a feature is written against one object with a coherent
/// lifetime rather than five that have to be wired up correctly and torn down in the right order.
/// </para>
/// <para>
/// Every part remains directly accessible. <see cref="Crdt"/> is the real document and the full
/// engine API is available through it; nothing here hides capability, it only removes ceremony.
/// </para>
/// </remarks>
public sealed class BcrdtCollaborativeDocument : IAsyncDisposable
{
    private readonly List<IAsyncDisposable> _ownedAsync = [];
    private readonly List<IDisposable> _owned = [];
    private BcrdtUndoManager? _undo;
    private bool _disposed;

    internal BcrdtCollaborativeDocument(string id, BcrdtDocument doc, BcrdtAwarenessStore awareness)
    {
        Id = id;
        Crdt = doc;
        Awareness = awareness;
        Presence = new BcrdtPresenceService(awareness, doc);
        _owned.Add(Presence);
        _owned.Add(awareness);
    }

    /// <summary>The document's identifier, as passed to the collaboration service.</summary>
    public string Id { get; }

    /// <summary>The underlying replicated document.</summary>
    public BcrdtDocument Crdt { get; }

    /// <summary>Ephemeral state published by every participant.</summary>
    public BcrdtAwarenessStore Awareness { get; }

    /// <summary>The typed view of who is here and where their cursors are.</summary>
    public BcrdtPresenceService Presence { get; }

    /// <summary>The synchronization provider, when the document is connected to a peer.</summary>
    public BcrdtSyncProvider? Sync { get; internal set; }

    /// <summary>The persistence binding, when the document is backed by a store.</summary>
    public BcrdtDocumentPersistence? Persistence { get; internal set; }

    /// <summary>The default undo stack for local edits, if one was requested.</summary>
    /// <remarks>
    /// It tracks untagged local edits - the origin application code and the built-in editing
    /// components use - to the default map (<see cref="Map"/>) and to every root type obtained through
    /// this object's accessors (<see cref="Text"/>, <see cref="Array"/>, <see cref="GetMap"/>,
    /// <see cref="Set"/>). Roots obtained directly from <see cref="Crdt"/> are not added; call
    /// <see cref="BcrdtUndoManager.AddToScope"/> for those. For an undo stack per editing surface, use
    /// <see cref="CreateUndoManager(BcrdtAbstractType[])"/>.
    /// </remarks>
    public BcrdtUndoManager? Undo
    {
        get => _undo;
        internal set
        {
            _undo = value;
            if (value is not null) _owned.Add(value);
        }
    }

    /// <summary>Where the connection stands, or <see cref="BcrdtConnectionStatus.Disconnected"/> if offline-only.</summary>
    public BcrdtConnectionStatus ConnectionStatus => Sync?.Status ?? BcrdtConnectionStatus.Disconnected;

    /// <summary>Whether the document currently matches the peer's copy.</summary>
    public bool IsSynced => Sync?.IsSynced ?? false;

    /// <summary>This replica's session identity.</summary>
    public ulong ClientId => Crdt.ClientId;

    /// <summary>The default root map, for documents that need only one.</summary>
    public BcrdtMap Map => Scoped(Crdt.GetMap(""));

    /// <summary>A named root map.</summary>
    public BcrdtMap GetMap(string name) => Scoped(Crdt.GetMap(name));

    /// <summary>A named root sequence.</summary>
    public BcrdtArray Array(string name) => Scoped(Crdt.GetArray(name));

    /// <summary>A named root text field.</summary>
    public BcrdtText Text(string name) => Scoped(Crdt.GetText(name));

    /// <summary>A named root set.</summary>
    public BcrdtSet Set(string name) => Scoped(Crdt.GetSet(name));

    /// <summary>Raised whenever anything in the document changes, locally or remotely.</summary>
    public event Action<BcrdtUpdateEventArgs>? Changed
    {
        add => Crdt.Updated += value;
        remove => Crdt.Updated -= value;
    }

    /// <summary>
    /// Batches several changes into one update and one round of events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Dispose the returned transaction to commit, or await <see cref="BcrdtCollaborativeTransaction.CommitAsync"/>
    /// to commit and additionally wait for the changes to reach storage.
    /// </para>
    /// <para>
    /// Do not hold a transaction open across an <c>await</c>. While it is open, every change made to
    /// the document - by another event handler that runs during the await, or by an update arriving
    /// from a peer - joins it and is published as part of this batch, with this batch's origin. A
    /// transaction opened inside another one joins it too, and committing it publishes nothing until
    /// the outer one commits.
    /// </para>
    /// </remarks>
    public BcrdtCollaborativeTransaction BeginTransaction(object? origin = null)
    {
        bool nested = Crdt.InTransaction;
        return new BcrdtCollaborativeTransaction(this, Crdt.BeginTransaction(origin), nested);
    }

    /// <summary>Runs a batch of changes as a single transaction.</summary>
    public void Transact(Action<BcrdtTransaction> action, object? origin = null) => Crdt.Transact(action, origin);

    /// <summary>Creates an undo stack scoped to particular types.</summary>
    /// <remarks>
    /// Scoping matters: an undo stack over a whole document will happily undo an edit the user made
    /// in an unrelated field five minutes ago. Scope one per editing surface.
    /// </remarks>
    public BcrdtUndoManager CreateUndoManager(params BcrdtAbstractType[] scope) => CreateUndoManager(null, scope);

    /// <summary>Creates an undo stack scoped to particular types, with explicit settings.</summary>
    public BcrdtUndoManager CreateUndoManager(BcrdtUndoManagerOptions? options, params BcrdtAbstractType[] scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var manager = new BcrdtUndoManager(Crdt, scope.Length > 0 ? scope : [Crdt.GetMap("")], options);
        _owned.Add(manager);
        return manager;
    }

    /// <summary>Connects to the configured peer and keeps the connection alive.</summary>
    /// <remarks>
    /// Disconnecting withdraws the local participant from everybody's presence list. Connecting again
    /// republishes the identity last set with <see cref="BcrdtPresenceService.SetUser"/>, so the participant
    /// reappears as who they are rather than as an anonymous cursor.
    /// </remarks>
    public ValueTask ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (Sync is null) return ValueTask.CompletedTask;

        if (Awareness.LocalState is null && !Presence.IsSuspended && Presence.LocalUser is { } user)
        {
            Presence.SetUser(user);
        }

        return Sync.ConnectAsync(cancellationToken);
    }

    /// <summary>Disconnects and stops reconnecting, leaving the document editable offline.</summary>
    public ValueTask DisconnectAsync(CancellationToken cancellationToken = default) =>
        Sync?.DisconnectAsync(cancellationToken) ?? ValueTask.CompletedTask;

    /// <summary>Waits until every pending change has reached storage.</summary>
    public Task FlushAsync() => Persistence?.FlushAsync() ?? Task.CompletedTask;

    /// <summary>A plain-object projection of the whole document, for inspection and tests.</summary>
    public SortedDictionary<string, object?> ToJson() => Crdt.ToJson();

    internal void Own(IAsyncDisposable resource) => _ownedAsync.Add(resource);

    private T Scoped<T>(T root) where T : BcrdtAbstractType
    {
        _undo?.AddToScope(root);
        return root;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Tears down in the order that keeps the document correct for as long as possible: say goodbye
    /// to peers while still connected, so they drop this participant immediately rather than after
    /// the presence timeout; stop the network, so nothing more arrives; then flush storage, which by
    /// then holds everything that did arrive; then release the document itself. A failure in one step
    /// does not skip the others.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        List<Exception>? failures = null;

        if (Sync is not null)
        {
            await Attempt(() => Sync.DisconnectAsync()).ConfigureAwait(false);
            await Attempt(Sync.DisposeAsync).ConfigureAwait(false);
        }

        for (int i = _ownedAsync.Count - 1; i >= 0; i--)
        {
            await Attempt(_ownedAsync[i].DisposeAsync).ConfigureAwait(false);
        }

        foreach (IDisposable resource in _owned)
        {
            try
            {
                resource.Dispose();
            }
            catch (Exception ex)
            {
                (failures ??= []).Add(ex);
            }
        }

        if (Persistence is not null) await Attempt(Persistence.DisposeAsync).ConfigureAwait(false);

        Crdt.Dispose();

        if (failures is not null)
        {
            throw new AggregateException($"Document '{Id}' did not close cleanly.", failures);
        }

        async ValueTask Attempt(Func<ValueTask> step)
        {
            try
            {
                await step().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                (failures ??= []).Add(ex);
            }
        }
    }
}

/// <summary>
/// A batch of changes that becomes one update when committed.
/// </summary>
/// <remarks>
/// <para>
/// There is no rollback. Every change made inside the batch is already part of the document's
/// history by the time the batch closes; what the batch controls is when the change is
/// <i>published</i> - as one update and one round of events rather than many.
/// </para>
/// <para>
/// Keep a batch synchronous: anything that runs while it is open joins it, including updates that
/// arrive from peers during an <c>await</c>. See <see cref="BcrdtCollaborativeDocument.BeginTransaction"/>.
/// </para>
/// </remarks>
public sealed class BcrdtCollaborativeTransaction : IDisposable, IAsyncDisposable
{
    private readonly BcrdtCollaborativeDocument _document;
    private readonly IDisposable _scope;
    private readonly bool _nested;
    private bool _committed;

    internal BcrdtCollaborativeTransaction(BcrdtCollaborativeDocument document, IDisposable scope, bool openedInsideAnother)
    {
        _document = document;
        _scope = scope;
        _nested = openedInsideAnother;
    }

    /// <summary>Whether this batch joined a transaction that was already open, and so publishes with it.</summary>
    public bool IsNested => _nested;

    /// <summary>Publishes the batch and waits for it to reach storage.</summary>
    /// <remarks>
    /// For a nested batch nothing is published until the enclosing transaction commits, so there is
    /// nothing yet to wait for; this then returns once anything already queued has been stored.
    /// </remarks>
    public async ValueTask CommitAsync()
    {
        Commit();
        await _document.FlushAsync().ConfigureAwait(false);
    }

    /// <summary>Publishes the batch without waiting for storage.</summary>
    public void Commit()
    {
        if (_committed) return;
        _committed = true;
        _scope.Dispose();
    }

    /// <inheritdoc/>
    public void Dispose() => Commit();

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => CommitAsync();
}
