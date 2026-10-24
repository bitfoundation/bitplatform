namespace Bit.Bcrdt.Tests;

/// <summary>
/// Hostile and extreme inputs found in review: each test failed against the engine as it was before
/// its fix, and each describes what an application or a peer would observe.
/// </summary>
[TestClass]
public class HardeningTests
{
    private static BcrdtDocument Doc(ulong clientId) => new(new BcrdtDocumentOptions { ClientId = clientId });

    private delegate void Writing(ref BcrdtBufferWriter writer);

    private static byte[] Build(Writing write)
    {
        var writer = new BcrdtBufferWriter(64);
        try
        {
            write(ref writer);
            return writer.ToArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    // ---------------------------------------------------------------- lengths and clocks

    [TestMethod]
    public void Adjacent_ranges_too_long_for_one_block_stay_two_blocks()
    {
        // Two neighbouring collected ranges, each as long as a block can be: well formed, and once
        // merged into one block of negative length enough to make the replica unreadable to everyone.
        byte[] update = Build((ref BcrdtBufferWriter writer) =>
        {
            writer.WriteVarUInt(1);
            writer.WriteVarUInt(2);
            writer.WriteVarUInt(99);
            writer.WriteVarUInt(0);
            writer.WriteByte((byte)BcrdtContentRef.GarbageCollected);
            writer.WriteVarUInt(int.MaxValue);
            writer.WriteByte((byte)BcrdtContentRef.GarbageCollected);
            writer.WriteVarUInt(int.MaxValue);
            writer.WriteVarUInt(0);
        });

        using BcrdtDocument victim = Doc(1);
        victim.ApplyUpdate(update);
        Assert.AreEqual(2L * int.MaxValue, victim.GetStateVector().Get(99));

        // And the replica can still be read by its peers.
        using BcrdtDocument peer = Doc(2);
        peer.ApplyUpdate(victim.EncodeStateAsUpdate());
        Assert.AreEqual(2L * int.MaxValue, peer.GetStateVector().Get(99));
        BcrdtConvergenceHarness.AssertStructurallyIdentical(victim, peer);
    }

    [TestMethod]
    public void Merging_updates_across_a_gap_wider_than_a_block_keeps_every_clock()
    {
        static byte[] Collected(ulong client, ulong clock) => Build((ref BcrdtBufferWriter writer) =>
        {
            writer.WriteVarUInt(1);
            writer.WriteVarUInt(1);
            writer.WriteVarUInt(client);
            writer.WriteVarUInt(clock);
            writer.WriteByte((byte)BcrdtContentRef.GarbageCollected);
            writer.WriteVarUInt(1);
            writer.WriteVarUInt(0);
        });

        byte[] merged = BcrdtUpdateOperations.Merge(Collected(7, 0), Collected(7, 3_000_000_000));

        IReadOnlyList<BcrdtAbstractStruct> blocks = BcrdtUpdateOperations.DecodeBlocks(merged)[7];
        BcrdtAbstractStruct last = blocks[^1];
        Assert.AreEqual(3_000_000_000L, last.Id.Clock, "the block after the gap must keep its clock");
        Assert.IsInstanceOfType<BcrdtGarbageCollected>(last);
    }

    [TestMethod]
    public void A_pending_update_beyond_a_wide_gap_does_not_poison_the_document()
    {
        // A block far ahead of anything received waits for what comes before it. Merging it with the
        // next deferred block used to wrap the gap and corrupt the waiting update, which then failed
        // on the next retry and took every legitimate waiting block with it.
        using BcrdtDocument source = Doc(7);
        source.GetText("t").Insert(0, "abcde");
        byte[] early = source.EncodeStateAsUpdate();

        static byte[] Collected(ulong clock) => Build((ref BcrdtBufferWriter writer) =>
        {
            writer.WriteVarUInt(1);
            writer.WriteVarUInt(1);
            writer.WriteVarUInt(7);
            writer.WriteVarUInt(clock);
            writer.WriteByte((byte)BcrdtContentRef.GarbageCollected);
            writer.WriteVarUInt(1);
            writer.WriteVarUInt(0);
        });

        using BcrdtDocument doc = Doc(1);

        // Both wait for clocks 0-4, and are folded into one waiting update three billion clocks wide.
        doc.ApplyUpdate(Collected(3_000_000_000));
        doc.ApplyUpdate(Collected(5));
        Assert.IsTrue(doc.HasPendingUpdates);

        doc.ApplyUpdate(early);
        Assert.AreEqual("abcde", doc.GetText("t").ToString());
        Assert.AreEqual(6L, doc.GetStateVector().Get(7), "the block right after the text was waiting for it and is now in");
        Assert.IsTrue(doc.HasPendingUpdates, "the far block still waits for the range before it");
    }

    // ---------------------------------------------------------------- nesting

    [TestMethod]
    public void Nesting_types_past_the_limit_is_refused_locally()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtMap map = doc.GetMap("m");
        for (int depth = 1; depth <= BcrdtAbstractType.MaxNestingDepth; depth++) map = map.EnsureMap("a");

        Assert.ThrowsExactly<InvalidOperationException>(() => map.EnsureMap("a"));

        // Nothing half-made was left behind: the document still synchronizes and still deletes.
        using BcrdtDocument peer = Doc(2);
        peer.ApplyUpdate(doc.EncodeStateAsUpdate());
        BcrdtConvergenceHarness.AssertConverged(doc, peer);
        doc.GetMap("m").Delete("a");
        Assert.AreEqual(0, doc.GetMap("m").Count);
    }

    [TestMethod]
    public void Types_nested_past_the_limit_by_a_peer_are_dropped_identically_everywhere()
    {
        // A chain of nested maps far deeper than the limit, as a hostile peer would send it: each map
        // names the previous one as its container. Deleting the top of such a chain used to recurse
        // once per level and overflow the stack, which no exception handler can catch.
        const int Depth = 2_000;
        byte[] update = Build((ref BcrdtBufferWriter writer) =>
        {
            List<(BcrdtAbstractStruct Block, int Offset)> blocks = [];
            for (int i = 0; i < Depth; i++)
            {
                object parent = i == 0 ? "m" : new BcrdtItemId(7, i - 1);
                blocks.Add((new BcrdtItem(new BcrdtItemId(7, i), null, null, null, null, parent, "a", new BcrdtContentType(new BcrdtMap())), 0));
            }

            BcrdtUpdateCodec.WriteBlockGroups(ref writer, [(7UL, 0L, blocks)]);
            new BcrdtDeleteSet().Write(ref writer);
        });

        using BcrdtDocument first = Doc(1);
        using BcrdtDocument second = Doc(2);
        first.ApplyUpdate(update);
        second.ApplyUpdate(update);

        int depth = 0;
        for (BcrdtMap? map = first.GetMap("m"); map is not null; map = map.Get("a") as BcrdtMap) depth++;
        Assert.AreEqual(BcrdtAbstractType.MaxNestingDepth + 1, depth, "the root and every level up to the limit survive");

        BcrdtConvergenceHarness.AssertConverged(first, second);

        first.GetMap("m").Delete("a");
        Assert.AreEqual(0, first.GetMap("m").Count);
        _ = first.ToJson();
    }

    // ---------------------------------------------------------------- decoding cost

    [TestMethod]
    public void Nested_declared_counts_cannot_make_the_decoder_reserve_far_more_than_the_payload()
    {
        // Sixty-odd nested objects, each claiming as many entries as the payload has bytes.
        byte[] payload = Build((ref BcrdtBufferWriter writer) =>
        {
            for (int i = 0; i < 70; i++)
            {
                writer.WriteByte((byte)BcrdtAnyTag.Object);
                writer.WriteVarUInt(900_000);
                writer.WriteVarUInt(0);
            }
            writer.WriteRaw(new byte[1_000_000]);
        });

        long before = GC.GetAllocatedBytesForCurrentThread();
        Assert.ThrowsExactly<BcrdtFormatException>(() => BcrdtAnyCodec.Decode(payload));
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.IsLessThan(8L * payload.Length, allocated, $"decoding allocated {allocated:N0} bytes for a {payload.Length:N0} byte payload");
    }

    [TestMethod]
    public void Splitting_a_long_text_block_does_not_copy_it()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("t");
        text.Insert(0, new string('x', 100_000));

        long before = GC.GetAllocatedBytesForCurrentThread();
        doc.Transact(_ =>
        {
            // Every other character: one split per deletion, each of what remains of the block.
            for (int i = 0; i < 50_000; i++) text.Delete(i, 1);
        });
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(new string('x', 50_000), text.ToString());
        Assert.IsLessThan(200_000_000L, allocated, $"fifty thousand splits allocated {allocated:N0} bytes");
    }

