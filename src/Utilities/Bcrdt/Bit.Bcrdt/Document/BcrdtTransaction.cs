namespace Bit.Bcrdt;

/// <summary>
/// A batch of changes applied to a document as one unit.
/// </summary>
/// <remarks>
/// <para>
/// Transactions are the engine's unit of observation and of network traffic, not a rollback
/// mechanism. Everything done inside one produces a single update message and a single round of
/// change events, so pasting five hundred rows costs one packet and one re-render rather than five
/// hundred of each. That batching is the difference between a collaborative list that stays
/// responsive under bulk edits and one that does not.
/// </para>
/// <para>
/// Transactions nest: calling into code that opens its own transaction joins the outer one instead
/// of starting a second. The outermost transaction is what commits.
/// </para>
/// <para>
/// There is deliberately no rollback. A CRDT operation is not a tentative proposal that a peer may
/// reject - once created it is part of the document's history, and "undoing" it is a new operation,
/// which is what <see cref="BcrdtUndoManager"/> provides.
/// </para>
/// </remarks>
public sealed class BcrdtTransaction
{
    private Dictionary<string, object?>? _meta;
    private List<Exception>? _observerFailures;

    internal BcrdtTransaction(BcrdtDocument doc, object? origin, bool local)
    {
        Doc = doc;
        Origin = origin;
        Local = local;
        BeforeState = doc.Store.GetStateVector();
        AfterState = BeforeState;
    }

    /// <summary>The document being changed.</summary>
    public BcrdtDocument Doc { get; }

    /// <summary>What was deleted during this transaction.</summary>
    public BcrdtDeleteSet DeleteSet { get; } = new();

    /// <summary>What the document knew before the transaction started.</summary>
    public BcrdtStateVector BeforeState { get; }

    /// <summary>What the document knows now. Only meaningful once the transaction has settled.</summary>
    public BcrdtStateVector AfterState { get; internal set; }

    /// <summary>
    /// A caller-supplied tag identifying who or what caused this change.
    /// </summary>
    /// <remarks>
    /// The origin is how the rest of the stack tells changes apart without inspecting them. A sync
    /// provider tags remote updates with itself so it does not echo them straight back to the peer
    /// that sent them; an undo manager uses it to capture only the local user's edits and leave
    /// everybody else's alone.
    /// </remarks>
    public object? Origin { get; }

    /// <summary>Whether the change originated on this replica rather than arriving from a peer.</summary>
    /// <remarks>
    /// This is how the transaction was opened, and an update applied inside a local transaction
    /// joins it without changing it: the transaction's own edits are still this replica's, and
    /// anything that keeps only local changes - persistence with remote updates turned off, say -
    /// must not lose them. Such a transaction also carries the update's content, so its
    /// <see cref="BcrdtDocument.Updated"/> payload includes that content too.
    /// </remarks>
    public bool Local { get; }

    /// <summary>
    /// Whether this transaction integrated content from an update, whatever it was opened as.
    /// </summary>
    /// <remarks>
    /// A remote change can insert, split or delete blocks anywhere, which is what invalidates the
    /// cached index positions; <see cref="Local"/> cannot say that once an update has joined a local
    /// transaction.
    /// </remarks>
    internal bool IntegratedRemoteContent { get; set; }

    /// <summary>
    /// Whether blocks from an update are being placed right now - as opposed to blocks this replica is
    /// creating, which is everything placed while this is false.
    /// </summary>
    /// <remarks>
    /// What a block may do differs between the two: a local insertion that breaks a rule is refused with
    /// an exception to its caller, while a block from a peer that breaks the same rule is dropped the same
    /// way on every replica. Deciding by the block's identifier instead would let a peer forge one that
    /// looks local.
    /// </remarks>
    internal bool IntegratingRemoteBlocks { get; set; }

    /// <summary>
    /// Whether this transaction integrated content carrying the document's own client identifier
    /// that arrived from somewhere else - the proof that another replica is using the same identity.
    /// </summary>
    internal bool ReceivedOwnClientContent { get; set; }

    /// <summary>
    /// Whether every observer has run. From here on the transaction's blocks are reclaimed and
    /// compacted, so anything derived lazily from them can no longer be computed correctly.
    /// </summary>
    internal bool ObserversCompleted { get; set; }

    /// <summary>Arbitrary per-transaction state, for extensions that need to carry context.</summary>
    public IDictionary<string, object?> Meta => _meta ??= new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>Which types changed, and which of their keys. A null key means sequence content.</summary>
    public Dictionary<BcrdtAbstractType, HashSet<string?>> Changed { get; } = [];

    /// <summary>Events routed to each type's deep subscribers.</summary>
    internal Dictionary<BcrdtAbstractType, List<BcrdtEvent>> ChangedParentTypes { get; } = [];

    /// <summary>Blocks that may be able to merge with a neighbour once the transaction settles.</summary>
    internal List<BcrdtAbstractStruct> MergeStructs { get; } = [];

    /// <summary>Whether anything actually changed.</summary>
    public bool HasChanges
    {
        get
        {
            if (!DeleteSet.IsEmpty) return true;
            foreach ((ulong client, long clock) in AfterState)
            {
                if (BeforeState.Get(client) != clock) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Deleted content an undo stack stopped protecting, to be reclaimed when this transaction settles.
    /// </summary>
    /// <remarks>
    /// The collector otherwise only looks at what a transaction deleted, so content that was kept for
    /// an undo step deleted long ago would be released and then never reclaimed.
    /// </remarks>
    internal BcrdtDeleteSet? ReleasedForCollection { get; private set; }

    internal void ReleaseForCollection(BcrdtDeleteSet ranges)
    {
        ReleasedForCollection ??= new BcrdtDeleteSet();
        foreach ((ulong client, List<BcrdtDeleteRange> clientRanges) in ranges.Clients)
        {
            foreach (BcrdtDeleteRange range in clientRanges) ReleasedForCollection.Add(client, range.Clock, range.Length);
        }
    }

    /// <summary>Records that a type changed, so an event is raised for it at commit.</summary>
    internal void AddChangedType(BcrdtAbstractType type, string? key)
    {
        // Content placed from an update can land anywhere in the type, so index positions cached on
        // the assumption of local edits only are void from this moment - not just from when the
        // type's observer runs. A local edit later in the same transaction, or one an observer of
        // another type makes before this type's observer has run, must not look them up.
        if (IntegratedRemoteContent) type.InvalidateSearchMarkers();

        BcrdtItem? item = type.ContainingItem;

        // A type created during this very transaction has nothing to report: subscribers never saw
        // its previous state, so the change is described by the insertion of the type itself.
        if (item is not null && (item.Id.Clock >= BeforeState.Get(item.Id.Client) || item.Deleted)) return;

        if (!Changed.TryGetValue(type, out HashSet<string?>? keys))
        {
            keys = [];
            Changed[type] = keys;
        }
        keys.Add(key);
    }

    internal void RecordObserverFailure(Exception exception) => (_observerFailures ??= []).Add(exception);

    /// <summary>Invokes every subscriber of an event, so that one throwing does not starve the rest.</summary>
    internal void InvokeIsolated<T>(Action<T>? handlers, T argument)
    {
        if (handlers is null) return;
        foreach (Action<T> handler in handlers.GetInvocationList().Cast<Action<T>>())
        {
            try { handler(argument); }
            catch (Exception ex) { RecordObserverFailure(ex); }
        }
    }

    internal IReadOnlyList<Exception>? ObserverFailures => _observerFailures;
}
