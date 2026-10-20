namespace Bit.Bcrdt;

/// <summary>How a map key changed within a transaction.</summary>
public enum BcrdtKeyChangeAction
{
    /// <summary>The key did not exist before and does now.</summary>
    Add,
    /// <summary>The key existed before and has a different value now.</summary>
    Update,
    /// <summary>The key existed before and does not now.</summary>
    Delete,
}

/// <summary>A single key's change within a transaction.</summary>
/// <param name="Action">Whether the key was added, updated or removed.</param>
/// <param name="OldValue">The value before the transaction, if there was one.</param>
/// <param name="NewValue">The value after the transaction, if there is one.</param>
public readonly record struct BcrdtKeyChange(BcrdtKeyChangeAction Action, object? OldValue, object? NewValue);

/// <summary>
/// What changed in one collaborative type during one transaction.
/// </summary>
/// <remarks>
/// <para>
/// Events describe the net effect of a whole transaction, not individual mutations. Pasting five
/// hundred rows raises one event with one delta, so a subscriber renders once rather than five
/// hundred times. Events are raised after the transaction has fully settled, so the document is
/// always in a consistent state when a handler runs.
/// </para>
/// <para>
/// The change summary is computed lazily. A subscriber that only cares that something changed pays
/// nothing for the delta it never reads.
/// </para>
/// </remarks>
public abstract class BcrdtEvent
{
    private IReadOnlyList<BcrdtDeltaOperation>? _delta;
    private IReadOnlyDictionary<string, BcrdtKeyChange>? _keys;

    /// <summary>Creates an event for a type.</summary>
    protected BcrdtEvent(BcrdtAbstractType target, BcrdtTransaction transaction)
    {
        Target = target;
        CurrentTarget = target;
        Transaction = transaction;
    }

    /// <summary>The type that actually changed.</summary>
    public BcrdtAbstractType Target { get; }

    /// <summary>
    /// The type whose subscriber is currently being notified.
    /// </summary>
    /// <remarks>
    /// For a shallow subscription this is the same as <see cref="Target"/>. For a deep subscription
    /// on an ancestor it is that ancestor, which is what makes <see cref="Path"/> meaningful - the
    /// path is relative to the observer, so a handler can locate the change within its own subtree.
    /// </remarks>
    public BcrdtAbstractType CurrentTarget { get; internal set; }

    /// <summary>The transaction that produced the change.</summary>
    public BcrdtTransaction Transaction { get; }

    /// <summary>
    /// Where <see cref="Target"/> sits relative to <see cref="CurrentTarget"/>, as map keys and
    /// array indices.
    /// </summary>
    public IReadOnlyList<object> Path => BcrdtAbstractType.ComputePath(CurrentTarget, Target);

    /// <summary>The change expressed as a delta: a sequence delta, or a rich-text delta for text.</summary>
    /// <exception cref="InvalidOperationException">
    /// Read for the first time after the transaction finished settling. See <see cref="Keys"/>.
    /// </exception>
    public IReadOnlyList<BcrdtDeltaOperation> Delta
    {
        get
        {
            if (_delta is null)
            {
                EnsureComputable();
                _delta = ComputeDelta();
            }
            return _delta;
        }
    }

    /// <summary>The change expressed as per-key map changes.</summary>
    /// <exception cref="InvalidOperationException">
    /// Read for the first time after the transaction finished settling.
    /// </exception>
    /// <remarks>
    /// Change summaries are derived lazily from the blocks the transaction touched. Once the
    /// transaction has settled those blocks are reclaimed and compacted, so a summary first read
    /// later - from a deferred callback, say - would describe the wrong thing. Read what you need
    /// inside the handler.
    /// </remarks>
    public IReadOnlyDictionary<string, BcrdtKeyChange> Keys
    {
        get
        {
            if (_keys is null)
            {
                EnsureComputable();
                _keys = ComputeKeys();
            }
            return _keys;
        }
    }

    private void EnsureComputable()
    {
        if (Transaction.ObserversCompleted)
        {
            throw new InvalidOperationException(
                "An event's change summary must first be read while the event is being handled. The " +
                "transaction has settled and the blocks it describes may already have been reclaimed.");
        }
    }

    /// <summary>Whether the transaction created this content.</summary>
    protected internal bool WasAdded(BcrdtItem item) =>
        item.Id.Clock >= Transaction.BeforeState.Get(item.Id.Client);

    /// <summary>Whether the transaction deleted this content.</summary>
    protected internal bool WasDeleted(BcrdtItem item) => Transaction.DeleteSet.IsDeleted(item.Id);

