namespace Bit.Bcrdt.Tests;

/// <summary>
/// <see cref="BcrdtOrderedCollection"/>: items move by rewriting a key, so concurrent moves never duplicate
/// an item and never lose an edit made to it.
/// </summary>
[TestClass]
public class OrderedCollectionTests
{
    public static IEnumerable<object[]> Seeds
    {
        get
        {
            for (int seed = 1; seed <= 25; seed++) yield return [seed];
        }
    }

    private static readonly string[] Columns = ["todo", "doing", "done"];

    private static BcrdtDocument Doc(ulong clientId) => new(new BcrdtDocumentOptions { ClientId = clientId });

    private static BcrdtOrderedCollection Board(BcrdtDocument doc, int seed = 1) =>
        new(doc.GetMap("cards"), random: new Random(seed));

    private static Func<BcrdtMap, bool> InColumn(string column) => card => card.Get("column") as string == column;

    private static string[] Ids(BcrdtOrderedCollection board, Func<BcrdtMap, bool>? group = null) =>
        [.. board.GetOrdered(group).Select(entry => entry.Id)];

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

    private static BcrdtMap Raw(BcrdtOrderedCollection board, string id, object? order)
    {
        var item = new BcrdtMap();
        if (order is not null) item.Set(board.OrderField, order);
        board.Items.Set(id, item);
        return item;
    }

    [TestMethod]
    public void Inserting_at_an_index_places_the_item_exactly_there()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtOrderedCollection board = Board(doc);

        board.Insert(0, id: "a");
        board.Insert(1, id: "b");
        board.Insert(1, id: "c");
        board.Add(id: "d");
        BcrdtMap e = board.Insert(0, card => card.Set("title", "first"), id: "e");

        Assert.AreSequenceEqual(new[] { "e", "a", "c", "b", "d" }, Ids(board));
        Assert.AreEqual(5, board.Count);
        Assert.AreEqual(2, board.IndexOf("c"));
        Assert.AreEqual(-1, board.IndexOf("missing"));
        Assert.AreSame(e, board.Get("e"));
        Assert.AreEqual("first", e.Get("title"));
        Assert.AreSequenceEqual(Ids(board), board.Select(entry => entry.Id).ToArray());
        Assert.IsTrue(board.All(entry => BcrdtOrderKey.IsValid(entry.OrderKey)));

