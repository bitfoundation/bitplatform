namespace Bit.Bcrdt;

/// <summary>Blocks read from an update for one client, plus how far integration has consumed them.</summary>
internal sealed class BcrdtClientBlocks(List<BcrdtAbstractStruct> blocks)
{
    public readonly List<BcrdtAbstractStruct> Blocks = blocks;
    public int Position;

    public bool Exhausted => Position >= Blocks.Count;
}

/// <summary>
/// The wire format for updates.
/// </summary>
/// <remarks>
/// <para>
/// An update is a set of blocks grouped by author, followed by a delete set. Blocks are written in
/// clock order within each author, and clocks are delta-encoded against the group's first clock, so
/// a long editing session by one client costs about a byte of clock per block.
/// </para>
/// <para>
/// The format concatenates: any two updates can be combined into a third that is equivalent to
/// applying both. That property is what makes append-only persistence and lossless compaction
/// possible without the storage layer understanding anything about CRDTs.
/// </para>
/// </remarks>
internal static class BcrdtUpdateCodec
{
    private const byte OriginFlag = 0x80;
    private const byte RightOriginFlag = 0x40;
    private const byte ParentKeyFlag = 0x20;
    private const byte ContentRefMask = 0x1F;
    private const byte ParentIsRootName = 1;
    private const byte ParentIsItemId = 0;

    internal const long MaxClockGap = 4L * int.MaxValue;

    /// <summary>Writes the block groups a peer is missing, given what it already has.</summary>
    public static void WriteClientBlocks(ref BcrdtBufferWriter writer, BcrdtStructStore store, BcrdtStateVector remoteState)
    {
        // Only clients whose content has advanced past what the peer holds are worth writing at all.
        List<(ulong Client, long FromClock)> groups = [];
        foreach (ulong client in store.Clients.Keys)
        {
            long fromClock = remoteState.Get(client);
            if (store.GetState(client) > fromClock) groups.Add((client, fromClock));
        }

        // Descending by client identifier, which is the same order integration uses to break ties.
        // Feeding the receiver blocks in that order means its conflict scan usually finds the
        // position it wants immediately instead of walking competitors.
        groups.Sort(static (left, right) => right.Client.CompareTo(left.Client));

        writer.WriteVarUInt((ulong)groups.Count);
        foreach ((ulong client, long fromClock) in groups)
        {
            WriteClientGroup(ref writer, store.GetClientStructs(client)!, client, fromClock);
        }
    }

    private static void WriteClientGroup(
        ref BcrdtBufferWriter writer,
        List<BcrdtAbstractStruct> structs,
        ulong client,
        long fromClock)
    {
        fromClock = Math.Max(fromClock, structs[0].Id.Clock);
        int start = BcrdtStructStore.FindIndex(structs, fromClock);

        writer.WriteVarUInt((ulong)(structs.Count - start));
        writer.WriteVarUInt(client);
        writer.WriteVarUInt((ulong)fromClock);

        // The first block may be partially known to the peer, so it is written from an offset.
        structs[start].Write(ref writer, (int)(fromClock - structs[start].Id.Clock));
        for (int i = start + 1; i < structs.Count; i++)
        {
            structs[i].Write(ref writer, 0);
        }
    }

    /// <summary>Writes explicit block groups, each already trimmed to the offset it should start at.</summary>
    public static void WriteBlockGroups(
        ref BcrdtBufferWriter writer,
        List<(ulong Client, long FromClock, List<(BcrdtAbstractStruct Block, int Offset)> Blocks)> groups)
    {
        writer.WriteVarUInt((ulong)groups.Count);
        foreach ((ulong client, long fromClock, List<(BcrdtAbstractStruct Block, int Offset)> blocks) in groups)
        {
            writer.WriteVarUInt((ulong)blocks.Count);
            writer.WriteVarUInt(client);
            writer.WriteVarUInt((ulong)fromClock);
            foreach ((BcrdtAbstractStruct block, int offset) in blocks) block.Write(ref writer, offset);
        }
    }

    /// <summary>
    /// Reads the block groups of an update without integrating them.
    /// </summary>
    /// <param name="reader">Positioned at the start of an update.</param>
    /// <remarks>
    /// Root names are left unresolved, as strings. That is what lets updates be merged and rewritten
    /// without a document being involved at all, and it means reading never touches a document:
    /// <see cref="ResolveRoots"/> binds the names only once the whole payload is known to be valid.
    /// </remarks>
    public static Dictionary<ulong, BcrdtClientBlocks> ReadClientBlocks(ref BcrdtBufferReader reader)
    {
        int clientCount = reader.ValidateCount(reader.ReadVarUInt32(), "client group");
        var result = new Dictionary<ulong, BcrdtClientBlocks>(clientCount);

        for (int groupIndex = 0; groupIndex < clientCount; groupIndex++)
        {
            int blockCount = reader.ValidateCount(reader.ReadVarUInt32(), "block");
            ulong client = reader.ReadVarUInt();
            ulong startClock = reader.ReadVarUInt();
            if (startClock > long.MaxValue) throw new BcrdtFormatException("Clock exceeds Int64 range.");

            long clock = (long)startClock;
            var blocks = new List<BcrdtAbstractStruct>(blockCount);

            for (int i = 0; i < blockCount; i++)
            {
                BcrdtAbstractStruct block = ReadBlock(ref reader, client, clock);
                if (block.Length <= 0)
                {
                    throw new BcrdtFormatException($"Block at {block.Id} declares a non-positive length.");
                }
                if (clock > long.MaxValue - block.Length)
                {
                    throw new BcrdtFormatException($"Block at {block.Id} runs past the end of the clock space.");
                }
                blocks.Add(block);
                clock += block.Length;
            }

            // A second group for the same client would silently overwrite the first and lose blocks.
            if (!result.TryAdd(client, new BcrdtClientBlocks(blocks)))
            {
                throw new BcrdtFormatException($"Update contains two groups for client {client}.");
            }
        }

        return result;
    }

