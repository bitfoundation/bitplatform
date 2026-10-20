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
    public static byte[] Merge(params ReadOnlyMemory<byte>[] updates)
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

                if (block.Id.Clock > cursor)
                {
                    output.Add((new BcrdtSkip(new BcrdtItemId(client, cursor), (int)(block.Id.Clock - cursor)), 0));
                    cursor = block.Id.Clock;
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
