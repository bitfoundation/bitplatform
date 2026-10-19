namespace Bit.Bcrdt;

/// <summary>
/// The document's block storage: for each client, that client's blocks in clock order.
/// </summary>
/// <remarks>
/// <para>
/// Blocks are grouped by author and stored contiguously, which gives three properties the rest of
/// the engine depends on. Locating an identifier is a binary search rather than a list walk.
/// Deciding whether the document already holds a block is one integer comparison against the last
/// entry. And computing the delta a peer is missing is a slice of each client's list rather than a
/// scan of the document.
/// </para>
/// <para>
/// The contiguity invariant - block <c>n+1</c> starts exactly where block <c>n</c> ends - is
/// enforced on insert. A violation means a causal dependency was skipped, which would silently
/// corrupt every later lookup, so it fails loudly instead.
/// </para>
/// </remarks>
public sealed class BcrdtStructStore
{
    private readonly Dictionary<ulong, List<BcrdtAbstractStruct>> _clients = [];

    /// <summary>The per-client block lists.</summary>
    public IReadOnlyDictionary<ulong, List<BcrdtAbstractStruct>> Clients => _clients;

    /// <summary>
    /// Blocks received but not yet integrable, held verbatim until their dependencies arrive.
    /// </summary>
    /// <remarks>
    /// An operation-based CRDT can only integrate a block once everything it references is present.
    /// Rather than dropping such a block - which would lose it permanently on an unreliable
    /// transport - it is parked here with a record of what it is waiting for and retried on every
    /// subsequent update.
    /// </remarks>
    internal byte[]? PendingUpdate { get; set; }

    /// <summary>What the parked update is waiting for.</summary>
    internal BcrdtStateVector? PendingMissing { get; set; }

    /// <summary>Deletions that referenced content we had not yet received.</summary>
    /// <remarks>
    /// A peer can legitimately tell us "this is deleted" before we have the thing being deleted, if
    /// the two facts travelled by different routes. Holding the deletion means the content does not
    /// briefly appear and then vanish once both messages land.
    /// </remarks>
    internal BcrdtDeleteSet? PendingDeleteSet { get; set; }

    /// <summary>The blocks written by one client, or <see langword="null"/> if that client is unknown.</summary>
    public List<BcrdtAbstractStruct>? GetClientStructs(ulong client) =>
        _clients.TryGetValue(client, out List<BcrdtAbstractStruct>? structs) ? structs : null;

    /// <summary>The next clock expected from a client.</summary>
    public long GetState(ulong client)
    {
        if (!_clients.TryGetValue(client, out List<BcrdtAbstractStruct>? structs) || structs.Count == 0) return 0;
        BcrdtAbstractStruct last = structs[^1];
        return last.Id.Clock + last.Length;
    }

    /// <summary>A snapshot of everything this store holds.</summary>
    public BcrdtStateVector GetStateVector()
    {
        var vector = new BcrdtStateVector(_clients.Count);
        foreach ((ulong client, List<BcrdtAbstractStruct> structs) in _clients)
        {
            if (structs.Count == 0) continue;
            BcrdtAbstractStruct last = structs[^1];
            vector.Set(client, last.Id.Clock + last.Length);
        }
        return vector;
    }

    /// <summary>Appends a block, which must start exactly where the client's last block ended.</summary>
    public void AddStruct(BcrdtAbstractStruct value)
    {
        if (!_clients.TryGetValue(value.Id.Client, out List<BcrdtAbstractStruct>? structs))
        {
            structs = [];
            _clients[value.Id.Client] = structs;
        }
        else if (structs.Count > 0)
        {
            BcrdtAbstractStruct last = structs[^1];
            if (last.Id.Clock + last.Length != value.Id.Clock)
            {
                throw new InvalidOperationException(
                    $"Non-contiguous insert for client {value.Id.Client}: expected clock " +
                    $"{last.Id.Clock + last.Length} but got {value.Id.Clock}. A causal dependency was skipped.");
            }
        }
        structs.Add(value);
    }

