using System.Text.Json.Nodes;
using Bit.Bcrdt.Blex;
using Bit.Blex;

namespace Bit.Bcrdt.Tests.Integrations;

public sealed record BlexBoard(string Title, IReadOnlyList<string> Cards, long Votes)
{
    public static BlexBoard Read(BcrdtMap board) => new(
        board.Get<string>("title") ?? "",
        [.. (board.GetArray("cards")?.ToArray() ?? []).OfType<string>()],
        board.GetCounter("votes")?.Value ?? 0);

    public string Describe() => $"{Title}|{string.Join(",", Cards)}|{Votes}";
}

/// <summary>A hand-written collaborative store: no generator, its actions call <c>Act</c>.</summary>
public sealed class BlexBoardStore(BcrdtMap board, string name = "board")
    : BcrdtBlexStore<BlexBoard>(name, board, () => BlexBoard.Read(board))
{
    public void Rename(string title) => Act("Rename", () => board.Set("title", title), new BlexActionArg("title", title));

    public void AddCard(string card) => Act("Add card", () => board.GetArray("cards")!.Push(card), new BlexActionArg("card", card));

    public void RemoveCard(int index) => Act("Remove card", () => board.GetArray("cards")!.Delete(index), new BlexActionArg("index", index));

    public void Vote() => Act("Vote", () => board.GetCounter("votes")!.Increment());
}

/// <summary>
/// <see cref="BcrdtBlexStore{TState}"/>: Blex state projected from a shared document, actions that are
/// tagged transactions, and collaborators' changes dispatched like any other action - once.
/// </summary>
[TestClass]
public class BlexStoreTests
{
    public static IEnumerable<object[]> Seeds
    {
        get
        {
            for (int seed = 1; seed <= 25; seed++) yield return [seed];
        }
    }

    private static BcrdtDocument Doc(ulong clientId) => new(new BcrdtDocumentOptions { ClientId = clientId });

    private static void Sync(params BcrdtDocument[] docs)
    {
        foreach (BcrdtDocument source in docs)
        {
            foreach (BcrdtDocument target in docs)
            {
                if (!ReferenceEquals(source, target)) target.ApplyUpdate(source.EncodeStateAsUpdate(target.GetStateVector()), origin: "sync");
            }
        }
    }

    /// <summary>Creates the board's collections once, on one replica, before anyone uses them.</summary>
    private static void Seed(BcrdtDocument doc)
    {
        BcrdtMap board = doc.GetMap("board");
        doc.Transact(_ =>
        {
            board.Set("title", "Planning");
            board.EnsureArray("cards");
            board.EnsureCounter("votes");
        });
    }

    [TestMethod]
    public void An_action_is_one_named_action_and_one_tagged_transaction()
    {
        using BcrdtDocument doc = Doc(1);
        Seed(doc);
        using var store = new BlexBoardStore(doc.GetMap("board"));
        var manager = new BlexManager();
        manager.Register(store);

        List<string> actions = [];
        manager.ActionDispatched += context => actions.Add(context.QualifiedName);
        List<object?> origins = [];
        doc.Updated += update => origins.Add(update.Origin);

        store.Rename("Q3");
        store.AddCard("Hiring");

        CollectionAssert.AreEqual(new[] { "board/Rename", "board/Add card" }, actions);
        CollectionAssert.AreEqual(new object?[] { store.Origin, store.Origin }, origins);
        Assert.AreEqual(new BlexBoard("Q3", ["Hiring"], 0).Describe(), store.State.Describe());
        Assert.AreEqual("Q3", store.SerializeState()["State"]!["title"]!.GetValue<string>());
    }

    [TestMethod]
    public void A_scoped_store_is_named_for_its_key()
    {
        using BcrdtDocument doc = Doc(1);
        Seed(doc);
        var manager = new BlexManager();
        List<string> actions = [];
        manager.ActionDispatched += context => actions.Add(context.QualifiedName);

        using (BlexStoreScope<BlexBoardStore> scope = manager.CreateScope(new BlexBoardStore(doc.GetMap("board")), key: "42"))
        {
            scope.Store.Vote();
            Assert.AreEqual("board:42", scope.Store.Name);
            Assert.AreSame(scope.Store, manager.GetStore("board:42"));
        }

        CollectionAssert.AreEqual(new[] { "board:42/Vote" }, actions);
        Assert.IsNull(manager.GetStore("board:42"));
    }

    [TestMethod]
    public void Remote_changes_are_dispatched_so_selectors_and_subscriptions_fire()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        Seed(a);
        Sync(a, b);
        using var local = new BlexBoardStore(a.GetMap("board"));
        using var remote = new BlexBoardStore(b.GetMap("board"));
        var manager = new BlexManager();
        manager.Register(remote);

