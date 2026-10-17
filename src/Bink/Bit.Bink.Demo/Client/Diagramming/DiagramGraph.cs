using System.Numerics;

namespace Bit.Bink.Demo.Client.Diagramming;

/// <summary>
/// Keeps a diagram's connectors attached to the nodes they join.
/// </summary>
/// <remarks>
/// <para>
/// This is the demo's answer to "can an application build derived geometry on top of the document model without
/// the library knowing anything about diagrams?". Nothing here is part of Bit.Bink: a node is an ordinary
/// <see cref="BinkRectangleShape"/> and a connector is an ordinary <see cref="BinkLineShape"/>. What makes them a
/// diagram is the element metadata, which the file format preserves, and this listener.
/// </para>
/// <para>
/// When a node moves the graph reroutes every connector touching it, applying those edits while it is still
/// handling the change that caused them. The document reports such an edit with a greater
/// <see cref="BinkDocumentChangedEventArgs.Depth"/>, and history places it in the same undo step as its cause, so
/// dragging a node and undoing puts both the node and its connectors back in one press.
/// </para>
/// <para>
/// A node's label is part of the node: its text is stored in the node's metadata. What is drawn is an ordinary
/// <see cref="BinkTextElement"/> derived from it and kept centred on the node, on the layer named by
/// <see cref="LabelLayerId"/>. Deleting a node deletes its label and its connectors in the same undo step.
/// </para>
/// </remarks>
public sealed class DiagramGraph : IDisposable
{
    /// <summary>Metadata key marking an element's part in the diagram.</summary>
    public const string RoleKey = "diagram-role";

    /// <summary>Metadata key on a connector naming the node it starts at.</summary>
    public const string FromKey = "diagram-from";

    /// <summary>Metadata key on a connector naming the node it ends at.</summary>
    public const string ToKey = "diagram-to";

    /// <summary>Role value for a node.</summary>
    public const string NodeRole = "node";

    /// <summary>Role value for a connector.</summary>
    public const string ConnectorRole = "connector";

    /// <summary>Role value for the text drawn on a node.</summary>
    public const string LabelRole = "label";

    /// <summary>Metadata key on a node holding its label text.</summary>
    public const string LabelKey = "diagram-label";

    /// <summary>Metadata key on a label naming the node it belongs to.</summary>
    public const string NodeKey = "diagram-node";

    /// <summary>Font size of node labels, in document units.</summary>
    public const float LabelFontSize = 14f;

    private readonly BinkDocument _document;
    private bool _rerouting;
    private bool _disposed;

    /// <summary>Attaches to a document and starts keeping its connectors routed.</summary>
    public DiagramGraph(BinkDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        _document = document;
        _document.Changed += OnDocumentChanged;
    }

    /// <summary>Number of connectors rerouted since the graph was created. Used by the demo's status line.</summary>
    public int RerouteCount { get; private set; }

    /// <summary>
    /// The layer labels are drawn on, or null to draw them on the active layer. A locked layer above the nodes
    /// keeps a click on the text selecting the node rather than the label.
    /// </summary>
    public Guid? LabelLayerId { get; set; }