    /// <summary>Finds the block covering an identifier.</summary>
    /// <exception cref="InvalidOperationException">No block covers that identifier.</exception>
    public BcrdtAbstractStruct Find(BcrdtItemId id)
    {
        if (!TryFind(id, out BcrdtAbstractStruct? result))
        {
            throw new InvalidOperationException($"No block covers {id}.");
        }
        return result;
    }

    /// <summary>Finds the block covering an identifier, if the store has it.</summary>
    public bool TryFind(BcrdtItemId id, [NotNullWhen(true)] out BcrdtAbstractStruct? result)
    {
        result = null;
        if (!_clients.TryGetValue(id.Client, out List<BcrdtAbstractStruct>? structs)) return false;
        int index = TryFindIndex(structs, id.Clock);
        if (index < 0) return false;
        result = structs[index];
        return true;
    }

    /// <summary>Finds the item covering an identifier.</summary>
    public BcrdtItem FindItem(BcrdtItemId id) =>
        Find(id) as BcrdtItem ?? throw new InvalidOperationException($"Block at {id} is not an item.");

    /// <summary>
    /// Binary-searches a client's blocks for the one covering <paramref name="clock"/>.
    /// </summary>
    public static int FindIndex(List<BcrdtAbstractStruct> structs, long clock)
    {
        int index = TryFindIndex(structs, clock);
        if (index < 0) throw new InvalidOperationException($"No block covers clock {clock}.");
        return index;
    }

    private static int TryFindIndex(List<BcrdtAbstractStruct> structs, long clock)
    {
        int low = 0;
        int high = structs.Count - 1;
        while (low <= high)
        {
            int mid = (int)(((uint)low + (uint)high) >> 1);
            BcrdtAbstractStruct candidate = structs[mid];
            if (clock < candidate.Id.Clock)
            {
                high = mid - 1;
            }
            else if (clock < candidate.Id.Clock + candidate.Length)
            {
                return mid;
            }
            else
            {
                low = mid + 1;
            }
        }
        return -1;
    }

    /// <summary>
    /// Returns the item that <i>starts</i> at an identifier, splitting a block if necessary.
    /// </summary>
    /// <remarks>
    /// Callers that need to address the interior of a run-length-encoded block - an incoming item
    /// whose origin points into the middle of a local block, say - use this to make that position a
    /// real boundary first.
    /// </remarks>
    public static BcrdtItem GetItemCleanStart(BcrdtTransaction transaction, BcrdtItemId id) =>
        (BcrdtItem)GetStructCleanStart(transaction, id);

    /// <summary>
    /// As <see cref="GetItemCleanStart"/>, but tolerating a range that has been garbage collected.
    /// </summary>
    /// <remarks>
    /// Callers resolving an identifier that arrived from a peer must use this: the peer may be
    /// anchored to content this replica has already reclaimed, and that is a legitimate state rather
    /// than an error.
    /// </remarks>
    public static BcrdtAbstractStruct GetStructCleanStart(BcrdtTransaction transaction, BcrdtItemId id)
    {
        List<BcrdtAbstractStruct> structs = transaction.Doc.Store.GetClientStructs(id.Client)
            ?? throw new InvalidOperationException($"Unknown client {id.Client}.");
        return structs[FindIndexCleanStart(transaction, structs, id.Clock)];
    }

    internal static int FindIndexCleanStart(BcrdtTransaction transaction, List<BcrdtAbstractStruct> structs, long clock)
    {
        int index = FindIndex(structs, clock);
        BcrdtAbstractStruct candidate = structs[index];
        if (candidate.Id.Clock < clock && candidate is BcrdtItem item)
        {
            structs.Insert(index + 1, Split(transaction, item, (int)(clock - item.Id.Clock)));
            return index + 1;
        }
        return index;
    }

