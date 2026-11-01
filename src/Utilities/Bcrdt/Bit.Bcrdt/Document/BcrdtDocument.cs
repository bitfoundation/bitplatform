namespace Bit.Bcrdt;

/// <summary>Configuration for a new <see cref="BcrdtDocument"/>.</summary>
public sealed class BcrdtDocumentOptions
{
    /// <summary>An application-level identifier for the document. Not used by the engine.</summary>
    public string? Id { get; init; }

    /// <summary>An explicit client identifier. Leave unset to have one generated.</summary>
    /// <remarks>Set this only in tests; two live sessions sharing an identifier will not converge.</remarks>
    public ulong? ClientId { get; init; }

    /// <summary>Where generated client identifiers come from.</summary>
    public IBcrdtClientIdProvider ClientIdProvider { get; init; } = BcrdtRandomClientIdProvider.Instance;

    /// <summary>
    /// Whether deleted content is reclaimed automatically at the end of each transaction.
    /// </summary>
    /// <remarks>
    /// Leave this on unless the application needs to reconstruct historical states from tombstones.
    /// Turning it off makes documents grow without bound in the presence of deletions.
    /// </remarks>
    public bool GarbageCollect { get; init; } = true;
}

/// <summary>Carries an update produced by a committed transaction.</summary>
/// <param name="Update">The encoded delta, ready to send to peers or append to storage.</param>
/// <param name="Origin">The <see cref="BcrdtTransaction.Origin"/> of the transaction that produced it.</param>
/// <param name="Transaction">The transaction itself.</param>
public readonly record struct BcrdtUpdateEventArgs(byte[] Update, object? Origin, BcrdtTransaction Transaction);

/// <summary>
/// A collaborative document: a set of named root types over one replicated CRDT state.
/// </summary>
/// <remarks>
/// <para>
/// This is the unit of replication and of synchronization. Everything inside it - every map, array,
/// text and set, however deeply nested - shares one clock space, one update stream and one state
/// vector, which is what allows an edit anywhere in the tree to be described by a single small
/// delta and merged without coordination.
/// </para>
/// <para>
/// A document is not thread-safe. Confine it to one thread, or one synchronization context; in
/// Blazor that happens naturally. All mutation goes through <see cref="Transact(Action{BcrdtTransaction}, object?, bool)"/>,
/// which is also the boundary at which events fire and updates are emitted.
/// </para>
/// </remarks>
public sealed class BcrdtDocument : IDisposable
{
    private readonly Dictionary<string, BcrdtAbstractType> _roots = new(StringComparer.Ordinal);
    private readonly List<BcrdtTransaction> _pendingCleanups = [];
    private BcrdtTransaction? _currentTransaction;
    private BcrdtDeleteSet? _cachedDeleteSet;
    private long _cachedDeleteSetVersion = -1;
    private long _version;
    private bool _disposed;

    /// <summary>Creates a document.</summary>
    public BcrdtDocument(BcrdtDocumentOptions? options = null)
    {
        options ??= new BcrdtDocumentOptions();
        Id = options.Id ?? Guid.NewGuid().ToString("N");
        ClientId = options.ClientId ?? options.ClientIdProvider.CreateClientId();
        GarbageCollect = options.GarbageCollect;
    }

    /// <summary>Creates a document with an application-level identifier.</summary>
    public BcrdtDocument(string id) : this(new BcrdtDocumentOptions { Id = id }) { }

    /// <summary>An application-level identifier. Not used by the engine.</summary>
    public string Id { get; }

    /// <summary>
    /// This replica's identity, used to stamp everything it creates.
    /// </summary>
    /// <remarks>
    /// Regenerated automatically if the engine detects that a remote peer is using the same
    /// identifier, which would otherwise break the contiguity invariant and corrupt both replicas.
    /// </remarks>
    public ulong ClientId { get; private set; }

    /// <summary>Whether deleted content is reclaimed at the end of each transaction.</summary>
    public bool GarbageCollect { get; set; }

    /// <summary>The block store. Exposed for extensions building new collaborative types.</summary>
    public BcrdtStructStore Store { get; } = new();

    /// <summary>The named root types.</summary>
    public IReadOnlyDictionary<string, BcrdtAbstractType> RootTypes => _roots;

    /// <summary>Whether a transaction is currently open.</summary>
    public bool InTransaction => _currentTransaction is not null;

