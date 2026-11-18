namespace Bit.Bcrdt;

/// <summary>What changed in a <see cref="BcrdtXmlFragment"/> or <see cref="BcrdtXmlElement"/> during one transaction.</summary>
/// <remarks>
/// <see cref="BcrdtEvent.Delta"/> describes the child list - retains, inserted nodes and deletions - in
/// the same child indices the container exposes, so an entry the container ignores (see
/// <see cref="BcrdtXmlContainer"/>) never shifts it. <see cref="BcrdtEvent.Keys"/> describes an element's
/// attributes. A change inside a child - typing into a text, an attribute of a nested element - is that
/// child's own event; observe the tree with <see cref="BcrdtAbstractType.ObserveDeep"/> to receive all of
/// them in one call.
/// </remarks>
public sealed class BcrdtXmlEvent(BcrdtXmlContainer target, BcrdtTransaction transaction) : BcrdtEvent(target, transaction)
{
    /// <summary>The fragment or element that changed.</summary>
    public new BcrdtXmlContainer Target => (BcrdtXmlContainer)base.Target;

    /// <summary>Whether child nodes were inserted or removed.</summary>
    public bool ChildListChanged => Delta.Count > 0;

    /// <summary>The names of the attributes that were added, changed or removed.</summary>
    /// <remarks>Read it while handling the event, like <see cref="BcrdtEvent.Keys"/>.</remarks>
    public IReadOnlyCollection<string> AttributesChanged =>
    [
        .. Keys
            .Where(change => BcrdtXmlSyntax.IsValidName(change.Key) &&
                (BcrdtXmlElement.IsAttributeValue(change.Value.OldValue) || BcrdtXmlElement.IsAttributeValue(change.Value.NewValue)))
            .Select(change => change.Key)
            .Order(StringComparer.Ordinal),
    ];

    /// <summary>The child-list delta, counting child nodes only.</summary>
    protected override IReadOnlyList<BcrdtDeltaOperation> ComputeDelta()
    {
        if (!Transaction.Changed.TryGetValue(base.Target, out HashSet<string?>? subs) || !subs.Contains(null))
        {
            return [];
        }

        List<BcrdtDeltaOperation> delta = [];
        BcrdtDeltaOperation? pending = null;
        List<object?>? inserted = null;

        void Flush()
        {
            if (pending is null) return;
            if (pending.Kind == BcrdtDeltaKind.Insert && inserted is not null)
            {
                pending.Content = inserted;
                pending.Length = inserted.Count;
                inserted = null;
            }
            delta.Add(pending);
            pending = null;
        }

        void Step(BcrdtDeltaKind kind)
        {
            if (pending?.Kind == kind) return;
            Flush();
            pending = kind switch
            {
                BcrdtDeltaKind.Insert => BcrdtDeltaOperation.Insert(null, 0),
                BcrdtDeltaKind.Delete => BcrdtDeltaOperation.Delete(0),
                _ => BcrdtDeltaOperation.Retain(0),
            };
            if (kind == BcrdtDeltaKind.Insert) inserted = [];
        }

        for (BcrdtItem? item = base.Target.Start; item is not null; item = item.Right)
        {
            // A node keeps its content until the transaction that deleted it has settled, so a node
            // deleted here is still recognisable as one.
            if (!BcrdtXmlContainer.IsNodeBlock(item)) continue;

            if (item.Deleted)
            {
                if (!WasDeleted(item) || WasAdded(item)) continue;
                Step(BcrdtDeltaKind.Delete);
                pending!.Length += 1;
            }
            else if (WasAdded(item))
            {
                Step(BcrdtDeltaKind.Insert);
                inserted!.Add(((BcrdtContentType)item.Content).Type);
            }
            else
            {
                Step(BcrdtDeltaKind.Retain);
                pending!.Length += 1;
            }
        }

        // Everything after the last real change is unchanged by definition.
        if (pending is not null && pending.Kind != BcrdtDeltaKind.Retain) Flush();
        return delta;
    }
}