    /// <summary>True when an element carries the given diagram role.</summary>
    public static bool HasRole(BinkElement element, string role)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.Metadata is { } metadata
            && metadata.TryGetValue(RoleKey, out string? value)
            && string.Equals(value, role, StringComparison.Ordinal);
    }

    /// <summary>Creates a node: a rounded rectangle carrying the metadata that makes it one.</summary>
    public static BinkRectangleShape CreateNode(BinkRect box, BinkShapeStyle style, string? label = null)
        => (BinkRectangleShape)WithLabel(new BinkRectangleShape(box.Normalize())
        {
            Style = style,
            CornerRadius = 8f,
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [RoleKey] = NodeRole,
            },
        }, label);

    /// <summary>The label text stored on a node, or null when it has none.</summary>
    public static string? LabelOf(BinkElement node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return node.Metadata is { } metadata && metadata.TryGetValue(LabelKey, out string? value) && value.Length > 0 ? value : null;
    }

    /// <summary>Returns the node with a new label. The drawn text follows when the change is applied.</summary>
    public static BinkElement WithLabel(BinkElement node, string? label)
    {
        ArgumentNullException.ThrowIfNull(node);
        label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
        if (node.Metadata is { } existing)
        {
            foreach ((string key, string value) in existing) metadata[key] = value;
        }
        if (label is null) metadata.Remove(LabelKey);
        else metadata[LabelKey] = label;
        return node with
        {
            Metadata = metadata,
            Description = label is null ? "Diagram node" : "Diagram node: " + label,
        };
    }

    /// <summary>The text element drawn on a node, if there is one.</summary>
    public BinkTextElement? LabelElementOf(Guid nodeId)
    {
        string id = nodeId.ToString("N");
        foreach (BinkElement element in _document.AllElements)
        {
            if (element is BinkTextElement text && HasRole(text, LabelRole) && Endpoint(text, NodeKey) == id) return text;
        }
        return null;
    }

    /// <summary>Creates or updates the text drawn on a node so it shows the node's label, centred on it.</summary>
    private static BinkTextElement LayoutLabel(BinkElement node, string text, BinkTextElement? existing)
    {
        BinkRect box = node.Bounds;
        BinkTextStyle style = BinkTextStyle.Default with
        {
            FontSize = LabelFontSize,
            Align = BinkTextAlign.Center,
            Color = node is BinkShapeElement shape ? shape.Style.StrokeColor : BinkColor.Black,
        };
        // Centre-aligned text is positioned by the middle of its top edge.
        var position = new Vector2(box.Center.X, box.Center.Y - LabelFontSize * style.LineHeight / 2f);
        BinkTextElement label = existing ?? new BinkTextElement(position, text);
        return label with
        {
            Position = position,
            Text = text,
            Transform = Matrix3x2.Identity,
            Style = style,
            Size = null,
            Padding = 0f,
            Description = "Label: " + text,
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [RoleKey] = LabelRole,
                [NodeKey] = node.Id.ToString("N"),
            },
        };
    }

    /// <summary>Creates a connector between two nodes, already routed to their edges.</summary>
    public static BinkLineShape CreateConnector(BinkElement from, BinkElement to, BinkShapeStyle style)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        (Vector2 start, Vector2 end) = Route(from.Bounds, to.Bounds);
        return new BinkLineShape(start, end)
        {
            Style = style,
            EndArrow = true,
            Description = "Connector",
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [RoleKey] = ConnectorRole,
                [FromKey] = from.Id.ToString("N"),
                [ToKey] = to.Id.ToString("N"),
            },
        };
    }

    /// <summary>
    /// Picks where a connector meets two boxes: the point on each box's edge along the line joining their
    /// centres. Simple, stable under rotation of the drag, and never leaves the line starting inside a node.
    /// </summary>
    public static (Vector2 Start, Vector2 End) Route(BinkRect from, BinkRect to)
    {
        Vector2 a = from.Center;
        Vector2 b = to.Center;
        Vector2 direction = b - a;
        if (direction.LengthSquared() < 1e-6f) return (a, b);
        return (EdgePoint(from, direction), EdgePoint(to, -direction));
    }

    /// <summary>The point where a ray leaving the rectangle's centre crosses its edge.</summary>
    private static Vector2 EdgePoint(BinkRect box, Vector2 direction)
    {
        Vector2 centre = box.Center;
        float halfWidth = MathF.Max(box.Width / 2f, 0.001f);
        float halfHeight = MathF.Max(box.Height / 2f, 0.001f);
        float dx = MathF.Abs(direction.X) < 1e-6f ? 1e-6f : direction.X;
        float dy = MathF.Abs(direction.Y) < 1e-6f ? 1e-6f : direction.Y;
        // Scale the ray until it touches whichever pair of edges it reaches first.
        float scale = MathF.Min(halfWidth / MathF.Abs(dx), halfHeight / MathF.Abs(dy));
        return centre + new Vector2(dx, dy) * scale;
    }

    /// <summary>The connectors currently attached to a node.</summary>
    public IEnumerable<BinkLineShape> ConnectorsOf(Guid nodeId)
    {
        string id = nodeId.ToString("N");
        foreach (BinkElement element in _document.AllElements)
        {
            if (element is not BinkLineShape line || !HasRole(line, ConnectorRole)) continue;
            if (Endpoint(line, FromKey) == id || Endpoint(line, ToKey) == id) yield return line;
        }
    }

    private static string? Endpoint(BinkElement connector, string key)
        => connector.Metadata is { } metadata && metadata.TryGetValue(key, out string? value) ? value : null;

    private void OnDocumentChanged(object? sender, BinkDocumentChangedEventArgs e)
    {
        // Remote and loaded changes are already consistent on the sender's side, and an edit of our own must
        // not trigger another one.
        if (_disposed || _rerouting) return;
        if (e.Origin is BinkChangeOrigin.Remote or BinkChangeOrigin.Load) return;

        List<Guid> moved = [];
        List<Guid> removed = [];
        Collect(e.Operation, moved, removed);
        // Undo and redo restore a state that was already consistent, including whatever left with a node.
        if (e.Origin is BinkChangeOrigin.Undo or BinkChangeOrigin.Redo) removed.Clear();
        if (moved.Count == 0 && removed.Count == 0) return;

        var updates = new List<BinkElement>();
        var additions = new List<BinkElement>();
        var deletions = new HashSet<Guid>();
        var seen = new HashSet<Guid>();
        foreach (Guid nodeId in moved)
        {
            foreach (BinkLineShape connector in ConnectorsOf(nodeId))
            {
                if (!seen.Add(connector.Id)) continue;
                if (Reroute(connector) is { } routed) updates.Add(routed);
            }
            if (_document.TryGetElement(nodeId, out BinkElement? node)) SyncLabel(node, updates, additions, deletions);
        }
        foreach (Guid nodeId in removed)
        {
            // Whatever was attached to a deleted node goes with it: a connector to nothing is not a diagram.
            foreach (BinkLineShape connector in ConnectorsOf(nodeId)) deletions.Add(connector.Id);
            if (LabelElementOf(nodeId) is { } label) deletions.Add(label.Id);
        }
        if (updates.Count == 0 && additions.Count == 0 && deletions.Count == 0) return;

        _rerouting = true;
        try
        {
            // Applied inside the notification on purpose: the document reports it as a consequence of the change
            // being handled, and history folds it into the same undo step.
            using (_document.BeginBatch("Update diagram"))
            {
                if (updates.Count > 0) _document.ReplaceElements(updates, "Route connectors");
                foreach (BinkElement label in additions) _document.AddElement(label, LabelLayerId);
                if (deletions.Count > 0) _document.RemoveElements(deletions);
            }
            RerouteCount += updates.Count(u => u is BinkLineShape);
        }
        catch (InvalidOperationException)
        {
            // A connector whose node was deleted in the same batch. The connector is left where it was rather
            // than failing the edit that caused this.
        }
        finally
        {
            _rerouting = false;
        }
    }

    /// <summary>Brings a node's drawn label in line with the label stored on the node.</summary>
    private void SyncLabel(BinkElement node, List<BinkElement> updates, List<BinkElement> additions, HashSet<Guid> deletions)
    {
        if (!HasRole(node, NodeRole)) return;
        string? text = LabelOf(node);
        BinkTextElement? existing = LabelElementOf(node.Id);
        if (text is null)
        {
            if (existing is not null) deletions.Add(existing.Id);
            return;
        }
        BinkTextElement laidOut = LayoutLabel(node, text, existing);
        if (existing is null) additions.Add(laidOut);
        else if (existing.Position != laidOut.Position || existing.Text != laidOut.Text
                 || existing.Style != laidOut.Style || !existing.Transform.IsIdentity)
        {
            updates.Add(laidOut);
        }
    }

    /// <summary>Returns the connector with its endpoints moved back onto its nodes, or null when nothing changed.</summary>
    private BinkLineShape? Reroute(BinkLineShape connector)
    {
        string? fromId = Endpoint(connector, FromKey);
        string? toId = Endpoint(connector, ToKey);
        if (fromId is null || toId is null) return null;
        if (!Guid.TryParseExact(fromId, "N", out Guid from) || !Guid.TryParseExact(toId, "N", out Guid to)) return null;
        if (!_document.TryGetElement(from, out BinkElement? fromNode) || !_document.TryGetElement(to, out BinkElement? toNode)) return null;

        (Vector2 start, Vector2 end) = Route(fromNode.Bounds, toNode.Bounds);
        if (Close(connector.Start, start) && Close(connector.End, end)) return null;
        // The connector's own transform is folded away: its geometry is the route, so a stale transform from an
        // earlier drag would move it off the nodes again.
        return connector with { Start = start, End = end, Transform = Matrix3x2.Identity };
    }

    private static bool Close(Vector2 a, Vector2 b) => Vector2.DistanceSquared(a, b) < 0.01f;

    /// <summary>Finds the nodes an operation moved, replaced or added, and the elements it removed.</summary>
    private static void Collect(BinkDrawingOperation operation, List<Guid> moved, List<Guid> removed)
    {
        switch (operation)
        {
            case BinkReplaceElementOperation replace when HasRole(replace.Element, NodeRole):
                moved.Add(replace.Element.Id);
                break;
            case BinkAddElementOperation add when HasRole(add.Element, NodeRole):
                moved.Add(add.Element.Id);
                break;
            case BinkRemoveElementOperation remove:
                // The element is already gone, so its role cannot be checked; anything attached to it is enough.
                removed.Add(remove.ElementId);
                break;
            case BinkBatchOperation batch:
                foreach (BinkDrawingOperation inner in batch.Operations) Collect(inner, moved, removed);
                break;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _document.Changed -= OnDocumentChanged;
    }
}
