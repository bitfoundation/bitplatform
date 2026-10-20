namespace Bit.Bcrdt;

/// <summary>Discriminates the built-in collaborative types on the wire.</summary>
/// <remarks>These values are persisted; never renumber an existing entry.</remarks>
public enum BcrdtTypeKind : byte
{
    /// <summary>A <see cref="BcrdtMap"/>.</summary>
    Map = 0,
    /// <summary>A <see cref="BcrdtArray"/>.</summary>
    Array = 1,
    /// <summary>A <see cref="BcrdtText"/>.</summary>
    Text = 2,
    /// <summary>A <see cref="BcrdtSet"/>.</summary>
    Set = 3,
    /// <summary>A root type whose kind is not yet known.</summary>
    Untyped = 4,
}

/// <summary>
/// The behaviour shared by every collaborative type.
/// </summary>
/// <remarks>
/// <para>
/// All four built-in types are views over one underlying structure: a doubly-linked list of blocks
/// for sequence content, plus a key index for map content. A map uses only the key index, an array
/// and a text use only the list, and a nested document uses both at different levels. Keeping them
/// unified means one integration algorithm, one encoder, one garbage collector and one undo manager
/// serve all of them - and any new type added later inherits all of that for free.
/// </para>
/// <para>
/// Types are not thread-safe. All mutation must happen inside a transaction on the owning document,
/// which is the synchronization boundary.
/// </para>
/// </remarks>
public abstract class BcrdtAbstractType
{
    private List<Action<BcrdtEvent, BcrdtTransaction>>? _observers;
    private List<Action<IReadOnlyList<BcrdtEvent>, BcrdtTransaction>>? _deepObservers;

    /// <summary>The first block of this type's sequence content.</summary>
    public BcrdtItem? Start;

    /// <summary>The number of visible units in this type's sequence content.</summary>
    public int Length;

    /// <summary>
    /// The key index for map content: key to the <i>rightmost</i> block written under that key.
    /// </summary>
    /// <remarks>
    /// Writes to a key chain left to right, and the rightmost link is the current value. Keeping the
    /// index pointed at that end makes reading a key a dictionary lookup instead of a chain walk,
    /// which matters because reading is far more frequent than writing.
    /// </remarks>
    internal readonly Dictionary<string, BcrdtItem> KeyIndex = new(StringComparer.Ordinal);

    /// <summary>The block that holds this type inside its parent, or null for a root type.</summary>
    public BcrdtItem? ContainingItem { get; internal set; }

    /// <summary>Whether this sequence has ever carried a rich-text mark.</summary>
    /// <remarks>
    /// A plain-text document should not pay for attribute tracking it never uses, so the readers
    /// check this flag and take a simpler path when it is false.
    /// </remarks>
    public bool HasFormatting { get; internal set; }

    internal List<BcrdtSearchMarker>? SearchMarkers;

    internal BcrdtDocument? AttachedDoc;

    /// <summary>
    /// The detached container this detached type is waiting in, if any. Lets a second container
    /// refuse it, and lets a container refuse to end up inside itself, before anything is integrated.
    /// </summary>
    internal BcrdtAbstractType? PendingOwner;

    /// <summary>The document this type belongs to.</summary>
    /// <exception cref="InvalidOperationException">The type has not been attached to a document.</exception>
    public BcrdtDocument Doc => AttachedDoc
        ?? throw new InvalidOperationException(
            "This type is not attached to a document yet. Insert it into a document before using it.");

    /// <summary>Whether the type belongs to a document.</summary>
    public bool IsAttached => AttachedDoc is not null;

    /// <summary>Whether the block holding this type has been deleted.</summary>
    public bool IsDeleted => ContainingItem is { Deleted: true };

    /// <summary>The wire discriminator for this type.</summary>
    public abstract BcrdtTypeKind Kind { get; }

    /// <summary>Creates an empty instance of the same kind.</summary>
    protected internal abstract BcrdtAbstractType CreateInstance();