    [TestMethod]
    public void Split_text_blocks_rejoin_into_the_original_text_including_split_surrogates()
    {
        const string Original = "a😀b😀c";
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("t");
        text.Insert(0, Original);

        // Split between the halves of each pair, then delete and undo nothing: the text must read
        // back whole, and a peer must read the same.
        text.Insert(2, "|");
        text.Delete(2, 1);
        text.Insert(6, "|");
        text.Delete(6, 1);
        Assert.AreEqual(Original, text.ToString());

        using BcrdtDocument peer = Doc(2);
        peer.ApplyUpdate(doc.EncodeStateAsUpdate());
        Assert.AreEqual(Original, peer.GetText("t").ToString());
        BcrdtConvergenceHarness.AssertStructurallyIdentical(doc, peer);
    }

    // ---------------------------------------------------------------- splitting updates

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    public void A_split_update_rebuilds_the_same_document_in_any_order(int seed)
    {
        var random = new Random(seed);
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);

        // Two authors, long runs, scattered deletions, values and nested types: everything a split must
        // keep straight.
        for (int round = 0; round < 20; round++)
        {
            BcrdtDocument author = round % 2 == 0 ? a : b;
            BcrdtText text = author.GetText("body");
            text.Insert(random.Next(text.Count + 1), new string((char)('a' + random.Next(26)), random.Next(1, 3000)));
            if (text.Count > 10 && random.Next(3) == 0) text.Delete(random.Next(text.Count - 5), random.Next(1, 5));
            author.GetArray("items").Push([.. Enumerable.Range(0, random.Next(1, 200)).Select(i => (object?)(long)i)]);
            author.GetMap("m").Set($"k{random.Next(5)}", new string('v', random.Next(1, 50)));

            byte[] toOther = author.EncodeStateAsUpdate((author == a ? b : a).GetStateVector());
            (author == a ? b : a).ApplyUpdate(toOther);
        }

