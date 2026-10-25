namespace Bit.Bcrdt;

/// <summary>
/// A block of collaborative content: the unit of replication, ordering and conflict resolution.
/// </summary>
/// <remarks>
/// <para>
/// Every value in a document - a map entry, an array element, a run of characters, a rich-text
/// mark, a nested type - is an item. Items form a doubly-linked list per container, and the order
/// of that list is the document order.
/// </para>
/// <para>
/// The two <i>origin</i> fields are what make this YATA rather than a simpler sequence CRDT. They
/// record the item's left and right neighbours <i>at the moment of insertion</i>, which describes
/// the interval the author meant to insert into rather than a single anchor point. When two clients
/// insert concurrently, <see cref="Integrate"/> uses that interval to keep each author's run
/// contiguous instead of interleaving them character by character - the difference between two
/// people typing "abc" and "xyz" and getting "abcxyz" rather than "axbycz".
/// </para>
/// </remarks>
public sealed class BcrdtItem : BcrdtAbstractStruct
{
    [Flags]
    private enum ItemFlags : byte
    {
        None = 0,
        Keep = 1,
        Countable = 2,
        Deleted = 4,
        Marker = 8,
    }

    private ItemFlags _flags;

    /// <summary>The item currently to the left. Mutated as neighbours are integrated.</summary>
    public BcrdtItem? Left;

    /// <summary>The item currently to the right. Mutated as neighbours are integrated.</summary>
    public BcrdtItem? Right;

    /// <summary>The identifier of the item that was to the left when this item was created.</summary>
    public BcrdtItemId? Origin;

    /// <summary>The identifier of the item that was to the right when this item was created.</summary>
    public BcrdtItemId? RightOrigin;

    /// <summary>
    /// The container. A <see cref="BcrdtAbstractType"/> once resolved; while an update is being
    /// decoded it may still be a <see cref="BcrdtItemId"/> or a root type name awaiting resolution.
    /// </summary>
    public object? Parent;

    /// <summary>The map key, when this item is a map entry rather than a sequence element.</summary>
    public string? ParentKey;

    /// <summary>The payload.</summary>
    public IBcrdtContent Content;

    /// <summary>
    /// Where this item's content was re-inserted by a later undo or redo, if anywhere.
    /// </summary>
    /// <remarks>
    /// Undo does not resurrect a deleted item; it inserts a fresh copy. This back-reference is how
    /// a subsequent redo finds that copy so it can be removed again, and how repeated undo/redo
    /// cycles stay anchored to the same logical content rather than drifting.
    /// </remarks>
    public BcrdtItemId? Redone;

    /// <summary>Creates an item.</summary>
    public BcrdtItem(
        BcrdtItemId id,
        BcrdtItem? left,
        BcrdtItemId? origin,
        BcrdtItem? right,
        BcrdtItemId? rightOrigin,
        object? parent,
        string? parentKey,
        IBcrdtContent content)
        : base(id, content.Length)
    {
        Left = left;
        Origin = origin;
        Right = right;
        RightOrigin = rightOrigin;
        Parent = parent;
        ParentKey = parentKey;
        Content = content;
        _flags = content.Countable ? ItemFlags.Countable : ItemFlags.None;
    }

    /// <inheritdoc/>
    public override bool Deleted => (_flags & ItemFlags.Deleted) != 0;

    /// <summary>Whether this item occupies a position in its container's index space.</summary>
    public bool Countable => (_flags & ItemFlags.Countable) != 0;

    /// <summary>
    /// Whether this item's content is protected from garbage collection.
    /// </summary>
    /// <remarks>
    /// Set while an undo stack or a live snapshot still needs the deleted content. Collection walks
    /// past kept items rather than reclaiming them, which is what stops "undo" from becoming
    /// "undo, unless the collector got there first".
    /// </remarks>
    public bool Keep
    {
        get => (_flags & ItemFlags.Keep) != 0;
        set => _flags = value ? _flags | ItemFlags.Keep : _flags & ~ItemFlags.Keep;
    }