    /// <summary>
    /// Returns the item that <i>ends</i> at an identifier, splitting a block if necessary.
    /// </summary>
    public static BcrdtItem GetItemCleanEnd(BcrdtTransaction transaction, BcrdtItemId id) =>
        (BcrdtItem)GetStructCleanEnd(transaction, id);

    /// <summary>
    /// As <see cref="GetItemCleanEnd"/>, but tolerating a range that has been garbage collected.
    /// </summary>
    public static BcrdtAbstractStruct GetStructCleanEnd(BcrdtTransaction transaction, BcrdtItemId id)
    {
        List<BcrdtAbstractStruct> structs = transaction.Doc.Store.GetClientStructs(id.Client)
            ?? throw new InvalidOperationException($"Unknown client {id.Client}.");
        int index = FindIndex(structs, id.Clock);
        BcrdtAbstractStruct candidate = structs[index];
        if (id.Clock != candidate.Id.Clock + candidate.Length - 1 && candidate is BcrdtItem item)
        {
            structs.Insert(index + 1, Split(transaction, item, (int)(id.Clock - item.Id.Clock + 1)));
        }
        return candidate;
    }

    /// <summary>
    /// Splits a block into two at <paramref name="offset"/>, returning the new right half.
    /// </summary>
    /// <remarks>
    /// Splitting is what lets run-length encoding coexist with arbitrary concurrent edits: the block
    /// stays one object until somebody needs to address its interior, and the halves can merge back
    /// together later. Because an identifier addresses a logical unit rather than a block, nothing
    /// that pointed into the original block is invalidated by the split.
    /// </remarks>
    public static BcrdtItem Split(BcrdtTransaction transaction, BcrdtItem left, int offset)
    {
        var right = new BcrdtItem(
            new BcrdtItemId(left.Id.Client, left.Id.Clock + offset),
            left,
            new BcrdtItemId(left.Id.Client, left.Id.Clock + offset - 1),
            left.Right,
            left.RightOrigin,
            left.Parent,
            left.ParentKey,
            left.Content.Splice(offset));

        if (left.Deleted) right.MarkDeleted();
        if (left.Keep) right.Keep = true;
        if (left.Redone is { } redone) right.Redone = new BcrdtItemId(redone.Client, redone.Clock + offset);

        left.Right = right;
        if (right.Right is not null) right.Right.Left = right;

        // The halves are candidates to merge back together once the transaction settles.
        transaction.MergeStructs.Add(right);

        if (right.ParentKey is not null && right.Right is null)
        {
            (right.Parent as BcrdtAbstractType)?.MapSetRaw(right.ParentKey, right);
        }

        left.Length = offset;
        return right;
    }

    /// <summary>Swaps a block for another covering the same clock range.</summary>
    public void ReplaceStruct(BcrdtAbstractStruct old, BcrdtAbstractStruct replacement)
    {
        List<BcrdtAbstractStruct> structs = _clients[old.Id.Client];
        structs[FindIndex(structs, old.Id.Clock)] = replacement;
    }

    /// <summary>
    /// Invokes <paramref name="action"/> for every block overlapping a clock range, splitting blocks
    /// at the range boundaries so the callback sees exactly the requested units.
    /// </summary>
    public static void IterateStructs(
        BcrdtTransaction transaction,
        List<BcrdtAbstractStruct> structs,
        long clockStart,
        long length,
        Action<BcrdtAbstractStruct> action)
    {
        if (length == 0) return;
        long clockEnd = clockStart + length;

        int index = FindIndexCleanStart(transaction, structs, clockStart);
        do
        {
            BcrdtAbstractStruct current = structs[index];
            if (clockEnd < current.Id.Clock + current.Length)
            {
                FindIndexCleanStart(transaction, structs, clockEnd);
            }
            action(structs[index]);
            index++;
        }
        while (index < structs.Count && structs[index].Id.Clock < clockEnd);
    }
}
