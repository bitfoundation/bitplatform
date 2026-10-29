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

    private static byte[] CollectedRange(ulong client, ulong clock, int length = 1, int count = 1) => Build((ref BcrdtBufferWriter writer) =>
    {
        writer.WriteVarUInt(1);
        writer.WriteVarUInt((ulong)count);
        writer.WriteVarUInt(client);
        writer.WriteVarUInt(clock);
        for (int i = 0; i < count; i++)
        {
            writer.WriteByte((byte)BcrdtContentRef.GarbageCollected);
            writer.WriteVarUInt((ulong)length);
        }
        writer.WriteVarUInt(0);
    });

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

    [TestMethod]
    public void Merging_blocks_further_apart_than_any_replica_writes_is_refused()
    {
        long limit = BcrdtUpdateCodec.MaxClockGap;

        byte[] widest = BcrdtUpdateOperations.Merge(CollectedRange(7, 0), CollectedRange(7, (ulong)limit + 1));
        Assert.AreEqual(limit + 1, BcrdtUpdateOperations.DecodeBlocks(widest)[7][^1].Id.Clock);

        Assert.ThrowsExactly<BcrdtFormatException>(() => BcrdtUpdateOperations.Merge(CollectedRange(7, 0), CollectedRange(7, (ulong)limit + 2)));
        Assert.ThrowsExactly<BcrdtFormatException>(() => BcrdtUpdateOperations.Merge(CollectedRange(7, 5), CollectedRange(7, 1UL << 62)));
    }

    [TestMethod]
    public void A_block_far_ahead_of_its_authors_content_is_refused_before_anything_changes()
    {
        using BcrdtDocument source = Doc(7);
        source.GetText("t").Insert(0, "abcde");

        using BcrdtDocument doc = Doc(1);
        doc.ApplyUpdate(source.EncodeStateAsUpdate());
        doc.ApplyUpdate(CollectedRange(7, 10));
        byte[] state = doc.EncodeStateAsUpdate();

        byte[] hostile = Build((ref BcrdtBufferWriter writer) =>
        {
            writer.WriteVarUInt(2);
            writer.WriteVarUInt(1);
            writer.WriteVarUInt(9);
            writer.WriteVarUInt(0);
            writer.WriteByte((byte)BcrdtContentRef.GarbageCollected);
            writer.WriteVarUInt(3);
            writer.WriteVarUInt(1);
            writer.WriteVarUInt(7);
            writer.WriteVarUInt(1UL << 62);
            writer.WriteByte((byte)BcrdtContentRef.GarbageCollected);
            writer.WriteVarUInt(1);
            writer.WriteVarUInt(0);
        });

        Assert.ThrowsExactly<BcrdtFormatException>(() => doc.ApplyUpdate(hostile));
        Assert.ThrowsExactly<BcrdtFormatException>(() => doc.ApplyUpdate(CollectedRange(7, 6 + (ulong)BcrdtUpdateCodec.MaxClockGap)));
        Assert.AreEqual(0L, doc.GetStateVector().Get(9));
        CollectionAssert.AreEqual(state, doc.EncodeStateAsUpdate());

        doc.ApplyUpdate(CollectedRange(7, 5 + (ulong)BcrdtUpdateCodec.MaxClockGap));
        Assert.AreEqual("abcde", doc.GetText("t").ToString());
        Assert.IsTrue(doc.HasPendingUpdates);
    }

    [TestMethod]
    public void Waiting_blocks_on_both_sides_of_a_long_collected_range_are_kept_and_integrate()
    {
        const ulong Collected = 6UL * int.MaxValue;

        using BcrdtDocument other = Doc(9);
        other.GetText("t").Insert(0, "x");

        using BcrdtDocument author = Doc(7);
        author.ApplyUpdate(other.EncodeStateAsUpdate());
        author.GetText("t").Insert(1, "y");
        byte[] waitsForOther = author.EncodeStateAsUpdate(other.GetStateVector());

        using BcrdtDocument doc = Doc(1);
        doc.ApplyUpdate(waitsForOther);
        doc.ApplyUpdate(CollectedRange(7, 0, int.MaxValue, 6));
        doc.ApplyUpdate(CollectedRange(7, Collected + 1));
        Assert.IsTrue(doc.HasPendingUpdates);

        using BcrdtDocument peer = Doc(2);
        peer.ApplyUpdate(doc.EncodeStateAsUpdate());
        Assert.AreEqual((long)Collected, peer.GetStateVector().Get(7));

        doc.ApplyUpdate(other.EncodeStateAsUpdate());
        doc.ApplyUpdate(CollectedRange(7, Collected));
        Assert.AreEqual((long)Collected + 2, doc.GetStateVector().Get(7));
        Assert.IsFalse(doc.HasPendingUpdates);
    }

    [TestMethod]
    public void An_update_whose_every_author_waits_is_held_whole_and_integrates_later()
    {
        const int Authors = 20_000;

        static byte[] Groups(ulong clock) => Build((ref BcrdtBufferWriter writer) =>
        {
            writer.WriteVarUInt(Authors);
            for (ulong client = 100; client < 100 + Authors; client++)
            {
                writer.WriteVarUInt(1);
                writer.WriteVarUInt(client);
                writer.WriteVarUInt(clock);
                writer.WriteByte((byte)BcrdtContentRef.GarbageCollected);
                writer.WriteVarUInt(1);
            }
            writer.WriteVarUInt(0);
        });

        using BcrdtDocument doc = Doc(1);
        doc.ApplyUpdate(Groups(1));
        Assert.IsTrue(doc.HasPendingUpdates);
        Assert.AreEqual(0, doc.GetStateVector().Count);

        doc.ApplyUpdate(Groups(0));
        Assert.IsFalse(doc.HasPendingUpdates);
        Assert.AreEqual(Authors, doc.GetStateVector().Count);
        Assert.AreEqual(2L, doc.GetStateVector().Get(100));
        Assert.AreEqual(2L, doc.GetStateVector().Get(100 + Authors - 1));
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

    [TestMethod]
    public void Deleting_every_other_value_of_a_long_run_does_not_copy_it()
    {
        using BcrdtDocument source = Doc(1);
        source.GetArray("a").Push([.. Enumerable.Repeat<object?>(null, 40_000)]);

        var everyOther = new BcrdtDeleteSet();
        for (int clock = 1; clock < 40_000; clock += 2) everyOther.Add(1, clock, 1);
        byte[] deletions = Build((ref BcrdtBufferWriter writer) =>
        {
            writer.WriteVarUInt(0);
            everyOther.Write(ref writer);
        });

        using BcrdtDocument doc = Doc(2);
        doc.ApplyUpdate(source.EncodeStateAsUpdate());

        long before = GC.GetAllocatedBytesForCurrentThread();
        doc.ApplyUpdate(deletions);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(20_000, doc.GetArray("a").Count);
        source.ApplyUpdate(deletions);
        BcrdtConvergenceHarness.AssertStructurallyIdentical(source, doc);
        Assert.IsLessThan(64_000_000L, allocated, $"twenty thousand deletions allocated {allocated:N0} bytes");
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(10)]
    [DataRow(50)]
    [DataRow(90)]
    [DataRow(99)]
    public void A_split_value_run_reads_grows_and_rejoins_exactly(int offset)
    {
        object?[] values = [.. Enumerable.Range(0, 100).Select(i => (object?)(long)i)];

        var left = new BcrdtContentAny(values);
        var right = (BcrdtContentAny)left.Splice(offset);
        CollectionAssert.AreEqual(values[..offset], left.GetContent().ToArray());
        CollectionAssert.AreEqual(values[offset..], right.GetContent().ToArray());
        CollectionAssert.AreEqual(values[offset..], Written(right));
        Assert.AreEqual(values[offset], right.GetContent()[0]);

        Assert.IsTrue(right.TryMergeWith(new BcrdtContentAny(100L)));
        Assert.IsTrue(left.TryMergeWith(right));
        CollectionAssert.AreEqual(values.Append(100L).ToArray(), left.GetContent().ToArray());
        CollectionAssert.AreEqual(values.Append(100L).ToArray(), Written(left));

        var head = new BcrdtContentAny(values);
        var tail = (BcrdtContentAny)head.Splice(offset);
        IBcrdtContent copy = tail.Copy();
        Assert.IsTrue(head.TryMergeWith(new BcrdtContentAny(-1L)));
        Assert.IsTrue(tail.TryMergeWith(new BcrdtContentAny(100L)));
        CollectionAssert.AreEqual(values[..offset].Append(-1L).ToArray(), head.GetContent().ToArray());
        CollectionAssert.AreEqual(values[offset..].Append(100L).ToArray(), tail.GetContent().ToArray());
        CollectionAssert.AreEqual(values[offset..], copy.GetContent().ToArray());
    }

    private static object?[] Written(BcrdtContentAny content)
    {
        var writer = new BcrdtBufferWriter(64);
        try
        {
            content.Write(ref writer, 0);
            var reader = new BcrdtBufferReader(writer.ToArray());
            return [.. BcrdtContentAny.Read(ref reader).GetContent()];
        }
        finally
        {
            writer.Dispose();
        }
    }

    [TestMethod]
    public void A_root_keeps_its_name_through_an_upgrade_among_many_roots()
    {
        using BcrdtDocument source = Doc(1);
        for (int i = 0; i < 5_000; i++) source.GetArray($"r{i}").Push((long)i);

        using BcrdtDocument doc = Doc(2);
        doc.ApplyUpdate(source.EncodeStateAsUpdate());

        BcrdtAbstractType placeholder = doc.RootTypes["r7"];
        Assert.AreEqual("r7", placeholder.FindRootKey());
        BcrdtArray typed = doc.GetArray("r7");
        Assert.AreEqual("r7", typed.FindRootKey());
        Assert.ThrowsExactly<InvalidOperationException>(() => placeholder.FindRootKey());

        using BcrdtDocument peer = Doc(3);
        peer.ApplyUpdate(doc.EncodeStateAsUpdate());
        BcrdtConvergenceHarness.AssertStructurallyIdentical(source, peer);
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

    [TestMethod]
    public void A_split_part_stays_within_its_limit_however_wide_its_group_headers()
    {
        // Hundreds of clients with the widest identifiers and clocks there are, one small block each:
        // a group header takes twenty bytes here, and every part holds dozens of them. Budgeting a fixed
        // sixteen bytes a group let a part overshoot by far more than its slack, and a relay limit set
        // just above the provider's would then disconnect the client the split was meant to protect.
        const int Clients = 300;
        const long Clock = 1L << 60;
        byte[] update = Build((ref BcrdtBufferWriter writer) =>
        {
            writer.WriteVarUInt(Clients);
            for (int i = 0; i < Clients; i++)
            {
                writer.WriteVarUInt(1);
                writer.WriteVarUInt(ulong.MaxValue - (ulong)i);
                writer.WriteVarUInt((ulong)(Clock + i));
                writer.WriteByte((byte)BcrdtContentRef.GarbageCollected);
                writer.WriteVarUInt(1);
            }

            // A delete set as wide, for the first part to carry as well.
            writer.WriteVarUInt(20);
            for (int i = 0; i < 20; i++)
            {
                writer.WriteVarUInt(ulong.MaxValue - 1_000 - (ulong)i);
                writer.WriteVarUInt(1);
                writer.WriteVarUInt((ulong)Clock);
                writer.WriteVarUInt(0);
            }
        });

        const int MaxBytes = 1_000;
        IReadOnlyList<byte[]> parts = BcrdtUpdateOperations.Split(update, MaxBytes);

        Assert.IsGreaterThan(1, parts.Count);
        foreach (byte[] part in parts) Assert.IsLessThanOrEqualTo(MaxBytes, part.Length, $"a part of {part.Length} bytes for a limit of {MaxBytes}");

        // Nothing was lost on the way: every block once, at its clock, and the deletions in the first part.
        Dictionary<ulong, long> clocks = [];
        foreach (byte[] part in parts)
        {
            foreach ((ulong client, IReadOnlyList<BcrdtAbstractStruct> blocks) in BcrdtUpdateOperations.DecodeBlocks(part))
            {
                foreach (BcrdtAbstractStruct block in blocks) clocks.Add(client, block.Id.Clock);
            }
        }
        Assert.HasCount(Clients, clocks);
        for (int i = 0; i < Clients; i++) Assert.AreEqual(Clock + i, clocks[ulong.MaxValue - (ulong)i]);
        Assert.HasCount(20, BcrdtUpdateOperations.DecodeDeleteSet(parts[0]).Clients);
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
