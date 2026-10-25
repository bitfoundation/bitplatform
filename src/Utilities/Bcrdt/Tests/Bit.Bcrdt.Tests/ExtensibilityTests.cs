using System.Globalization;

namespace Bit.Bcrdt.Tests;

/// <summary>
/// A collaborative counter, built entirely from the public extension surface.
/// </summary>
/// <remarks>
/// <para>
/// This is the worked example from the Bcrdt README's "Writing a collaborative type" section, kept
/// here so the documentation cannot drift away from an API that actually compiles.
/// </para>
/// <para>
/// It also demonstrates the central idea of extending the engine: the merge semantics you want come
/// from <i>how you lay data out over the primitives</i>, not from custom merge code. Each replica
/// accumulates into a key of its own, so concurrent increments add together instead of overwriting
/// one another - and there is no merge function anywhere in it, which is exactly where convergence
/// bugs would otherwise live.
/// </para>
/// </remarks>
public sealed class CrdtCounter : BcrdtAbstractType
{
    public override BcrdtTypeKind Kind => BcrdtTypeKind.Map;

    protected internal override BcrdtAbstractType CreateInstance() => new CrdtCounter();

    protected internal override BcrdtEvent CreateEvent(BcrdtTransaction transaction, HashSet<string?> changedKeys) =>
        new BcrdtGenericEvent(this, transaction);

    public override object ToJson() => Value;

    public long Value
    {
        get
        {
            long total = 0;
            foreach ((string _, object? value) in BcrdtTypeOperations.MapEntries(this))
            {
                if (value is long amount) total += amount;
            }
            return total;
        }
    }

    public void Add(long amount) => Doc.Transact(transaction =>
    {
        string key = Doc.ClientId.ToString(CultureInfo.InvariantCulture);
        long current = BcrdtTypeOperations.MapGet(this, key) as long? ?? 0;
        BcrdtTypeOperations.MapSet(transaction, this, key, current + amount);
    });
}

/// <summary>Checks that a type written outside the engine behaves like one written inside it.</summary>
[TestClass]
public class ExtensibilityTests
{
    private static BcrdtDocument Doc(ulong clientId) => new(new BcrdtDocumentOptions { ClientId = clientId });

    private static void Sync(BcrdtDocument from, BcrdtDocument to) =>
        to.ApplyUpdate(from.EncodeStateAsUpdate(to.GetStateVector()), origin: "sync");

    [TestMethod]
    public void A_custom_type_works_as_a_document_root()
    {
        using BcrdtDocument doc = Doc(1);
        CrdtCounter counter = doc.GetRoot<CrdtCounter>("votes");

        counter.Add(3);
        counter.Add(4);

        Assert.AreEqual(7, counter.Value);
    }

    [TestMethod]
    public void Concurrent_increments_add_up_rather_than_overwriting()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        using BcrdtDocument c = Doc(3);

        // Three replicas, none aware of the others.
        a.GetRoot<CrdtCounter>("votes").Add(5);
        b.GetRoot<CrdtCounter>("votes").Add(3);
        c.GetRoot<CrdtCounter>("votes").Add(2);

        foreach (BcrdtDocument source in new[] { a, b, c })
        {
            foreach (BcrdtDocument target in new[] { a, b, c })
            {
                if (!ReferenceEquals(source, target)) Sync(source, target);
            }
        }

        // A last-writer-wins field would report 5, 3 or 2 here. The layout is what makes it 10.
        Assert.AreEqual(10, a.GetRoot<CrdtCounter>("votes").Value);
        Assert.AreEqual(10, b.GetRoot<CrdtCounter>("votes").Value);
        Assert.AreEqual(10, c.GetRoot<CrdtCounter>("votes").Value);
    }

    [TestMethod]
    public void A_custom_type_converges_under_an_adversarial_network()
    {
        using var network = new BcrdtSimulatedNetwork(new BcrdtSimulatedNetworkOptions
        {
            Seed = 17,
            DropProbability = 0.2,
            DuplicateProbability = 0.2,
            MaxLatencySteps = 4,
        });

        BcrdtSimulatedPeer[] peers = network.AddPeers(4);
        var random = new Random(17);

        for (int round = 0; round < 30; round++)
        {
            foreach (BcrdtSimulatedPeer peer in peers)
            {
                peer.Doc.GetRoot<CrdtCounter>("votes").Add(random.Next(1, 5));
            }
            network.Step();
        }

        network.Flush();
        network.SynchronizeAll();

        long expected = peers[0].Doc.GetRoot<CrdtCounter>("votes").Value;
        foreach (BcrdtSimulatedPeer peer in peers)
        {
            Assert.AreEqual(expected, peer.Doc.GetRoot<CrdtCounter>("votes").Value);
        }
        BcrdtConvergenceHarness.AssertConverged(network);
    }

    [TestMethod]
    public void A_custom_type_participates_in_undo()
    {
        using BcrdtDocument doc = Doc(1);
        CrdtCounter counter = doc.GetRoot<CrdtCounter>("votes");
        using var undo = new BcrdtUndoManager(counter, new BcrdtUndoManagerOptions
        {
            CaptureTimeout = TimeSpan.Zero,
        });

        counter.Add(5);
        counter.Add(3);
        Assert.AreEqual(8, counter.Value);

        // Undo, garbage collection, encoding and events all come with the base type.
        undo.Undo();
        Assert.AreEqual(5, counter.Value);

        undo.Redo();
        Assert.AreEqual(8, counter.Value);
    }

    [TestMethod]
    public void A_custom_type_raises_change_events()
    {
        using BcrdtDocument doc = Doc(1);
        CrdtCounter counter = doc.GetRoot<CrdtCounter>("votes");

        int notifications = 0;
        using IDisposable subscription = counter.Observe((_, _) => notifications++);

        counter.Add(1);
        counter.Add(1);

        Assert.AreEqual(2, notifications);
    }

    [TestMethod]
    public void Asking_for_a_root_as_the_wrong_kind_fails_loudly()
    {
        using BcrdtDocument doc = Doc(1);
        doc.GetText("body").Insert(0, "text");

        // Two replicas that disagreed about a root's kind could not converge, so this is caught
        // rather than reinterpreted.
        InvalidOperationException error = Assert.ThrowsExactly<InvalidOperationException>(() => doc.GetArray("body"));
        Assert.Contains("body", error.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_root_receiving_remote_content_before_its_kind_is_known_adopts_it()
    {
        using BcrdtDocument author = Doc(1);
        author.GetArray("items").Push("a", "b", "c");

        using BcrdtDocument reader = Doc(2);

        // The update names the root but not its kind, so the content lands in a placeholder.
        reader.ApplyUpdate(author.EncodeStateAsUpdate());

        // Asking for it by kind re-parents the accumulated blocks onto a properly typed instance.
        Assert.AreSequenceEqual(new object?[] { "a", "b", "c" }, reader.GetArray("items").ToArray());
    }
}