        Assert.AreEqual("e", board.IdOf(e));
        Assert.IsNull(board.IdOf(new BcrdtMap()));
        Assert.IsNull(board.IdOf(doc.GetMap("elsewhere").EnsureMap("e")));
        Assert.IsTrue(board.Remove("e"));
        Assert.IsNull(board.IdOf(e));
        Assert.IsNull(board.Get("e"));
    }

    [TestMethod]
    public void The_task_board_in_the_guide_works()
    {
        using BcrdtDocument document = Doc(1);
        var board = new BcrdtOrderedCollection(document.GetMap().EnsureMap("cards"));
        Func<BcrdtMap, bool> InColumn(string column) => item => item.Get<string>("column") == column;
        board.Add(fields => fields.Set("column", "doing"), InColumn("doing"), "already-doing");

        BcrdtMap card = board.Add(fields =>
        {
            fields.Set("column", "todo");
            fields.EnsureText("title").Insert(0, "Write the release notes");
        }, InColumn("todo"));

        board.Move(board.IdOf(card)!, 1, InColumn("doing"), moved => moved.Set("column", "doing"));

        IReadOnlyList<BcrdtOrderedEntry> doing = board.GetOrdered(InColumn("doing"));
        Assert.AreSequenceEqual(new[] { "already-doing", board.IdOf(card) }, doing.Select(entry => entry.Id).ToArray());
        Assert.AreSame(card, doing[1].Item);
        Assert.AreEqual("Write the release notes", card.GetText("title")!.ToString());
        Assert.IsEmpty(board.GetOrdered(InColumn("todo")));
    }

    [TestMethod]
    public void An_insert_is_one_transaction_and_generates_an_identifier()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtOrderedCollection board = Board(doc);

        int updates = 0;
        doc.Updated += _ => updates++;
        BcrdtMap card = board.Add(card =>
        {
            card.Set("column", "todo");
            card.EnsureText("notes").Insert(0, "hello");
        });

        Assert.AreEqual(1, updates);
        string id = board.GetOrdered().Single().Id;
        Assert.AreEqual(32, id.Length);
        Assert.AreSame(card, board.Get(id));
        Assert.AreEqual("hello", card.GetText("notes")!.ToString());
    }

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public void Inserts_moves_and_removes_agree_with_a_list(int seed)
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtOrderedCollection board = Board(doc, seed);
        var random = new Random(seed);
        List<string> oracle = [];

        for (int step = 0; step < 300; step++)
        {
            int choice = random.Next(10);
            if (oracle.Count == 0 || choice < 4)
            {
                int index = random.Next(oracle.Count + 1);
                string id = $"item-{step}";
                board.Insert(index, id: id);
                oracle.Insert(index, id);
            }
            else if (choice < 8)
            {
                string id = oracle[random.Next(oracle.Count)];
                oracle.Remove(id);
                int index = random.Next(oracle.Count + 1);
                board.Move(id, index);
                oracle.Insert(index, id);
            }
            else
            {
                string id = oracle[random.Next(oracle.Count)];
                Assert.IsTrue(board.Remove(id));
                oracle.Remove(id);
            }

            Assert.AreSequenceEqual(oracle, Ids(board), $"Seed {seed}, step {step}.");
        }
    }

    [TestMethod]
    public void A_move_writes_only_the_order_key_of_the_same_item()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtOrderedCollection board = Board(doc);
        board.Add(id: "a");
        BcrdtMap b = board.Add(card => card.EnsureText("notes").Insert(0, "keep me"), id: "b");
        board.Add(id: "c");

        List<string> changed = [];
        using IDisposable deep = board.Items.ObserveDeep((events, _) =>
        {
            foreach (BcrdtEvent e in events)
            {
                foreach (string key in e.Keys.Keys) changed.Add($"{string.Join("/", e.Path)}:{key}");
            }
        });
        int updates = 0;
        doc.Updated += _ => updates++;

        board.Move("b", 0);

        Assert.AreSequenceEqual(new[] { "b", "a", "c" }, Ids(board));
        Assert.AreSequenceEqual(new[] { "b:order" }, changed);
        Assert.AreEqual(1, updates);
        Assert.AreSame(b, board.Get("b"));
        Assert.AreEqual("keep me", b.GetText("notes")!.ToString());
    }

    [TestMethod]
    public void Moving_an_item_to_where_it_already_is_writes_nothing()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtOrderedCollection board = Board(doc);
        board.Add(id: "a");
        board.Add(id: "b");
        board.Add(id: "c");

        int updates = 0;
        doc.Updated += _ => updates++;
        board.Move("b", 1);
        board.Move("a", 0);
        board.Move("c", 2);

        Assert.AreEqual(0, updates);
    }

    [TestMethod]
    public void A_card_moves_between_columns_with_one_transaction_and_keeps_its_place_in_the_order()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtOrderedCollection board = Board(doc);
        foreach (string id in new[] { "t1", "t2", "t3" }) board.Add(card => card.Set("column", "todo"), InColumn("todo"), id);
        foreach (string id in new[] { "d1", "d2" }) board.Add(card => card.Set("column", "done"), InColumn("done"), id);
        board.Insert(1, card => card.Set("column", "done"), InColumn("done"), "d3");

        Assert.AreSequenceEqual(new[] { "t1", "t2", "t3" }, Ids(board, InColumn("todo")));
        Assert.AreSequenceEqual(new[] { "d1", "d3", "d2" }, Ids(board, InColumn("done")));

        int updates = 0;
        doc.Updated += _ => updates++;
        board.Move("t2", 1, InColumn("done"), card => card.Set("column", "done"));

        Assert.AreEqual(1, updates);
        Assert.AreSequenceEqual(new[] { "t1", "t3" }, Ids(board, InColumn("todo")));
        Assert.AreSequenceEqual(new[] { "d1", "t2", "d3", "d2" }, Ids(board, InColumn("done")));
        Assert.AreEqual(1, board.IndexOf("t2", InColumn("done")));
        Assert.AreEqual(-1, board.IndexOf("t2", InColumn("todo")));
    }

    [TestMethod]
    public void Inserting_between_tied_items_rewrites_only_the_shorter_side_of_the_tie()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtOrderedCollection board = Board(doc);
        Raw(board, "w", "Zz");
        foreach (string id in new[] { "t1", "t2", "t3", "t4" }) Raw(board, id, "a0");
        Raw(board, "x", "a1");

        Assert.AreSequenceEqual(new[] { "w", "t1", "t2", "t3", "t4", "x" }, Ids(board));

        int updates = 0;
        doc.Updated += _ => updates++;

        // Between t1 and t2: one item on the left, three on the right - only t1 is rewritten.
        board.Insert(2, id: "n1");
        Assert.AreSequenceEqual(new[] { "w", "t1", "n1", "t2", "t3", "t4", "x" }, Ids(board));
        Assert.AreNotEqual("a0", board.Get("t1")!.Get("order"));
        foreach (string id in new[] { "t2", "t3", "t4" }) Assert.AreEqual("a0", board.Get(id)!.Get("order"));

        // Between t3 and t4: two tied on the left, one on the right - only t4 is rewritten.
        board.Insert(5, id: "n2");
        Assert.AreSequenceEqual(new[] { "w", "t1", "n1", "t2", "t3", "n2", "t4", "x" }, Ids(board));
        Assert.AreNotEqual("a0", board.Get("t4")!.Get("order"));
        Assert.AreEqual("a0", board.Get("t2")!.Get("order"));
        Assert.AreEqual("a0", board.Get("t3")!.Get("order"));

        // Between t2 and t3, a tie with nothing tied around it: one of them is rewritten.
        board.Move("x", 4);
        Assert.AreSequenceEqual(new[] { "w", "t1", "n1", "t2", "x", "t3", "n2", "t4" }, Ids(board));

        Assert.AreEqual(3, updates);
        Assert.HasCount(board.Count, board.Select(entry => entry.OrderKey).Distinct());
    }

    [TestMethod]
    public void A_tie_that_runs_to_an_end_of_the_list_is_broken_into_the_open_end()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtOrderedCollection board = Board(doc);
        foreach (string id in new[] { "t1", "t2", "t3" }) Raw(board, id, "a5");

        // Two tied on the left, one on the right: t3 moves up into the open end.
        board.Insert(2, id: "late");
        Assert.AreSequenceEqual(new[] { "t1", "t2", "late", "t3" }, Ids(board));
        Assert.AreEqual("a5", board.Get("t1")!.Get("order"));
        Assert.AreEqual("a5", board.Get("t2")!.Get("order"));

        // One on each side: t1 moves down into the open start.
        board.Insert(1, id: "early");
        Assert.AreSequenceEqual(new[] { "t1", "early", "t2", "late", "t3" }, Ids(board));
        Assert.AreEqual("a5", board.Get("t2")!.Get("order"));
        Assert.IsLessThan(0, string.CompareOrdinal(board.Get("t1")!.Get("order") as string, "a5"));
    }

    [TestMethod]
    public void Items_without_a_valid_key_sort_last_by_identifier_and_are_repaired_when_passed()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtOrderedCollection board = Board(doc);
        Raw(board, "k1", "a0");
        Raw(board, "k2", "a1");
        Raw(board, "z-missing", null);
        Raw(board, "m-garbage", "not a key!");
        Raw(board, "q-number", 42L);
        Raw(board, "b-trailing-zero", "a10");
        board.Items.Set("note", "not an item");

        Assert.AreSequenceEqual(new[] { "k1", "k2", "b-trailing-zero", "m-garbage", "q-number", "z-missing" }, Ids(board));
        Assert.AreEqual(6, board.Count);
        Assert.IsNull(board.GetOrdered()[2].OrderKey);
        Assert.IsNull(board.Get("note"));

        // Before the unkeyed items, nothing of theirs changes.
        board.Insert(2, id: "n1");
        Assert.AreSequenceEqual(new[] { "k1", "k2", "n1", "b-trailing-zero", "m-garbage", "q-number", "z-missing" }, Ids(board));
        Assert.AreEqual("not a key!", board.Get("m-garbage")!.Get("order"));

        // After two of them, those two get valid keys, in the order they had.
        int updates = 0;
        doc.Updated += _ => updates++;
        board.Insert(5, id: "n2");

        Assert.AreSequenceEqual(new[] { "k1", "k2", "n1", "b-trailing-zero", "m-garbage", "n2", "q-number", "z-missing" }, Ids(board));
        Assert.AreEqual(1, updates);
        Assert.IsTrue(BcrdtOrderKey.IsValid(board.Get("m-garbage")!.Get("order") as string));
        Assert.AreEqual(42L, board.Get("q-number")!.Get("order"));
    }

    [TestMethod]
    public void Arguments_are_checked()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtOrderedCollection board = Board(doc);
        board.Add(id: "a");

        Assert.ThrowsExactly<ArgumentNullException>(() => new BcrdtOrderedCollection(null!));
        Assert.ThrowsExactly<ArgumentException>(() => new BcrdtOrderedCollection(doc.GetMap("cards"), ""));
        Assert.ThrowsExactly<ArgumentException>(() => board.Insert(0, id: "a"));
        Assert.ThrowsExactly<ArgumentException>(() => board.Add(id: "a"));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => board.Insert(2));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => board.Insert(-1));
        Assert.ThrowsExactly<ArgumentException>(() => board.Move("missing", 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => board.Move("a", 1));
        Assert.IsFalse(board.Remove("missing"));
        Assert.AreEqual(1, board.Count);
    }

    [TestMethod]
    public void Concurrent_moves_of_one_item_leave_it_in_one_place()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        BcrdtOrderedCollection boardA = Board(a, 1);
        foreach (string id in new[] { "p", "q", "r", "s" }) boardA.Add(id: id);
        Sync(a, b);
        BcrdtOrderedCollection boardB = Board(b, 2);

        boardA.Move("q", 0);
        boardB.Move("q", 3);
        SyncAll(a, b);

        string[] ids = Ids(boardA);
        Assert.AreSequenceEqual(ids, Ids(boardB));
        Assert.HasCount(4, ids);
        Assert.ContainsSingle(id => id == "q", ids);
        Assert.IsTrue(ids[0] == "q" || ids[3] == "q");
        BcrdtConvergenceHarness.AssertConverged(a, b);
    }

    [TestMethod]
    public void An_edit_to_an_item_survives_a_concurrent_move()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        BcrdtOrderedCollection boardA = Board(a, 1);
        boardA.Add(card => card.Set("column", "todo"), id: "x");
        boardA.Add(card =>
        {
            card.Set("column", "todo");
            card.Set("title", "Draft");
            card.EnsureText("notes").Insert(0, "Notes");
        }, id: "y");
        Sync(a, b);
        BcrdtOrderedCollection boardB = Board(b, 2);

        boardA.Move("y", 0, InColumn("done"), card => card.Set("column", "done"));
        BcrdtMap y = boardB.Get("y")!;
        y.Set("title", "Final");
        y.GetText("notes")!.Append(" and more");
        SyncAll(a, b);

        foreach (BcrdtOrderedCollection board in new[] { boardA, boardB })
        {
            BcrdtMap card = board.Get("y")!;
            Assert.AreEqual("Final", card.Get("title"));
            Assert.AreEqual("Notes and more", card.GetText("notes")!.ToString());
            Assert.AreEqual("done", card.Get("column"));
            Assert.AreSequenceEqual(new[] { "y" }, Ids(board, InColumn("done")));
            Assert.AreSequenceEqual(new[] { "x" }, Ids(board, InColumn("todo")));
        }
    }

    [TestMethod]
    public void Concurrent_inserts_at_the_same_place_both_survive_in_one_order()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        BcrdtOrderedCollection boardA = Board(a, 1);
        boardA.Add(id: "first");
        boardA.Add(id: "last");
        Sync(a, b);
        BcrdtOrderedCollection boardB = Board(b, 2);

        boardA.Insert(1, id: "from-a");
        boardB.Insert(1, id: "from-b");
        SyncAll(a, b);

        string[] ids = Ids(boardA);
        Assert.AreSequenceEqual(ids, Ids(boardB));
        Assert.AreEqual("first", ids[0]);
        Assert.AreEqual("last", ids[3]);
        Assert.AreSequenceEqual(new[] { "from-a", "from-b" }, ids[1..3], Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);

        // Whatever the keys, there is always room between the two.
        boardB.Insert(2, id: "between");
        Assert.AreSequenceEqual(new[] { ids[0], ids[1], "between", ids[2], ids[3] }, Ids(boardB));
    }

    [TestMethod]
    public void Removing_wins_over_a_concurrent_move()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        BcrdtOrderedCollection boardA = Board(a, 1);
        foreach (string id in new[] { "p", "q", "r" }) boardA.Add(id: id);
        Sync(a, b);
        BcrdtOrderedCollection boardB = Board(b, 2);

        Assert.IsTrue(boardA.Remove("q"));
        boardB.Move("q", 0);
        SyncAll(a, b);

        Assert.AreSequenceEqual(new[] { "p", "r" }, Ids(boardA));
        Assert.AreSequenceEqual(new[] { "p", "r" }, Ids(boardB));
    }

    [TestMethod]
    public void Undo_puts_a_moved_item_back()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtOrderedCollection board = Board(doc);
        foreach (string id in new[] { "p", "q", "r" }) board.Add(id: id);
        using var undo = new BcrdtUndoManager(board.Items, new BcrdtUndoManagerOptions { CaptureTimeout = TimeSpan.Zero });

        board.Move("r", 0);
        Assert.AreSequenceEqual(new[] { "r", "p", "q" }, Ids(board));

        undo.Undo();
        Assert.AreSequenceEqual(new[] { "p", "q", "r" }, Ids(board));

        undo.Redo();
        Assert.AreSequenceEqual(new[] { "r", "p", "q" }, Ids(board));
    }

    [TestMethod]
    public void A_collection_survives_a_round_trip_and_reads_with_another_order_field()
    {
        using BcrdtDocument original = Doc(1);
        var board = new BcrdtOrderedCollection(original.GetMap("cards"), "rank", new Random(3));
        foreach (string id in new[] { "p", "q", "r" }) board.Add(id: id);
        board.Move("r", 1);

        using BcrdtDocument loaded = Doc(2);
        loaded.ApplyUpdate(original.EncodeStateAsUpdate());
        var reloaded = new BcrdtOrderedCollection(loaded.GetMap("cards"), "rank");

        Assert.AreEqual("rank", reloaded.OrderField);
        Assert.AreSequenceEqual(new[] { "p", "r", "q" }, Ids(reloaded));
        Assert.IsNull(board.Get("p")!.Get(BcrdtOrderedCollection.DefaultOrderField));
    }

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public void Random_concurrent_board_edits_converge_without_duplicates_or_lost_edits(int seed)
    {
        using var network = new BcrdtSimulatedNetwork(new BcrdtSimulatedNetworkOptions
        {
            Seed = seed,
            DropProbability = 0.25,
            DuplicateProbability = 0.25,
            MaxLatencySteps = 5,
        });

        BcrdtSimulatedPeer[] peers = network.AddPeers(4);
        BcrdtOrderedCollection[] boards = [.. peers.Select((peer, i) => new BcrdtOrderedCollection(peer.Doc.GetMap("board"), random: new Random((seed * 10) + i)))];
        var random = new Random(seed);

        HashSet<string> inserted = [];
        HashSet<string> removed = [];
        Dictionary<string, List<string>> notes = [];
        List<string> history = [];
        int sequence = 0;

        for (int round = 0; round < 50; round++)
        {
            if (round == 10) network.Partition(peers[0], peers[1]);
            if (round == 20) peers[2].GoOffline();
            if (round == 35)
            {
                network.HealPartitions();
                peers[2].GoOnline();
            }

            for (int p = 0; p < peers.Length; p++)
            {
                BcrdtOrderedCollection board = boards[p];
                IReadOnlyList<BcrdtOrderedEntry> all = board.GetOrdered();
                int choice = random.Next(100);
                string column = Columns[random.Next(Columns.Length)];

                if (all.Count == 0 || choice < 30)
                {
                    string id = $"{peers[p].Name}-{sequence++}";
                    int index = random.Next(board.GetOrdered(InColumn(column)).Count + 1);
                    board.Insert(index, card =>
                    {
                        card.Set("column", column);
                        card.Set("title", id);
                        card.EnsureText("notes");
                    }, InColumn(column), id);
                    inserted.Add(id);
                    notes[id] = [];
                    history.Add($"{peers[p].Name}: insert {id} at {column}[{index}]");
                    continue;
                }

                string target = all[random.Next(all.Count)].Id;
                if (choice < 60)
                {
                    int index = random.Next(board.GetOrdered(InColumn(column)).Count(entry => entry.Id != target) + 1);
                    board.Move(target, index, InColumn(column), card => card.Set("column", column));
                    history.Add($"{peers[p].Name}: move {target} to {column}[{index}]");
                }
                else if (choice < 70)
                {
                    board.Remove(target);
                    removed.Add(target);
                    history.Add($"{peers[p].Name}: remove {target}");
                }
                else if (choice < 93)
                {
                    string token = $"[{peers[p].Name}:{sequence++}]";
                    BcrdtMap card = board.Get(target)!;
                    // Appended, so no later token lands inside this one.
                    card.GetText("notes")!.Append(token);
                    card.Set("title", token);
                    notes[target].Add(token);
                    history.Add($"{peers[p].Name}: edit {target} {token}");
                }
                else
                {
                    // A misbehaving or racing peer: a key copied from a neighbour, or garbage.
                    BcrdtMap card = board.Get(target)!;
                    object? key = random.Next(2) == 0 ? all[random.Next(all.Count)].OrderKey : "garbage";
                    card.Set(board.OrderField, key);
                    history.Add($"{peers[p].Name}: set {target} key {key}");
                }
            }

            network.Step();

            try
            {
                foreach (BcrdtOrderedCollection board in boards) AssertEveryItemOnce(board, seed, settled: false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Seed {seed} failed in round {round}:" + Environment.NewLine + string.Join(Environment.NewLine, history.TakeLast(30)), ex);
            }
        }

        network.Flush();
        network.SynchronizeAll();

        try
        {
            BcrdtConvergenceHarness.AssertConverged(network);

            string[] expected = [.. inserted.Except(removed).Order(StringComparer.Ordinal)];
            string[] reference = Ids(boards[0]);
            foreach (BcrdtOrderedCollection board in boards)
            {
                Assert.AreSequenceEqual(reference, Ids(board), "The replicas disagree on the order.");
                Assert.AreSequenceEqual(expected, Ids(board).Order(StringComparer.Ordinal).ToArray(), "An item was lost or resurrected.");
                AssertEveryItemOnce(board, seed, settled: true);

                foreach (string id in expected)
                {
                    string text = board.Get(id)!.GetText("notes")!.ToString();
                    foreach (string token in notes[id]) Assert.IsTrue(text.Contains(token, StringComparison.Ordinal), $"An edit to {id} was lost: {token}.");
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Seed {seed} failed. Last operations:\n{string.Join("\n", history.TakeLast(20))}", ex);
        }
    }

    /// <param name="settled">
    /// Whether every update has arrived. Until then a field can be briefly empty: an update whose
    /// earlier changes are still missing removes the value it replaced at once, but has to wait for
    /// them before its own value can be placed. An item in no column is therefore only a failure once
    /// the network is quiet; an item listed twice is a failure at any moment.
    /// </param>
    private static void AssertEveryItemOnce(BcrdtOrderedCollection board, int seed, bool settled)
    {
        string[] all = Ids(board);
        Assert.HasCount(all.Length, all.Distinct(), $"Seed {seed}: an item is listed twice.");
        Assert.HasCount(board.Count, all, $"Seed {seed}: an item is missing from the order.");

        string[] byColumn = [.. Columns.SelectMany(column => Ids(board, InColumn(column)))];
        Assert.HasCount(byColumn.Length, byColumn.Distinct(), $"Seed {seed}: an item is in two columns.");
        if (settled) Assert.AreSequenceEqual(all, byColumn, Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder, $"Seed {seed}: an item is in no column.");
    }
}