    /// <summary>
    /// Whether a cached index marker currently points at this item.
    /// </summary>
    /// <remarks>
    /// Sequential access caches are what keep editing a large document cheap, and two markers
    /// pointing at the same item would drift apart as edits land. This flag lets the cache detect
    /// and drop the duplicate instead.
    /// </remarks>
    public bool HasMarker
    {
        get => (_flags & ItemFlags.Marker) != 0;
        set => _flags = value ? _flags | ItemFlags.Marker : _flags & ~ItemFlags.Marker;
    }

    /// <summary>The resolved parent type, or <see langword="null"/> if not yet resolved.</summary>
    public BcrdtAbstractType? ParentType => Parent as BcrdtAbstractType;

    /// <summary>Marks the item deleted without recording it in a transaction.</summary>
    public void MarkDeleted() => _flags |= ItemFlags.Deleted;

    /// <summary>The next item to the right that is visible, skipping deleted ones.</summary>
    public BcrdtItem? Next
    {
        get
        {
            BcrdtItem? next = Right;
            while (next is not null && next.Deleted) next = next.Right;
            return next;
        }
    }

    /// <summary>The next item to the left that is visible, skipping deleted ones.</summary>
    public BcrdtItem? Previous
    {
        get
        {
            BcrdtItem? previous = Left;
            while (previous is not null && previous.Deleted) previous = previous.Left;
            return previous;
        }
    }

    /// <summary>
    /// Places this item into the document, resolving concurrent insertions deterministically.
    /// </summary>
    /// <param name="transaction">The transaction the integration happens in.</param>
    /// <param name="offset">
    /// How many leading logical units the document already has. Non-zero when a peer sends a block
    /// that partially overlaps content we already hold; the overlap is trimmed off the front.
    /// </param>
    /// <remarks>
    /// <para>
    /// The interesting case is when the item's recorded neighbours no longer sit next to each other,
    /// which means other items were inserted into the same interval concurrently. The scan below
    /// walks that interval and decides, for each competing item, whether this item belongs before or
    /// after it. The decision uses only data every replica has - origins and client identifiers -
    /// so every replica reaches the same answer regardless of the order the items arrived in. That
    /// is the whole convergence argument, and it is why nothing here may consult wall-clock time,
    /// arrival order or local state.
    /// </para>
    /// </remarks>
    public override void Integrate(BcrdtTransaction transaction, int offset)
    {
        if (offset > 0)
        {
            Id = new BcrdtItemId(Id.Client, Id.Clock + offset);
            BcrdtAbstractStruct left = BcrdtStructStore.GetStructCleanEnd(transaction, new BcrdtItemId(Id.Client, Id.Clock - 1));
            Origin = left.LastId;
            Content = Content.Splice(offset);
            Length -= offset;

            if (left is BcrdtItem leftItem)
            {
                Left = leftItem;
            }
            else
            {
                // The overlap was collected here, which only happens once its container is gone. The
                // remainder lived in that same container, so it is reduced to a clock range below.
                Left = null;
                Parent = null;
            }
        }

        if (Parent is not BcrdtAbstractType parent)
        {
            // The container was garbage collected before this item arrived, so the item has nowhere
            // to live. Keep its clock range so causality still resolves and drop the payload.
            new BcrdtGarbageCollected(Id, Length).Integrate(transaction, 0);
            return;
        }

        if (Content is BcrdtContentType && parent.NestingDepth >= BcrdtAbstractType.MaxNestingDepth)
        {
            // Being created by this replica: refused before this block is placed. Blocks already placed
            // earlier in the same transaction - the outer levels of a detached tree being inserted - stay.
            if (!transaction.IntegratingRemoteBlocks)
            {
                throw new InvalidOperationException(
                    $"Collaborative types cannot be nested more than {BcrdtAbstractType.MaxNestingDepth} levels deep.");
            }

            // From a peer: dropped like content whose container is gone. Everything inside it then
            // finds no container either and goes the same way, on every replica alike.
            new BcrdtGarbageCollected(Id, Length).Integrate(transaction, 0);
            return;
        }

        bool neighboursMoved =
            (Left is null && (Right is null || Right.Left is not null)) ||
            (Left is not null && Left.Right != Right);

        if (neighboursMoved)
        {
            Left = ResolveConcurrentInsertPosition(transaction, parent);
        }

        // Splice into the linked list.
        if (Left is not null)
        {
            Right = Left.Right;
            Left.Right = this;
        }
        else
        {
            BcrdtItem? right;
            if (ParentKey is not null)
            {
                right = parent.MapGetRaw(ParentKey);
                while (right is not null && right.Left is not null) right = right.Left;
            }
            else
            {
                right = parent.Start;
                parent.Start = this;
            }
            Right = right;
        }

        if (Right is not null)
        {
            Right.Left = this;
        }
        else if (ParentKey is not null)
        {
            // Map entries chain left-to-right in integration order and the rightmost wins. Having no
            // right neighbour means this write is the current value for the key, so the previous
            // current value is superseded.
            parent.MapSetRaw(ParentKey, this);
            Left?.Delete(transaction);
        }

        if (ParentKey is null && Countable && !Deleted)
        {
            parent.Length += Length;
        }

        transaction.Doc.Store.AddStruct(this);
        Content.Integrate(transaction, this);
        transaction.AddChangedType(parent, ParentKey);

        bool parentIsDeleted = parent.ContainingItem is { Deleted: true };
        bool supersededMapWrite = ParentKey is not null && Right is not null;
        if (parentIsDeleted || supersededMapWrite)
        {
            Delete(transaction);
        }
    }