    /// <summary>
    /// The undo managers over this document, so one releasing deleted content it kept can see whether
    /// another still needs it.
    /// </summary>
    internal List<BcrdtUndoManager> UndoManagers { get; } = [];

    /// <summary>
    /// Whether this replica holds changes it received but cannot apply yet, because something they
    /// build on - an earlier change by the same author, a block they were inserted next to, the
    /// content a deletion targets - has not arrived.
    /// </summary>
    /// <remarks>
    /// Such changes are kept and applied automatically as soon as what they wait for arrives, and they
    /// are passed on to peers by <see cref="EncodeStateAsUpdate"/> in the meantime. A replica that stays
    /// in this state is missing an update - a lost message, usually - and the next handshake
    /// (<see cref="BcrdtSyncProvider.ResyncAsync"/>) asks for it.
    /// </remarks>
    public bool HasPendingUpdates => Store.PendingUpdate is not null || Store.PendingDeleteSet is not null;

    /// <summary>
    /// Whether a transaction is open or still being settled, so that a change made now would not
    /// settle - and raise its events - before the call making it returns.
    /// </summary>
    /// <remarks>
    /// Wider than <see cref="InTransaction"/>: while queued transactions are being settled none is
    /// open, yet one opened by an observer is only settled once those ahead of it have been.
    /// </remarks>
    internal bool HasUnsettledTransactions => _pendingCleanups.Count > 0;

    /// <summary>
    /// Raised once every queued transaction has settled, for work that had to wait for the document
    /// to come to rest. A change a handler makes settles before it returns.
    /// </summary>
    internal event Action? Settled;

    /// <summary>Raised once per committed transaction that changed anything.</summary>
    /// <remarks>
    /// This is the outbound edge of the whole system: a sync provider subscribes here and forwards
    /// the delta to peers, and a persistence provider subscribes here and appends it to storage.
    /// Neither needs to know anything about the change beyond its bytes.
    /// </remarks>
    public event Action<BcrdtUpdateEventArgs>? Updated;

    /// <summary>Raised before a top-level transaction begins.</summary>
    public event Action<BcrdtTransaction>? BeforeTransaction;

    /// <summary>Raised after a transaction's observers have run.</summary>
    public event Action<BcrdtTransaction>? AfterTransaction;

    /// <summary>Raised when the document is disposed.</summary>
    public event Action<BcrdtDocument>? Destroyed;

    /// <summary>The map registered under a root name, created if absent.</summary>
    public BcrdtMap GetMap(string name = "") => GetRoot<BcrdtMap>(name);

    /// <summary>The array registered under a root name, created if absent.</summary>
    public BcrdtArray GetArray(string name = "") => GetRoot<BcrdtArray>(name);

    /// <summary>The text registered under a root name, created if absent.</summary>
    public BcrdtText GetText(string name = "") => GetRoot<BcrdtText>(name);

    /// <summary>The set registered under a root name, created if absent.</summary>
    public BcrdtSet GetSet(string name = "") => GetRoot<BcrdtSet>(name);

    /// <summary>
    /// The root type registered under a name, created as <typeparamref name="T"/> if absent.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The root already exists with a different, known kind. Two replicas that disagree about a
    /// root's kind cannot converge, so this fails loudly rather than silently reinterpreting data.
    /// </exception>
    public T GetRoot<T>(string name) where T : BcrdtAbstractType, new()
    {
        ArgumentNullException.ThrowIfNull(name);

        if (_roots.TryGetValue(name, out BcrdtAbstractType? existing))
        {
            if (existing is T typed) return typed;

            if (existing is not BcrdtUntypedType)
            {
                throw new InvalidOperationException(
                    $"Root '{name}' already exists as {existing.Kind} and cannot be used as {typeof(T).Name}.");
            }

            return (T)AdoptUntypedRoot<T>(name, existing);
        }

        var created = new T();
        _roots[name] = created;
        created.RootKey = name;
        created.Integrate(this, null);
        return created;
    }

    /// <summary>
    /// Re-parents content that arrived for a root before the application declared its kind.
    /// </summary>
    private BcrdtAbstractType AdoptUntypedRoot<T>(string name, BcrdtAbstractType placeholder)
        where T : BcrdtAbstractType, new()
    {
        var upgraded = new T();

        upgraded.Start = placeholder.Start;
        for (BcrdtItem? item = upgraded.Start; item is not null; item = item.Right) item.Parent = upgraded;

        foreach ((string key, BcrdtItem head) in placeholder.KeyIndex)
        {
            upgraded.KeyIndex[key] = head;
            for (BcrdtItem? item = head; item is not null; item = item.Left) item.Parent = upgraded;
        }

        upgraded.Length = placeholder.Length;
        upgraded.HasFormatting = placeholder.HasFormatting;

        _roots[name] = upgraded;
        upgraded.RootKey = name;
        upgraded.Integrate(this, null);
        return upgraded;
    }