    /// <summary>
    /// Walks the target's sequence once, folding every block into retain, insert or delete steps.
    /// </summary>
    /// <remarks>
    /// Content both created and deleted within the same transaction produces no step at all: from a
    /// subscriber's point of view it never existed. That is why the two predicates are checked
    /// together rather than independently. Types whose sequence carries more than plain elements -
    /// rich text - override this.
    /// </remarks>
    protected virtual IReadOnlyList<BcrdtDeltaOperation> ComputeDelta()
    {
        if (!Transaction.Changed.TryGetValue(Target, out HashSet<string?>? subs) || !subs.Contains(null))
        {
            return [];
        }

        List<BcrdtDeltaOperation> delta = [];
        BcrdtDeltaOperation? pending = null;
        List<object?>? insertBuffer = null;

        void Flush()
        {
            if (pending is null) return;
            if (pending.Kind == BcrdtDeltaKind.Insert && insertBuffer is not null)
            {
                pending.Content = insertBuffer;
                pending.Length = insertBuffer.Count;
                insertBuffer = null;
            }
            delta.Add(pending);
            pending = null;
        }

        for (BcrdtItem? item = Target.Start; item is not null; item = item.Right)
        {
            if (!item.Countable) continue;

            if (item.Deleted)
            {
                if (!WasDeleted(item) || WasAdded(item)) continue;
                if (pending is null || pending.Kind != BcrdtDeltaKind.Delete)
                {
                    Flush();
                    pending = BcrdtDeltaOperation.Delete(0);
                }
                pending.Length += item.Length;
            }
            else if (WasAdded(item))
            {
                if (pending is null || pending.Kind != BcrdtDeltaKind.Insert)
                {
                    Flush();
                    pending = BcrdtDeltaOperation.Insert(null, 0);
                    insertBuffer = [];
                }
                insertBuffer!.AddRange(item.Content.GetContent());
            }
            else
            {
                if (pending is null || pending.Kind != BcrdtDeltaKind.Retain)
                {
                    Flush();
                    pending = BcrdtDeltaOperation.Retain(0);
                }
                pending.Length += item.Length;
            }
        }

        // A trailing retain describes nothing: everything after the last real change is unchanged by
        // definition, so emitting it would only make deltas noisier to consume.
        if (pending is not null && pending.Kind != BcrdtDeltaKind.Retain) Flush();

        return delta;
    }

    /// <summary>
    /// Reconstructs, for each touched key, whether it was added, updated or removed and what its
    /// previous value was.
    /// </summary>
    /// <remarks>
    /// A map key's history is a chain of items; the current value is the rightmost. Recovering the
    /// previous value means walking left past everything this transaction created until reaching an
    /// item that predates it. Keys whose entire chain was created and deleted within the transaction
    /// are omitted, because no subscriber ever observed them.
    /// </remarks>
    private IReadOnlyDictionary<string, BcrdtKeyChange> ComputeKeys()
    {
        if (!Transaction.Changed.TryGetValue(Target, out HashSet<string?>? subs)) return
            new Dictionary<string, BcrdtKeyChange>(StringComparer.Ordinal);

        var keys = new Dictionary<string, BcrdtKeyChange>(StringComparer.Ordinal);
        foreach (string? key in subs)
        {
            if (key is null) continue;

            BcrdtItem? item = Target.MapGetRaw(key);
            if (item is null) continue;

            BcrdtKeyChangeAction action;
            object? oldValue = null;

            if (WasAdded(item))
            {
                BcrdtItem? previous = item.Left;
                while (previous is not null && WasAdded(previous)) previous = previous.Left;

                if (WasDeleted(item))
                {
                    if (previous is not null && WasDeleted(previous))
                    {
                        action = BcrdtKeyChangeAction.Delete;
                        oldValue = LastValue(previous);
                    }
                    else
                    {
                        continue;
                    }
                }
                else if (previous is not null && WasDeleted(previous))
                {
                    action = BcrdtKeyChangeAction.Update;
                    oldValue = LastValue(previous);
                }
                else
                {
                    action = BcrdtKeyChangeAction.Add;
                }
            }
            else if (WasDeleted(item))
            {
                action = BcrdtKeyChangeAction.Delete;
                oldValue = LastValue(item);
            }
            else
            {
                continue;
            }

            object? newValue = action == BcrdtKeyChangeAction.Delete ? null : LastValue(item);
            keys[key] = new BcrdtKeyChange(action, oldValue, newValue);
        }
        return keys;
    }

    private static object? LastValue(BcrdtItem item)
    {
        IReadOnlyList<object?> content = item.Content.GetContent();
        return content.Count == 0 ? null : content[^1];
    }
}