    /// <summary>Builds the event this type raises for a set of changed keys.</summary>
    protected internal abstract BcrdtEvent CreateEvent(BcrdtTransaction transaction, HashSet<string?> changedKeys);

    /// <summary>Attaches the type to a document.</summary>
    protected internal virtual void Integrate(BcrdtDocument doc, BcrdtItem? item)
    {
        AttachedDoc = doc;
        ContainingItem = item;
        PendingOwner = null;
    }

    /// <summary>
    /// Creates a detached deep copy of this type's current contents, ready to be inserted elsewhere.
    /// </summary>
    /// <remarks>
    /// A type instance lives in exactly one place, so putting the same contents somewhere else - or
    /// moving them - means inserting a copy. Nested types are copied recursively; the copy shares
    /// nothing with the original and has no collaborative history of its own.
    /// </remarks>
    /// <exception cref="NotSupportedException">This kind of type cannot be copied.</exception>
    public virtual BcrdtAbstractType Clone() =>
        throw new NotSupportedException($"{GetType().Name} does not support cloning.");

    /// <summary>Copies a stored value for <see cref="Clone"/>, deep-copying nested types.</summary>
    protected static object? CloneValue(object? value) => value is BcrdtAbstractType nested ? nested.Clone() : value;

    /// <summary>The current value stored under a map key, as a raw block.</summary>
    public BcrdtItem? MapGetRaw(string key) => KeyIndex.TryGetValue(key, out BcrdtItem? item) ? item : null;

    /// <summary>Points the key index at a block.</summary>
    public void MapSetRaw(string key, BcrdtItem? item)
    {
        if (item is null) KeyIndex.Remove(key);
        else KeyIndex[key] = item;
    }

    /// <summary>Drops every cached index position for this type.</summary>
    public void InvalidateSearchMarkers()
    {
        if (SearchMarkers is null) return;
        foreach (BcrdtSearchMarker marker in SearchMarkers) marker.Item.HasMarker = false;
        SearchMarkers = null;
    }

    /// <summary>Repoints cached positions after two blocks merged into one.</summary>
    /// <remarks>
    /// Called before the survivor absorbs the other block's length, so <c>survivor.Length</c> is
    /// still the distance between the two blocks' starts. A marker on the absorbed block moves back
    /// to the survivor's start, which lies that many visible units earlier - or at the same index, if
    /// the survivor occupies no index space.
    /// </remarks>
    internal void OnItemsMerged(BcrdtItem absorbed, BcrdtItem survivor)
    {
        if (SearchMarkers is null) return;
        foreach (BcrdtSearchMarker marker in SearchMarkers)
        {
            if (marker.Item == absorbed)
            {
                marker.Item = survivor;
                survivor.HasMarker = true;
                if (survivor.Countable && !survivor.Deleted) marker.Index -= survivor.Length;
            }
        }
    }

    /// <summary>The name this type is registered under at the document root.</summary>
    /// <exception cref="InvalidOperationException">The type is not a root type.</exception>
    public string FindRootKey()
    {
        foreach ((string name, BcrdtAbstractType type) in Doc.RootTypes)
        {
            if (ReferenceEquals(type, this)) return name;
        }
        throw new InvalidOperationException("This type is not registered as a document root.");
    }

    /// <summary>A plain-object projection of this type's contents.</summary>
    /// <remarks>
    /// Primarily an inspection and testing affordance: two replicas that have converged produce
    /// equal projections, which is the assertion most correctness tests make.
    /// </remarks>
    public abstract object ToJson();

    /// <summary>
    /// Subscribes to changes in this type only, not its descendants.
    /// </summary>
    /// <returns>A subscription; dispose it to stop receiving events.</returns>
    public IDisposable Observe(Action<BcrdtEvent, BcrdtTransaction> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _observers ??= [];
        _observers.Add(handler);
        return new Subscription(() => _observers?.Remove(handler));
    }

