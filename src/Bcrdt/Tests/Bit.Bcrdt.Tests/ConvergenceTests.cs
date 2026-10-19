namespace Bit.Bcrdt.Tests;

/// <summary>
/// The property the entire system exists to guarantee, tested by randomized exploration.
/// </summary>
/// <remarks>
/// Every test here states the same law - replicas that have seen the same operations hold the same
/// state - and attacks it from a different angle. Each is seeded and prints its seed on failure, so
/// a red build is reproducible rather than a mystery.
/// </remarks>
[TestClass]
public class ConvergenceTests
{
    public static IEnumerable<object[]> Seeds
    {
        get
        {
            for (int seed = 1; seed <= 25; seed++) yield return [seed];
        }
    }

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public void Random_concurrent_edits_converge_on_a_reliable_network(int seed)
    {
        using var network = new BcrdtSimulatedNetwork(new BcrdtSimulatedNetworkOptions { Seed = seed });
        BcrdtSimulatedPeer[] peers = network.AddPeers(4);
        var generator = new BcrdtOperationGenerator(seed);

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

        AssertConverged(network, seed, history);
    }

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public void Random_edits_converge_despite_loss_duplication_and_reordering(int seed)
    {
        using var network = new BcrdtSimulatedNetwork(new BcrdtSimulatedNetworkOptions
        {
            Seed = seed,
            DropProbability = 0.25,
            DuplicateProbability = 0.25,
            MaxLatencySteps = 5,
        });

        BcrdtSimulatedPeer[] peers = network.AddPeers(3);
        var generator = new BcrdtOperationGenerator(seed);

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

        // A quarter of every message was thrown away, including a quarter of the repair attempts, so
        // a single handshake round is not guaranteed to succeed. Retrying is exactly what a real
        // client does, and the number of rounds it takes is itself worth observing.
        int roundsNeeded = 0;
        for (int round = 1; round <= 12; round++)
        {
            foreach (BcrdtSimulatedPeer peer in peers) peer.RequestSync();
            network.Flush();

            if (AllAgree(peers))
            {
                roundsNeeded = round;
                break;
            }
        }

        // Whatever the lossy channel did, delivering the outstanding state must reconcile it. This
        // models the one assumption every operation-based CRDT genuinely needs: that a message which
        // matters is eventually delivered at least once.
        network.SynchronizeAll();

        AssertConverged(network, seed, history);
        Assert.IsTrue(
            roundsNeeded > 0,
            $"Seed {seed}: handshake retries never reconciled the replicas within 12 rounds, " +
            "which suggests repair is not making progress rather than merely being unlucky.");
    }

