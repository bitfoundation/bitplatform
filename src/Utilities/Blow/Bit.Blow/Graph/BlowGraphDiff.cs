using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bit.Blow;

/// <summary>A node present in both graphs with something about it changed.</summary>
/// <param name="Before">The node as it was.</param>
/// <param name="After">The node as it is.</param>
/// <param name="Properties">The names of the <see cref="BlowNode"/> properties that differ: "Position", "Label", "Data", ...</param>
public sealed record BlowNodeChange(BlowNode Before, BlowNode After, IReadOnlyList<string> Properties);

/// <summary>An edge present in both graphs with something about it changed.</summary>
/// <param name="Before">The edge as it was.</param>
/// <param name="After">The edge as it is.</param>
/// <param name="Properties">The names of the <see cref="BlowEdge"/> properties that differ: "Target", "Label", "MarkerEnd", ...</param>
public sealed record BlowEdgeChange(BlowEdge Before, BlowEdge After, IReadOnlyList<string> Properties);

/// <summary>How <see cref="BlowGraphDiff"/> tells a change from none.</summary>
public sealed class BlowGraphDiffOptions
{
    /// <summary>How far a node may move, in flow units on each axis, and still count as where it was. 0 by default.</summary>
    public double PositionTolerance { get; set; }

    /// <summary>When true, selection counts as a change. It does not by default, as clicking around is not editing.</summary>
    public bool IncludeSelection { get; set; }

    /// <summary>
    /// Whether two data payloads are the same. By default: the same object, equal by <see cref="object.Equals(object?)"/>,
    /// or JSON with the same content (<see cref="JsonNode"/>, <see cref="JsonElement"/>) - which is what data read from a
    /// document is.
    /// </summary>
    public Func<object?, object?, bool>? DataEquals { get; set; }

    /// <summary>Property names to leave out of the comparison: "Position" to compare structure only, say.</summary>
    public ISet<string>? Ignore { get; set; }
}

/// <summary>
/// What changed between two versions of a graph: the nodes and edges added, removed and changed, matched by id - for
/// a "what changed" view, a review of an import, a merge, or a patch sent to a collaborator. A diff can be applied to
/// the first graph to get the second (<see cref="ApplyTo"/>) and turned round (<see cref="Invert"/>).
/// </summary>
public sealed class BlowGraphDiff
{
    private BlowGraphDiff(IReadOnlyList<BlowNode> addedNodes, IReadOnlyList<BlowNode> removedNodes, IReadOnlyList<BlowNodeChange> changedNodes,
        IReadOnlyList<BlowEdge> addedEdges, IReadOnlyList<BlowEdge> removedEdges, IReadOnlyList<BlowEdgeChange> changedEdges)
    {
        AddedNodes = addedNodes;
        RemovedNodes = removedNodes;
        ChangedNodes = changedNodes;
        AddedEdges = addedEdges;
        RemovedEdges = removedEdges;
        ChangedEdges = changedEdges;
    }

    /// <summary>Nodes only in the second graph, in its order.</summary>
    public IReadOnlyList<BlowNode> AddedNodes { get; }

    /// <summary>Nodes only in the first graph, in its order.</summary>
    public IReadOnlyList<BlowNode> RemovedNodes { get; }

    /// <summary>Nodes in both, changed, in the second graph's order.</summary>
    public IReadOnlyList<BlowNodeChange> ChangedNodes { get; }

    /// <summary>Edges only in the second graph.</summary>
    public IReadOnlyList<BlowEdge> AddedEdges { get; }

    /// <summary>Edges only in the first graph.</summary>
    public IReadOnlyList<BlowEdge> RemovedEdges { get; }

    /// <summary>Edges in both, changed.</summary>
    public IReadOnlyList<BlowEdgeChange> ChangedEdges { get; }

    /// <summary>True when the graphs are the same.</summary>
    public bool IsEmpty => AddedNodes.Count == 0 && RemovedNodes.Count == 0 && ChangedNodes.Count == 0
                           && AddedEdges.Count == 0 && RemovedEdges.Count == 0 && ChangedEdges.Count == 0;

    /// <summary>How many elements differ.</summary>
    public int Count => AddedNodes.Count + RemovedNodes.Count + ChangedNodes.Count + AddedEdges.Count + RemovedEdges.Count + ChangedEdges.Count;