    /// <summary>
    /// Walks the interval this item was inserted into and picks its position among competitors.
    /// </summary>
    private BcrdtItem? ResolveConcurrentInsertPosition(BcrdtTransaction transaction, BcrdtAbstractType parent)
    {
        BcrdtItem? left = Left;
        BcrdtItem? candidate;

        if (left is not null)
        {
            candidate = left.Right;
        }
        else if (ParentKey is not null)
        {
            candidate = parent.MapGetRaw(ParentKey);
            while (candidate is not null && candidate.Left is not null) candidate = candidate.Left;
        }
        else
        {
            candidate = parent.Start;
        }

        // Items we have walked past, and the subset of those we are currently losing to. The two
        // sets are what distinguish "this competitor was inserted before my origin" (walk past it)
        // from "this competitor is a genuine concurrent peer" (compare identifiers).
        var seen = new HashSet<BcrdtItem>();
        var conflicting = new HashSet<BcrdtItem>();
        BcrdtStructStore store = transaction.Doc.Store;

        while (candidate is not null && candidate != Right)
        {
            seen.Add(candidate);
            conflicting.Add(candidate);

            if (Nullable.Equals(Origin, candidate.Origin))
            {
                // A genuine tie: both items were inserted into the same interval. Break it by client
                // identifier. The comparison is arbitrary but total and known to every replica,
                // which is all convergence requires.
                if (candidate.Id.Client < Id.Client)
                {
                    left = candidate;
                    conflicting.Clear();
                }
                else if (Nullable.Equals(RightOrigin, candidate.RightOrigin))
                {
                    break;
                }
            }
            else if (candidate.Origin is BcrdtItemId candidateOrigin &&
                     store.TryFind(candidateOrigin, out BcrdtAbstractStruct? originStruct) &&
                     originStruct is BcrdtItem originItem &&
                     seen.Contains(originItem))
            {
                // The competitor was anchored to something inside the interval we have already
                // walked, so it is transitively to our left unless we are already losing to it.
                if (!conflicting.Contains(originItem))
                {
                    left = candidate;
                    conflicting.Clear();
                }
            }
            else
            {
                break;
            }

            candidate = candidate.Right;
        }

        return left;
    }

