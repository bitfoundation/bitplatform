namespace Bit.Bcrdt;

/// <summary>
/// Operations on encoded updates that do not require a document.
/// </summary>
/// <remarks>
/// These exist so that storage backends, relay servers and gateways can do useful work - compact a
/// log, answer a sync request, decide whether a peer needs anything - without materializing a
/// document or understanding CRDT semantics. A relay can serve a thousand documents it never parses.
/// </remarks>
public static class BcrdtUpdateOperations
{
    /// <summary>
    /// Combines several updates into one that is equivalent to applying all of them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is what makes append-only persistence practical: a log of ten thousand keystroke deltas
    /// collapses into a single update the size of the document, losslessly, without loading it.
    /// </para>
    /// <para>
    /// Overlapping content is deduplicated by clock range rather than by comparing payloads, so
    /// merging a document's full history with a recent delta costs one pass and no allocation per
    /// duplicate block. Ranges that none of the inputs covers become explicit gap markers, so the
    /// receiver waits for them rather than mistaking the result for a complete history.
    /// </para>
    /// </remarks>
    /// <exception cref="BcrdtFormatException">
    /// An update is malformed, or the updates leave a gap in one author's clock range wider than any
    /// replica writes - which only a crafted update produces, and which would otherwise take billions
    /// of gap markers to describe. Every update can still be well-formed on its own.
    /// </exception>
    public static byte[] Merge(params ReadOnlyMemory<byte>[] updates) => Merge(updates, BcrdtUpdateCodec.MaxClockGap);