    /// <summary>Registers a root type under a name without inferring its kind.</summary>
    internal BcrdtAbstractType GetOrCreateUntypedRoot(string name)
    {
        if (_roots.TryGetValue(name, out BcrdtAbstractType? existing)) return existing;

        var created = new BcrdtUntypedType();
        _roots[name] = created;
        created.RootKey = name;
        created.Integrate(this, null);
        return created;
    }

    /// <summary>
    /// Runs <paramref name="action"/> as a single atomic change.
    /// </summary>
    /// <param name="action">The mutations to apply.</param>
    /// <param name="origin">A tag identifying the cause of the change. See <see cref="BcrdtTransaction.Origin"/>.</param>
    /// <param name="local">Whether the change originated on this replica.</param>
    /// <remarks>
    /// Nested calls join the enclosing transaction rather than opening a new one, so a method that
    /// transacts internally composes safely into a larger batch.
    /// </remarks>
    public void Transact(Action<BcrdtTransaction> action, object? origin = null, bool local = true)
    {
        ArgumentNullException.ThrowIfNull(action);
        Transact<object?>(transaction => { action(transaction); return null; }, origin, local);
    }

    /// <summary>Runs <paramref name="action"/> as a single atomic change and returns its result.</summary>
    public T Transact<T>(Func<BcrdtTransaction, T> action, object? origin = null, bool local = true)
    {
        ArgumentNullException.ThrowIfNull(action);

        bool isOutermost = BeginScope(origin, local);
        try
        {
            return action(_currentTransaction!);
        }
        finally
        {
            if (isOutermost) EndScope();
        }
    }

    /// <summary>
    /// Opens a transaction that stays open until the returned scope is disposed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The imperative form of <see cref="Transact(Action{BcrdtTransaction}, object?, bool)"/>, for the
    /// cases where the changes cannot be expressed in one callback - a UI event handler batching
    /// several user actions, for instance.
    /// </para>
    /// <para>
    /// Disposing the scope commits. There is no rollback, and that is a property of the model rather
    /// than a missing feature: a CRDT operation is not a proposal awaiting approval, and by the time
    /// the scope closes the operations already exist. To reverse them, use
    /// <see cref="BcrdtUndoManager"/>, which reverses them the way a peer would - with new operations.
    /// </para>
    /// </remarks>
    public IDisposable BeginTransaction(object? origin = null, bool local = true) =>
        new TransactionScope(this, BeginScope(origin, local));

    /// <summary>The transaction currently open, if any.</summary>
    public BcrdtTransaction? CurrentTransaction => _currentTransaction;

    private bool BeginScope(object? origin, bool local)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // A nested call joins the enclosing transaction, so composing transactional methods produces
        // one batch rather than several.
        if (_currentTransaction is not null) return false;

        _currentTransaction = new BcrdtTransaction(this, origin, local);
        _pendingCleanups.Add(_currentTransaction);

