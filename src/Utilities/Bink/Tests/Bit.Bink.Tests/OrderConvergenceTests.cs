using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>
/// Concurrent reorders reconcile: the stacking order is a function of order keys, and concurrent moves of one thing
/// resolve the same way everywhere, so every replica ends with the same order whatever order the moves arrive in.
/// </summary>
[TestClass]
public sealed class OrderConvergenceTests
{
    private static BinkDocument Copy(BinkDocument document) => BinkSerializer.Deserialize(BinkSerializer.Serialize(document));

    private static BinkStroke Stroke(int i) => TestData.Stroke(new Vector2(i * 30, 0), new Vector2(i * 30 + 20, 20));

    private static Guid[] Order(BinkDocument document) => document.AllElements.Select(e => e.Id).ToArray();

    private static string LayerOrder(BinkDocument document) => string.Join(",", document.Layers.Select(l => l.Name));

    /// <summary>Two replicas, each with a session, and a function delivering everything each has sent to the other.</summary>
    private static async Task<(BinkDocument Alex, BinkDocument Zoe, Func<Task> Exchange, IAsyncDisposable Sessions)> PairAsync(BinkDocument start)
    {
        BinkDocument alex = start, zoe = Copy(start);
        ManualTransport ta = new(), tz = new();
        var sa = new BinkCollaborationSession(alex, ta, "alex");
        var sz = new BinkCollaborationSession(zoe, tz, "zoe");
        int deliveredA = 0, deliveredZ = 0;
        async Task Exchange()
        {
            // Crosswise, in the order each sent: alex's to zoe, zoe's to alex, as two links would.
            List<BinkCollaborationMessage> fromA = ta.Sent.Skip(deliveredA).ToList(), fromZ = tz.Sent.Skip(deliveredZ).ToList();
            deliveredA += fromA.Count;
            deliveredZ += fromZ.Count;
            foreach (BinkCollaborationMessage m in fromA) await tz.DeliverAsync(m);
            foreach (BinkCollaborationMessage m in fromZ) await ta.DeliverAsync(m);
        }
        await Task.CompletedTask;
        return (alex, zoe, Exchange, new Both(sa, sz));
    }

