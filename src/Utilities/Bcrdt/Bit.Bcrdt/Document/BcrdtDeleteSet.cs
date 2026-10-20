namespace Bit.Bcrdt;

/// <summary>A contiguous run of deleted clocks belonging to one client.</summary>
/// <param name="Clock">The first deleted clock.</param>
/// <param name="Length">How many consecutive clocks are deleted.</param>
public readonly record struct BcrdtDeleteRange(long Clock, long Length)
{
    /// <summary>The clock just past the end of the range.</summary>
    public long End => Clock + Length;
}

/// <summary>
/// Which content has been deleted, expressed as clock ranges rather than as a list of tombstones.
/// </summary>
/// <remarks>
/// <para>
/// Deletion in an operation-based CRDT cannot simply remove content: a peer may still send an
/// operation anchored to it. What the engine records instead is that a range of clock space is no
/// longer visible. Expressing that as ranges rather than per-item flags means deleting a paragraph
/// costs one entry regardless of how many blocks it spanned, and the whole set for a large document
/// is typically a few hundred bytes.
/// </para>
/// <para>
/// The set is kept sorted and coalesced by <see cref="SortAndMerge"/>, which runs at the end of
/// every transaction. Ranges are only mergeable once sorted, so <see cref="IsDeleted"/> - which
/// binary-searches - must not be called on an unsorted set.
/// </para>
/// </remarks>
public sealed class BcrdtDeleteSet
{
    private readonly Dictionary<ulong, List<BcrdtDeleteRange>> _clients = [];

    /// <summary>The deleted ranges, grouped by the client that authored the deleted content.</summary>
    public IReadOnlyDictionary<ulong, List<BcrdtDeleteRange>> Clients => _clients;

    /// <summary>Whether nothing at all is recorded as deleted.</summary>
    public bool IsEmpty
    {
        get
        {
            foreach (List<BcrdtDeleteRange> ranges in _clients.Values)
            {
                if (ranges.Count > 0) return false;
            }
            return true;
        }
    }

    /// <summary>Records that a range of a client's clock space is deleted.</summary>
    public void Add(ulong client, long clock, long length)
    {
        if (length <= 0) return;
        if (!_clients.TryGetValue(client, out List<BcrdtDeleteRange>? ranges))
        {
            ranges = [];
            _clients[client] = ranges;
        }
        ranges.Add(new BcrdtDeleteRange(clock, length));
    }

    /// <summary>Sorts ranges and coalesces adjacent or overlapping ones.</summary>
    public void SortAndMerge()
    {
        foreach ((ulong client, List<BcrdtDeleteRange> ranges) in _clients)
        {
            if (ranges.Count == 0)
            {
                continue;
            }
            if (ranges.Count > 1)
            {
                ranges.Sort(static (a, b) => a.Clock.CompareTo(b.Clock));

                int write = 0;
                for (int read = 1; read < ranges.Count; read++)
                {
                    BcrdtDeleteRange current = ranges[write];
                    BcrdtDeleteRange next = ranges[read];
                    if (current.End >= next.Clock)
                    {
                        ranges[write] = new BcrdtDeleteRange(current.Clock, Math.Max(current.End, next.End) - current.Clock);
                    }
                    else
                    {
                        ranges[++write] = next;
                    }
                }
                ranges.RemoveRange(write + 1, ranges.Count - write - 1);
            }
            _clients[client] = ranges;
        }

        // Clients whose ranges all coalesced away would otherwise be written to the wire as empty
        // entries, which the reader rejects.
        List<ulong> empty = [.. _clients.Where(p => p.Value.Count == 0).Select(p => p.Key)];
        foreach (ulong client in empty) _clients.Remove(client);
    }

    /// <summary>Whether a specific logical unit is recorded as deleted.</summary>
    /// <remarks>Requires the set to be sorted; call <see cref="SortAndMerge"/> first.</remarks>
    public bool IsDeleted(BcrdtItemId id)
    {
        if (!_clients.TryGetValue(id.Client, out List<BcrdtDeleteRange>? ranges)) return false;
        return FindRangeIndex(ranges, id.Clock) >= 0;
    }

    private static int FindRangeIndex(List<BcrdtDeleteRange> ranges, long clock)
    {
        int low = 0;
        int high = ranges.Count - 1;
        while (low <= high)
        {
            int mid = (int)(((uint)low + (uint)high) >> 1);
            BcrdtDeleteRange candidate = ranges[mid];
            if (clock < candidate.Clock) high = mid - 1;
            else if (clock < candidate.End) return mid;
            else low = mid + 1;
        }
        return -1;
    }

    /// <summary>Combines several delete sets into one.</summary>
    public static BcrdtDeleteSet Merge(IEnumerable<BcrdtDeleteSet> sets)
    {
        var merged = new BcrdtDeleteSet();
        foreach (BcrdtDeleteSet set in sets)
        {
            foreach ((ulong client, List<BcrdtDeleteRange> ranges) in set._clients)
            {
                foreach (BcrdtDeleteRange range in ranges) merged.Add(client, range.Clock, range.Length);
            }
        }
        merged.SortAndMerge();
        return merged;
    }