        List<string> actions = [];
        manager.ActionDispatched += context => actions.Add(context.QualifiedName);
        List<string> titles = [];
        using IDisposable titleSubscription = remote.Subscribe(() => remote.State.Title, titles.Add);
        List<long> votes = [];
        using IDisposable voteSubscription = manager.Subscribe(() => remote.State.Votes, votes.Add);

        local.Rename("Roadmap");
        Sync(a, b);
        local.Vote();
        Sync(a, b);

        CollectionAssert.AreEqual(new[] { "board/Remote change", "board/Remote change" }, actions);
        CollectionAssert.AreEqual(new[] { "Roadmap" }, titles, "a selector fires for what it selects, and only then");
        CollectionAssert.AreEqual(new long[] { 1 }, votes);
        Assert.AreEqual(local.State.Describe(), remote.State.Describe());
    }

    [TestMethod]
    public void Nothing_is_echoed_back()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        Seed(a);
        Sync(a, b);
        using var first = new BlexBoardStore(a.GetMap("board"));
        using var second = new BlexBoardStore(b.GetMap("board"));
        var managerA = new BlexManager();
        var managerB = new BlexManager();
        managerA.Register(first);
        managerB.Register(second);

        int dispatchesA = 0, dispatchesB = 0, localUpdatesA = 0, localUpdatesB = 0;
        managerA.ActionDispatched += _ => dispatchesA++;
        managerB.ActionDispatched += _ => dispatchesB++;
        a.Updated += update => { if (!Equals(update.Origin, "sync")) localUpdatesA++; };
        b.Updated += update => { if (!Equals(update.Origin, "sync")) localUpdatesB++; };

        first.AddCard("one");
        Sync(a, b);
        first.Vote();
        Sync(a, b);
        Sync(a, b);

        Assert.AreEqual(2, dispatchesA);
        Assert.AreEqual(2, localUpdatesA);
        Assert.AreEqual(2, dispatchesB, "each remote change is dispatched once");
        Assert.AreEqual(0, localUpdatesB, "and writes nothing back");

        // A subscriber reacting to a remote change with an action of its own does not set off a loop.
        using IDisposable reaction = managerB.SubscribeToAction("Remote change", _ =>
        {
            if (second.State.Cards.Count < 3) second.AddCard("reply");
        });
        first.AddCard("two");
        Sync(a, b);
        Sync(a, b);

        Assert.AreEqual(1, localUpdatesB);
        CollectionAssert.AreEqual(new[] { "one", "two", "reply" }, first.State.Cards.ToArray());
        Assert.AreEqual(first.State.Describe(), second.State.Describe());
    }

    [TestMethod]
    public void Two_stores_converge_under_concurrent_actions()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        Seed(a);
        Sync(a, b);
        using var first = new BlexBoardStore(a.GetMap("board"));
        using var second = new BlexBoardStore(b.GetMap("board"));

        first.Rename("From a");
        first.AddCard("a1");
        first.Vote();
        second.AddCard("b1");
        second.Vote();
        second.Vote();
        Sync(a, b);

        Assert.AreEqual(first.State.Describe(), second.State.Describe());
        Assert.AreEqual(3, first.State.Votes, "every vote counts");
        CollectionAssert.AreEquivalent(new[] { "a1", "b1" }, first.State.Cards.ToArray());
        Assert.AreEqual("From a", second.State.Title);
    }

    [TestMethod]
    public void A_vetoed_action_changes_neither_the_document_nor_the_state()
    {
        using BcrdtDocument doc = Doc(1);
        Seed(doc);
        using var store = new BlexBoardStore(doc.GetMap("board"));
        var manager = new BlexManager([new BlexFilterMiddleware(context => context.ActionName != "Rename")]);
        manager.Register(store);

        int updates = 0;
        doc.Updated += _ => updates++;
        store.Rename("Nope");
        store.AddCard("Yes");

        Assert.AreEqual("Planning", store.State.Title);
        Assert.AreEqual("Planning", doc.GetMap("board").Get("title"));
        Assert.AreEqual(1, updates);
        CollectionAssert.AreEqual(new[] { "Yes" }, store.State.Cards.ToArray());
    }

    [TestMethod]
    public void A_text_store_merges_concurrent_typing()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        a.GetText("notes").Insert(0, "Hello world");
        Sync(a, b);

        BcrdtText textA = a.GetText("notes");
        BcrdtText textB = b.GetText("notes");
        using var first = new BcrdtBlexStore<string>("notes", textA, textA.ToString);
        using var second = new BcrdtBlexStore<string>("notes", textB, textB.ToString);

        first.Act("Type", () => BcrdtTextSynchronizer.Apply(textA, "Hello big world"));
        second.Act("Type", () => BcrdtTextSynchronizer.Apply(textB, "Hello world!"));
        Sync(a, b);

        Assert.AreEqual("Hello big world!", first.State);
        Assert.AreEqual("Hello big world!", second.State);
    }

    [TestMethod]
    public void A_restore_reads_the_document_again()
    {
        using BcrdtDocument doc = Doc(1);
        Seed(doc);
        using var store = new BlexBoardStore(doc.GetMap("board"));
        var manager = new BlexManager();
        manager.Register(store);
        store.Rename("Current");

        store.RestoreState(new JsonObject { ["State"] = new JsonObject { ["title"] = "Stale" } });

        Assert.AreEqual("Current", store.State.Title);
        Assert.AreEqual("Current", doc.GetMap("board").Get("title"));
    }

    [TestMethod]
    public void Undo_tracking_the_store_takes_back_its_actions_and_the_state_follows()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        Seed(a);
        Sync(a, b);
        using var first = new BlexBoardStore(a.GetMap("board"));
        using var second = new BlexBoardStore(b.GetMap("board"));
        using var undo = new BcrdtUndoManager(a.GetMap("board"), new BcrdtUndoManagerOptions { TrackedOrigins = [first.Origin] });

        first.AddCard("mine");
        second.AddCard("theirs");
        Sync(a, b);

        undo.Undo();
        Sync(a, b);

        CollectionAssert.AreEqual(new[] { "theirs" }, first.State.Cards.ToArray());
        CollectionAssert.AreEqual(new[] { "theirs" }, second.State.Cards.ToArray());
    }

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public void Random_actions_converge_on_a_lossy_network(int seed)
    {
        using var network = new BcrdtSimulatedNetwork(new BcrdtSimulatedNetworkOptions
        {
            Seed = seed,
            DropProbability = 0.2,
            DuplicateProbability = 0.2,
            MaxLatencySteps = 4,
        });

        BcrdtSimulatedPeer[] peers = network.AddPeers(3);
        Seed(peers[0].Doc);
        network.Flush();
        network.SynchronizeAll();

        var random = new Random(seed);
        List<BlexBoardStore> stores = [.. peers.Select(peer => new BlexBoardStore(peer.Doc.GetMap("board")))];
        List<int> dispatches = [.. stores.Select(_ => 0)];
        for (int i = 0; i < stores.Count; i++)
        {
            int index = i;
            var manager = new BlexManager();
            manager.Register(stores[i]);
            manager.ActionDispatched += _ => dispatches[index]++;
        }

        List<string> history = [];
        try
        {
            for (int round = 0; round < 40; round++)
            {
                for (int i = 0; i < stores.Count; i++)
                {
                    BlexBoardStore store = stores[i];
                    switch (random.Next(4))
                    {
                        case 0:
                            string title = "t" + random.Next(100);
                            store.Rename(title);
                            history.Add($"{peers[i].Name}: rename {title}");
                            break;
                        case 1:
                            string card = "c" + random.Next(100);
                            store.AddCard(card);
                            history.Add($"{peers[i].Name}: add {card}");
                            break;
                        case 2 when store.State.Cards.Count > 0:
                            int removed = random.Next(store.State.Cards.Count);
                            store.RemoveCard(removed);
                            history.Add($"{peers[i].Name}: remove {removed}");
                            break;
                        default:
                            store.Vote();
                            history.Add($"{peers[i].Name}: vote");
                            break;
                    }
                }
                network.Step();
            }

            network.Flush();
            network.SynchronizeAll();

            BcrdtConvergenceHarness.AssertConverged(network);
            for (int i = 0; i < stores.Count; i++)
            {
                Assert.AreEqual(BlexBoard.Read(peers[i].Doc.GetMap("board")).Describe(), stores[i].State.Describe(), "the state is what the document holds");
                Assert.AreEqual(stores[0].State.Describe(), stores[i].State.Describe());
            }
            Assert.IsTrue(dispatches.All(count => count >= 40), "every action, and every change from elsewhere, went through the store");
        }
        catch (Exception ex)
        {
            throw new AssertFailedException(
                $"Seed {seed} diverged. Last operations:\n{string.Join("\n", history.TakeLast(20))}", ex);
        }
        finally
        {
            foreach (BlexBoardStore store in stores) store.Dispose();
        }
    }
}