    private sealed class Both(IAsyncDisposable a, IAsyncDisposable b) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await a.DisposeAsync();
            await b.DisposeAsync();
        }
    }

    private static BinkDocument Page(int strokes)
    {
        var document = new BinkDocument();
        for (int i = 0; i < strokes; i++) document.AddElement(Stroke(i));
        return document;
    }

    [TestMethod]
    public async Task Bringing_different_elements_to_front_at_once_ends_in_one_order()
    {
        // Index-based moves applied in a different order on each side left alex with [2 3 0 1] and zoe with [2 3 1 0].
        (BinkDocument alex, BinkDocument zoe, Func<Task> exchange, IAsyncDisposable sessions) = await PairAsync(Page(4));
        await using (sessions)
        {
            Guid[] ids = Order(alex);
            alex.Arrange([ids[0]], BinkArrangement.BringToFront);
            zoe.Arrange([ids[1]], BinkArrangement.BringToFront);
            await exchange();

            CollectionAssert.AreEqual(Order(alex), Order(zoe));
            CollectionAssert.AreEquivalent(new[] { ids[0], ids[1] }, Order(alex)[2..]);
        }
    }

    [TestMethod]
    public async Task Moving_one_element_two_ways_at_once_ends_where_one_of_the_moves_put_it_on_both_sides()
    {
        (BinkDocument alex, BinkDocument zoe, Func<Task> exchange, IAsyncDisposable sessions) = await PairAsync(Page(5));
        await using (sessions)
        {
            Guid target = Order(alex)[2];
            alex.Arrange([target], BinkArrangement.BringToFront);
            zoe.Arrange([target], BinkArrangement.SendToBack);
            await exchange();

            CollectionAssert.AreEqual(Order(alex), Order(zoe));
            int at = Array.IndexOf(Order(alex), target);
            Assert.IsTrue(at is 0 or 4, "It is at the front or the back, as one of the two moves left it.");
        }
    }

    [TestMethod]
    public async Task Moving_one_element_to_different_layers_at_once_ends_on_one_layer_everywhere()
    {
        BinkDocument start = Page(3);
        BinkLayer notes = start.AddLayer("Notes");
        BinkLayer marks = start.AddLayer("Marks");
        (BinkDocument alex, BinkDocument zoe, Func<Task> exchange, IAsyncDisposable sessions) = await PairAsync(start);
        await using (sessions)
        {
            Guid target = alex.Layers[0].Elements[1].Id;
            alex.Apply(new BinkReorderElementOperation(target, notes.Id, null));
            zoe.Apply(new BinkReorderElementOperation(target, marks.Id, 0));
            await exchange();

            Assert.AreEqual(alex.GetLayerOf(target)!.Id, zoe.GetLayerOf(target)!.Id);
            CollectionAssert.AreEqual(Order(alex), Order(zoe));
        }
    }

    [TestMethod]
    public async Task Reordering_layers_at_once_ends_in_one_layer_order()
    {
        var start = new BinkDocument();
        start.UpdateLayer(start.Layers[0].Id, p => p with { Name = "A" });
        start.AddLayer("B");
        start.AddLayer("C");
        start.AddLayer("D");
        (BinkDocument alex, BinkDocument zoe, Func<Task> exchange, IAsyncDisposable sessions) = await PairAsync(start);
        await using (sessions)
        {
            alex.ReorderLayer(alex.Layers[0].Id, 3);
            zoe.ReorderLayer(zoe.Layers[1].Id, 3);
            await exchange();

            Assert.AreEqual(LayerOrder(alex), LayerOrder(zoe));
        }
    }

    [TestMethod]
    public async Task Undoing_a_move_after_someone_else_moved_things_still_ends_in_one_order()
    {
        (BinkDocument alex, BinkDocument zoe, Func<Task> exchange, IAsyncDisposable sessions) = await PairAsync(Page(4));
        await using (sessions)
        {
            using var history = new BinkHistoryManager(alex);
            Guid[] ids = Order(alex);
            alex.Arrange([ids[0]], BinkArrangement.BringToFront);
            await exchange();
            zoe.Arrange([ids[3]], BinkArrangement.SendToBack);
            await exchange();

            Assert.IsTrue(history.Undo());
            await exchange();

            CollectionAssert.AreEqual(Order(alex), Order(zoe));
            // Undo returns the element to its old key, the bottom one, which the concurrent send-to-back happened to
            // pick too: the two share it and their ids decide, the same way on both sides.
            Assert.IsLessThanOrEqualTo(1, Array.IndexOf(Order(alex), ids[0]));
        }
    }

    [TestMethod]
    public async Task Drawing_at_once_on_both_sides_ends_in_one_order()
    {
        (BinkDocument alex, BinkDocument zoe, Func<Task> exchange, IAsyncDisposable sessions) = await PairAsync(Page(2));
        await using (sessions)
        {
            alex.AddElement(Stroke(5));
            alex.AddElement(Stroke(6));
            zoe.AddElement(Stroke(7));
            await exchange();

            CollectionAssert.AreEqual(Order(alex), Order(zoe));
            Assert.AreEqual(5, alex.ElementCount);
        }
    }

    [TestMethod]
    public async Task A_participant_who_joins_by_snapshot_continues_with_everyone_elses_keys()
    {
        BinkDocument alex = Page(3);
        alex.Arrange([alex.Layers[0].Elements[0].Id], BinkArrangement.BringToFront);
        var zoe = new BinkDocument();
        ManualTransport ta = new(), tz = new();
        await using var sa = new BinkCollaborationSession(alex, ta, "alex");
        await using var sz = new BinkCollaborationSession(zoe, tz, "zoe");

        await sz.RequestSnapshotAsync();
        await ta.DeliverAsync(Assert.ContainsSingle(tz.Sent));
        await tz.DeliverAsync(Assert.ContainsSingle(ta.Sent.Where(m => m.Kind == BinkCollaborationMessageKind.Snapshot)));

        CollectionAssert.AreEqual(alex.Layers[0].Keys.ToArray(), zoe.Layers[0].Keys.ToArray());
        CollectionAssert.AreEqual(Order(alex), Order(zoe));
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public async Task Three_participants_moving_and_drawing_at_random_always_end_in_one_order(int seed)
    {
        // Every round each participant moves elements to random places, sometimes onto another layer, reorders the
        // layers and draws, and the messages then arrive in a random interleaving (each sender's in the order sent).
        var random = new Random(seed);
        BinkDocument start = Page(12);
        start.AddLayer("Notes");
        start.AddLayer("Marks");
        BinkDocument[] docs = [start, Copy(start), Copy(start)];
        ManualTransport[] transports = [new(), new(), new()];
        var sessions = new List<BinkCollaborationSession>();
        for (int p = 0; p < 3; p++) sessions.Add(new BinkCollaborationSession(docs[p], transports[p], "p" + p));
        int[] delivered = new int[3];
        try
        {
            for (int round = 0; round < 25; round++)
            {
                for (int p = 0; p < 3; p++)
                {
                    BinkDocument d = docs[p];
                    for (int m = 0; m < 3; m++)
                    {
                        BinkElement[] all = d.AllElements.ToArray();
                        BinkElement pick = all[random.Next(all.Length)];
                        BinkLayer target = d.Layers[random.Next(d.Layers.Count)];
                        int? index = random.Next(3) == 0 ? null : random.Next(target.Count + 1);
                        d.Apply(new BinkReorderElementOperation(pick.Id, target.Id, index));
                    }
                    if (random.Next(3) == 0) d.ReorderLayer(d.Layers[random.Next(d.Layers.Count)].Id, random.Next(d.Layers.Count));
                    if (random.Next(2) == 0) d.AddElement(Stroke(100 + round * 3 + p), d.Layers[random.Next(d.Layers.Count)].Id);
                }

                // Deliver everything sent so far, interleaving the senders at random.
                var queues = Enumerable.Range(0, 3).Select(p => new Queue<BinkCollaborationMessage>(transports[p].Sent.Skip(delivered[p]))).ToArray();
                for (int p = 0; p < 3; p++) delivered[p] = transports[p].Sent.Count;
                while (queues.Any(q => q.Count > 0))
                {
                    int from = random.Next(3);
                    if (queues[from].Count == 0) continue;
                    BinkCollaborationMessage message = queues[from].Dequeue();
                    for (int to = 0; to < 3; to++) if (to != from) await transports[to].DeliverAsync(message);
                }
            }

            for (int p = 1; p < 3; p++)
            {
                Assert.AreEqual(LayerOrder(docs[0]), LayerOrder(docs[p]), $"layers, participant {p}");
                for (int l = 0; l < docs[0].Layers.Count; l++)
                {
                    CollectionAssert.AreEqual(docs[0].Layers[l].Elements.Select(e => e.Id).ToArray(), docs[p].Layers[l].Elements.Select(e => e.Id).ToArray(), $"layer {l}, participant {p}");
                }
            }
        }
        finally
        {
            foreach (BinkCollaborationSession s in sessions) await s.DisposeAsync();
        }
    }

    // Found by review: each failed before its fix.

    [TestMethod]
    public async Task A_move_that_changes_nothing_does_not_undo_someone_elses_real_move()
    {
        // Sent with clock 0, it skipped the comparison on the receiver and took a tick there, outranking the real move.
        (BinkDocument alex, BinkDocument zoe, Func<Task> exchange, IAsyncDisposable sessions) = await PairAsync(Page(3));
        await using (sessions)
        {
            Guid top = Order(alex)[2];
            alex.Apply(new BinkReorderElementOperation(top, alex.Layers[0].Id, null));
            zoe.Arrange([top], BinkArrangement.SendToBack);
            await exchange();

            CollectionAssert.AreEqual(Order(alex), Order(zoe));
            Assert.AreEqual(top, Order(alex)[0]);
        }
    }

    [TestMethod]
    public async Task A_layer_move_that_changes_nothing_does_not_undo_someone_elses_real_one()
    {
        var start = new BinkDocument();
        start.UpdateLayer(start.Layers[0].Id, p => p with { Name = "A" });
        start.AddLayer("B");
        start.AddLayer("C");
        (BinkDocument alex, BinkDocument zoe, Func<Task> exchange, IAsyncDisposable sessions) = await PairAsync(start);
        await using (sessions)
        {
            Guid c = alex.Layers[2].Id;
            alex.ReorderLayer(c, 2);
            zoe.ReorderLayer(c, 0);
            await exchange();

            Assert.AreEqual("C,A,B", LayerOrder(alex));
            Assert.AreEqual(LayerOrder(alex), LayerOrder(zoe));
        }
    }

    [TestMethod]
    public async Task A_participant_who_joined_by_snapshot_takes_the_next_layer_move()
    {
        // The snapshot's layer clocks of 0 were stamped afresh on the joiner, which then refused the host's next move.
        var hub = new TestHub();
        var alex = new BinkDocument();
        alex.UpdateLayer(alex.Layers[0].Id, p => p with { Name = "A" });
        alex.AddLayer("B");
        var zoe = new BinkDocument();
        await using var sa = new BinkCollaborationSession(alex, hub.CreateClient(), "alex");
        await using var sz = new BinkCollaborationSession(zoe, hub.CreateClient(), "zoe");
        await sz.RequestSnapshotAsync();
        await Task.Yield();
        Assert.AreEqual(LayerOrder(alex), LayerOrder(zoe));

        alex.ReorderLayer(alex.Layers[1].Id, 0);
        await Task.Yield();

        Assert.AreEqual("B,A", LayerOrder(zoe));
    }

    [TestMethod]
    public void A_rolled_back_batch_leaves_the_clocks_as_they_were()
    {
        // Rolling back a move gave the element a new tick, and then it refused a collaborator's concurrent move.
        BinkDocument alex = Page(4);
        BinkDocument zoe = Copy(alex);
        Guid x = Order(alex)[1];
        BinkDrawingOperation? sent = null;
        zoe.Changed += (_, e) => sent = e.Operation;
        zoe.Arrange([x], BinkArrangement.SendToBack);

        Assert.ThrowsExactly<InvalidOperationException>(() => alex.Apply(new BinkBatchOperation([new BinkReorderElementOperation(x, alex.Layers[0].Id, null), new BinkRemoveElementOperation(Guid.NewGuid())])));
        alex.Apply(sent!, BinkChangeOrigin.Remote);

        CollectionAssert.AreEqual(Order(alex), Order(zoe));
    }

    [TestMethod]
    public async Task Undoing_a_delete_while_someone_moves_the_element_ends_in_one_place()
    {
        // One side applied the move then the return, the other the return then the move; the return now takes a tick
        // and is weighed against the place the element last had, so both keep the same one.
        (BinkDocument alex, BinkDocument zoe, Func<Task> exchange, IAsyncDisposable sessions) = await PairAsync(Page(4));
        await using (sessions)
        {
            using var history = new BinkHistoryManager(alex);
            Guid x = Order(alex)[1];
            alex.RemoveElement(x);
            history.Undo();
            zoe.Arrange([x], BinkArrangement.BringToFront);
            await exchange();

            CollectionAssert.AreEqual(Order(alex), Order(zoe));
        }
    }

    [TestMethod]
    public async Task Undoing_a_delete_after_a_move_of_the_deleted_element_arrived_ends_in_one_place()
    {
        // The move reached the deleting side while the element was gone and was dropped, so its tombstone kept the old
        // place: the undo put it back there, while the mover, applying the delete and then the return, kept it in front.
        (BinkDocument alex, BinkDocument zoe, Func<Task> exchange, IAsyncDisposable sessions) = await PairAsync(Page(4));
        await using (sessions)
        {
            using var history = new BinkHistoryManager(alex);
            Guid x = Order(alex)[1];
            alex.RemoveElement(x);
            zoe.Arrange([x], BinkArrangement.BringToFront);
            await exchange();
            history.Undo();
            await exchange();

            Assert.IsTrue(alex.ContainsElement(x));
            CollectionAssert.AreEqual(Order(alex), Order(zoe));
        }
    }

    [TestMethod]
    [DataRow(false, DisplayName = "undone before the move arrives")]
    [DataRow(true, DisplayName = "undone after the move arrived")]
    public async Task Undoing_a_layer_removal_while_someone_reorders_the_layer_ends_in_one_order(bool exchangeFirst)
    {
        // A removed layer left no record of where it was, and came back with a clock of 0, so the reorder won on one
        // side and was never applied on the other.
        BinkDocument start = Page(1);
        start.AddLayer("B");
        start.AddLayer("C");
        (BinkDocument alex, BinkDocument zoe, Func<Task> exchange, IAsyncDisposable sessions) = await PairAsync(start);
        await using (sessions)
        {
            using var history = new BinkHistoryManager(alex);
            Guid b = alex.Layers[1].Id;
            alex.RemoveLayer(b);
            zoe.ReorderLayer(b, 2);
            if (exchangeFirst) await exchange();
            history.Undo();
            await exchange();

            Assert.IsTrue(alex.TryGetLayer(b, out _));
            Assert.AreEqual(LayerOrder(alex), LayerOrder(zoe));
        }
    }

    [TestMethod]
    public void Moves_leave_no_tombstones_and_a_real_one_is_not_pushed_out_by_them()
    {
        // Every move took a place in the tombstone queue: thousands of moves filled it with ids that were never
        // removed, pushing real tombstones out, and a move followed by a delete queued the id twice, so the stale
        // copy later removed the real tombstone.
        BinkDocument document = Page(3);
        Guid[] ids = Order(document);
        for (int i = 0; i < 5000; i++) document.Arrange([Order(document)[0]], BinkArrangement.BringToFront);
        Assert.AreEqual(0, document.ElementTombstoneCount);

        document.Arrange([ids[1]], BinkArrangement.BringToFront);
        document.RemoveElement(ids[1]);
        Guid layer = document.Layers[0].Id;
        for (int i = 0; i < BinkDocument.MaxTombstones - 1; i++)
        {
            BinkStroke s = Stroke(i % 20);
            document.AddElement(s, layer);
            document.RemoveElement(s.Id);
        }

        Assert.IsTrue(document.HasElementTombstone(ids[1]), "The real tombstone is still among the newest.");
        Assert.AreEqual(BinkDocument.MaxTombstones, document.ElementTombstoneCount);
    }

    [TestMethod]
    public void A_snapshot_leaves_no_tombstones_for_the_elements_it_keeps()
    {
        BinkDocument document = Page(4);
        BinkDocument copy = Copy(document);
        var operations = new List<BinkDrawingOperation>();
        foreach (BinkElement e in document.Layers[0].Elements.Reverse()) operations.Add(new BinkRemoveElementOperation(e.Id));
        for (int i = 0; i < copy.Layers[0].Count; i++)
        {
            operations.Add(new BinkAddElementOperation(document.Layers[0].Id, copy.Layers[0].Elements[i]) { OrderKey = copy.Layers[0].KeyAt(i), OrderClock = copy.Layers[0].ClockAt(i), Authoritative = true });
        }

        document.Apply(new BinkBatchOperation(operations, "Synchronize"), BinkChangeOrigin.Remote);

        Assert.AreEqual(0, document.ElementTombstoneCount);
    }

    [TestMethod]
    public void An_element_renamed_for_a_duplicate_id_keeps_the_other_elements_keys()
    {
        // Elements tied on a key are ordered by id. The fresh id given to a duplicate could sort it before its tied
        // neighbour, and keys out of order had the whole layer keyed afresh, so a replica joining from the file had
        // keys no one else had.
        BinkDocument document = Page(4);
        string[] keys = document.Layers[0].Keys.ToArray();
        Guid[] ids = Order(document);
        var root = System.Text.Json.Nodes.JsonNode.Parse(BinkSerializer.Serialize(document, new BinkSerializerOptions { IncludeOrderKeys = true }))!;
        var layer = root["layers"]![0]!;
        layer["elementKeys"]![2] = keys[1];
        layer["elements"]![1]!["id"] = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff").ToString();
        layer["elements"]![2]!["id"] = ids[0].ToString();

        BinkDocument read = BinkSerializer.Deserialize(root.ToJsonString());

        Guid[] order = Order(read);
        Assert.AreEqual(ids[0], order[0]);
        Assert.AreEqual(Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), order[1]);
        Assert.AreEqual(ids[3], order[3]);
        Assert.AreEqual(keys[0], read.Layers[0].KeyAt(0), "Elements outside the tie keep their keys.");
        Assert.AreEqual(keys[3], read.Layers[0].KeyAt(3), "Elements outside the tie keep their keys.");
    }

    [TestMethod]
    public void A_layer_added_with_the_keys_it_keeps_is_reported_as_it_was_given()
    {
        // The keys and clocks of a layer's elements were copied into a new operation on every apply, even unchanged.
        BinkDocument document = Page(3);
        BinkDocument other = Copy(document);
        BinkDrawingOperation? applied = null;
        document.Changed += (_, e) => applied = e.Operation;
        document.DuplicateLayer(document.Layers[0].Id);

        BinkDrawingOperation? reported = null;
        other.Changed += (_, e) => reported = e.Operation;
        other.Apply(applied!, BinkChangeOrigin.Remote);

        Assert.AreSame(applied, reported);
    }

    [TestMethod]
    public void Inserting_thousands_of_times_at_one_spot_still_works_and_past_the_limit_is_refused_as_a_change()
    {
        // Keys grew past the old 256 characters at the 1,271st insertion, and Between's ArgumentException escaped
        // Apply, whose callers catch InvalidOperationException.
        var document = new BinkDocument(BinkDocumentSettings.Default, BinkDocumentLimits.Default with { MaxElements = 100_000 });
        document.AddElement(Stroke(0));
        document.AddElement(Stroke(1));
        for (int i = 0; i < 5000; i++) document.Apply(new BinkAddElementOperation(document.Layers[0].Id, Stroke(i + 2), 1));
        Assert.AreEqual(5002, document.ElementCount);

        string longest = new('V', BinkOrderKey.MaxLength - 2);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            string? key = "a0" + longest[..^1] + "U";
            string? next = "a0" + longest;
            for (int i = 0; i < 100; i++) next = BinkOrderKey.Between(key, next);
        });
    }

    [TestMethod]
    public void A_move_from_a_peer_that_sends_no_keys_is_placed_by_its_index()
    {
        // Mixed versions: an older participant's move has an index and no key, and lands where the index says.
        BinkDocument document = Page(3);
        Guid[] ids = Order(document);

        document.Apply(new BinkReorderElementOperation(ids[2], document.Layers[0].Id, 0), BinkChangeOrigin.Remote);

        CollectionAssert.AreEqual(new[] { ids[2], ids[0], ids[1] }, Order(document));
    }

    [TestMethod]
    public void Keys_are_saved_only_when_asked_and_a_document_read_back_has_the_same_ones()
    {
        BinkDocument document = Page(4);
        document.Arrange([Order(document)[0]], BinkArrangement.BringToFront);
        document.AddLayer("Second");

        string plain = BinkSerializer.Serialize(document);
        StringAssert.DoesNotMatch(plain, new System.Text.RegularExpressions.Regex("elementKeys"), "A file's order is its list; it carries no keys unless asked.");
        CollectionAssert.AreEqual(Order(document), Order(BinkSerializer.Deserialize(plain)));

        BinkDocument keyed = BinkSerializer.Deserialize(BinkSerializer.Serialize(document, new BinkSerializerOptions { IncludeOrderKeys = true }));
        CollectionAssert.AreEqual(document.Layers[0].Keys.ToArray(), keyed.Layers[0].Keys.ToArray());
        Assert.AreEqual(document.OrderKeyOf(document.Layers[1].Id), keyed.OrderKeyOf(keyed.Layers[1].Id));
    }

    [TestMethod]
    public void Keys_out_of_step_with_the_list_are_made_afresh_from_it()
    {
        // A file (or a peer) whose keys contradict its list: the list wins, and the keys are made from it.
        BinkDocument document = Page(3);
        string json = BinkSerializer.Serialize(document, new BinkSerializerOptions { IncludeOrderKeys = true });
        string[] keys = document.Layers[0].Keys.ToArray();
        string swapped = json.Replace($"\"elementKeys\":[\"{keys[0]}\",\"{keys[1]}\"", $"\"elementKeys\":[\"{keys[1]}\",\"{keys[0]}\"", StringComparison.Ordinal);
        Assert.AreNotEqual(json, swapped);

        BinkDocument read = BinkSerializer.Deserialize(swapped);

        CollectionAssert.AreEqual(Order(document), Order(read));
        for (int i = 1; i < read.Layers[0].Keys.Count; i++) Assert.IsLessThan(0, string.CompareOrdinal(read.Layers[0].Keys[i - 1], read.Layers[0].Keys[i]));
    }

    [TestMethod]
    public void A_hostile_key_from_a_peer_is_ignored_and_the_index_used()
    {
        BinkDocument document = Page(2);
        var stroke = Stroke(9);

        document.Apply(new BinkAddElementOperation(document.Layers[0].Id, stroke, 0) { OrderKey = "not a key!" }, BinkChangeOrigin.Remote);

        Assert.AreEqual(stroke.Id, Order(document)[0]);
    }
}
