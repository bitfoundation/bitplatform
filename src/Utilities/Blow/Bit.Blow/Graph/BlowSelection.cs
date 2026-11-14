namespace Bit.Blow;

/// <summary>
/// The sets of elements an editor's "select" commands pick: everything upstream or downstream of the selection, the
/// edges among or around it, a group's members, the inverse. Pure functions over a <see cref="BlowGraphIndex"/>; hand
/// what they return to <c>BlowCanvas.SelectAsync</c>.
/// </summary>
public static class BlowSelection
{
    /// <summary>The nodes with a path to any of the given ones, and those nodes too unless <paramref name="includeStart"/> is false.</summary>
    public static IReadOnlyList<BlowNode> Upstream(BlowGraphIndex graph, IEnumerable<string> nodeIds, bool includeStart = true)
        => Walk(graph, nodeIds, includeStart, BlowTraversalDirection.Incoming);

    /// <summary>The nodes reachable from any of the given ones, and those nodes too unless <paramref name="includeStart"/> is false.</summary>
    public static IReadOnlyList<BlowNode> Downstream(BlowGraphIndex graph, IEnumerable<string> nodeIds, bool includeStart = true)
        => Walk(graph, nodeIds, includeStart, BlowTraversalDirection.Outgoing);

    /// <summary>The nodes joined to the given ones by any path, either way: their connected component.</summary>
    public static IReadOnlyList<BlowNode> Connected(BlowGraphIndex graph, IEnumerable<string> nodeIds)
        => Walk(graph, nodeIds, includeStart: true, BlowTraversalDirection.Both);

    /// <summary>The edges with both ends among the given nodes.</summary>
    public static IReadOnlyList<BlowEdge> EdgesAmong(BlowGraphIndex graph, IEnumerable<string> nodeIds)
    {
        ArgumentNullException.ThrowIfNull(graph);
        var ids = nodeIds.ToHashSet(StringComparer.Ordinal);
        return [.. ids.SelectMany(graph.Outgoing).Where(e => ids.Contains(e.Target)).Distinct()];
    }

    /// <summary>The edges with at least one end among the given nodes.</summary>
    public static IReadOnlyList<BlowEdge> EdgesTouching(BlowGraphIndex graph, IEnumerable<string> nodeIds)
    {
        ArgumentNullException.ThrowIfNull(graph);
        var ids = nodeIds.ToHashSet(StringComparer.Ordinal);
        return [.. graph.Edges.Where(e => ids.Contains(e.Source) || ids.Contains(e.Target))];
    }

    /// <summary>The nodes inside the given groups (by <see cref="BlowNode.ParentId"/>): their direct children, or every level down when <paramref name="deep"/>.</summary>
    public static IReadOnlyList<BlowNode> Members(IEnumerable<BlowNode> nodes, IEnumerable<string> groupIds, bool deep = true)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        var all = nodes as IReadOnlyList<BlowNode> ?? [.. nodes];
        var children = all.Where(n => n.ParentId is not null).ToLookup(n => n.ParentId!, StringComparer.Ordinal);
        var result = new List<BlowNode>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Stack<string>(groupIds);
        foreach (var id in pending) seen.Add(id);
        while (pending.Count > 0)
        {
            foreach (var child in children[pending.Pop()])
            {
                if (!seen.Add(child.Id)) continue;
                result.Add(child);
                if (deep) pending.Push(child.Id);
            }
        }
        return result;
    }

    /// <summary>The nodes that are not among the given ones, and can be selected.</summary>
    public static IReadOnlyList<BlowNode> Invert(IEnumerable<BlowNode> nodes, IEnumerable<BlowNode> selected)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        var chosen = selected.Select(n => n.Id).ToHashSet(StringComparer.Ordinal);
        return [.. nodes.Where(n => n.Selectable && !n.Hidden && !chosen.Contains(n.Id))];
    }

    private static List<BlowNode> Walk(BlowGraphIndex graph, IEnumerable<string> nodeIds, bool includeStart, BlowTraversalDirection direction)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(nodeIds);
        var starts = nodeIds.Where(graph.ContainsNode).Distinct(StringComparer.Ordinal).ToList();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<BlowNode>();
        var pending = new Stack<string>();
        foreach (var id in starts)
        {
            seen.Add(id);
            pending.Push(id);
            if (includeStart) result.Add(graph.GetNode(id)!);
        }
        while (pending.Count > 0)
        {
            var id = pending.Pop();
            IEnumerable<string> next = direction switch
            {
                BlowTraversalDirection.Incoming => graph.Incoming(id).Select(e => e.Source),
                BlowTraversalDirection.Outgoing => graph.Outgoing(id).Select(e => e.Target),
                _ => graph.Incoming(id).Select(e => e.Source).Concat(graph.Outgoing(id).Select(e => e.Target)),
            };
            foreach (var n in next)
            {
                if (!seen.Add(n)) continue;
                pending.Push(n);
                // A walk that leads back to a start node does not add it when it was asked to be left out.
                result.Add(graph.GetNode(n)!);
            }
        }
        return result;
    }
}