    /// <summary>Derives the complete delete set of a store by scanning its blocks.</summary>
    /// <remarks>
    /// Used when encoding a whole document, where the transaction-local delete sets have long since
    /// been discarded and the truth lives in the blocks themselves.
    /// </remarks>
    public static BcrdtDeleteSet FromStore(BcrdtStructStore store)
    {
        var set = new BcrdtDeleteSet();
        foreach ((ulong client, List<BcrdtAbstractStruct> structs) in store.Clients)
        {
            List<BcrdtDeleteRange> ranges = [];
            for (int i = 0; i < structs.Count; i++)
            {
                BcrdtAbstractStruct current = structs[i];
                if (!current.Deleted) continue;

                long clock = current.Id.Clock;
                long length = current.Length;

                // Coalesce during the scan rather than afterwards: deleted blocks are usually
                // adjacent, so this keeps the intermediate list small on large documents.
                while (i + 1 < structs.Count && structs[i + 1].Deleted)
                {
                    i++;
                    length += structs[i].Length;
                }
                ranges.Add(new BcrdtDeleteRange(clock, length));
            }
            if (ranges.Count > 0) set._clients[client] = ranges;
        }
        return set;
    }

    /// <summary>Invokes <paramref name="action"/> for every item covered by this set.</summary>
    public void IterateDeletedStructs(BcrdtTransaction transaction, Action<BcrdtAbstractStruct> action)
    {
        foreach ((ulong client, List<BcrdtDeleteRange> ranges) in _clients)
        {
            List<BcrdtAbstractStruct>? structs = transaction.Doc.Store.GetClientStructs(client);
            if (structs is null) continue;

            foreach (BcrdtDeleteRange range in ranges)
            {
                if (range.Clock >= transaction.Doc.Store.GetState(client)) continue;
                BcrdtStructStore.IterateStructs(transaction, structs, range.Clock, range.Length, action);
            }
        }
    }

    /// <summary>Creates an independent copy.</summary>
    public BcrdtDeleteSet Clone()
    {
        var clone = new BcrdtDeleteSet();
        foreach ((ulong client, List<BcrdtDeleteRange> ranges) in _clients)
        {
            clone._clients[client] = [.. ranges];
        }
        return clone;
    }

    /// <summary>Writes the set with clocks delta-encoded within each client run.</summary>
    /// <remarks>
    /// The delta encoding only works on sorted, non-overlapping ranges; anything else would encode
    /// negative gaps. A set that has not been through <see cref="SortAndMerge"/> yet - one belonging
    /// to a transaction that is still open, say - is therefore written from a normalized copy.
    /// </remarks>
    public void Write(ref BcrdtBufferWriter writer)
    {
        if (!IsNormalized())
        {
            BcrdtDeleteSet normalized = Clone();
            normalized.SortAndMerge();
            normalized.Write(ref writer);
            return;
        }

        writer.WriteVarUInt((ulong)_clients.Count);
        foreach ((ulong client, List<BcrdtDeleteRange> ranges) in _clients)
        {
            writer.WriteVarUInt(client);
            writer.WriteVarUInt((ulong)ranges.Count);

            long cursor = 0;
            foreach (BcrdtDeleteRange range in ranges)
            {
                writer.WriteVarUInt((ulong)(range.Clock - cursor));
                writer.WriteVarUInt((ulong)(range.Length - 1));
                cursor = range.End;
            }
        }
    }

    private bool IsNormalized()
    {
        foreach (List<BcrdtDeleteRange> ranges in _clients.Values)
        {
            if (ranges.Count == 0) return false;
            for (int i = 1; i < ranges.Count; i++)
            {
                if (ranges[i].Clock <= ranges[i - 1].End) return false;
            }
        }
        return true;
    }

    /// <summary>Reads a set written by <see cref="Write"/>.</summary>
    public static BcrdtDeleteSet Read(ref BcrdtBufferReader reader)
    {
        var set = new BcrdtDeleteSet();
        int clientCount = reader.ValidateCount(reader.ReadVarUInt32(), "delete set client");

        for (int i = 0; i < clientCount; i++)
        {
            ulong client = reader.ReadVarUInt();
            int rangeCount = reader.ValidateCount(reader.ReadVarUInt32(), "delete range");
            if (rangeCount == 0) throw new BcrdtFormatException("Delete set client has no ranges.");

            List<BcrdtDeleteRange> ranges = new(rangeCount);
            long cursor = 0;
            for (int r = 0; r < rangeCount; r++)
            {
                ulong gap = reader.ReadVarUInt();
                ulong lengthMinusOne = reader.ReadVarUInt();

                // Every range must lie wholly inside the non-negative Int64 clock space; a range whose
                // end overflowed would wrap around and address clocks it never named.
                if (gap > (ulong)(long.MaxValue - cursor) || lengthMinusOne >= (ulong)(long.MaxValue - cursor - (long)gap))
                {
                    throw new BcrdtFormatException("Malformed delete range.");
                }

                long clock = cursor + (long)gap;
                long length = (long)lengthMinusOne + 1;
                ranges.Add(new BcrdtDeleteRange(clock, length));
                cursor = clock + length;
            }
            set._clients[client] = ranges;
        }
        return set;
    }

    /// <summary>Encodes the set to a standalone byte array.</summary>
    public byte[] Encode()
    {
        var writer = new BcrdtBufferWriter(64);
        try
        {
            Write(ref writer);
            return writer.ToArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>Decodes a set produced by <see cref="Encode"/>.</summary>
    public static BcrdtDeleteSet Decode(ReadOnlyMemory<byte> data)
    {
        var reader = new BcrdtBufferReader(data);
        return Read(ref reader);
    }
}
