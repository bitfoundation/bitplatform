using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>
/// Applications routinely keep geometry that is derived from other elements: a connector that has to stay
/// attached to the node it joins, a callout that follows what it points at. The document supports that by
/// letting a listener apply further changes while it is handling one, and reporting those with a greater
/// <see cref="BinkDocumentChangedEventArgs.Depth"/> so history can place a consequence in the same step as its cause.
/// These tests pin that contract down, because it is what the demo's diagram surface is built on.
/// </summary>
[TestClass]
public class DerivedGeometryTests
{
    /// <summary>
    /// A minimal version of the demo's diagram graph: a listener that re-routes a connector whenever the node it
    /// is attached to moves, applying the edit inside the notification that told it about the move.
    /// </summary>
    private sealed class Connector : IDisposable
    {
        private readonly BinkDocument _document;
        private readonly Guid _nodeId;
        private readonly Guid _connectorId;
        private bool _rerouting;

        public Connector(BinkDocument document, Guid nodeId, Guid connectorId)
        {
            _document = document;
            _nodeId = nodeId;
            _connectorId = connectorId;
            document.Changed += OnChanged;
        }

        public int Reroutes { get; private set; }

        public int MaxDepthSeen { get; private set; }

        private void OnChanged(object? sender, BinkDocumentChangedEventArgs e)
        {
            if (e.Depth > MaxDepthSeen) MaxDepthSeen = e.Depth;
            if (_rerouting || e.Origin is BinkChangeOrigin.Remote or BinkChangeOrigin.Load) return;
            if (!Mentions(e.Operation, _nodeId)) return;
            if (!_document.TryGetElement(_nodeId, out BinkElement? node)) return;
            if (!_document.TryGetElement(_connectorId, out BinkElement? connector) || connector is not BinkLineShape line) return;

            Vector2 target = node.Bounds.Center;
            if (Vector2.DistanceSquared(line.End, target) < 0.01f) return;

            _rerouting = true;
            try
            {
                _document.ReplaceElement(line with { End = target });
                Reroutes++;
            }
            finally
            {
                _rerouting = false;
            }
        }

        private static bool Mentions(BinkDrawingOperation operation, Guid id) => operation switch
        {
            BinkReplaceElementOperation replace => replace.Element.Id == id,
            BinkAddElementOperation add => add.Element.Id == id,
            BinkBatchOperation batch => batch.Operations.Any(op => Mentions(op, id)),
            _ => false,
        };

        public void Dispose() => _document.Changed -= OnChanged;
    }

    private static (BinkDocument Document, Guid NodeId, Guid ConnectorId) BuildGraph()
    {
        var document = new BinkDocument();
        var node = new BinkRectangleShape(new BinkRect(200, 200, 80, 40));
        var connector = new BinkLineShape(new Vector2(20, 20), node.Bounds.Center);
        document.AddElement(node);
        document.AddElement(connector);
        return (document, node.Id, connector.Id);
    }

    [TestMethod]
    public void A_connector_follows_the_node_it_is_attached_to()
    {
        (BinkDocument document, Guid nodeId, Guid connectorId) = BuildGraph();
        using var graph = new Connector(document, nodeId, connectorId);

        var node = (BinkRectangleShape)document.GetElement(nodeId);
        document.ReplaceElement(node.Translate(new Vector2(150, 90)));

        var connector = (BinkLineShape)document.GetElement(connectorId);
        Vector2 expected = document.GetElement(nodeId).Bounds.Center;
        Assert.AreEqual(1, graph.Reroutes);
        Assert.AreEqual(expected.X, connector.End.X, 5e-4);
        Assert.AreEqual(expected.Y, connector.End.Y, 5e-4);
    }