    /// <summary>
    /// Checks whether everything this item depends on is present, and resolves its links if so.
    /// </summary>
    /// <returns>
    /// The client whose content is still missing, or <see langword="null"/> if the item is ready to
    /// integrate.
    /// </returns>
    /// <remarks>
    /// <para>
    /// An item names its neighbours and its container by identifier, and it cannot be placed until
    /// those identifiers resolve. Reporting <i>which</i> client is missing - rather than just
    /// "not ready" - lets the decoder jump straight to that client's blocks in the same update
    /// instead of blindly retrying, which is what keeps applying a large multi-client update linear
    /// rather than quadratic.
    /// </para>
    /// <para>
    /// A neighbour that turns out to have been garbage collected means the container is gone too,
    /// so the item is marked parentless and will be reduced to a bare clock range on integration.
    /// </para>
    /// </remarks>
    public ulong? GetMissing(BcrdtTransaction transaction, BcrdtStructStore store)
    {
        if (Origin is { } origin && origin.Client != Id.Client && origin.Clock >= store.GetState(origin.Client))
        {
            return origin.Client;
        }
        if (RightOrigin is { } rightOrigin &&
            rightOrigin.Client != Id.Client &&
            rightOrigin.Clock >= store.GetState(rightOrigin.Client))
        {
            return rightOrigin.Client;
        }
        if (Parent is BcrdtItemId parentId &&
            parentId.Client != Id.Client &&
            parentId.Clock >= store.GetState(parentId.Client))
        {
            return parentId.Client;
        }

        bool neighbourCollected = false;

        if (Origin is { } resolvedOrigin)
        {
            BcrdtAbstractStruct resolved = BcrdtStructStore.GetStructCleanEnd(transaction, resolvedOrigin);
            if (resolved is BcrdtItem leftItem)
            {
                Left = leftItem;
                Origin = leftItem.LastId;
            }
            else
            {
                neighbourCollected = true;
            }
        }
        if (RightOrigin is { } resolvedRightOrigin)
        {
            BcrdtAbstractStruct resolved = BcrdtStructStore.GetStructCleanStart(transaction, resolvedRightOrigin);
            if (resolved is BcrdtItem rightItem)
            {
                Right = rightItem;
                RightOrigin = rightItem.Id;
            }
            else
            {
                neighbourCollected = true;
            }
        }

        if (neighbourCollected)
        {
            Parent = null;
        }
        else if (Parent is null)
        {
            // Derive the container from whichever neighbour resolved. Both agree for any genuine
            // item, because they were adjacent when it was created; preferring the left one, as the
            // reference algorithm does, keeps even a malformed item's placement identical on every
            // replica.
            if (Left is not null)
            {
                Parent = Left.Parent;
                ParentKey = Left.ParentKey;
            }
            else if (Right is not null)
            {
                Parent = Right.Parent;
                ParentKey = Right.ParentKey;
            }
        }
        else if (Parent is BcrdtItemId containerId)
        {
            BcrdtAbstractStruct container = store.Find(containerId);
            Parent = container is BcrdtItem { Content: BcrdtContentType nested } ? nested.Type : null;
        }

        return null;
    }

    /// <summary>Marks the item deleted and records the deletion in the transaction.</summary>
    public void Delete(BcrdtTransaction transaction)
    {
        if (Deleted) return;

        if (Parent is BcrdtAbstractType parent)
        {
            if (Countable && ParentKey is null) parent.Length -= Length;
            MarkDeleted();
            transaction.DeleteSet.Add(Id.Client, Id.Clock, Length);
            transaction.AddChangedType(parent, ParentKey);
            Content.Delete(transaction);
        }
        else
        {
            MarkDeleted();
            transaction.DeleteSet.Add(Id.Client, Id.Clock, Length);
        }
    }

