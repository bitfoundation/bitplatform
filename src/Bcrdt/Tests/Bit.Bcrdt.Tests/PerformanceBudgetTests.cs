namespace Bit.Bcrdt.Tests;

/// <summary>
/// Guards the performance properties that are load-bearing for the design.
/// </summary>
/// <remarks>
/// These are not benchmarks - they do not measure time, which is far too noisy to assert on in a
/// test suite. They assert <i>algorithmic</i> properties that would otherwise degrade silently: that
/// an incremental sync stays proportional to the delta, that run-length encoding actually collapses
/// sequential typing, that a keystroke produces a small update, and that position lookups start
/// from the cached position nearest them. A regression in any of these turns into an unexplained
/// slowdown at scale long before anyone thinks to run a benchmark.
/// </remarks>
[TestClass]
public class PerformanceBudgetTests
{
    private static long Measure(Action action, int iterations)
    {
        // Settle first, so the measurement is not attributing warm-up allocations to the work.
        action();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < iterations; i++) action();
        return (GC.GetAllocatedBytesForCurrentThread() - before) / iterations;
    }

    /// <summary>
    /// Builds a document with content and a realistic scattering of deletions.
    /// </summary>
    /// <remarks>The deletions matter: they are what makes the delete set non-trivial.</remarks>
    private static BcrdtDocument BuildDocument(int characters, int deletions, ulong clientId = 1)
    {
        var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = clientId });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, new string('a', characters));

        var random = new Random(3);
        for (int i = 0; i < deletions; i++)
        {
            text.Delete(random.Next(text.Count - 10), 5);
        }
        return doc;
    }

    [TestMethod]
    public void Answering_an_up_to_date_peer_does_not_scale_with_the_documents_content()
    {
        // Same number of deletions, forty times the content. A sync response always carries the
        // whole delete set - that is how a lost deletion gets repaired, since deletions do not
        // advance a state vector and so cannot be detected as missing - but it must not carry, or
        // rebuild, anything proportional to the content itself.
        using BcrdtDocument small = BuildDocument(2_000, 200);
        using BcrdtDocument large = BuildDocument(80_000, 200);

        BcrdtStateVector smallState = small.GetStateVector();
        BcrdtStateVector largeState = large.GetStateVector();

        long smallCost = Measure(() => small.EncodeStateAsUpdate(smallState), 200);
        long largeCost = Measure(() => large.EncodeStateAsUpdate(largeState), 200);

        // This is the case a busy server hits constantly: many clients asking "anything new?" about
        // a document that has not changed. Without the document-level delete-set cache this scaled
        // linearly with content, which is invisible in small tests and ruinous at scale.
        Assert.IsTrue(
            largeCost < smallCost * 2,
            $"Answering an up-to-date peer allocated {smallCost:N0} bytes for a small document and " +
            $"{largeCost:N0} bytes for one holding 40x the content with the same number of " +
            "deletions. The cost is scaling with content rather than with what has to be sent.");
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    public void The_cached_delete_set_never_disagrees_with_a_freshly_derived_one(int seed)
    {
        // The cache behind GetDeleteSet is an optimization, and a stale delete set would corrupt
        // synchronization in a way that is very hard to trace back here. So it is checked against
        // the ground truth after every single change.
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var peer = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        var generator = new BcrdtOperationGenerator(seed);

        for (int round = 0; round < 60; round++)
        {
            generator.ApplyRandomTransaction(doc);
            AssertDeleteSetsAgree(doc, $"seed {seed}, after local change {round}");

            // Remote updates delete content too, and they take a different code path.
            generator.ApplyRandomTransaction(peer);
            doc.ApplyUpdate(peer.EncodeStateAsUpdate(doc.GetStateVector()), "sync");
            AssertDeleteSetsAgree(doc, $"seed {seed}, after remote change {round}");
        }
    }

    private static void AssertDeleteSetsAgree(BcrdtDocument doc, string context)
    {
        BcrdtDeleteSet cached = doc.GetDeleteSet();
        BcrdtDeleteSet derived = BcrdtDeleteSet.FromStore(doc.Store);

        Assert.AreEqual(derived.Clients.Count, cached.Clients.Count);
        foreach ((ulong client, List<BcrdtDeleteRange> expected) in derived.Clients)
        {
            Assert.IsTrue(cached.Clients.TryGetValue(client, out List<BcrdtDeleteRange>? actual),
                $"Cached delete set is missing client {client} ({context}).");
            Assert.AreSequenceEqual(expected, actual);
        }
    }

    [TestMethod]
    public void An_incremental_update_is_proportional_to_the_change_not_the_document()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, new string('a', 40_000));

        BcrdtStateVector peerState = doc.GetStateVector();
        text.Insert(0, "x");

        byte[] delta = doc.EncodeStateAsUpdate(peerState);
        byte[] whole = doc.EncodeStateAsUpdate();

        Assert.IsTrue(
            delta.Length < 100,
            $"A one-character delta encoded to {delta.Length} bytes against a {whole.Length} byte document.");
    }

    [TestMethod]
    public void Sequential_typing_collapses_into_a_handful_of_blocks()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");

        for (int i = 0; i < 20_000; i++) text.Insert(text.Count, "x");

        int blocks = doc.Store.GetClientStructs(doc.ClientId)!.Count;

        // The whole affordability argument for collaborative text rests on this number staying tiny.
        Assert.IsTrue(blocks < 20, $"20 000 keystrokes produced {blocks} blocks; they should have merged.");
    }

    [TestMethod]
    public void A_keystroke_produces_a_small_update_regardless_of_document_size()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, new string('a', 100_000));

        byte[]? lastUpdate = null;
        doc.Updated += args => lastUpdate = args.Update;

        text.Insert(50_000, "x");

        Assert.IsNotNull(lastUpdate);
        Assert.IsTrue(
            lastUpdate!.Length < 64,
            $"A keystroke in a 100 000 character document produced a {lastUpdate.Length} byte update.");
    }

    [TestMethod]
    public void A_state_vector_stays_small_as_the_document_grows()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, new string('a', 100_000));

        // Sync cost is driven by this, and it must scale with the number of authors rather than with
        // the content - otherwise reconnecting to a large document gets slower forever.
        Assert.IsTrue(doc.GetStateVector().Encode().Length < 32);
    }

    [TestMethod]
    public void Reads_near_the_end_of_a_fragmented_sequence_start_from_a_cached_position()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtArray array = doc.GetArray("items");

        // Scattered inserts, so the sequence really is thousands of separate blocks rather than one.
        var random = new Random(11);
        for (int i = 0; i < 5_000; i++) array.Insert(random.Next(array.Count + 1), (long)i);

        int blocks = doc.Store.GetClientStructs(doc.ClientId)!.Count;
        Assert.IsTrue(blocks > 1_000, $"The fixture holds only {blocks} blocks, too few to exercise the cache.");

        List<object?> expected = array.ToArray();
        int target = array.Count - 10;

        // Start cold, so every marker examined below was placed by the reads in this test.
        array.InvalidateSearchMarkers();

        // The first read near the end has nothing to start from, so it walks there once - and must
        // leave a marker where it stopped.
        Assert.AreEqual(expected[target], array[target]);
        BcrdtSearchMarker placed = Assert.ContainsSingle(array.SearchMarkers!);
        Assert.IsInRange(0, 63, target - placed.Index);

        // Every neighbouring read must then start from that marker and carry it along. A lookup that
        // ignored the cache and walked from the head of the list would either leave a fresh marker
        // behind on each read or leave this one stranded at the first position; both fail here.
        int lastRead = target;
        for (int index = target - 1; index > target - 100; index--)
        {
            Assert.AreEqual(expected[index], array[index]);
            lastRead = index;
        }

        BcrdtSearchMarker carried = Assert.ContainsSingle(array.SearchMarkers!);
        Assert.IsInRange(0, 63, lastRead - carried.Index);
    }
}