    /// <summary>
    /// Subscribes to changes in this type and everything nested inside it.
    /// </summary>
    /// <remarks>
    /// Handlers receive every event raised anywhere in the subtree during one transaction, ordered
    /// from the shallowest change to the deepest. Each event's <see cref="BcrdtEvent.Path"/> is
    /// relative to this type, so a handler can locate a change without knowing the tree's shape.
    /// </remarks>
    /// <returns>A subscription; dispose it to stop receiving events.</returns>
    public IDisposable ObserveDeep(Action<IReadOnlyList<BcrdtEvent>, BcrdtTransaction> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _deepObservers ??= [];
        _deepObservers.Add(handler);
        return new Subscription(() => _deepObservers?.Remove(handler));
    }

    internal bool HasObservers => _observers is { Count: > 0 };

    internal bool HasDeepObservers => _deepObservers is { Count: > 0 };

    internal void EmitShallow(BcrdtEvent crdtEvent, BcrdtTransaction transaction)
    {
        if (_observers is null || _observers.Count == 0) return;
        foreach (Action<BcrdtEvent, BcrdtTransaction> handler in _observers.ToArray())
        {
            try { handler(crdtEvent, transaction); }
            catch (Exception ex) { transaction.RecordObserverFailure(ex); }
        }
    }

    internal void EmitDeep(IReadOnlyList<BcrdtEvent> events, BcrdtTransaction transaction)
    {
        if (_deepObservers is null || _deepObservers.Count == 0) return;
        foreach (Action<IReadOnlyList<BcrdtEvent>, BcrdtTransaction> handler in _deepObservers.ToArray())
        {
            try { handler(events, transaction); }
            catch (Exception ex) { transaction.RecordObserverFailure(ex); }
        }
    }

    /// <summary>Raises this type's event and routes it to every ancestor's deep subscribers.</summary>
    internal void CallObserver(BcrdtTransaction transaction, HashSet<string?> changedKeys)
    {
        // A remote change can insert, split or delete blocks anywhere in this type, so every cached
        // index position may now be describing the wrong place. Local edits keep the cache in step
        // as they go, because they know where they are editing; an incoming update does not, and a
        // marker left pointing at a stale position silently corrupts every later index lookup.
        if (!transaction.Local || transaction.IntegratedRemoteContent) InvalidateSearchMarkers();

        BcrdtEvent crdtEvent = CreateEvent(transaction, changedKeys);

        BcrdtAbstractType? ancestor = this;
        while (ancestor is not null)
        {
            if (!transaction.ChangedParentTypes.TryGetValue(ancestor, out List<BcrdtEvent>? events))
            {
                events = [];
                transaction.ChangedParentTypes[ancestor] = events;
            }
            events.Add(crdtEvent);
            ancestor = ancestor.ContainingItem?.Parent as BcrdtAbstractType;
        }

        EmitShallow(crdtEvent, transaction);
    }

    /// <summary>
    /// The route from <paramref name="from"/> down to <paramref name="to"/>, as keys and indices.
    /// </summary>
    public static IReadOnlyList<object> ComputePath(BcrdtAbstractType from, BcrdtAbstractType to)
    {
        List<object> path = [];
        BcrdtAbstractType current = to;

        while (current.ContainingItem is { } item && !ReferenceEquals(current, from))
        {
            if (item.ParentKey is not null)
            {
                path.Insert(0, item.ParentKey);
            }
            else
            {
                int index = 0;
                BcrdtItem? cursor = (item.Parent as BcrdtAbstractType)?.Start;
                while (cursor is not null && cursor != item)
                {
                    if (!cursor.Deleted && cursor.Countable) index += cursor.Length;
                    cursor = cursor.Right;
                }
                path.Insert(0, index);
            }

            if (item.Parent is not BcrdtAbstractType parent) break;
            current = parent;
        }

        return path;
    }

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        private Action? _unsubscribe = unsubscribe;

        public void Dispose()
        {
            Interlocked.Exchange(ref _unsubscribe, null)?.Invoke();
        }
    }
}
