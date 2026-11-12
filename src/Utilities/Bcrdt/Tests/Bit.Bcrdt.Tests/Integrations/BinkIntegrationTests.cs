using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bit.Bcrdt.Bink;
using Bit.Bink;

namespace Bit.Bcrdt.Tests.Integrations;

/// <summary>
/// <see cref="BcrdtBinkBinding"/>: Bink drawings bound to a shared map converge, keep concurrent edits of different
/// elements and properties, never echo, and undo selectively.
/// </summary>
[TestClass]
public class BinkIntegrationTests
{
    public static IEnumerable<object[]> Seeds
    {
        get
        {
            for (int seed = 1; seed <= 25; seed++) yield return [seed];
        }
    }

    private sealed class Replica : IDisposable
    {
        public Replica(ulong clientId, BinkDocument? drawing = null, Func<BinkDocument, BinkHistoryManager>? history = null)
        {
            Doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = clientId });
            Drawing = drawing ?? new BinkDocument();

            // Counted from before the binding exists, so what it writes when it is created counts too.
            string origin = $"bink-{clientId}";
            Doc.Updated += args =>
            {
                if (ReferenceEquals(args.Origin, origin)) Transactions++;
            };
            History = history?.Invoke(Drawing);
            Binding = new BcrdtBinkBinding(Doc.GetMap("drawing"), Drawing, new BcrdtBinkBindingOptions { Origin = origin, History = History });
            Drawing.Changed += (_, e) =>
            {
                if (e.Origin == BinkChangeOrigin.Remote) RemoteBatches++;
            };
        }

        public BcrdtDocument Doc { get; }
        public BinkDocument Drawing { get; }
        public BinkHistoryManager? History { get; }
        public BcrdtBinkBinding Binding { get; }

        /// <summary>Transactions the binding wrote.</summary>
        public int Transactions { get; set; }

        /// <summary>Changes the binding applied to the drawing.</summary>
        public int RemoteBatches { get; set; }

        public void Dispose()
        {
            Binding.Dispose();
            Doc.Dispose();
        }
    }

    private static void Sync(BcrdtDocument from, BcrdtDocument to) =>
        to.ApplyUpdate(from.EncodeStateAsUpdate(to.GetStateVector()), origin: "sync");

    private static void SyncAll(params Replica[] replicas)
    {
        foreach (Replica source in replicas)
        {
            foreach (Replica target in replicas)
            {
                if (!ReferenceEquals(source, target)) Sync(source.Doc, target.Doc);
            }
        }
    }

    private static BinkStroke Stroke(float x, float y, BinkColor color) =>
        new(BinkStrokePoints.FromPositions([new Vector2(x, y), new Vector2(x + 10.25f, y + 4.5f), new Vector2(x + 20.75f, y + 1.125f)]), new BinkPenBrush { Color = color, Width = 3 });

    private static BinkRectangleShape Rectangle(float x, float y) => new(new BinkRect(x, y, 40, 30));

    /// <summary>A drawing as text, versions left out: they are each replica's own bookkeeping.</summary>
    private static string Describe(BinkDocument drawing)
    {
        var text = new StringBuilder();
        text.Append(FormattableString.Invariant($"settings {drawing.Settings.Width}x{drawing.Settings.Height} {drawing.Settings.Background.ToCss()}\n"));
        foreach ((string key, string value) in drawing.Metadata.OrderBy(static pair => pair.Key, StringComparer.Ordinal)) text.Append($"meta {key}={value}\n");
        foreach (BinkLayer layer in drawing.Layers)
        {
            text.Append(FormattableString.Invariant($"layer {layer.Id} {layer.Name} {layer.IsVisible} {layer.IsLocked} {layer.Opacity} {layer.BlendMode}\n"));
            foreach (BinkElement element in layer.Elements) text.Append("  ").Append(Json(element)).Append('\n');
        }
        return text.ToString();
    }

    private static string Json(BinkElement element)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) BinkSerializer.WriteElementTo(writer, element);
        JsonObject json = JsonNode.Parse(stream.ToArray())!.AsObject();
        json.Remove("v");
        return json.ToJsonString();
    }

    private static void AssertConverged(string context, params Replica[] replicas)
    {
        string expected = Describe(replicas[0].Drawing);
        foreach (Replica replica in replicas.Skip(1)) Assert.AreEqual(expected, Describe(replica.Drawing), context);
    }

    [TestMethod]
    public void Fresh_drawings_write_nothing_and_share_one_default_layer()
    {
        using var a = new Replica(1);
        using var b = new Replica(2);

        Assert.AreEqual(0, a.Transactions);
        Assert.AreEqual(0, a.Doc.GetMap("drawing").Count);
        Assert.AreEqual(BcrdtBinkBinding.DefaultLayerId, a.Drawing.Layers.Single().Id);
        Assert.AreEqual(BcrdtBinkBinding.DefaultLayerId, b.Drawing.Layers.Single().Id);

        a.Drawing.AddElement(Stroke(0, 0, BinkColor.Black));
        b.Drawing.AddElement(Rectangle(100, 100));
        SyncAll(a, b);

        Assert.ContainsSingle(a.Drawing.Layers, "Both drew into the same layer.");
        Assert.AreEqual(2, a.Drawing.ElementCount);
        AssertConverged("fresh drawings", a, b);
    }

    [TestMethod]
    public void A_drawing_with_content_seeds_an_empty_map_and_a_joining_drawing_takes_the_shared_one()
    {
        var existing = new BinkDocument();
        existing.AddElement(Stroke(5, 5, BinkColor.Black));
        BinkLayer notes = existing.AddLayer("Notes");
        existing.AddElement(Rectangle(10, 10), notes.Id);
        existing.SetMetadata("title", "Plan");

        using var a = new Replica(1, existing);
        Assert.AreEqual(1, a.Transactions, "Seeding is one transaction.");

        using var b = new Replica(2);
        Sync(a.Doc, b.Doc);
        AssertConverged("joined", a, b);
        Assert.HasCount(2, b.Drawing.Layers);
        Assert.AreEqual("Plan", b.Drawing.Metadata["title"]);
        Assert.AreEqual(0, b.Transactions, "Joining writes nothing.");
    }

    [TestMethod]
    public void Binding_to_a_map_that_already_holds_a_drawing_replaces_the_local_content()
    {
        using var a = new Replica(1);
        a.Drawing.AddElement(Stroke(1, 1, BinkColor.Black));

        using var target = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        Sync(a.Doc, target);
        var local = new BinkDocument();
        local.AddElement(Rectangle(300, 300));
        using var binding = new BcrdtBinkBinding(target.GetMap("drawing"), local);

        Assert.AreEqual(Describe(a.Drawing), Describe(local));
    }

    [TestMethod]
    public void Concurrent_additions_moves_and_deletions_converge()
    {
        using var a = new Replica(1);
        using var b = new Replica(2);
        BinkStroke stroke = Stroke(0, 0, BinkColor.Black);
        BinkRectangleShape box = Rectangle(50, 50);
        BinkRectangleShape doomed = Rectangle(90, 90);
        a.Drawing.AddElement(stroke);
        a.Drawing.AddElement(box);
        a.Drawing.AddElement(doomed);
        SyncAll(a, b);

        a.Drawing.ReplaceElement(a.Drawing.GetElement(stroke.Id).Translate(new Vector2(15, 0)));
        a.Drawing.AddElement(Stroke(200, 0, BinkColor.Black));
        b.Drawing.ReplaceElement(b.Drawing.GetElement(box.Id).Translate(new Vector2(0, 25)));
        b.Drawing.RemoveElement(doomed.Id);
        b.Drawing.AddElement(Rectangle(300, 300));
        SyncAll(a, b);

        AssertConverged("after concurrent edits", a, b);
        Assert.AreEqual(4, a.Drawing.ElementCount);
        Assert.IsFalse(a.Drawing.ContainsElement(doomed.Id));
        Assert.AreEqual(15f, a.Drawing.GetElement(stroke.Id).Transform.M31);
        Assert.AreEqual(25f, a.Drawing.GetElement(box.Id).Transform.M32);
    }

    [TestMethod]
    public void Concurrent_edits_of_different_properties_of_one_element_both_survive()
    {
        using var a = new Replica(1);
        using var b = new Replica(2);
        BinkRectangleShape box = Rectangle(0, 0);
        a.Drawing.AddElement(box);
        SyncAll(a, b);

        // One moves it, the other recolours it, neither seeing the other.
        a.Drawing.ReplaceElement(a.Drawing.GetElement(box.Id).Translate(new Vector2(40, 0)));
        var shared = (BinkRectangleShape)b.Drawing.GetElement(box.Id);
        b.Drawing.ReplaceElement(shared with { Style = shared.Style with { StrokeColor = new BinkColor(200, 0, 0) } });
        SyncAll(a, b);

        AssertConverged("move and recolour", a, b);
        var merged = (BinkRectangleShape)a.Drawing.GetElement(box.Id);
        Assert.AreEqual(40f, merged.Transform.M31, "The move survived.");
        Assert.AreEqual(new BinkColor(200, 0, 0), merged.Style.StrokeColor, "The recolour survived.");
    }

    [TestMethod]
    public void A_deletion_wins_over_a_concurrent_edit_of_the_deleted_element()
    {
        using var a = new Replica(1);
        using var b = new Replica(2);
        BinkRectangleShape box = Rectangle(0, 0);
        a.Drawing.AddElement(box);
        SyncAll(a, b);

        a.Drawing.RemoveElement(box.Id);
        b.Drawing.ReplaceElement(b.Drawing.GetElement(box.Id).Translate(new Vector2(5, 5)));
        SyncAll(a, b);

        AssertConverged("delete and move", a, b);
        Assert.IsFalse(b.Drawing.ContainsElement(box.Id));
    }

    [TestMethod]
    public void Concurrent_reorders_and_layer_moves_converge_on_one_stacking_order()
    {
        using var a = new Replica(1);
        using var b = new Replica(2);
        BinkElement[] elements = [Rectangle(0, 0), Rectangle(10, 10), Rectangle(20, 20), Rectangle(30, 30)];
        foreach (BinkElement element in elements) a.Drawing.AddElement(element);
        BinkLayer top = a.Drawing.AddLayer("Top");
        SyncAll(a, b);

        Guid layer = BcrdtBinkBinding.DefaultLayerId;
        a.Drawing.Apply(new BinkReorderElementOperation(elements[0].Id, layer, null));      // to the front
        b.Drawing.Apply(new BinkReorderElementOperation(elements[3].Id, layer, 0));         // to the back
        b.Drawing.Apply(new BinkReorderElementOperation(elements[1].Id, top.Id, null));     // to the other layer
        a.Drawing.Apply(new BinkReorderLayerOperation(top.Id, 0));
        SyncAll(a, b);

        AssertConverged("reorders", a, b);
        Assert.AreSequenceEqual(
            new[] { elements[3].Id, elements[2].Id, elements[0].Id }, a.Drawing.GetLayer(layer).Elements.Select(static element => element.Id).ToArray());
        Assert.AreEqual(top.Id, a.Drawing.Layers[0].Id);
        Assert.AreEqual(elements[1].Id, a.Drawing.GetLayer(top.Id).Elements.Single().Id);
    }

    [TestMethod]
    public void Removing_a_layer_removes_its_elements_everywhere_including_ones_drawn_into_it_concurrently()
    {
        using var a = new Replica(1);
        using var b = new Replica(2);
        BinkLayer scratch = a.Drawing.AddLayer("Scratch");
        a.Drawing.AddElement(Rectangle(0, 0), scratch.Id);
        a.Drawing.AddElement(Rectangle(5, 5), BcrdtBinkBinding.DefaultLayerId);
        SyncAll(a, b);

        a.Drawing.RemoveLayer(scratch.Id);
        b.Drawing.AddElement(Stroke(1, 1, BinkColor.Black), scratch.Id);
        b.Drawing.UpdateLayer(BcrdtBinkBinding.DefaultLayerId, properties => properties with { Name = "Base", Opacity = 0.5f });
        SyncAll(a, b);

        AssertConverged("layer removed", a, b);
        Assert.ContainsSingle(b.Drawing.Layers);
        Assert.AreEqual(1, b.Drawing.ElementCount);
        Assert.AreEqual("Base", b.Drawing.Layers[0].Name);
        Assert.AreEqual(0.5f, b.Drawing.Layers[0].Opacity);
    }

    [TestMethod]
    public void Settings_and_metadata_converge()
    {
        using var a = new Replica(1);
        using var b = new Replica(2);

        a.Drawing.UpdateSettings(a.Drawing.Settings with { Width = 1024, Background = new BinkColor(10, 20, 30) });
        b.Drawing.SetMetadata("owner", "ana");
        SyncAll(a, b);
        b.Drawing.SetMetadata("owner", null);
        SyncAll(a, b);

        AssertConverged("settings", a, b);
        Assert.AreEqual(1024f, b.Drawing.Settings.Width);
        Assert.IsFalse(a.Drawing.Metadata.ContainsKey("owner"));
    }

    [TestMethod]
    public void A_local_change_is_one_transaction_and_a_remote_one_is_applied_without_echo()
    {
        using var a = new Replica(1);
        using var b = new Replica(2);

        using (a.Drawing.BeginBatch("Import"))
        {
            for (int i = 0; i < 10; i++) a.Drawing.AddElement(Rectangle(i * 10, 0));
        }
        Assert.AreEqual(1, a.Transactions, "One batch is one transaction.");

        int updatesFromB = 0;
        b.Doc.Updated += args => { if (!ReferenceEquals(args.Origin, "sync")) updatesFromB++; };
        Sync(a.Doc, b.Doc);

        Assert.AreEqual(0, b.Transactions, "Applying a remote change writes nothing back.");
        Assert.AreEqual(0, updatesFromB);
        Assert.AreEqual(1, b.RemoteBatches, "A remote transaction is one change of the drawing.");
        Assert.AreEqual(10, b.Drawing.ElementCount);
        Assert.IsFalse(b.Binding.HasPendingChanges);

        // Back and forth: nothing a replica applied comes back to it.
        Sync(b.Doc, a.Doc);
        Assert.AreEqual(1, a.Transactions);
        Assert.AreEqual(0, a.RemoteBatches);
    }

    [TestMethod]
    public void A_gesture_inside_a_history_group_is_written_once_when_it_ends()
    {
        using var a = new Replica(1, history: static drawing => new BinkHistoryManager(drawing));
        BinkHistoryManager history = a.History!;
        using var b = new Replica(2);
        BinkRectangleShape box = Rectangle(0, 0);
        a.Drawing.AddElement(box);
        Assert.AreEqual(1, a.Transactions, "A change outside a group is a step of its own, written at once.");

        using (history.BeginGroup("Move"))
        {
            for (int frame = 1; frame <= 30; frame++)
            {
                a.Drawing.ReplaceElement(a.Drawing.GetElement(box.Id).Translate(new Vector2(1, 0)));
            }
            Assert.AreEqual(1, a.Transactions, "Nothing is written while the drag is under way.");
            Assert.IsTrue(a.Binding.HasPendingChanges);
        }

        Assert.AreEqual(2, a.Transactions, "The whole drag is one transaction.");
        Assert.IsFalse(a.Binding.HasPendingChanges);

        Assert.IsTrue(history.Undo());
        Assert.AreEqual(3, a.Transactions, "Bink's own undo is written like any other change.");

        SyncAll(a, b);
        AssertConverged("after the drag and its undo", a, b);
        Assert.AreEqual(0f, b.Drawing.GetElement(box.Id).Transform.M31);
    }

    [TestMethod]
    public void A_drawing_with_only_a_custom_page_or_layer_seeds_rather_than_being_replaced()
    {
        var page = new BinkDocument(BinkDocumentSettings.Default with { Width = 1920, Height = 1080 });
        using var a = new Replica(1, page);
        Assert.AreEqual(1, a.Transactions, "A sized page is content to start from.");
        Assert.AreEqual(1920f, a.Drawing.Settings.Width);

        var renamed = new BinkDocument();
        renamed.UpdateLayer(renamed.Layers[0].Id, properties => properties with { Name = "Sketch" });
        using var b = new Replica(2, renamed);
        Assert.AreEqual(1, b.Transactions, "So is a renamed layer.");
        Assert.AreEqual("Sketch", b.Drawing.Layers.Single().Name);
    }

    [TestMethod]
    public void A_remote_change_to_a_layer_being_changed_here_keeps_the_local_properties_still_to_write()
    {
        using var a = new Replica(1, history: static drawing => new BinkHistoryManager(drawing));
        using var b = new Replica(2);
        BinkLayer layer = a.Drawing.AddLayer("Notes");
        SyncAll(a, b);

        using (a.History!.BeginGroup("Adjust"))
        {
            a.Drawing.UpdateLayer(layer.Id, properties => properties with { Opacity = 0.5f });
            Assert.IsTrue(a.Binding.HasPendingChanges);

            b.Drawing.UpdateLayer(layer.Id, properties => properties with { Name = "Renamed" });
            Sync(b.Doc, a.Doc);

            Assert.AreEqual("Renamed", a.Drawing.GetLayer(layer.Id).Name, "the property the collaborator changed arrives");
            Assert.AreEqual(0.5f, a.Drawing.GetLayer(layer.Id).Opacity, "and the one still to write stays");
        }

        SyncAll(a, b);
        AssertConverged("after both layer changes", a, b);
        Assert.AreEqual("Renamed", b.Drawing.GetLayer(layer.Id).Name);
        Assert.AreEqual(0.5f, b.Drawing.GetLayer(layer.Id).Opacity);
    }

    [TestMethod]
    public void A_change_made_by_a_handler_of_another_is_written_with_the_step_it_belongs_to()
    {
        var drawing = new BinkDocument();
        var history = new BinkHistoryManager(drawing);
        BinkRectangleShape label = Rectangle(0, 100);
        drawing.AddElement(label);

        // A derived element that follows another, the way a diagram re-routes a connector when a node moves.
        BinkRectangleShape node = Rectangle(0, 0);
        drawing.Changed += (_, e) =>
        {
            if (e.Origin == BinkChangeOrigin.Local && e.Depth == 0 && e.Operation is BinkReplaceElementOperation { Element: var moved } && moved.Id == node.Id)
            {
                drawing.ReplaceElement(drawing.GetElement(label.Id) with { Transform = moved.Transform });
            }
        };
        drawing.AddElement(node);

        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var binding = new BcrdtBinkBinding(doc.GetMap("drawing"), drawing, new BcrdtBinkBindingOptions { Origin = "bink", History = history });
        int transactions = 0;
        doc.Updated += args => { if (ReferenceEquals(args.Origin, "bink")) transactions++; };

        drawing.ReplaceElement(drawing.GetElement(node.Id).Translate(new Vector2(30, 0)));

        Assert.AreEqual(1, transactions, "The cause and its consequence are one step, and one transaction.");
        Assert.IsFalse(binding.HasPendingChanges);
        using var other = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        Sync(doc, other);
        var copy = new BinkDocument();
        using var reader = new BcrdtBinkBinding(other.GetMap("drawing"), copy);
        Assert.AreEqual(30f, copy.GetElement(label.Id).Transform.M31);
    }

    [TestMethod]
    public void Collaborative_undo_takes_back_only_this_participants_change()
    {
        using var a = new Replica(1);
        using var b = new Replica(2);
        using BcrdtUndoManager undo = a.Binding.CreateUndoManager(new BcrdtUndoManagerOptions { CaptureTimeout = TimeSpan.Zero });

        BinkRectangleShape box = Rectangle(0, 0);
        a.Drawing.AddElement(box);
        SyncAll(a, b);

        a.Drawing.ReplaceElement(a.Drawing.GetElement(box.Id).Translate(new Vector2(60, 0)));
        var shared = (BinkRectangleShape)b.Drawing.GetElement(box.Id);
        b.Drawing.ReplaceElement(shared with { Style = shared.Style with { StrokeColor = new BinkColor(0, 128, 0) } });
        SyncAll(a, b);

        int writes = a.Transactions;
        Assert.IsNotNull(undo.Undo());
        Assert.AreEqual(writes, a.Transactions, "The undo is applied to the drawing without being written back as an edit.");

        var undone = (BinkRectangleShape)a.Drawing.GetElement(box.Id);
        Assert.AreEqual(0f, undone.Transform.M31, "The move is undone.");
        Assert.AreEqual(new BinkColor(0, 128, 0), undone.Style.StrokeColor, "The collaborator's recolour stays.");
        SyncAll(a, b);
        AssertConverged("after undo", a, b);

        // Undoing the addition removes the element everywhere; redoing brings it back with both properties.
        Assert.IsNotNull(undo.Undo());
        SyncAll(a, b);
        Assert.IsFalse(b.Drawing.ContainsElement(box.Id));
        Assert.IsNotNull(undo.Redo());
        Assert.IsNotNull(undo.Redo());
        SyncAll(a, b);
        AssertConverged("after redo", a, b);
        var redone = (BinkRectangleShape)b.Drawing.GetElement(box.Id);
        Assert.AreEqual(60f, redone.Transform.M31);
    }

    [TestMethod]
    public void Remote_changes_are_not_recorded_in_binks_own_history()
    {
        using var a = new Replica(1);
        using var b = new Replica(2, history: static drawing => new BinkHistoryManager(drawing));

        a.Drawing.AddElement(Rectangle(0, 0));
        SyncAll(a, b);

        Assert.AreEqual(1, b.Drawing.ElementCount);
        Assert.IsFalse(b.History!.CanUndo, "A collaborator's change is theirs to undo.");
    }

    [TestMethod]
    public void An_element_a_participant_wrote_wrong_is_reported_and_left_out_rather_than_breaking_the_drawing()
    {
        using var a = new Replica(1);
        var reasons = new List<string>();
        a.Binding.RemoteChangeRejected += reasons.Add;
        BinkRectangleShape box = Rectangle(0, 0);
        a.Drawing.AddElement(box);

        // Somebody else's code writes an entry Bink cannot read, next to a good change in the same transaction.
        using var other = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        Sync(a.Doc, other);
        Guid broken = Guid.NewGuid();
        other.Transact(_ =>
        {
            BcrdtMap drawing = other.GetMap("drawing");
            drawing.Set("e:" + broken, new BcrdtMap { ["type"] = "stroke", ["points"] = "not points", ["$layer"] = BcrdtBinkBinding.DefaultLayerId.ToString() });
            drawing.GetMap("e:" + box.Id)!.Set("transform", new List<object?> { 1.0, 0.0, 0.0, 1.0, 7.0, 0.0 });
        });
        Sync(other, a.Doc);

        Assert.IsFalse(a.Drawing.ContainsElement(broken));
        Assert.ContainsSingle(reasons, string.Join('\n', reasons));
        Assert.AreEqual(7f, a.Drawing.GetElement(box.Id).Transform.M31, "The rest of the change still applies.");
        Assert.AreEqual(1, a.Transactions, "Nothing is written back over it.");
    }

    [TestMethod]
    public void Presence_publishes_the_cursor_and_the_selection()
    {
        using var network = new BcrdtSimulatedNetwork(new BcrdtSimulatedNetworkOptions { Seed = 7 });
        BcrdtSimulatedPeer[] peers = network.AddPeers(2);
        using var presenceA = new BcrdtPresenceService(peers[0].Awareness, peers[0].Doc);
        using var presenceB = new BcrdtPresenceService(peers[1].Awareness, peers[1].Doc);
        var drawingA = new BinkDocument();
        var drawingB = new BinkDocument();
        using var selection = new BinkSelection(drawingA);
        using var a = new BcrdtBinkBinding(peers[0].Doc.GetMap("drawing"), drawingA, new BcrdtBinkBindingOptions { Presence = presenceA, Selection = selection });
        using var b = new BcrdtBinkBinding(peers[1].Doc.GetMap("drawing"), drawingB, new BcrdtBinkBindingOptions { Presence = presenceB });

        BinkRectangleShape box = Rectangle(0, 0);
        drawingA.AddElement(box);
        presenceA.SetUser(new BcrdtCollaboratorInfo("ana", "Ana", "#e5484d"));
        a.PublishCursor(new Vector2(12.5f, 40));
        selection.Set(box.Id);
        network.Flush();

        BcrdtBinkParticipant ana = b.Participants.Single();
        Assert.AreEqual("Ana", ana.Collaborator.User.Name);
        Assert.AreEqual(new Vector2(12.5f, 40), ana.Cursor);
        Assert.AreSequenceEqual(new[] { box.Id }, ana.Selection.ToArray());

        a.PublishCursor(null);
        network.Flush();
        Assert.IsNull(b.Participants.Single().Cursor);

        using var withoutPresence = new BcrdtBinkBinding(peers[0].Doc.GetMap("other"), new BinkDocument());
        Assert.ThrowsExactly<InvalidOperationException>(() => withoutPresence.PublishCursor(Vector2.Zero));
    }

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public void Random_concurrent_drawing_converges_despite_loss_duplication_and_reordering(int seed)
    {
        using var network = new BcrdtSimulatedNetwork(new BcrdtSimulatedNetworkOptions
        {
            Seed = seed,
            DropProbability = 0.2,
            DuplicateProbability = 0.2,
            MaxLatencySteps = 4,
        });

        BcrdtSimulatedPeer[] peers = network.AddPeers(3);
        var drawings = peers.Select(static _ => new BinkDocument()).ToArray();
        var bindings = peers.Select((peer, index) => new BcrdtBinkBinding(peer.Doc.GetMap("drawing"), drawings[index])).ToArray();
        var random = new Random(seed);
        var history = new List<string>();

        try
        {
            for (int round = 0; round < 30; round++)
            {
                for (int index = 0; index < peers.Length; index++)
                {
                    history.Add($"{peers[index].Name}: {RandomEdit(drawings[index], random)}");
                }
                if (round == 12) peers[2].GoOffline();
                if (round == 20) peers[2].GoOnline();
                network.Step();
            }

            network.Flush();
            for (int round = 0; round < 12; round++)
            {
                foreach (BcrdtSimulatedPeer peer in peers) peer.RequestSync();
                network.Flush();
            }
            network.SynchronizeAll();

            string context = $"Seed {seed}; edits:\n{string.Join('\n', history)}";
            BcrdtConvergenceHarness.AssertConverged(network);
            string expected = Describe(drawings[0]);
            for (int index = 1; index < drawings.Length; index++) Assert.AreEqual(expected, Describe(drawings[index]), context);

            // What every replica shows is exactly what the shared state projects to.
            using var fresh = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 99 });
            fresh.ApplyUpdate(peers[0].Doc.EncodeStateAsUpdate());
            var projected = new BinkDocument();
            using var binding = new BcrdtBinkBinding(fresh.GetMap("drawing"), projected);
            Assert.AreEqual(expected, Describe(projected), context);
            Assert.IsTrue(bindings.All(static b => !b.HasPendingChanges), context);
        }
        finally
        {
            foreach (BcrdtBinkBinding binding in bindings) binding.Dispose();
        }
    }

    private static string RandomEdit(BinkDocument drawing, Random random)
    {
        BinkElement[] elements = [.. drawing.AllElements];
        BinkElement? any = elements.Length > 0 ? elements[random.Next(elements.Length)] : null;
        BinkLayer layer = drawing.Layers[random.Next(drawing.Layers.Count)];
        var color = new BinkColor((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));

        try
        {
            switch (random.Next(12))
            {
                case 0:
                case 1:
                    drawing.AddElement(Stroke(random.Next(500), random.Next(500), color), layer.Id);
                    return "stroke";
                case 2:
                    drawing.AddElement(Rectangle(random.Next(500), random.Next(500)), layer.Id);
                    return "rectangle";
                case 3 when any is not null:
                    drawing.ReplaceElement(any.Translate(new Vector2(random.Next(-20, 20), random.Next(-20, 20))));
                    return $"move {any.Id}";
                case 4 when any is BinkRectangleShape box:
                    drawing.ReplaceElement(box with { Style = box.Style with { StrokeColor = color } });
                    return $"recolour {box.Id}";
                case 5 when any is not null:
                    drawing.RemoveElement(any.Id);
                    return $"delete {any.Id}";
                case 6 when any is not null:
                    drawing.Apply(new BinkReorderElementOperation(any.Id, layer.Id, random.Next(layer.Count + 1)));
                    return $"reorder {any.Id}";
                case 7 when drawing.Layers.Count < 4:
                    drawing.AddLayer("L" + random.Next(100));
                    return "add layer";
                case 8 when drawing.Layers.Count > 1 && random.Next(3) == 0:
                    drawing.RemoveLayer(layer.Id);
                    return $"remove layer {layer.Id}";
                case 9:
                    drawing.UpdateLayer(layer.Id, properties => properties with { Name = "N" + random.Next(100), IsVisible = random.Next(2) == 0 });
                    return $"update layer {layer.Id}";
                case 10:
                    drawing.ReorderLayer(layer.Id, random.Next(drawing.Layers.Count));
                    return $"reorder layer {layer.Id}";
                case 11:
                    drawing.SetMetadata("k" + random.Next(3), random.Next(3) == 0 ? null : "v" + random.Next(10));
                    return "metadata";
                default:
                    return "nothing";
            }
        }
        catch (InvalidOperationException ex)
        {
            return "refused: " + ex.Message;
        }
    }
}