    /// <summary>
    /// Compares two versions of a graph. Elements are matched by id; when a list repeats an id, its first element with
    /// it is the one compared, as everywhere else in the library.
    /// </summary>
    public static BlowGraphDiff Compare(IEnumerable<BlowNode> beforeNodes, IEnumerable<BlowEdge> beforeEdges,
        IEnumerable<BlowNode> afterNodes, IEnumerable<BlowEdge> afterEdges, BlowGraphDiffOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(beforeNodes);
        ArgumentNullException.ThrowIfNull(beforeEdges);
        ArgumentNullException.ThrowIfNull(afterNodes);
        ArgumentNullException.ThrowIfNull(afterEdges);
        options ??= new BlowGraphDiffOptions();

        var (addedNodes, removedNodes, changedNodes) = Diff(beforeNodes, afterNodes, n => n.Id,
            (a, b) => NodeChanges(a, b, options), (a, b, p) => new BlowNodeChange(a, b, p));
        var (addedEdges, removedEdges, changedEdges) = Diff(beforeEdges, afterEdges, e => e.Id,
            (a, b) => EdgeChanges(a, b, options), (a, b, p) => new BlowEdgeChange(a, b, p));
        return new BlowGraphDiff(addedNodes, removedNodes, changedNodes, addedEdges, removedEdges, changedEdges);
    }

    /// <summary>Compares two documents' graphs.</summary>
    public static BlowGraphDiff Compare(BlowDocument before, BlowDocument after, BlowGraphDiffOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        return Compare(before.Nodes, before.Edges, after.Nodes, after.Edges, options);
    }

    /// <summary>The diff that undoes this one: what was added is removed, and each change goes back.</summary>
    public BlowGraphDiff Invert() => new(RemovedNodes, AddedNodes, [.. ChangedNodes.Select(c => c with { Before = c.After, After = c.Before })],
        RemovedEdges, AddedEdges, [.. ChangedEdges.Select(c => c with { Before = c.After, After = c.Before })]);