    private static bool AllAgree(IReadOnlyList<BcrdtSimulatedPeer> peers)
    {
        string reference = BcrdtConvergenceHarness.Describe(peers[0].Doc);
        return peers.All(peer => BcrdtConvergenceHarness.Describe(peer.Doc) == reference);
    }

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public void Replicas_converge_after_a_network_partition_heals(int seed)
    {
        using var network = new BcrdtSimulatedNetwork(new BcrdtSimulatedNetworkOptions { Seed = seed, MaxLatencySteps = 2 });
        BcrdtSimulatedPeer[] peers = network.AddPeers(4);
        var generator = new BcrdtOperationGenerator(seed);

        foreach (BcrdtSimulatedPeer peer in peers) generator.ApplyRandomTransaction(peer.Doc);
        network.Flush();

        // Split the group in two and let each half diverge independently.
        network.Partition(peers[0], peers[2]);
        network.Partition(peers[0], peers[3]);
        network.Partition(peers[1], peers[2]);
        network.Partition(peers[1], peers[3]);

        for (int round = 0; round < 25; round++)
        {
            foreach (BcrdtSimulatedPeer peer in peers) generator.ApplyRandomTransaction(peer.Doc);
            network.Step();
        }
        network.Flush();

        network.HealPartitions();
        network.Flush();

        AssertConverged(network, seed, []);
    }

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public void A_replica_that_edits_while_offline_converges_when_it_returns(int seed)
    {
        using var network = new BcrdtSimulatedNetwork(new BcrdtSimulatedNetworkOptions { Seed = seed });
        BcrdtSimulatedPeer[] peers = network.AddPeers(3);
        var generator = new BcrdtOperationGenerator(seed);

        foreach (BcrdtSimulatedPeer peer in peers) generator.ApplyRandomTransaction(peer.Doc);
        network.Flush();

        peers[0].GoOffline();

        // Both sides keep working, neither aware of the other.
        for (int round = 0; round < 30; round++)
        {
            generator.ApplyRandomTransaction(peers[0].Doc);
            generator.ApplyRandomTransaction(peers[1].Doc);
            generator.ApplyRandomTransaction(peers[2].Doc);
            network.Step();
        }
        network.Flush();

        // The returning peer's own handshake has to reconcile both directions: no other peer is asked
        // to announce itself, so its offline work reaches them only through their replies to it.
        peers[0].GoOnline();
        network.Flush();

        AssertConverged(network, seed, []);
        BcrdtConvergenceHarness.AssertStructurallyIdentical([.. network.Peers.Select(peer => peer.Doc)]);
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(5)]
    [DataRow(12)]
    public void Many_replicas_converge_under_simultaneous_editing(int replicaCount)
    {
        const int seed = 4242;
        using var network = new BcrdtSimulatedNetwork(new BcrdtSimulatedNetworkOptions { Seed = seed, MaxLatencySteps = 3 });
        BcrdtSimulatedPeer[] peers = network.AddPeers(replicaCount);
        var generator = new BcrdtOperationGenerator(seed);

        for (int round = 0; round < 20; round++)
        {
            foreach (BcrdtSimulatedPeer peer in peers) generator.ApplyRandomTransaction(peer.Doc);
            network.Step();
        }

        network.Flush();
        network.SynchronizeAll();
        AssertConverged(network, seed, []);
    }

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public void Applying_updates_in_any_order_produces_identical_state(int seed)
    {
        var random = new Random(seed);
        var generator = new BcrdtOperationGenerator(seed);

        // Build a history from three authors, capturing every individual update.
        List<byte[]> updates = [];
        using (var authorA = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 }))
        using (var authorB = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 }))
        using (var authorC = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 3 }))
        {
            BcrdtDocument[] authors = [authorA, authorB, authorC];
            foreach (BcrdtDocument author in authors) author.Updated += args => updates.Add(args.Update);

            for (int round = 0; round < 15; round++)
            {
                foreach (BcrdtDocument author in authors)
                {
                    generator.ApplyRandomTransaction(author);

                    // Cross-pollinate so the histories are genuinely causally interleaved rather
                    // than three independent documents that happen to share a name.
                    foreach (BcrdtDocument other in authors)
                    {
                        if (ReferenceEquals(author, other)) continue;
                        other.ApplyUpdate(author.EncodeStateAsUpdate(other.GetStateVector()), origin: "sync");
                    }
                }
            }
        }

        // Replay that same set of updates in three different orders.
        using var forward = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 10 });
        using var backward = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 11 });
        using var shuffled = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 12 });

        foreach (byte[] update in updates) forward.ApplyUpdate(update);
        for (int i = updates.Count - 1; i >= 0; i--) backward.ApplyUpdate(updates[i]);

        List<byte[]> order = [.. updates.OrderBy(_ => random.Next())];
        foreach (byte[] update in order) shuffled.ApplyUpdate(update);

        // Reverse and shuffled orders leave blocks whose dependencies never arrived parked as
        // pending, so replay each set once more to let them settle.
        for (int i = updates.Count - 1; i >= 0; i--) backward.ApplyUpdate(updates[i]);
        foreach (byte[] update in order) shuffled.ApplyUpdate(update);

        try
        {
            BcrdtConvergenceHarness.AssertConverged(forward, backward, shuffled);
        }
        catch (BcrdtConvergenceException ex)
        {
            Assert.Fail($"Seed {seed}: {ex.Message}");
        }
    }

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public void Redelivering_every_update_many_times_changes_nothing(int seed)
    {
        var generator = new BcrdtOperationGenerator(seed);

        using var source = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        List<byte[]> updates = [];
        source.Updated += args => updates.Add(args.Update);

        for (int i = 0; i < 30; i++) generator.ApplyRandomTransaction(source);

        using var replica = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        foreach (byte[] update in updates) replica.ApplyUpdate(update);

        string afterFirstPass = BcrdtConvergenceHarness.Describe(replica);

        // Every update, three more times, in a different order each pass.
        var random = new Random(seed);
        for (int pass = 0; pass < 3; pass++)
        {
            foreach (byte[] update in updates.OrderBy(_ => random.Next())) replica.ApplyUpdate(update);
        }

        Assert.AreEqual(afterFirstPass, BcrdtConvergenceHarness.Describe(replica));
    }

    private static void AssertConverged(BcrdtSimulatedNetwork network, int seed, IReadOnlyList<string> history)
    {
        try
        {
            BcrdtConvergenceHarness.AssertConverged(network);
        }
        catch (BcrdtConvergenceException ex)
        {
            string trace = history.Count == 0
                ? string.Empty
                : Environment.NewLine + "--- operation history ---" + Environment.NewLine +
                  string.Join(Environment.NewLine, history.TakeLast(40));

            Assert.Fail(
                $"Seed {seed}. {network.MessagesSent} message(s) sent, {network.MessagesDropped} dropped." +
                Environment.NewLine + ex.Message + trace);
        }
    }
}
