namespace Bit.Bcrdt.Tests;

/// <summary>
/// <see cref="BcrdtCounter"/>: concurrent changes add up, and no increment is ever lost.
/// </summary>
[TestClass]
public class CounterTests
{
    public static IEnumerable<object[]> Seeds
    {
        get
        {
            for (int seed = 1; seed <= 25; seed++) yield return [seed];
        }
    }

    private static BcrdtDocument Doc(ulong clientId, bool garbageCollect = true) =>
        new(new BcrdtDocumentOptions { ClientId = clientId, GarbageCollect = garbageCollect });

    private static void Sync(BcrdtDocument from, BcrdtDocument to) =>
        to.ApplyUpdate(from.EncodeStateAsUpdate(to.GetStateVector()), origin: "sync");

    private static void SyncAll(params BcrdtDocument[] docs)
    {
        foreach (BcrdtDocument source in docs)
        {
            foreach (BcrdtDocument target in docs)
            {
                if (!ReferenceEquals(source, target)) Sync(source, target);
            }
        }
    }

    [TestMethod]
    public void A_counter_starts_at_zero_and_adds_and_subtracts()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtCounter counter = doc.GetCounter("votes");

        Assert.AreEqual(0, counter.Value);

        counter.Increment();
        counter.Add(10);
        counter.Decrement();
        counter.Add(-3);