    /// <summary>Reclaims this deleted item's payload.</summary>
    /// <param name="store">The store holding the item.</param>
    /// <param name="parentCollected">
    /// Whether the container was collected too. If so the item is replaced by a bare clock range;
    /// otherwise a shell is left behind so neighbouring items stay linked.
    /// </param>
    public void Collect(BcrdtStructStore store, bool parentCollected)
    {
        if (!Deleted) throw new InvalidOperationException("Only deleted items can be collected.");

        Content.Collect(store);
        if (parentCollected)
        {
            store.ReplaceStruct(this, new BcrdtGarbageCollected(Id, Length));
        }
        else
        {
            Content = new BcrdtContentDeleted(Length);
        }
    }

    /// <inheritdoc/>
    public override bool TryMergeWith(BcrdtAbstractStruct right)
    {
        if (right is not BcrdtItem other) return false;

        // Two items merge only when the result would be indistinguishable from them: same author,
        // adjacent clocks, physically adjacent, same deleted state, same right anchor, and neither
        // referenced by an undo stack. Anything less and a later split could not reproduce them.
        if (Right != other ||
            !FitsOneBlock(Length, other.Length) ||
            Id.Client != other.Id.Client ||
            Id.Clock + Length != other.Id.Clock ||
            Deleted != other.Deleted ||
            Redone is not null || other.Redone is not null ||
            !Nullable.Equals(other.Origin, LastId) ||
            !Nullable.Equals(RightOrigin, other.RightOrigin) ||
            Content.GetType() != other.Content.GetType())
        {
            return false;
        }

        if (!Content.TryMergeWith(other.Content)) return false;

        (Parent as BcrdtAbstractType)?.OnItemsMerged(other, this);

        if (other.Keep) Keep = true;
        Right = other.Right;
        if (Right is not null) Right.Left = this;
        Length += other.Length;
        return true;
    }

    /// <inheritdoc/>
    public override void Write(ref BcrdtBufferWriter writer, int offset)
    {
        // When only part of the item is being sent, the receiver's left anchor is the unit just
        // before the slice, not the item's original origin.
        BcrdtItemId? origin = offset > 0 ? new BcrdtItemId(Id.Client, Id.Clock + offset - 1) : Origin;

        byte info = (byte)((byte)Content.Ref & 0x1F);
        if (origin.HasValue) info |= 0x80;
        if (RightOrigin.HasValue) info |= 0x40;
        if (ParentKey is not null) info |= 0x20;
        writer.WriteByte(info);

        if (origin.HasValue) origin.Value.Write(ref writer);
        if (RightOrigin.HasValue) RightOrigin.Value.Write(ref writer);

        if (!origin.HasValue && !RightOrigin.HasValue)
        {
            // The parent is only written when it cannot be derived from a neighbour, which for a
            // typical document is a handful of items out of thousands.
            switch (Parent)
            {
                // An unresolved parent, which happens when an update is being rewritten without a
                // document to resolve names against - during a merge or a compaction.
                case string rootName:
                    writer.WriteByte(1);
                    writer.WriteString(rootName);
                    break;

                case BcrdtItemId parentId:
                    writer.WriteByte(0);
                    parentId.Write(ref writer);
                    break;

                case BcrdtAbstractType { ContainingItem: { } parentItem }:
                    writer.WriteByte(0);
                    parentItem.Id.Write(ref writer);
                    break;

                case BcrdtAbstractType rootType:
                    writer.WriteByte(1);
                    writer.WriteString(rootType.FindRootKey());
                    break;

                default:
                    throw new InvalidOperationException($"BcrdtItem {Id} has no parent to encode.");
            }

            // The map key only needs writing alongside the parent. Once there is an origin, the
            // receiver derives both from the neighbour, which is the common case for every write to
            // a key after the first.
            if (ParentKey is not null) writer.WriteString(ParentKey);
        }

        Content.Write(ref writer, offset);
    }

    /// <inheritdoc/>
    public override string ToString() =>
        $"BcrdtItem({Id}, len={Length}{(Deleted ? ", deleted" : "")}, {Content.Ref})";
}