    /// <summary>Binds the root names read by <see cref="ReadClientBlocks"/> to a document's root types.</summary>
    public static void ResolveRoots(Dictionary<ulong, BcrdtClientBlocks> groups, BcrdtDocument doc)
    {
        foreach (BcrdtClientBlocks group in groups.Values)
        {
            foreach (BcrdtAbstractStruct block in group.Blocks)
            {
                if (block is BcrdtItem { Parent: string rootName } item) item.Parent = doc.GetOrCreateUntypedRoot(rootName);
            }
        }
    }

    public static void RefuseFarClocks(Dictionary<ulong, BcrdtClientBlocks> groups, BcrdtStructStore store)
    {
        foreach ((ulong client, BcrdtClientBlocks group) in groups) RefuseFarClocks(group, store.GetState(client));
    }

    /// <summary>Refuses a block further past the content before it than any replica writes.</summary>
    /// <param name="group">One author's blocks.</param>
    /// <param name="reach">How far that author's content already reaches.</param>
    /// <returns>How far it reaches with the group's blocks.</returns>
    public static long RefuseFarClocks(BcrdtClientBlocks group, long reach)
    {
        foreach (BcrdtAbstractStruct block in group.Blocks)
        {
            if (block is BcrdtSkip) continue;
            if (block.Id.Clock - reach > MaxClockGap)
            {
                throw new BcrdtFormatException($"Block at {block.Id} is {block.Id.Clock - reach} clocks past the content before it.");
            }
            reach = Math.Max(reach, block.Id.Clock + block.Length);
        }
        return reach;
    }

    private static BcrdtAbstractStruct ReadBlock(ref BcrdtBufferReader reader, ulong client, long clock)
    {
        byte info = reader.ReadByte();
        var contentRef = (BcrdtContentRef)(info & ContentRefMask);

        switch (contentRef)
        {
            case BcrdtContentRef.GarbageCollected:
                return new BcrdtGarbageCollected(new BcrdtItemId(client, clock), reader.ReadVarUInt32());

            case BcrdtContentRef.Skip:
                return new BcrdtSkip(new BcrdtItemId(client, clock), reader.ReadVarUInt32());
        }

        bool hasOrigin = (info & OriginFlag) != 0;
        bool hasRightOrigin = (info & RightOriginFlag) != 0;
        bool hasParentKey = (info & ParentKeyFlag) != 0;

        BcrdtItemId? origin = hasOrigin ? ReadReference(ref reader, client, clock) : null;
        BcrdtItemId? rightOrigin = hasRightOrigin ? ReadReference(ref reader, client, clock) : null;

        object? parent = null;
        string? parentKey = null;

        if (!hasOrigin && !hasRightOrigin)
        {
            byte parentKind = reader.ReadByte();
            parent = parentKind switch
            {
                ParentIsRootName => reader.ReadString(),
                ParentIsItemId => ReadReference(ref reader, client, clock),
                _ => throw BcrdtFormatException.UnknownTag("parent reference", parentKind),
            };

            if (hasParentKey) parentKey = reader.ReadString();
        }

        IBcrdtContent content = ReadContent(ref reader, contentRef);
        return new BcrdtItem(new BcrdtItemId(client, clock), null, origin, null, rightOrigin, parent, parentKey, content);
    }

    /// <summary>Reads an identifier a block refers to - a neighbour or a container.</summary>
    /// <remarks>
    /// Anything a block refers to existed when the block was created, so a reference to the same
    /// client must name an earlier clock. A reference to the block's own clock or later cannot come
    /// from a genuine peer, and accepting it would send integration looking for content that by
    /// construction can never arrive.
    /// </remarks>
    private static BcrdtItemId ReadReference(ref BcrdtBufferReader reader, ulong client, long clock)
    {
        BcrdtItemId id = BcrdtItemId.Read(ref reader);
        if (id.Client == client && id.Clock >= clock)
        {
            throw new BcrdtFormatException($"Block {client}@{clock} refers to its own future at {id}.");
        }
        return id;
    }

    private static IBcrdtContent ReadContent(ref BcrdtBufferReader reader, BcrdtContentRef contentRef) => contentRef switch
    {
        BcrdtContentRef.Deleted => BcrdtContentDeleted.Read(ref reader),
        BcrdtContentRef.Binary => BcrdtContentBinary.Read(ref reader),
        BcrdtContentRef.String => BcrdtContentString.Read(ref reader),
        BcrdtContentRef.Embed => BcrdtContentEmbed.Read(ref reader),
        BcrdtContentRef.Format => BcrdtContentFormat.Read(ref reader),
        BcrdtContentRef.Type => BcrdtContentType.Read(ref reader),
        BcrdtContentRef.Any => BcrdtContentAny.Read(ref reader),
        _ => throw BcrdtFormatException.UnknownTag("content", (int)contentRef),
    };
}