        byte[] whole = a.EncodeStateAsUpdate();
        // Above the largest indivisible value here - a map value is one unit - so every part can fit.
        int budget = 256 + random.Next(2_000);
        IReadOnlyList<byte[]> parts = BcrdtUpdateOperations.Split(whole, budget);

        Assert.IsGreaterThan(1, parts.Count);
        foreach (byte[] part in parts) Assert.IsLessThanOrEqualTo(budget, part.Length, $"a part of {part.Length} bytes for a budget of {budget}");

        using BcrdtDocument inOrder = Doc(3);
        foreach (byte[] part in parts) inOrder.ApplyUpdate(part);
        BcrdtConvergenceHarness.AssertStructurallyIdentical(a, inOrder);

        using BcrdtDocument shuffled = Doc(4);
        foreach (byte[] part in parts.OrderBy(_ => random.Next())) shuffled.ApplyUpdate(part);
        BcrdtConvergenceHarness.AssertStructurallyIdentical(a, shuffled);
        Assert.IsFalse(shuffled.HasPendingUpdates);
    }

    [TestMethod]
    public void An_update_that_fits_is_not_split_and_an_indivisible_value_goes_whole()
    {
        using BcrdtDocument doc = Doc(1);
        doc.GetText("t").Insert(0, "small");
        byte[] small = doc.EncodeStateAsUpdate();
        CollectionAssert.AreEqual(small, BcrdtUpdateOperations.Split(small, 1024).Single());

        doc.GetMap("m").Set("blob", new byte[10_000]);
        IReadOnlyList<byte[]> parts = BcrdtUpdateOperations.Split(doc.EncodeStateAsUpdate(), 1024);
        Assert.IsTrue(parts.Any(part => part.Length > 10_000), "one byte array cannot be divided");

        using BcrdtDocument rebuilt = Doc(2);
        foreach (byte[] part in parts) rebuilt.ApplyUpdate(part);
        BcrdtConvergenceHarness.AssertStructurallyIdentical(doc, rebuilt);
    }

    // ---------------------------------------------------------------- index caches

    [TestMethod]
    public void A_local_edit_after_an_update_in_the_same_transaction_lands_where_it_was_aimed()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);

        void Exchange()
        {
            byte[] toB = a.EncodeStateAsUpdate(b.GetStateVector());
            byte[] toA = b.EncodeStateAsUpdate(a.GetStateVector());
            b.ApplyUpdate(toB);
            a.ApplyUpdate(toA);
        }

        // Interleaved authors, so the array is many blocks and lookups use the index cache.
        for (int i = 0; i < 10; i++)
        {
            (i % 2 == 0 ? a : b).GetArray("x").Push((long)i);
            Exchange();
        }

        BcrdtArray array = a.GetArray("x");
        _ = array[5];

        b.GetArray("x").Insert(0, "front");
        byte[] update = b.EncodeStateAsUpdate(a.GetStateVector());

        a.Transact(_ =>
        {
            a.ApplyUpdate(update);
            array.Insert(5, "X");
        });

        Assert.AreEqual("X", array[5]);
    }

    // ---------------------------------------------------------------- presence

    [TestMethod]
    public void A_forged_presence_clock_at_the_top_of_the_range_cannot_wrap_a_victims_clock()
    {
        static byte[] Forge(ulong clientId, ulong clock, string name) => Build((ref BcrdtBufferWriter writer) =>
        {
            writer.WriteVarUInt(1);
            writer.WriteVarUInt(clientId);
            writer.WriteVarUInt(clock);
            BcrdtAnyCodec.Write(ref writer, new Dictionary<string, object?> { ["name"] = name });
        });

        using var victim = new BcrdtAwarenessStore(1);
        using var peer = new BcrdtAwarenessStore(2);

        // At the top of the range: refused outright.
        Assert.ThrowsExactly<BcrdtFormatException>(() => victim.ApplyUpdate(Forge(1, long.MaxValue, "Mallory")));

        // Far ahead, but in range: peers believe it for now, and the victim does not adopt it.
        byte[] farAhead = Forge(1, 1UL << 40, "Mallory");
        peer.ApplyUpdate(farAhead);
        victim.ApplyUpdate(farAhead);
        victim.SetLocalField("name", "Ada");

        // The victim's own updates stay well formed, and still decode everywhere.
        byte[] own = victim.EncodeUpdate([1]);
        peer.ApplyUpdate(own);
        Assert.IsLessThan(1L << 32, victim.Entries[1].Clock);
    }
}