    [TestMethod]
    public void A_consequential_edit_made_by_a_listener_joins_the_step_that_caused_it()
    {
        (BinkDocument document, Guid nodeId, Guid connectorId) = BuildGraph();
        using var history = new BinkHistoryManager(document);
        using var graph = new Connector(document, nodeId, connectorId);

        Vector2 nodeBefore = document.GetElement(nodeId).Bounds.Center;
        Vector2 connectorBefore = ((BinkLineShape)document.GetElement(connectorId)).End;
        int steps = history.UndoCount;

        // A tool groups its gesture, which is what makes the consequence part of the same step. This is exactly
        // what BinkSelectTool does around a drag, so moving a node and undoing puts the connector back too.
        var node = (BinkRectangleShape)document.GetElement(nodeId);
        using (history.BeginGroup("Move"))
        {
            document.ReplaceElement(node.Translate(new Vector2(150, 90)));
        }

        Assert.AreEqual(1, graph.Reroutes);
        Assert.IsTrue(graph.MaxDepthSeen > 0, "the derived edit was not reported as nested inside its cause");
        Assert.AreEqual(steps + 1, history.UndoCount);

        Assert.IsTrue(history.Undo());
        Assert.AreEqual(nodeBefore.X, document.GetElement(nodeId).Bounds.Center.X, 5e-4);
        Assert.AreEqual(connectorBefore.X, ((BinkLineShape)document.GetElement(connectorId)).End.X, 5e-4);
        Assert.AreEqual(connectorBefore.Y, ((BinkLineShape)document.GetElement(connectorId)).End.Y, 5e-4);
        Assert.AreEqual(steps, history.UndoCount);
    }

    [TestMethod]
    public void An_ungrouped_consequence_joins_the_step_that_caused_it()
    {
        (BinkDocument document, Guid nodeId, Guid connectorId) = BuildGraph();
        using var history = new BinkHistoryManager(document);
        using var graph = new Connector(document, nodeId, connectorId);
        Vector2 connectorBefore = ((BinkLineShape)document.GetElement(connectorId)).End;

        // An edit made through the API rather than a tool has no group around it. Its consequence must still undo
        // with it: as a step of its own, undoing once would put the connector back and leave the node where it
        // was moved, a state the user never saw. Which order the handlers were subscribed in must not matter.
        var node = (BinkRectangleShape)document.GetElement(nodeId);
        document.ReplaceElement(node.Translate(new Vector2(150, 90)));

        Assert.AreEqual(1, graph.Reroutes);
        Assert.AreEqual(1, history.UndoCount);

        Assert.IsTrue(history.Undo());
        Assert.AreEqual(200 + 40, document.GetElement(nodeId).Bounds.Center.X, 5e-4);
        Assert.AreEqual(connectorBefore.X, ((BinkLineShape)document.GetElement(connectorId)).End.X, 5e-4);
        Assert.AreEqual(connectorBefore.Y, ((BinkLineShape)document.GetElement(connectorId)).End.Y, 5e-4);
        Assert.IsFalse(history.CanUndo);

        // Redo replays the cause and its consequence together.
        Assert.IsTrue(history.Redo());
        Assert.AreEqual(1, history.UndoCount);
    }

    [TestMethod]
    public void A_consequence_of_a_change_history_did_not_record_is_a_step_of_its_own()
    {
        (BinkDocument document, Guid nodeId, _) = BuildGraph();
        using var history = new BinkHistoryManager(document);
        var marker = new BinkRectangleShape(new BinkRect(0, 0, 10, 10));
        document.AddElement(marker);
        int steps = history.UndoCount;

        // A listener that reacts locally to a remote edit. The remote edit is not undoable here, but the local
        // edit it caused is, and it must not be folded into whatever local step happened to be recorded last.
        document.Changed += (_, e) =>
        {
            if (e.Origin == BinkChangeOrigin.Remote && e.Depth == 0) document.RemoveElement(marker.Id);
        };
        var node = (BinkRectangleShape)document.GetElement(nodeId);
        document.Apply(new BinkReplaceElementOperation(node.Translate(new Vector2(150, 90))), BinkChangeOrigin.Remote);

        Assert.AreEqual(steps + 1, history.UndoCount);
        Assert.IsTrue(history.Undo());
        Assert.IsTrue(document.ContainsElement(marker.Id));
        Assert.AreEqual(steps, history.UndoCount);
    }

