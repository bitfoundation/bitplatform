using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bit.Bcrdt.Blow;
using Bit.Blow;

namespace Bit.Bcrdt.Tests.Integrations;

/// <summary>
/// <see cref="BcrdtBlowBinding"/>: Blow graphs bound to a shared map converge, keep concurrent edits of different
/// elements and properties, never echo, and undo selectively.
/// </summary>
[TestClass]
public class BlowIntegrationTests
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
        public Replica(ulong clientId, IEnumerable<BlowNode>? nodes = null, IEnumerable<BlowEdge>? edges = null, Func<Action, Task>? scope = null)
        {
            Doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = clientId });
            Nodes = [.. nodes ?? []];
            Edges = [.. edges ?? []];

            // Counted from before the binding exists, so what it writes when it is created counts too.
            string origin = $"blow-{clientId}";
            Doc.Updated += args =>
            {
                if (ReferenceEquals(args.Origin, origin)) Transactions++;
            };
            Binding = new BcrdtBlowBinding(Doc.GetMap("flow"), Nodes, Edges, new BcrdtBlowBindingOptions { Origin = origin, RemoteEditScope = scope });
            Binding.RemoteChanged += _ => RemoteChanges++;
        }

        public BcrdtDocument Doc { get; }
        public List<BlowNode> Nodes { get; }
        public List<BlowEdge> Edges { get; }
        public BcrdtBlowBinding Binding { get; }

        /// <summary>Transactions the binding wrote.</summary>
        public int Transactions { get; set; }

        /// <summary>Changes the binding applied to the lists.</summary>
        public int RemoteChanges { get; set; }

        public BlowNode Node(string id) => Nodes.Single(node => node.Id == id);

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

    private static BlowNode Node(string id, double x = 0, double y = 0, string? label = null) =>
        new() { Id = id, Position = new BlowXYPosition(x, y), Label = label ?? id };

    private static BlowEdge Edge(string id, string source, string target) => new() { Id = id, Source = source, Target = target };

    /// <summary>
    /// A graph as Blow saves it, in list order; selection is left out, as it is each participant's own. Keys inside an
    /// object are sorted: the shared state keeps data objects in ordinal key order, as the value model does, so only the
    /// replica that wrote one still has it in the order it was written.
    /// </summary>
    private static string Describe(IEnumerable<BlowNode> nodes, IEnumerable<BlowEdge> edges) =>
        Sorted(JsonNode.Parse(BlowSerializer.Serialize(new BlowDocument { Nodes = [.. nodes], Edges = [.. edges] })))?.ToJsonString() ?? "null";

    private static JsonNode? Sorted(JsonNode? node) => node switch
    {
        JsonObject obj => new JsonObject(obj.OrderBy(static pair => pair.Key, StringComparer.Ordinal).Select(static pair => KeyValuePair.Create(pair.Key, Sorted(pair.Value)))),
        JsonArray array => new JsonArray([.. array.Select(Sorted)]),
        _ => node?.DeepClone(),
    };

    private static string Describe(Replica replica) => Describe(replica.Nodes, replica.Edges);

    private static void AssertConverged(string context, params Replica[] replicas)
    {
        string expected = Describe(replicas[0]);
        foreach (Replica replica in replicas.Skip(1)) Assert.AreEqual(expected, Describe(replica), context);
    }

    [TestMethod]
    public void A_graph_seeds_an_empty_map_and_a_joining_replica_takes_the_shared_one()
    {
        using var a = new Replica(1, [Node("1"), Node("2", 100, 120)], [Edge("e1-2", "1", "2")]);
        Assert.AreEqual(1, a.Transactions, "Seeding is one transaction.");

        using var b = new Replica(2);
        Assert.AreEqual(0, b.Transactions, "An empty graph writes nothing.");
        Sync(a.Doc, b.Doc);
        Assert.AreEqual(Describe(a), Describe(b));
        Assert.AreEqual(0, b.Transactions, "Joining writes nothing.");

        // Bound to a map that already holds a graph, the lists are replaced by it.
        using var joined = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 3 });
        Sync(a.Doc, joined);
        List<BlowNode> nodes = [Node("mine")];
        List<BlowEdge> edges = [];
        using var binding = new BcrdtBlowBinding(joined.GetMap("flow"), nodes, edges);
        Assert.AreEqual(Describe(a), Describe(nodes, edges));
    }

    [TestMethod]
    public void Concurrent_additions_moves_and_deletions_converge()
    {
        using var a = new Replica(1, [Node("1"), Node("2", 0, 100), Node("3", 0, 200)], [Edge("e1-2", "1", "2"), Edge("e2-3", "2", "3")]);
        using var b = new Replica(2);
        SyncAll(a, b);
        AssertConverged("joined", a, b);

        a.Node("1").Position = new BlowXYPosition(50, 0);
        a.Nodes.Add(Node("4", 300, 0));
        a.Edges.Add(Edge("e1-4", "1", "4"));
        a.Binding.Commit();

        b.Nodes.RemoveAll(node => node.Id == "3");
        b.Edges.RemoveAll(edge => edge.Target == "3" || edge.Source == "3");
        b.Nodes.Add(Node("5", 300, 300));
        b.Binding.Commit();
        SyncAll(a, b);

        AssertConverged("after concurrent edits", a, b);
        CollectionAssert.AreEquivalent(new[] { "1", "2", "4", "5" }, a.Nodes.Select(static node => node.Id).ToArray());
        CollectionAssert.AreEquivalent(new[] { "e1-2", "e1-4" }, b.Edges.Select(static edge => edge.Id).ToArray());
        Assert.AreEqual(new BlowXYPosition(50, 0), b.Node("1").Position);
    }

    [TestMethod]
    public void Concurrent_edits_of_different_properties_of_one_node_all_survive()
    {
        using var a = new Replica(1, [Node("n", label: "Review")]);
        using var b = new Replica(2);
        using var c = new Replica(3);
        SyncAll(a, b, c);

        // One drags it, one renames it, one resizes it and changes its data - nobody seeing the others.
        a.Node("n").Position = new BlowXYPosition(120, 40);
        a.Binding.Commit();
        b.Node("n").Label = "Approve";
        b.Binding.Commit();
        c.Node("n").Width = 220;
        c.Node("n").Data = JsonNode.Parse("""{"owner":"ana","hours":3}""");
        c.Binding.Commit();
        SyncAll(a, b, c);

        AssertConverged("move, rename and resize", a, b, c);
        BlowNode merged = a.Node("n");
        Assert.AreEqual(new BlowXYPosition(120, 40), merged.Position);
        Assert.AreEqual("Approve", merged.Label);
        Assert.AreEqual(220, merged.Width);
        Assert.AreEqual("ana", ((JsonElement)merged.Data!).GetProperty("owner").GetString());
    }

    [TestMethod]
    public void Remote_changes_update_the_same_objects_in_place_and_leave_local_state_alone()
    {
        using var a = new Replica(1, [Node("n")]);
        using var b = new Replica(2);
        SyncAll(a, b);

        BlowNode local = b.Node("n");
        local.Selected = true;
        a.Node("n").Label = "Renamed";
        a.Binding.Commit();
        SyncAll(a, b);

        Assert.AreSame(local, b.Node("n"), "The canvas keeps the instance it holds.");
        Assert.AreEqual("Renamed", local.Label);
        Assert.IsTrue(local.Selected, "Selection is each participant's own.");
        Assert.IsFalse(a.Node("n").Selected);
        Assert.IsFalse(b.Binding.Commit(), "A selection is not a change to share.");
    }

    [TestMethod]
    public void A_remote_change_to_one_property_leaves_an_uncommitted_local_change_to_another()
    {
        using var a = new Replica(1, [Node("n")]);
        using var b = new Replica(2);
        SyncAll(a, b);

        // A drag in progress here: the position has moved, nothing is committed yet.
        b.Node("n").Position = new BlowXYPosition(99, 99);
        a.Node("n").Label = "Renamed";
        a.Binding.Commit();
        SyncAll(a, b);

        Assert.AreEqual(new BlowXYPosition(99, 99), b.Node("n").Position, "The drag keeps following the pointer.");
        Assert.AreEqual("Renamed", b.Node("n").Label);
        Assert.IsTrue(b.Binding.Commit());
        SyncAll(a, b);
        AssertConverged("after the drag ends", a, b);
    }

    [TestMethod]
    public void A_deletion_wins_over_a_concurrent_edit_and_an_edge_to_a_deleted_node_is_hidden_everywhere()
    {
        using var a = new Replica(1, [Node("1"), Node("2")]);
        using var b = new Replica(2);
        SyncAll(a, b);

        a.Nodes.RemoveAll(node => node.Id == "2");
        a.Binding.Commit();
        b.Node("2").Label = "Edited";
        b.Edges.Add(Edge("late", "1", "2"));
        b.Binding.Commit();
        SyncAll(a, b);

        AssertConverged("delete and edit", a, b);
        Assert.AreEqual(1, b.Nodes.Count);
        Assert.AreEqual(0, b.Edges.Count, "An edge whose node is gone is not shown.");
        Assert.IsTrue(b.Doc.GetMap("flow").ContainsKey("e:late"), "It is kept, for if the node comes back.");
        Assert.IsFalse(b.Binding.Commit(), "A hidden edge is not a deleted one.");
    }

    [TestMethod]
    public void Concurrent_reorders_converge_on_one_list_order()
    {
        using var a = new Replica(1, [Node("1"), Node("2"), Node("3"), Node("4")], [Edge("a", "1", "2"), Edge("b", "2", "3"), Edge("c", "3", "4")]);
        using var b = new Replica(2);
        SyncAll(a, b);

        // Bring 1 to the front here, send 4 to the back there, the way BringToFrontAsync and SendToBackAsync do.
        BlowNode first = a.Node("1");
        a.Nodes.Remove(first);
        a.Nodes.Add(first);
        a.Binding.Commit();
        BlowNode last = b.Node("4");
        b.Nodes.Remove(last);
        b.Nodes.Insert(0, last);
        BlowEdge edge = b.Edges[2];
        b.Edges.RemoveAt(2);
        b.Edges.Insert(0, edge);
        b.Binding.Commit();
        SyncAll(a, b);

        AssertConverged("reorders", a, b);
        CollectionAssert.AreEqual(new[] { "4", "2", "3", "1" }, a.Nodes.Select(static node => node.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "c", "a", "b" }, a.Edges.Select(static edge => edge.Id).ToArray());
    }

    [TestMethod]
    public void A_commit_is_one_transaction_and_a_remote_change_is_applied_without_echo()
    {
        using var a = new Replica(1);
        int scoped = 0;
        using var b = new Replica(2, scope: edit =>
        {
            scoped++;
            edit();
            return Task.CompletedTask;
        });

        for (int i = 0; i < 10; i++) a.Nodes.Add(Node("n" + i, i * 10));
        for (int i = 1; i < 10; i++) a.Edges.Add(Edge("e" + i, "n" + (i - 1), "n" + i));
        Assert.IsTrue(a.Binding.Commit());
        Assert.AreEqual(1, a.Transactions, "One commit is one transaction.");
        Assert.IsFalse(a.Binding.Commit(), "Committing an unchanged graph writes nothing.");
        Assert.AreEqual(1, a.Transactions);

        Sync(a.Doc, b.Doc);
        Assert.AreEqual(1, scoped, "A remote transaction is one edit of the canvas.");
        Assert.AreEqual(1, b.RemoteChanges);
        Assert.AreEqual(0, b.Transactions, "Applying a remote change writes nothing back.");
        Assert.IsFalse(b.Binding.Commit(), "What was applied is what the shared state holds.");
        Assert.AreEqual(10, b.Nodes.Count);
        Assert.AreEqual(9, b.Edges.Count);

        // The canvas reports the remote edit back with its origin; the binding does not write it.
        b.Binding.OnGraphChangedAsync(new BlowGraphChange(BlowChangeKind.Edited, BlowChangeOrigin.Remote) { Label = "remote" });
        Assert.AreEqual(0, b.Transactions);
        Sync(b.Doc, a.Doc);
        Assert.AreEqual(0, a.RemoteChanges);

        // And a gesture reported by the canvas is committed.
        b.Node("n3").Position = new BlowXYPosition(5, 5);
        b.Binding.OnGraphChangedAsync(new BlowGraphChange(BlowChangeKind.Moved, BlowChangeOrigin.User) { Label = "move" });
        Assert.AreEqual(1, b.Transactions);
    }

    [TestMethod]
    public void An_edit_scope_that_does_not_run_the_edit_at_once_cannot_delay_it()
    {
        using var a = new Replica(1, [Node("n")]);
        Action? deferred = null;
        using var b = new Replica(2, scope: edit =>
        {
            deferred = edit;
            return Task.CompletedTask;
        });

        Sync(a.Doc, b.Doc);
        Assert.AreEqual(1, b.Nodes.Count, "The change was applied while the shared document reported it.");

        deferred!();
        Assert.AreEqual(1, b.Nodes.Count, "Running the edit later does nothing more.");
    }

    [TestMethod]
    public void Every_node_and_edge_property_blow_saves_is_shared()
    {
        var node = new BlowNode
        {
            Id = "full",
            Type = "group",
            Label = "Label",
            Position = new BlowXYPosition(1.5, 2.5),
            Width = 300,
            Height = 200,
            ParentId = "parent",
            Origin = new BlowXYPosition(0.5, 0.5),
            ExtentParent = true,
            Extent = new BlowRect(0, 0, 10, 10),
            ExpandParent = true,
            SourcePosition = BlowPosition.Right,
            TargetPosition = BlowPosition.Left,
            Hidden = true,
            Collapsed = true,
            Draggable = false,
            Selectable = false,
            Connectable = false,
            Deletable = false,
            Focusable = false,
            UseCustomDragHandle = true,
            AriaLabel = "Aria",
            Class = "cls",
            Style = "color: red",
            ZIndex = 4,
            Ports = [new BlowPort { Id = "in", Type = BlowHandleType.Target, DataType = "number", MaxConnections = 2 }],
            Data = "data",
        };
        var edge = new BlowEdge
        {
            Id = "edge",
            Source = "parent",
            Target = "full",
            SourceHandle = "out",
            TargetHandle = "in",
            Type = BlowEdgeType.SmoothStep,
            PathOptions = new BlowEdgePathOptions { BorderRadius = 8 },
            Label = "Label",
            LabelStyle = "font-weight: bold",
            LabelShowBg = false,
            LabelBgStyle = "fill: white",
            LabelBgPaddingX = 6,
            LabelBgPaddingY = 3,
            LabelBgBorderRadius = 2,
            Animated = true,
            Hidden = true,
            Selectable = false,
            Focusable = false,
            Deletable = false,
            Reconnectable = false,
            StrokeWidth = 3,
            Stroke = "#111111",
            MarkerEnd = false,
            MarkerStart = true,
            MarkerType = BlowMarkerType.ArrowClosed,
            MarkerColor = "#222222",
            MarkerWidth = 9,
            MarkerHeight = 10,
            Class = "cls",
            Style = "opacity: 0.5",
            InteractionWidth = 20,
            ZIndex = 2,
            Data = 42L,
        };

        // Created on one replica, the other receives them whole; then every property changes on the first, and the
        // second takes each one onto the objects it already holds.
        using var a = new Replica(1, [new BlowNode { Id = "parent" }, new BlowNode { Id = "full" }], [new BlowEdge { Id = "edge", Source = "parent", Target = "full" }]);
        using var b = new Replica(2);
        SyncAll(a, b);
        BlowNode heldNode = b.Node("full");
        BlowEdge heldEdge = b.Edges.Single();

        a.Nodes[1] = node;
        a.Edges[0] = edge;
        a.Binding.Commit();
        SyncAll(a, b);

        Assert.AreSame(heldNode, b.Node("full"));
        Assert.AreSame(heldEdge, b.Edges.Single());
        AssertConverged("every property", a, b);

        // The binding copies the properties it knows by name. Blow adding a persistent property shows up here.
        string[] runtime = ["Id", "Selected"];
        CollectionAssert.AreEquivalent(
            new[] { "Position", "Type", "Label", "Data", "Width", "Height", "Draggable", "Selectable", "Connectable", "Hidden", "Collapsed", "Deletable", "SourcePosition", "TargetPosition", "UseCustomDragHandle", "Focusable", "AriaLabel", "ParentId", "ExtentParent", "Extent", "ExpandParent", "Class", "Style", "ZIndex", "Origin", "Ports" },
            Settable(typeof(BlowNode)).Except(runtime).ToArray());
        CollectionAssert.AreEquivalent(
            new[] { "Source", "Target", "SourceHandle", "TargetHandle", "Type", "PathOptions", "Label", "LabelStyle", "LabelShowBg", "LabelBgStyle", "LabelBgPaddingX", "LabelBgPaddingY", "LabelBgBorderRadius", "Animated", "Hidden", "Selectable", "Focusable", "Deletable", "Reconnectable", "StrokeWidth", "Stroke", "MarkerEnd", "MarkerStart", "MarkerType", "MarkerColor", "MarkerWidth", "MarkerHeight", "Class", "Style", "InteractionWidth", "ZIndex", "Data" },
            Settable(typeof(BlowEdge)).Except(runtime).ToArray());

        // Back to the defaults: a property Blow leaves out of its file is removed from the shared state too.
        a.Nodes[1] = new BlowNode { Id = "full" };
        a.Edges[0] = new BlowEdge { Id = "edge", Source = "parent", Target = "full" };
        a.Binding.Commit();
        SyncAll(a, b);
        AssertConverged("back to defaults", a, b);
        Assert.IsNull(heldNode.Label);
        Assert.IsTrue(heldEdge.MarkerEnd);
    }

    private static string[] Settable(Type type) =>
        [.. type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(static p => p.SetMethod is { IsPublic: true }).Select(static p => p.Name)];

    [TestMethod]
    public void Collaborative_undo_takes_back_only_this_participants_change()
    {
        using var a = new Replica(1, [Node("n", label: "Draft")]);
        using var b = new Replica(2);
        using BcrdtUndoManager undo = a.Binding.CreateUndoManager(new BcrdtUndoManagerOptions { CaptureTimeout = TimeSpan.Zero });
        SyncAll(a, b);

        a.Node("n").Position = new BlowXYPosition(200, 0);
        a.Binding.Commit();
        b.Node("n").Label = "Final";
        b.Binding.Commit();
        SyncAll(a, b);

        int writes = a.Transactions;
        Assert.IsNotNull(undo.Undo());
        Assert.AreEqual(writes, a.Transactions, "The undo is applied to the lists without being written back as an edit.");
        Assert.AreEqual(new BlowXYPosition(0, 0), a.Node("n").Position, "The move is undone.");
        Assert.AreEqual("Final", a.Node("n").Label, "The collaborator's rename stays.");
        Assert.IsFalse(a.Binding.Commit());
        SyncAll(a, b);
        AssertConverged("after undo", a, b);

        Assert.IsNotNull(undo.Redo());
        SyncAll(a, b);
        AssertConverged("after redo", a, b);
        Assert.AreEqual(new BlowXYPosition(200, 0), b.Node("n").Position);

        // An added node undone is removed everywhere, with its edges; redone, it comes back with them.
        a.Nodes.Add(Node("extra", 50, 50));
        a.Edges.Add(Edge("to-extra", "n", "extra"));
        a.Binding.Commit();
        SyncAll(a, b);
        Assert.IsNotNull(undo.Undo());
        SyncAll(a, b);
        Assert.AreEqual(1, b.Nodes.Count);
        Assert.AreEqual(0, b.Edges.Count);
        Assert.IsNotNull(undo.Redo());
        SyncAll(a, b);
        AssertConverged("after redoing the addition", a, b);
        Assert.AreEqual("to-extra", b.Edges.Single().Id);
    }

    [TestMethod]
    public void An_element_a_participant_wrote_wrong_is_reported_and_left_out_rather_than_breaking_the_graph()
    {
        using var a = new Replica(1, [Node("1"), Node("2")]);
        var reasons = new List<string>();
        a.Binding.RemoteChangeRejected += reasons.Add;

        // Somebody else's code writes an edge Blow cannot read - it names no source - next to a good change.
        using var other = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        Sync(a.Doc, other);
        other.Transact(_ =>
        {
            BcrdtMap flow = other.GetMap("flow");
            flow.Set("e:broken", new BcrdtMap { ["target"] = "2" });
            flow.GetMap("n:1")!.Set("label", "Renamed");
        });
        Sync(other, a.Doc);

        Assert.AreEqual(0, a.Edges.Count);
        Assert.AreEqual(1, reasons.Count, string.Join('\n', reasons));
        Assert.AreEqual("Renamed", a.Node("1").Label, "The rest of the change still applies.");
        Assert.AreEqual(1, a.Transactions, "Nothing is written back over it.");
        Assert.IsFalse(a.Binding.Commit());
    }

    [TestMethod]
    public void Presence_publishes_the_cursor_and_the_selection()
    {
        using var network = new BcrdtSimulatedNetwork(new BcrdtSimulatedNetworkOptions { Seed = 3 });
        BcrdtSimulatedPeer[] peers = network.AddPeers(2);
        using var presenceA = new BcrdtPresenceService(peers[0].Awareness, peers[0].Doc);
        using var presenceB = new BcrdtPresenceService(peers[1].Awareness, peers[1].Doc);
        List<BlowNode> nodesA = [Node("1"), Node("2")];
        List<BlowEdge> edgesA = [Edge("e", "1", "2")];
        List<BlowNode> nodesB = [];
        List<BlowEdge> edgesB = [];
        using var a = new BcrdtBlowBinding(peers[0].Doc.GetMap("flow"), nodesA, edgesA, new BcrdtBlowBindingOptions { Presence = presenceA });
        using var b = new BcrdtBlowBinding(peers[1].Doc.GetMap("flow"), nodesB, edgesB, new BcrdtBlowBindingOptions { Presence = presenceB });
        network.Flush();

        presenceA.SetUser(new BcrdtCollaboratorInfo("ana", "Ana", "#e5484d"));
        nodesA[1].Selected = true;
        edgesA[0].Selected = true;
        a.PublishSelection();
        a.PublishCursor(new BlowXYPosition(10, 20));
        network.Flush();

        BcrdtBlowParticipant ana = b.Participants.Single();
        Assert.AreEqual("Ana", ana.Collaborator.User.Name);
        Assert.AreEqual(new BlowXYPosition(10, 20), ana.Cursor);
        CollectionAssert.AreEqual(new[] { "2" }, ana.SelectedNodes.ToArray());
        CollectionAssert.AreEqual(new[] { "e" }, ana.SelectedEdges.ToArray());
        Assert.IsFalse(nodesB.Any(static node => node.Selected), "Selection reaches others as presence, not as state.");

        using var withoutPresence = new BcrdtBlowBinding(peers[0].Doc.GetMap("other"), [], []);
        Assert.ThrowsExactly<InvalidOperationException>(() => withoutPresence.PublishSelection());
    }

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public void Random_concurrent_graph_edits_converge_despite_loss_duplication_and_reordering(int seed)
    {
        using var network = new BcrdtSimulatedNetwork(new BcrdtSimulatedNetworkOptions
        {
            Seed = seed,
            DropProbability = 0.2,
            DuplicateProbability = 0.2,
            MaxLatencySteps = 4,
        });

        BcrdtSimulatedPeer[] peers = network.AddPeers(3);
        var graphs = peers.Select(static _ => (Nodes: new List<BlowNode>(), Edges: new List<BlowEdge>())).ToArray();
        var bindings = peers.Select((peer, index) => new BcrdtBlowBinding(peer.Doc.GetMap("flow"), graphs[index].Nodes, graphs[index].Edges)).ToArray();
        var random = new Random(seed);
        var history = new List<string>();
        int next = 0;

        try
        {
            for (int round = 0; round < 30; round++)
            {
                for (int index = 0; index < peers.Length; index++)
                {
                    history.Add($"{peers[index].Name}: {RandomEdit(graphs[index].Nodes, graphs[index].Edges, random, $"{index}-{next++}")}");
                    bindings[index].Commit();
                }
                if (round == 12) peers[1].GoOffline();
                if (round == 20) peers[1].GoOnline();
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
            string expected = Describe(graphs[0].Nodes, graphs[0].Edges);
            for (int index = 1; index < graphs.Length; index++) Assert.AreEqual(expected, Describe(graphs[index].Nodes, graphs[index].Edges), context);
            Assert.IsTrue(bindings.All(static binding => !binding.Commit()), $"Nothing is left to write. {context}");

            // What every replica shows is exactly what the shared state projects to.
            using var fresh = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 99 });
            fresh.ApplyUpdate(peers[0].Doc.EncodeStateAsUpdate());
            List<BlowNode> nodes = [];
            List<BlowEdge> edges = [];
            using var binding = new BcrdtBlowBinding(fresh.GetMap("flow"), nodes, edges);
            Assert.AreEqual(expected, Describe(nodes, edges), context);
            Assert.IsTrue(edges.All(edge => nodes.Any(node => node.Id == edge.Source) && nodes.Any(node => node.Id == edge.Target)), context);
        }
        finally
        {
            foreach (BcrdtBlowBinding binding in bindings) binding.Dispose();
        }
    }

    private static string RandomEdit(List<BlowNode> nodes, List<BlowEdge> edges, Random random, string id)
    {
        BlowNode? node = nodes.Count > 0 ? nodes[random.Next(nodes.Count)] : null;
        BlowNode? other = nodes.Count > 0 ? nodes[random.Next(nodes.Count)] : null;
        BlowEdge? edge = edges.Count > 0 ? edges[random.Next(edges.Count)] : null;

        switch (random.Next(10))
        {
            case 0:
            case 1:
                nodes.Add(Node("n" + id, random.Next(500), random.Next(500)));
                return "add node";
            case 2 when node is not null && other is not null:
                edges.Add(Edge("e" + id, node.Id, other.Id));
                return $"connect {node.Id} {other.Id}";
            case 3 when node is not null:
                node.Position = new BlowXYPosition(random.Next(500), random.Next(500));
                return $"move {node.Id}";
            case 4 when node is not null:
                node.Label = "L" + random.Next(100);
                node.Data = random.Next(2) == 0 ? null : JsonNode.Parse($$"""{"v":{{random.Next(10)}}}""");
                return $"edit {node.Id}";
            case 5 when node is not null:
                // A deletion takes the node's edges with it, as the canvas does.
                nodes.Remove(node);
                edges.RemoveAll(e => e.Source == node.Id || e.Target == node.Id);
                return $"delete {node.Id}";
            case 6 when edge is not null:
                edges.Remove(edge);
                return $"disconnect {edge.Id}";
            case 7 when edge is not null:
                edge.Label = "E" + random.Next(100);
                edge.Animated = random.Next(2) == 0;
                return $"edit edge {edge.Id}";
            case 8 when node is not null:
                nodes.Remove(node);
                nodes.Insert(random.Next(nodes.Count + 1), node);
                return $"reorder {node.Id}";
            case 9 when node is not null:
                node.Width = random.Next(2) == 0 ? null : random.Next(50, 300);
                return $"resize {node.Id}";
            default:
                return "nothing";
        }
    }
}