    /// <summary>
    /// Makes the lists the second graph, from the first: removed elements go, changed ones are replaced by copies of
    /// what they became, added ones are appended as copies. Elements not in the diff stay as they are - which is what
    /// makes a diff a patch: two diffs touching different elements of one graph both apply. Edges of a removed node
    /// that the diff does not name are removed with it.
    /// </summary>
    public void ApplyTo(List<BlowNode> nodes, List<BlowEdge> edges)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);

        var removedNodes = RemovedNodes.Select(n => n.Id).ToHashSet(StringComparer.Ordinal);
        var removedEdges = RemovedEdges.Select(e => e.Id).ToHashSet(StringComparer.Ordinal);
        var changedEdges = ChangedEdges.ToDictionary(c => c.After.Id, c => c.After, StringComparer.Ordinal);
        nodes.RemoveAll(n => removedNodes.Contains(n.Id));
        // A changed edge is judged by the ends it is getting, not the ones it had: one moved off a removed node stays.
        edges.RemoveAll(e =>
        {
            if (removedEdges.Contains(e.Id)) return true;
            var end = changedEdges.GetValueOrDefault(e.Id) ?? e;
            return removedNodes.Contains(end.Source) || removedNodes.Contains(end.Target);
        });

        Replace(nodes, ChangedNodes.ToDictionary(c => c.After.Id, c => c.After, StringComparer.Ordinal), n => n.Id, n => n.Clone());
        Replace(edges, changedEdges, e => e.Id, e => e.Clone());

        var nodeIds = nodes.Select(n => n.Id).ToHashSet(StringComparer.Ordinal);
        nodes.AddRange(AddedNodes.Where(n => nodeIds.Add(n.Id)).Select(n => n.Clone()));
        var edgeIds = edges.Select(e => e.Id).ToHashSet(StringComparer.Ordinal);
        edges.AddRange(AddedEdges.Where(e => edgeIds.Add(e.Id)).Select(e => e.Clone()));
    }

    private static void Replace<T>(List<T> list, Dictionary<string, T> replacements, Func<T, string> id, Func<T, T> copy)
    {
        if (replacements.Count == 0) return;
        for (var i = 0; i < list.Count; i++)
        {
            if (replacements.TryGetValue(id(list[i]), out var after)) list[i] = copy(after);
        }
    }

    private static (List<T> Added, List<T> Removed, List<TChange> Changed) Diff<T, TChange>(IEnumerable<T> before, IEnumerable<T> after,
        Func<T, string> id, Func<T, T, List<string>> changes, Func<T, T, IReadOnlyList<string>, TChange> change)
    {
        var old = new Dictionary<string, T>(StringComparer.Ordinal);
        var oldOrder = new List<T>();
        foreach (var element in before)
        {
            if (old.TryAdd(id(element), element)) oldOrder.Add(element);
        }

        var added = new List<T>();
        var changed = new List<TChange>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var element in after)
        {
            if (!seen.Add(id(element))) continue;
            if (!old.TryGetValue(id(element), out var was)) added.Add(element);
            else if (changes(was, element) is { Count: > 0 } properties) changed.Add(change(was, element, properties));
        }
        var removed = oldOrder.Where(e => !seen.Contains(id(e))).ToList();
        return (added, removed, changed);
    }

    private static List<string> NodeChanges(BlowNode a, BlowNode b, BlowGraphDiffOptions o)
    {
        var changes = new List<string>();
        void Check(string name, bool same)
        {
            if (!same && o.Ignore?.Contains(name) != true) changes.Add(name);
        }

        var t = o.PositionTolerance;
        Check(nameof(BlowNode.Position), Math.Abs(a.Position.X - b.Position.X) <= t && Math.Abs(a.Position.Y - b.Position.Y) <= t);
        Check(nameof(BlowNode.Type), a.Type == b.Type);
        Check(nameof(BlowNode.Label), a.Label == b.Label);
        Check(nameof(BlowNode.Data), DataEquals(a.Data, b.Data, o));
        Check(nameof(BlowNode.Width), a.Width == b.Width);
        Check(nameof(BlowNode.Height), a.Height == b.Height);
        Check(nameof(BlowNode.ParentId), a.ParentId == b.ParentId);
        Check(nameof(BlowNode.Ports), SamePorts(a.Ports, b.Ports));
        Check(nameof(BlowNode.Hidden), a.Hidden == b.Hidden);
        Check(nameof(BlowNode.Collapsed), a.Collapsed == b.Collapsed);
        Check(nameof(BlowNode.Draggable), a.Draggable == b.Draggable);
        Check(nameof(BlowNode.Selectable), a.Selectable == b.Selectable);
        Check(nameof(BlowNode.Connectable), a.Connectable == b.Connectable);
        Check(nameof(BlowNode.Deletable), a.Deletable == b.Deletable);
        Check(nameof(BlowNode.Focusable), a.Focusable == b.Focusable);
        Check(nameof(BlowNode.SourcePosition), a.SourcePosition == b.SourcePosition);
        Check(nameof(BlowNode.TargetPosition), a.TargetPosition == b.TargetPosition);
        Check(nameof(BlowNode.UseCustomDragHandle), a.UseCustomDragHandle == b.UseCustomDragHandle);
        Check(nameof(BlowNode.AriaLabel), a.AriaLabel == b.AriaLabel);
        Check(nameof(BlowNode.ExtentParent), a.ExtentParent == b.ExtentParent);
        Check(nameof(BlowNode.Extent), a.Extent == b.Extent);
        Check(nameof(BlowNode.ExpandParent), a.ExpandParent == b.ExpandParent);
        Check(nameof(BlowNode.Class), a.Class == b.Class);
        Check(nameof(BlowNode.Style), a.Style == b.Style);
        Check(nameof(BlowNode.ZIndex), a.ZIndex == b.ZIndex);
        Check(nameof(BlowNode.Origin), a.Origin == b.Origin);
        if (o.IncludeSelection) Check(nameof(BlowNode.Selected), a.Selected == b.Selected);
        return changes;
    }

    private static List<string> EdgeChanges(BlowEdge a, BlowEdge b, BlowGraphDiffOptions o)
    {
        var changes = new List<string>();
        void Check(string name, bool same)
        {
            if (!same && o.Ignore?.Contains(name) != true) changes.Add(name);
        }

        Check(nameof(BlowEdge.Source), a.Source == b.Source);
        Check(nameof(BlowEdge.Target), a.Target == b.Target);
        Check(nameof(BlowEdge.SourceHandle), a.SourceHandle == b.SourceHandle);
        Check(nameof(BlowEdge.TargetHandle), a.TargetHandle == b.TargetHandle);
        Check(nameof(BlowEdge.Type), a.Type == b.Type);
        Check(nameof(BlowEdge.PathOptions), BlowEdgePathOptions.SameValues(a.PathOptions, b.PathOptions));
        Check(nameof(BlowEdge.Label), a.Label == b.Label);
        Check(nameof(BlowEdge.Data), DataEquals(a.Data, b.Data, o));
        Check(nameof(BlowEdge.Animated), a.Animated == b.Animated);
        Check(nameof(BlowEdge.Hidden), a.Hidden == b.Hidden);
        Check(nameof(BlowEdge.Selectable), a.Selectable == b.Selectable);
        Check(nameof(BlowEdge.Focusable), a.Focusable == b.Focusable);
        Check(nameof(BlowEdge.Deletable), a.Deletable == b.Deletable);
        Check(nameof(BlowEdge.Reconnectable), a.Reconnectable == b.Reconnectable);
        Check(nameof(BlowEdge.Stroke), a.Stroke == b.Stroke);
        Check(nameof(BlowEdge.StrokeWidth), a.StrokeWidth == b.StrokeWidth);
        Check(nameof(BlowEdge.MarkerEnd), a.MarkerEnd == b.MarkerEnd);
        Check(nameof(BlowEdge.MarkerStart), a.MarkerStart == b.MarkerStart);
        Check(nameof(BlowEdge.MarkerType), a.MarkerType == b.MarkerType);
        Check(nameof(BlowEdge.MarkerColor), a.MarkerColor == b.MarkerColor);
        Check(nameof(BlowEdge.MarkerWidth), a.MarkerWidth == b.MarkerWidth);
        Check(nameof(BlowEdge.MarkerHeight), a.MarkerHeight == b.MarkerHeight);
        Check(nameof(BlowEdge.LabelStyle), a.LabelStyle == b.LabelStyle);
        Check(nameof(BlowEdge.LabelShowBg), a.LabelShowBg == b.LabelShowBg);
        Check(nameof(BlowEdge.LabelBgStyle), a.LabelBgStyle == b.LabelBgStyle);
        Check(nameof(BlowEdge.LabelBgPaddingX), a.LabelBgPaddingX == b.LabelBgPaddingX);
        Check(nameof(BlowEdge.LabelBgPaddingY), a.LabelBgPaddingY == b.LabelBgPaddingY);
        Check(nameof(BlowEdge.LabelBgBorderRadius), a.LabelBgBorderRadius == b.LabelBgBorderRadius);
        Check(nameof(BlowEdge.Class), a.Class == b.Class);
        Check(nameof(BlowEdge.Style), a.Style == b.Style);
        Check(nameof(BlowEdge.InteractionWidth), a.InteractionWidth == b.InteractionWidth);
        Check(nameof(BlowEdge.ZIndex), a.ZIndex == b.ZIndex);
        if (o.IncludeSelection) Check(nameof(BlowEdge.Selected), a.Selected == b.Selected);
        return changes;
    }

    private static bool SamePorts(List<BlowPort>? x, List<BlowPort>? y)
    {
        if ((x?.Count ?? 0) != (y?.Count ?? 0)) return false;
        if (x is null || y is null) return true;
        for (var i = 0; i < x.Count; i++)
        {
            var (a, b) = (x[i], y[i]);
            if (a.Id != b.Id || a.Type != b.Type || a.Position != b.Position || a.Label != b.Label || a.DataType != b.DataType
                || a.MaxConnections != b.MaxConnections || a.MinConnections != b.MinConnections || !JsonEquals(a.Data, b.Data))
            {
                return false;
            }
        }
        return true;
    }

    private static bool DataEquals(object? a, object? b, BlowGraphDiffOptions o)
        => o.DataEquals is { } custom ? custom(a, b) : JsonEquals(a, b);

    private static bool JsonEquals(object? a, object? b) => (a, b) switch
    {
        _ when ReferenceEquals(a, b) => true,
        (JsonNode x, JsonNode y) => JsonNode.DeepEquals(x, y),
        (JsonElement x, JsonElement y) => JsonElement.DeepEquals(x, y),
        // Data read from a document is a JsonElement on one side and a JsonNode on the other as often as not.
        (JsonElement x, JsonNode y) => JsonNode.DeepEquals(JsonNode.Parse(x.GetRawText()), y),
        (JsonNode x, JsonElement y) => JsonNode.DeepEquals(x, JsonNode.Parse(y.GetRawText())),
        _ => Equals(a, b),
    };
}