        Assert.AreEqual(7, counter.Value);
        Assert.AreEqual(7, counter.LocalShare);
        Assert.AreEqual(7L, doc.ToJson()["votes"]);
    }

    [TestMethod]
    public void The_harness_takes_an_untouched_counter_for_an_absent_one_but_compares_one_that_was_changed()
    {
        using BcrdtDocument declared = Doc(1);
        using BcrdtDocument absent = Doc(2);
        declared.GetCounter("votes");

        BcrdtConvergenceHarness.AssertConverged(declared, absent);

        // Changes that sum to zero are still replicated state, which the other replica does not have.
        declared.GetCounter("votes").Increment();
        declared.GetCounter("votes").Decrement();
        Assert.ThrowsExactly<BcrdtConvergenceException>(() => BcrdtConvergenceHarness.AssertConverged(declared, absent));

        Sync(declared, absent);
        BcrdtConvergenceHarness.AssertConverged(declared, absent);
    }

    [TestMethod]
    public void Adding_zero_writes_nothing()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtCounter counter = doc.GetCounter("votes");

        int updates = 0;
        doc.Updated += _ => updates++;
        counter.Add(0);

        Assert.AreEqual(0, updates);
    }

    [TestMethod]
    public void Concurrent_increments_and_decrements_add_up()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        using BcrdtDocument c = Doc(3);

        // Every replica starts from the same value and none sees the others: the read-modify-write a
        // map field would do here ends at whichever single write wins.
        a.GetCounter("votes").Add(5);
        b.GetCounter("votes").Add(3);
        c.GetCounter("votes").Add(-2);
        a.GetCounter("votes").Add(1);

        SyncAll(a, b, c);

        Assert.AreEqual(7, a.GetCounter("votes").Value);
        Assert.AreEqual(7, b.GetCounter("votes").Value);
        Assert.AreEqual(7, c.GetCounter("votes").Value);
        Assert.AreEqual(6, a.GetCounter("votes").LocalShare);
        Assert.AreEqual(-2, c.GetCounter("votes").LocalShare);
    }

    [TestMethod]
    public void Applying_the_same_update_twice_counts_it_once()
    {
        using BcrdtDocument author = Doc(1);
        using BcrdtDocument reader = Doc(2);

        byte[]? last = null;
        author.Updated += e => last = e.Update;
        author.GetCounter("votes").Add(4);

        reader.ApplyUpdate(last!);
        reader.ApplyUpdate(last!);
        Sync(author, reader);

        Assert.AreEqual(4, reader.GetCounter("votes").Value);
    }

    [TestMethod]
    public void A_counter_nests_inside_maps_and_arrays_and_keeps_its_kind_on_the_wire()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);

        BcrdtCounter likes = a.GetMap("post").EnsureCounter("likes");
        likes.Add(2);
        a.GetArray("polls").Push(new BcrdtCounter(10));

        Sync(a, b);

        BcrdtCounter remoteLikes = b.GetMap("post").GetCounter("likes")!;
        var remotePoll = (BcrdtCounter)b.GetArray("polls")[0]!;
        Assert.AreEqual(2, remoteLikes.Value);
        Assert.AreEqual(10, remotePoll.Value);

        remoteLikes.Add(3);
        likes.Add(1);
        SyncAll(a, b);

        Assert.AreEqual(6, likes.Value);
        Assert.AreEqual(6, remoteLikes.Value);
        Assert.AreEqual(BcrdtConvergenceHarness.Describe(a), BcrdtConvergenceHarness.Describe(b));
    }

    [TestMethod]
    public void A_detached_counter_carries_its_value_into_the_document()
    {
        var counter = new BcrdtCounter(4);
        counter.Add(2);
        Assert.AreEqual(6, counter.Value);

        using BcrdtDocument doc = Doc(1);
        doc.GetMap("meta").Set("count", counter);

        Assert.IsTrue(counter.IsAttached);
        Assert.AreEqual(6, counter.Value);
        Assert.AreEqual(6, counter.LocalShare);

        BcrdtCounter clone = counter.Clone();
        Assert.IsFalse(clone.IsAttached);
        Assert.AreEqual(6, clone.Value);
    }

    [TestMethod]
    public void A_counter_survives_a_round_trip_through_storage()
    {
        using BcrdtDocument original = Doc(1);
        original.GetCounter("votes").Add(41);
        original.GetCounter("votes").Increment();

        using BcrdtDocument loaded = Doc(2);
        loaded.ApplyUpdate(original.EncodeStateAsUpdate());

        Assert.AreEqual(42, loaded.GetCounter("votes").Value);

        // A new session adds its own share alongside the one it loaded.
        loaded.GetCounter("votes").Add(8);
        Assert.AreEqual(50, loaded.GetCounter("votes").Value);
        Assert.AreEqual(8, loaded.GetCounter("votes").LocalShare);
    }

    [TestMethod]
    public void Content_that_arrives_before_the_root_kind_is_declared_is_adopted()
    {
        using BcrdtDocument author = Doc(1);
        author.GetCounter("votes").Add(9);

        using BcrdtDocument reader = Doc(2);
        reader.ApplyUpdate(author.EncodeStateAsUpdate());

        Assert.AreEqual(9, reader.GetCounter("votes").Value);
    }

    [TestMethod]
    public void A_change_event_reports_how_far_the_total_moved()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        a.GetCounter("votes").Add(10);
        Sync(a, b);

        List<long> changes = [];
        using IDisposable subscription = b.GetCounter("votes").Observe((e, _) => changes.Add(((BcrdtCounterEvent)e).Change));

        a.GetCounter("votes").Add(-4);
        Sync(a, b);
        b.GetCounter("votes").Add(7);

        Assert.AreSequenceEqual(new long[] { -4, 7 }, changes);
    }

    [TestMethod]
    public void Undo_takes_back_only_this_replicas_changes()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        BcrdtCounter counter = a.GetCounter("votes");
        using var undo = new BcrdtUndoManager(counter, new BcrdtUndoManagerOptions { CaptureTimeout = TimeSpan.Zero });

        counter.Add(5);
        b.GetCounter("votes").Add(100);
        Sync(b, a);
        counter.Add(3);

        Assert.AreEqual(108, counter.Value);

        undo.Undo();
        Assert.AreEqual(105, counter.Value);
        undo.Undo();
        Assert.AreEqual(100, counter.Value);

        undo.Redo();
        undo.Redo();
        Assert.AreEqual(108, counter.Value);

        SyncAll(a, b);
        Assert.AreEqual(108, b.GetCounter("votes").Value);
    }

    [TestMethod]
    public void A_local_change_that_would_overflow_its_share_throws_and_changes_nothing()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtCounter counter = doc.GetCounter("votes");
        counter.Add(long.MaxValue);

        Assert.ThrowsExactly<OverflowException>(() => counter.Add(1));
        Assert.AreEqual(long.MaxValue, counter.Value);
    }

    [TestMethod]
    public void A_total_past_long_range_wraps_identically_on_every_replica()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        a.GetCounter("votes").Add(long.MaxValue);
        b.GetCounter("votes").Add(2);

        SyncAll(a, b);

        Assert.AreEqual(unchecked(long.MaxValue + 2), a.GetCounter("votes").Value);
        Assert.AreEqual(a.GetCounter("votes").Value, b.GetCounter("votes").Value);
    }

    [TestMethod]
    public void Shares_written_by_a_peer_that_are_not_integers_are_ignored_by_every_replica()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);

        a.GetCounter("votes").Add(3);

        // What a misbehaving peer could write through the generic surface.
        BcrdtCounter counter = b.GetCounter("votes");
        b.Transact(transaction => BcrdtTypeOperations.MapSet(transaction, counter, "junk", "not a number"));

        SyncAll(a, b);

        Assert.AreEqual(3, a.GetCounter("votes").Value);
        Assert.AreEqual(3, b.GetCounter("votes").Value);
    }

    [TestMethod]
    [DataRow(4UL)]
    [DataRow(9UL)]
    [DataRow(200UL)]
    public void A_nested_type_kind_that_is_untyped_or_unknown_is_rejected(ulong kind)
    {
        byte[] payload = HandWrittenUpdates.Build((ref BcrdtBufferWriter writer) => writer.WriteVarUInt(kind));
        var reader = new BcrdtBufferReader(payload);

        try
        {
            BcrdtContentType.Read(ref reader);
            Assert.Fail($"Kind {kind} was accepted.");
        }
        catch (BcrdtFormatException)
        {
        }
    }

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public void No_increment_is_lost_under_loss_duplication_reordering_and_partitions(int seed)
    {
        using var network = new BcrdtSimulatedNetwork(new BcrdtSimulatedNetworkOptions
        {
            Seed = seed,
            DropProbability = 0.25,
            DuplicateProbability = 0.25,
            MaxLatencySteps = 5,
        });

        BcrdtSimulatedPeer[] peers = network.AddPeers(4);
        var random = new Random(seed);
        long expected = 0;

        for (int round = 0; round < 40; round++)
        {
            if (round == 10) network.Partition(peers[0], peers[1]);
            if (round == 20) peers[2].GoOffline();
            if (round == 30)
            {
                network.HealPartitions();
                peers[2].GoOnline();
            }

            foreach (BcrdtSimulatedPeer peer in peers)
            {
                long amount = random.Next(-5, 10);
                peer.Doc.GetCounter("votes").Add(amount);
                expected += amount;
            }
            network.Step();
        }

        network.Flush();
        network.SynchronizeAll();

        BcrdtConvergenceHarness.AssertConverged(network);
        foreach (BcrdtSimulatedPeer peer in peers)
        {
            Assert.AreEqual(expected, peer.Doc.GetCounter("votes").Value, $"Seed {seed}: {peer.Name} lost or doubled a change.");
        }
    }

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public void Random_edits_with_sets_and_counters_converge_on_a_lossy_network(int seed)
    {
        using var network = new BcrdtSimulatedNetwork(new BcrdtSimulatedNetworkOptions
        {
            Seed = seed,
            DropProbability = 0.2,
            DuplicateProbability = 0.2,
            MaxLatencySteps = 4,
        });

        BcrdtSimulatedPeer[] peers = network.AddPeers(3);
        var generator = new BcrdtOperationGenerator(seed, new BcrdtOperationGeneratorOptions { Set = true, Counter = true });

        List<string> history = [];
        for (int round = 0; round < 40; round++)
        {
            foreach (BcrdtSimulatedPeer peer in peers)
            {
                history.Add($"{peer.Name}: {generator.ApplyRandomTransaction(peer.Doc)}");
            }
            network.Step();
        }

        network.Flush();
        network.SynchronizeAll();

        try
        {
            BcrdtConvergenceHarness.AssertConverged(network);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Seed {seed} diverged. Last operations:\n{string.Join("\n", history.TakeLast(20))}", ex);
        }
    }
}