    internal static byte[] Merge(ReadOnlyMemory<byte>[] updates, long maxGap)
    {
        ArgumentNullException.ThrowIfNull(updates);
        if (updates.Length == 0) return BcrdtUpdateEncoder.Empty;
        if (updates.Length == 1) return updates[0].ToArray();

        Dictionary<ulong, List<BcrdtAbstractStruct>> byClient = [];
        List<BcrdtDeleteSet> deleteSets = new(updates.Length);

        foreach (ReadOnlyMemory<byte> update in updates)
        {
            var reader = new BcrdtBufferReader(update);
            Dictionary<ulong, BcrdtClientBlocks> groups = BcrdtUpdateCodec.ReadClientBlocks(ref reader);
            deleteSets.Add(BcrdtDeleteSet.Read(ref reader));

            foreach ((ulong client, BcrdtClientBlocks blocks) in groups)
            {
                if (!byClient.TryGetValue(client, out List<BcrdtAbstractStruct>? accumulated))
                {
                    accumulated = [];
                    byClient[client] = accumulated;
                }

                // Gap markers carry no content and are regenerated below from what is actually
                // present, so keeping them would only risk masking real blocks from another input.
                foreach (BcrdtAbstractStruct block in blocks.Blocks)
                {
                    if (block is not BcrdtSkip) accumulated.Add(block);
                }
            }
        }

        List<(ulong Client, long FromClock, List<(BcrdtAbstractStruct Block, int Offset)> Blocks)> groupsOut = [];

        foreach ((ulong client, List<BcrdtAbstractStruct> blocks) in byClient)
        {
            if (blocks.Count == 0) continue;
            blocks.Sort(static (left, right) => left.Id.Clock.CompareTo(right.Id.Clock));

            long start = blocks[0].Id.Clock;
            long cursor = start;
            List<(BcrdtAbstractStruct Block, int Offset)> output = [];

            foreach (BcrdtAbstractStruct block in blocks)
            {
                long end = block.Id.Clock + block.Length;
                if (end <= cursor) continue;

                if (block.Id.Clock - cursor > maxGap)
                {
                    throw new BcrdtFormatException($"Block at {block.Id} is {block.Id.Clock - cursor} clocks past the block before it.");
                }

                // A gap wider than one struct can describe becomes several skips, rather than one whose
                // length wraps and silently shifts every later block to the wrong clock.
                while (block.Id.Clock > cursor)
                {
                    int gap = (int)Math.Min(block.Id.Clock - cursor, int.MaxValue);
                    output.Add((new BcrdtSkip(new BcrdtItemId(client, cursor), gap), 0));
                    cursor += gap;
                }

                output.Add((block, (int)(cursor - block.Id.Clock)));
                cursor = end;
            }

            if (output.Count > 0) groupsOut.Add((client, start, CoalesceAdjacent(output)));
        }

        groupsOut.Sort(static (left, right) => right.Client.CompareTo(left.Client));

        var writer = new BcrdtBufferWriter(1024);
        try
        {
            BcrdtUpdateCodec.WriteBlockGroups(ref writer, groupsOut);
            BcrdtDeleteSet.Merge(deleteSets).Write(ref writer);
            return writer.ToArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>
    /// Folds neighbouring blocks back into single blocks wherever they are equivalent to one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is what makes compaction actually compact. A thousand keystrokes produce a thousand
    /// one-character blocks, each carrying its own identifier, origin and framing - perhaps six
    /// bytes of overhead for one byte of text. Deduplicating them by clock range removes nothing,
    /// because they do not overlap; they have to be recognised as describing one contiguous run and
    /// rewritten as such.
    /// </para>
    /// <para>
    /// The result is what the author would have produced had they typed it in one go, which is
    /// exactly what the in-memory engine converges to as well. Deletions are unaffected: they are
    /// recorded as clock ranges in the delete set, independently of how blocks happen to be divided.
    /// </para>
    /// </remarks>
    private static List<(BcrdtAbstractStruct Block, int Offset)> CoalesceAdjacent(
        List<(BcrdtAbstractStruct Block, int Offset)> blocks)
    {
        List<(BcrdtAbstractStruct Block, int Offset)> output = [];

        foreach ((BcrdtAbstractStruct block, int offset) in blocks)
        {
            // A block that starts partway in carries units the previous block already covered;
            // appending it whole would duplicate them.
            if (offset == 0 && output.Count > 0 && TryAppend(output[^1].Block, block))
            {
                continue;
            }
            output.Add((block, offset));
        }

        return output;
    }

    /// <summary>Appends one block to another if the pair is indistinguishable from a single block.</summary>
    /// <remarks>
    /// Deliberately separate from <see cref="BcrdtItem.TryMergeWith"/>, which additionally requires the
    /// two blocks to be linked neighbours in a live document. Blocks decoded from the wire have no
    /// links at all, so the test here is purely on identity, anchors and payload.
    /// </remarks>
    private static bool TryAppend(BcrdtAbstractStruct left, BcrdtAbstractStruct right)
    {
        if (left.GetType() != right.GetType()) return false;
        if (left.Id.Client != right.Id.Client) return false;
        if (left.Id.Clock + left.Length != right.Id.Clock) return false;
        if (!BcrdtAbstractStruct.FitsOneBlock(left.Length, right.Length)) return false;

        if (left is not BcrdtItem leftItem)
        {
            // Garbage-collected and skipped ranges carry no payload, so contiguity is enough.
            left.Length += right.Length;
            return true;
        }

        var rightItem = (BcrdtItem)right;

        if (!Nullable.Equals(rightItem.Origin, leftItem.LastId)) return false;
        if (!Nullable.Equals(leftItem.RightOrigin, rightItem.RightOrigin)) return false;
        if (!string.Equals(leftItem.ParentKey, rightItem.ParentKey, StringComparison.Ordinal)) return false;
        if (leftItem.Redone is not null || rightItem.Redone is not null) return false;
        if (leftItem.Content.GetType() != rightItem.Content.GetType()) return false;
        if (!leftItem.Content.TryMergeWith(rightItem.Content)) return false;

        leftItem.Length += rightItem.Length;
        return true;
    }

    /// <summary>
    /// Splits an update into several, each at most <paramref name="maxBytes"/> long where that is
    /// possible, that together are equivalent to it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For a transport or relay with a message size limit: a document's full state, or one enormous
    /// paste, can be larger than any single message may be. Every part is an ordinary update. Applied in
    /// order they rebuild exactly what the original would have; applied out of order, or with one lost,
    /// each waits for what it builds on like any other update, and the next handshake repairs a loss.
    /// The delete set travels with the first part - a deletion of content still to come waits for it -
    /// so a receiver that has every block has every deletion too.
    /// </para>
    /// <para>
    /// A run of text or of values is cut into pieces the way a replica splits a block when someone types
    /// into the middle of it, so even a single block larger than the limit is divided. A single
    /// indivisible value - one byte array, one embedded object - larger than the limit is sent whole in
    /// a part of its own.
    /// </para>
    /// </remarks>
    /// <param name="update">The update to split.</param>
    /// <param name="maxBytes">The most a part should take; at least 64.</param>
    /// <returns>The parts, in the order they should be sent; just the update itself if it already fits.</returns>
    /// <exception cref="BcrdtFormatException">The update is malformed.</exception>
    public static IReadOnlyList<byte[]> Split(ReadOnlyMemory<byte> update, int maxBytes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxBytes, 64);
        if (update.Length <= maxBytes) return [update.ToArray()];

        var reader = new BcrdtBufferReader(update);
        Dictionary<ulong, BcrdtClientBlocks> groups = BcrdtUpdateCodec.ReadClientBlocks(ref reader);
        BcrdtDeleteSet deleteSet = BcrdtDeleteSet.Read(ref reader);

        // Every size is counted exactly, except a part's group count, which is reserved at its widest.
        // The first part carries the delete set and every later one an empty one.
        int deleteSetSize = MeasureDeleteSet(deleteSet);
        int emptyDeleteSetSize = MeasureDeleteSet(new BcrdtDeleteSet());
        bool hasDeletions = deleteSetSize > emptyDeleteSetSize;

        // What one piece may take: a later part of its own, under the widest group header there is - a
        // block count, a 64-bit client and a 64-bit starting clock.
        int budget = maxBytes - MaxCountSize - (MaxCountSize + 2 * MaxVarUIntSize) - emptyDeleteSetSize;

        List<byte[]> parts = [];
        List<(ulong Client, long FromClock, List<(BcrdtAbstractStruct Block, int Offset)> Blocks)> current = [];
        int currentSize = MaxCountSize + deleteSetSize;

        void Flush()
        {
            var writer = new BcrdtBufferWriter(Math.Max(64, currentSize + 64));
            try
            {
                BcrdtUpdateCodec.WriteBlockGroups(ref writer, current);
                (parts.Count == 0 ? deleteSet : new BcrdtDeleteSet()).Write(ref writer);
                parts.Add(writer.ToArray());
            }
            finally
            {
                writer.Dispose();
            }
            current = [];
            currentSize = MaxCountSize + emptyDeleteSetSize;
        }

        // What a piece adds to the part being filled: its own size, and either a new group's header or
        // whatever the current group's block count grows by.
        int Cost(ulong client, BcrdtAbstractStruct piece, int size)
        {
            if (current.Count > 0 && current[^1].Client == client)
            {
                int count = current[^1].Blocks.Count;
                return size + VarUIntSize((ulong)count + 1) - VarUIntSize((ulong)count);
            }

            return size + VarUIntSize(1) + VarUIntSize(client) + VarUIntSize((ulong)piece.Id.Clock);
        }

        // The same order the encoder writes groups in, so the parts read like the original.
        foreach ((ulong client, BcrdtClientBlocks blocks) in groups.OrderByDescending(group => group.Key))
        {
            foreach (BcrdtAbstractStruct block in blocks.Blocks)
            {
                foreach (BcrdtAbstractStruct piece in Pieces(block, budget))
                {
                    int size = MeasureBlock(piece);

                    // A first part that holds nothing but the delete set is still worth sending alone
                    // when the piece does not fit beside it; an empty later part never is.
                    bool holdsSomething = current.Count > 0 || (parts.Count == 0 && hasDeletions);
                    if (holdsSomething && currentSize + Cost(client, piece, size) > maxBytes) Flush();

                    // Counted before the piece joins: a part that starts partway through a client's run
                    // starts a new group for it.
                    currentSize += Cost(client, piece, size);
                    if (current.Count == 0 || current[^1].Client != client) current.Add((client, piece.Id.Clock, []));
                    current[^1].Blocks.Add((piece, 0));
                }
            }
        }

        if (current.Count > 0 || parts.Count == 0) Flush();
        return parts;
    }

    /// <summary>The widest a 32-bit count is as a varuint.</summary>
    private const int MaxCountSize = 5;

    /// <summary>The widest a 64-bit value is as a varuint.</summary>
    private const int MaxVarUIntSize = 10;

    private static int VarUIntSize(ulong value)
    {
        int size = 1;
        while (value >= 0x80)
        {
            value >>= 7;
            size++;
        }
        return size;
    }

    private static int MeasureDeleteSet(BcrdtDeleteSet deleteSet)
    {
        var writer = new BcrdtBufferWriter(64);
        try
        {
            deleteSet.Write(ref writer);
            return writer.Length;
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>The encoded size of one block.</summary>
    private static int MeasureBlock(BcrdtAbstractStruct block)
    {
        var writer = new BcrdtBufferWriter(64);
        try
        {
            block.Write(ref writer, 0);
            return writer.Length;
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>
    /// A block cut into pieces that each encode within <paramref name="budget"/>, where its content can
    /// be cut at all - exactly as a replica splits a block, so the pieces mean what the block did.
    /// </summary>
    private static IEnumerable<BcrdtAbstractStruct> Pieces(BcrdtAbstractStruct block, int budget)
    {
        // A map entry is never cut: its later pieces would integrate as later writes to the same key and
        // delete the earlier ones, which the entry as written never did.
        if (block is not BcrdtItem item || item.Length < 2 || item.ParentKey is not null ||
            item.Content is not (BcrdtContentString or BcrdtContentAny))
        {
            yield return block;
            yield break;
        }

        int size = MeasureBlock(item);
        if (size <= budget)
        {
            yield return item;
            yield break;
        }

        // Proportionally, with a margin for content whose units vary in size; any piece still too large
        // is cut again.
        int units = Math.Max(1, (int)((long)item.Length * budget / size / 2));
        IBcrdtContent? rest = item.Content.Copy();
        long clock = item.Id.Clock;
        BcrdtItemId? origin = item.Origin;
        object? parent = item.Parent;
        string? parentKey = item.ParentKey;

        while (rest is not null)
        {
            IBcrdtContent head = rest;
            int take = Math.Min(units, head.Length);
            rest = take < head.Length ? head.Splice(take) : null;

            var piece = new BcrdtItem(new BcrdtItemId(item.Id.Client, clock), null, origin, null, item.RightOrigin, parent, parentKey, head);
            foreach (BcrdtAbstractStruct smaller in Pieces(piece, budget)) yield return smaller;

            // Every later piece hangs off the one before it, so its container travels with that one.
            clock += take;
            origin = piece.LastId;
            parent = null;
            parentKey = null;
        }
    }

    /// <summary>
    /// Trims an update to only the parts a peer with <paramref name="remoteState"/> is missing.
    /// </summary>
    /// <remarks>
    /// Lets a server answer a sync request from a stored update without loading the document. The
    /// delete set is passed through whole: it is small, and deletions are idempotent, so trimming it
    /// would cost more than it saves.
    /// </remarks>
    public static byte[] Diff(ReadOnlyMemory<byte> update, BcrdtStateVector remoteState)
    {
        ArgumentNullException.ThrowIfNull(remoteState);

        var reader = new BcrdtBufferReader(update);
        Dictionary<ulong, BcrdtClientBlocks> groups = BcrdtUpdateCodec.ReadClientBlocks(ref reader);
        BcrdtDeleteSet deleteSet = BcrdtDeleteSet.Read(ref reader);

        List<(ulong Client, long FromClock, List<(BcrdtAbstractStruct Block, int Offset)> Blocks)> groupsOut = [];

        foreach ((ulong client, BcrdtClientBlocks blocks) in groups)
        {
            long knownThrough = remoteState.Get(client);
            List<(BcrdtAbstractStruct Block, int Offset)> output = [];
            long start = -1;

            foreach (BcrdtAbstractStruct block in blocks.Blocks)
            {
                long end = block.Id.Clock + block.Length;
                if (end <= knownThrough) continue;

                int offset = (int)Math.Max(0, knownThrough - block.Id.Clock);
                if (start < 0) start = block.Id.Clock + offset;
                output.Add((block, offset));
            }

            if (output.Count > 0) groupsOut.Add((client, start, output));
        }

        groupsOut.Sort(static (left, right) => right.Client.CompareTo(left.Client));

        var writer = new BcrdtBufferWriter(512);
        try
        {
            BcrdtUpdateCodec.WriteBlockGroups(ref writer, groupsOut);
            deleteSet.Write(ref writer);
            return writer.ToArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>
    /// Reads the state vector an update implies, without applying it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lets a storage backend answer "what does this snapshot contain?" cheaply, so a sync request
    /// can be served, or skipped entirely, without a document ever being built.
    /// </para>
    /// <para>
    /// A state vector claims everything below a clock, so only a run that starts at clock zero and
    /// has no gaps counts. An update holding a later delta, or one with ranges skipped, implies
    /// nothing beyond its first gap: claiming more would tell a peer not to send content that the
    /// holder of this update does not actually have.
    /// </para>
    /// </remarks>
    public static BcrdtStateVector DecodeStateVector(ReadOnlyMemory<byte> update)
    {
        var reader = new BcrdtBufferReader(update);
        Dictionary<ulong, BcrdtClientBlocks> groups = BcrdtUpdateCodec.ReadClientBlocks(ref reader);

        var vector = new BcrdtStateVector(groups.Count);
        foreach ((ulong client, BcrdtClientBlocks blocks) in groups)
        {
            long contiguousTo = 0;
            foreach (BcrdtAbstractStruct block in blocks.Blocks)
            {
                if (block is BcrdtSkip || block.Id.Clock != contiguousTo) break;
                contiguousTo = block.Id.Clock + block.Length;
            }
            if (contiguousTo > 0) vector.Set(client, contiguousTo);
        }
        return vector;
    }

    /// <summary>Reads the blocks an update carries, grouped by author, without applying it.</summary>
    /// <remarks>
    /// <para>
    /// The counterpart of <see cref="DecodeStateVector"/> and <see cref="DecodeDeleteSet"/> for the
    /// content itself: what a tool that inspects, audits or logs updates needs, with no document
    /// involved.
    /// </para>
    /// <para>
    /// Each author's blocks are listed in clock order, exactly as they were written: a
    /// <see cref="BcrdtItem"/> per run of content, a <see cref="BcrdtGarbageCollected"/> per collected
    /// range and a <see cref="BcrdtSkip"/> per range the sender left out. The items belong to no
    /// document. One written without origins names its container as the wire does - a root name
    /// (<see cref="string"/>) or the <see cref="BcrdtItemId"/> of the item holding the nested type -
    /// and only such an item carries its map key; a later write to the same key derives it from its
    /// origin.
    /// </para>
    /// </remarks>
    /// <exception cref="BcrdtFormatException">
    /// The payload is not a well-formed update. The delete set is read too, so a payload this accepts
    /// is one <see cref="BcrdtUpdateDecoder.ApplyUpdate"/> would decode.
    /// </exception>
    public static IReadOnlyDictionary<ulong, IReadOnlyList<BcrdtAbstractStruct>> DecodeBlocks(ReadOnlyMemory<byte> update)
    {
        var reader = new BcrdtBufferReader(update);
        Dictionary<ulong, BcrdtClientBlocks> groups = BcrdtUpdateCodec.ReadClientBlocks(ref reader);
        BcrdtDeleteSet.Read(ref reader);

        var blocks = new Dictionary<ulong, IReadOnlyList<BcrdtAbstractStruct>>(groups.Count);
        foreach ((ulong client, BcrdtClientBlocks group) in groups) blocks[client] = group.Blocks;
        return blocks;
    }

    /// <summary>Reads the deletions an update carries, without applying it.</summary>
    public static BcrdtDeleteSet DecodeDeleteSet(ReadOnlyMemory<byte> update)
    {
        var reader = new BcrdtBufferReader(update);
        BcrdtUpdateCodec.ReadClientBlocks(ref reader);
        return BcrdtDeleteSet.Read(ref reader);
    }

    /// <summary>Whether an update carries no blocks and no deletions.</summary>
    public static bool IsEmpty(ReadOnlyMemory<byte> update)
    {
        var reader = new BcrdtBufferReader(update);
        Dictionary<ulong, BcrdtClientBlocks> groups = BcrdtUpdateCodec.ReadClientBlocks(ref reader);
        return groups.Count == 0 && BcrdtDeleteSet.Read(ref reader).IsEmpty;
    }
}