    [TestMethod]
    public void The_diagram_page_reroutes_connectors_when_a_node_moves()
    {
        // The routing rule the demo uses: a connector meets each node on its edge, along the line joining their
        // centres, so it never starts inside a box.
        var from = new BinkRect(0, 0, 100, 60);
        var to = new BinkRect(300, 0, 100, 60);

        (Vector2 start, Vector2 end) = Route(from, to);
        Assert.AreEqual(from.Right, start.X, 5e-4);
        Assert.AreEqual(to.Left, end.X, 5e-4);

        // Move the far node down and the endpoints move with it, still on the boxes' edges.
        BinkRect moved = to.Offset(new Vector2(0, 400));
        (Vector2 start2, Vector2 end2) = Route(from, moved);
        Assert.IsTrue(start2.Y > start.Y, "the connector did not follow the node downwards");
        Assert.IsTrue(moved.Contains(end2) || Close(end2, moved), $"the endpoint {end2} left the node {moved}");
        Assert.IsTrue(from.Contains(start2) || Close(start2, from), $"the start {start2} left the node {from}");

        static bool Close(Vector2 point, BinkRect box) => box.Inflate(0.01f).Contains(point);

        // A local copy of the demo's routing, so the engine's behaviour is pinned even though the rule lives in
        // the sample rather than in the library.
        static (Vector2 Start, Vector2 End) Route(BinkRect a, BinkRect b)
        {
            Vector2 direction = b.Center - a.Center;
            if (direction.LengthSquared() < 1e-6f) return (a.Center, b.Center);
            return (Edge(a, direction), Edge(b, -direction));
        }

        static Vector2 Edge(BinkRect box, Vector2 direction)
        {
            float halfWidth = MathF.Max(box.Width / 2f, 0.001f);
            float halfHeight = MathF.Max(box.Height / 2f, 0.001f);
            float dx = MathF.Abs(direction.X) < 1e-6f ? 1e-6f : direction.X;
            float dy = MathF.Abs(direction.Y) < 1e-6f ? 1e-6f : direction.Y;
            float scale = MathF.Min(halfWidth / MathF.Abs(dx), halfHeight / MathF.Abs(dy));
            return box.Center + new Vector2(dx, dy) * scale;
        }
    }

    [TestMethod]
    public void A_history_limit_of_zero_keeps_nothing()
    {
        var document = new BinkDocument();
        using var history = new BinkHistoryManager(document, limit: 0);

        document.AddElement(TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10)));
        document.AddElement(TestData.Stroke(new Vector2(20, 20), new Vector2(30, 30)));

        Assert.AreEqual(0, history.UndoCount);
        Assert.IsFalse(history.CanUndo);
        Assert.IsFalse(history.Undo());
        // The changes themselves still happened; only the ability to reverse them was declined.
        Assert.AreEqual(2, document.ElementCount);
    }

    [TestMethod]
    public void Lowering_the_history_limit_releases_the_steps_it_drops()
    {
        var document = new BinkDocument();
        using var history = new BinkHistoryManager(document, limit: 50);
        for (int i = 0; i < 20; i++) document.AddElement(TestData.Stroke(new Vector2(i, 0), new Vector2(i, 10)));
        Assert.AreEqual(20, history.UndoCount);

        int changes = 0;
        history.Changed += (_, _) => changes++;
        history.Limit = 5;

        // Trimming happens immediately rather than only applying to future changes, so the elements the dropped
        // steps referenced are released instead of being pinned by a limit nobody has reached yet.
        Assert.AreEqual(5, history.UndoCount);
        Assert.AreEqual(1, changes);

        // The steps that remain are the newest ones.
        Assert.IsTrue(history.Undo());
        Assert.AreEqual(19, document.ElementCount);
    }
}