        // A throwing subscriber is reported when the transaction commits, like any other observer
        // failure. Letting it escape here would leave the transaction open with nobody to close it,
        // and every later change would silently join that dead transaction.
        _currentTransaction.InvokeIsolated(BeforeTransaction, _currentTransaction);
        return true;
    }

    private void EndScope()
    {
        // Only the transaction that opened the queue drains it; anything opened by an observer
        // during the drain is picked up by the loop already running.
        bool shouldFlush = _pendingCleanups.Count > 0 && ReferenceEquals(_currentTransaction, _pendingCleanups[0]);
        _currentTransaction = null;
        if (shouldFlush) FlushPendingTransactions();
    }

    private sealed class TransactionScope(BcrdtDocument doc, bool isOutermost) : IDisposable
    {
        private bool _closed;

        public void Dispose()
        {
            if (_closed) return;
            _closed = true;
            if (isOutermost) doc.EndScope();
        }
    }

    /// <summary>
    /// Settles every queued transaction: raise events, emit updates, reclaim and compact.
    /// </summary>
    /// <remarks>
    /// The queue exists because an observer may itself change the document. Rather than recursing -
    /// which would interleave one transaction's events with another's commit - the nested
    /// transaction is appended here and settled in turn, so subscribers always observe complete,
    /// consistent states in causal order.
    /// </remarks>
    private void FlushPendingTransactions()
    {
        List<Exception>? failures = null;

        for (int index = 0; index < _pendingCleanups.Count; index++)
        {
            BcrdtTransaction transaction = _pendingCleanups[index];
            try
            {
                Settle(transaction);
            }
            catch (Exception ex)
            {
                (failures ??= []).Add(ex);
            }

            if (transaction.ObserverFailures is { Count: > 0 } observerFailures)
            {
                (failures ??= []).AddRange(observerFailures);
            }
        }

        _pendingCleanups.Clear();

        if (Settled is { } settled)
        {
            foreach (Action handler in settled.GetInvocationList().Cast<Action>())
            {
                try
                {
                    handler();
                }
                catch (Exception ex)
                {
                    (failures ??= []).Add(ex);
                }
            }
        }

        if (failures is { Count: > 0 })
        {
            throw new AggregateException(
                "One or more change subscribers threw while the transaction was being committed. " +
                "The document state is consistent; the exceptions below come from subscriber code.",
                failures);
        }
    }

    private void Settle(BcrdtTransaction transaction)
    {
        transaction.DeleteSet.SortAndMerge();
        transaction.AfterState = Store.GetStateVector();

        // Any change invalidates whatever was derived from the previous state. This has to happen
        // before the observers run, because an observer that encodes the document - to persist it,
        // or to answer a peer - must see this transaction's deletions, not the cached set from before.
        if (transaction.HasChanges) _version++;

        try
        {
            foreach ((BcrdtAbstractType type, HashSet<string?> keys) in transaction.Changed.ToArray())
            {
                if (type.ContainingItem is null || !type.ContainingItem.Deleted)
                {
                    type.CallObserver(transaction, keys);
                }
            }

            foreach ((BcrdtAbstractType type, List<BcrdtEvent> events) in transaction.ChangedParentTypes.ToArray())
            {
                if (!type.HasDeepObservers) continue;
                if (type.ContainingItem is { Deleted: true }) continue;

                List<BcrdtEvent> live = [.. events.Where(e => e.Target.ContainingItem is null || !e.Target.ContainingItem.Deleted)];
                foreach (BcrdtEvent crdtEvent in live) crdtEvent.CurrentTarget = type;

                // Shallowest first, so a handler that rebuilds a subtree sees the change that
                // created it before the changes inside it.
                live.Sort(static (left, right) => left.Path.Count.CompareTo(right.Path.Count));
                if (live.Count > 0) type.EmitDeep(live, transaction);
            }

            transaction.InvokeIsolated(AfterTransaction, transaction);
        }
        finally
        {
            transaction.ObserversCompleted = true;

            if (GarbageCollect) CollectDeleted(transaction);
            CompactBlocks(transaction);
            DetectClientIdCollision(transaction);
            EmitUpdate(transaction);
        }
    }

    /// <summary>
    /// Reclaims the payloads of everything deleted in this transaction, and of anything an undo stack
    /// released during it.
    /// </summary>
    private void CollectDeleted(BcrdtTransaction transaction)
    {
        CollectDeleted(transaction.DeleteSet);

        if (transaction.ReleasedForCollection is { } released)
        {
            released.SortAndMerge();
            CollectDeleted(released);
        }
    }

    private void CollectDeleted(BcrdtDeleteSet deleteSet)
    {
        foreach ((ulong client, List<BcrdtDeleteRange> ranges) in deleteSet.Clients)
        {
            List<BcrdtAbstractStruct>? structs = Store.GetClientStructs(client);
            if (structs is null) continue;

            for (int rangeIndex = ranges.Count - 1; rangeIndex >= 0; rangeIndex--)
            {
                BcrdtDeleteRange range = ranges[rangeIndex];
                if (range.Clock >= Store.GetState(client)) continue;

                for (int i = BcrdtStructStore.FindIndex(structs, range.Clock);
                     i < structs.Count && structs[i].Id.Clock < range.End;
                     i++)
                {
                    // Kept blocks are still needed by an undo stack or a live snapshot.
                    if (structs[i] is BcrdtItem { Deleted: true, Keep: false } item)
                    {
                        item.Collect(Store, parentCollected: false);
                    }
                }
            }
        }
    }

    /// <summary>Merges blocks that editing left split but that are once again indistinguishable.</summary>
    private void CompactBlocks(BcrdtTransaction transaction)
    {
        // Deleting next to something already deleted leaves two tombstones that are now identical
        // in kind. Folding them together - right to left, so no merge target is skipped - is what
        // stops a document that is edited for long enough from accumulating one block per deletion.
        foreach ((ulong client, List<BcrdtDeleteRange> ranges) in transaction.DeleteSet.Clients)
        {
            List<BcrdtAbstractStruct>? structs = Store.GetClientStructs(client);
            if (structs is null || structs.Count < 2) continue;

            long state = Store.GetState(client);
            for (int rangeIndex = ranges.Count - 1; rangeIndex >= 0; rangeIndex--)
            {
                BcrdtDeleteRange range = ranges[rangeIndex];
                if (range.Clock >= state) continue;

                long lastClock = Math.Min(range.End, state) - 1;
                int position = Math.Min(structs.Count - 1, BcrdtStructStore.FindIndex(structs, lastClock) + 1);
                while (position > 0 && structs[position].Id.Clock >= range.Clock)
                {
                    position -= 1 + TryMergeWithLefts(structs, position);
                }
            }
        }

        foreach (BcrdtAbstractStruct candidate in transaction.MergeStructs)
        {
            List<BcrdtAbstractStruct>? structs = Store.GetClientStructs(candidate.Id.Client);
            if (structs is null) continue;

            int position = BcrdtStructStore.FindIndex(structs, candidate.Id.Clock);
            if (position + 1 < structs.Count && TryMergeWithLefts(structs, position + 1) > 0) continue;
            if (position > 0) TryMergeWithLefts(structs, position);
        }

        foreach ((ulong client, long afterClock) in transaction.AfterState)
        {
            long beforeClock = transaction.BeforeState.Get(client);
            if (beforeClock == afterClock) continue;

            List<BcrdtAbstractStruct>? structs = Store.GetClientStructs(client);
            if (structs is null || structs.Count < 2) continue;

            int firstChanged = Math.Max(BcrdtStructStore.FindIndex(structs, beforeClock), 1);
            for (int i = structs.Count - 1; i >= firstChanged;)
            {
                i -= 1 + TryMergeWithLefts(structs, i);
            }
        }
    }

    /// <summary>Folds a block, and its left neighbours for as long as they are mergeable, into one.</summary>
    /// <returns>How many blocks disappeared.</returns>
    /// <remarks>
    /// The blocks are found first and folded from the left, so the block that grows only ever appends
    /// the one neighbour it absorbs. Folded from the right, each block in turn would copy everything
    /// gathered so far, and a run of n blocks - one update can carry hundreds of thousands, each a value
    /// or a character long - would cost time in the square of n.
    /// </remarks>
    private static int TryMergeWithLefts(List<BcrdtAbstractStruct> structs, int position)
    {
        if (position <= 0 || position >= structs.Count) return 0;

        int first = position;
        int length = structs[position].Length;
        while (first > 0)
        {
            BcrdtAbstractStruct left = structs[first - 1];
            BcrdtAbstractStruct right = structs[first];
            if (left.Deleted != right.Deleted || left.GetType() != right.GetType()) break;
            if (!left.CanMergeWith(right) || !BcrdtAbstractStruct.FitsOneBlock(left.Length, length)) break;

            length += left.Length;
            first--;
        }

        int survivor = first;
        for (int index = first + 1; index <= position; index++)
        {
            BcrdtAbstractStruct left = structs[survivor];
            BcrdtAbstractStruct right = structs[index];

            // Only a payload that misjudged whether it could merge gets here; it carries on as a block of its own.
            if (!left.TryMergeWith(right))
            {
                structs[++survivor] = right;
                continue;
            }

            // The key index may have pointed at the block that just disappeared.
            if (right is BcrdtItem { ParentKey: not null } mapItem &&
                mapItem.Parent is BcrdtAbstractType parent &&
                ReferenceEquals(parent.MapGetRaw(mapItem.ParentKey), mapItem))
            {
                parent.MapSetRaw(mapItem.ParentKey, left as BcrdtItem);
            }
        }

        int merged = position - survivor;
        if (merged > 0) structs.RemoveRange(survivor + 1, merged);
        return merged;
    }

    /// <summary>
    /// Detects a peer using this replica's client identifier and moves out of its way.
    /// </summary>
    /// <remarks>
    /// Two replicas sharing an identifier would write into the same clock space and produce blocks
    /// that are different but indistinguishable, which corrupts both documents beyond repair.
    /// A remote update that carried content under our own identifier is proof that has happened, so
    /// we take a fresh identity; the blocks already written keep the old one and stay valid. The
    /// test is on the integrated content itself rather than on the clock moving, because a
    /// transaction that mixes local edits with an applied update advances our clock legitimately.
    /// </remarks>
    private void DetectClientIdCollision(BcrdtTransaction transaction)
    {
        if (!transaction.ReceivedOwnClientContent) return;

        ClientId = BcrdtRandomClientIdProvider.Instance.CreateClientId();
    }

    private void EmitUpdate(BcrdtTransaction transaction)
    {
        Action<BcrdtUpdateEventArgs>? handlers = Updated;
        if (handlers is null || !transaction.HasChanges) return;

        byte[] update = BcrdtUpdateEncoder.EncodeTransaction(transaction);

        // Each subscriber is isolated from the others. One throwing must not stop the update from
        // reaching the rest: a failing sync provider must not cost the persistence layer its write.
        transaction.InvokeIsolated(handlers, new BcrdtUpdateEventArgs(update, transaction.Origin, transaction));
    }

    /// <summary>Merges an encoded update from a peer or from storage into this document.</summary>
    /// <param name="update">Bytes produced by <see cref="EncodeStateAsUpdate"/> or the <see cref="Updated"/> event.</param>
    /// <param name="origin">A tag identifying where the update came from.</param>
    /// <exception cref="BcrdtFormatException">The payload is not a well-formed update.</exception>
    public void ApplyUpdate(ReadOnlyMemory<byte> update, object? origin = null) =>
        BcrdtUpdateDecoder.ApplyUpdate(this, update, origin);

    /// <summary>
    /// Encodes everything this document holds that a peer does not.
    /// </summary>
    /// <param name="remoteState">
    /// What the peer already has. Pass <see langword="null"/> to encode the whole document.
    /// </param>
    public byte[] EncodeStateAsUpdate(BcrdtStateVector? remoteState = null) =>
        BcrdtUpdateEncoder.EncodeStateAsUpdate(this, remoteState);

    /// <summary>A summary of everything this document holds, for requesting a delta from a peer.</summary>
    public BcrdtStateVector GetStateVector() => Store.GetStateVector();

    /// <summary>
    /// The document's complete delete set, derived once per change rather than once per request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deriving it means scanning every block in the document, which costs time and allocation in
    /// proportion to the whole document. That is fine once; it is not fine on every sync response,
    /// and a sync response is exactly where it is needed. A busy server answers "anything new?" for
    /// the same idle document many times a second, and each of those answers was rebuilding the set
    /// from scratch to say "nothing".
    /// </para>
    /// <para>
    /// The cache is invalidated by <i>any</i> change to the document, not by reasoning about which
    /// changes could affect deletions. That is deliberate: a stale delete set would corrupt
    /// synchronization in a way that is extremely hard to trace, and the conservative rule costs
    /// nothing - a document that is being edited was going to re-derive it anyway.
    /// </para>
    /// <para>
    /// The returned instance is shared and must not be mutated. Call <see cref="BcrdtDeleteSet.Clone"/>
    /// if you need one to keep.
    /// </para>
    /// </remarks>
    public BcrdtDeleteSet GetDeleteSet()
    {
        // Inside an open transaction the blocks are still changing and the version has not moved
        // yet, so neither the cache nor a value derived now can be trusted for later.
        if (_currentTransaction is not null) return BcrdtDeleteSet.FromStore(Store);

        if (_cachedDeleteSet is null || _cachedDeleteSetVersion != _version)
        {
            _cachedDeleteSet = BcrdtDeleteSet.FromStore(Store);
            _cachedDeleteSetVersion = _version;
        }
        return _cachedDeleteSet;
    }

    /// <summary>
    /// A plain-object projection of every root type, in a deterministic order.
    /// </summary>
    /// <remarks>
    /// Determinism is what makes this useful as an assertion: two converged replicas produce
    /// identical projections, so comparing them is a direct test of the convergence property.
    /// </remarks>
    public SortedDictionary<string, object?> ToJson()
    {
        var result = new SortedDictionary<string, object?>(StringComparer.Ordinal);
        foreach ((string name, BcrdtAbstractType type) in _roots) result[name] = type.ToJson();
        return result;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Destroyed?.Invoke(this);
        Updated = null;
        BeforeTransaction = null;
        AfterTransaction = null;
        Settled = null;
        Destroyed = null;
    }
}
