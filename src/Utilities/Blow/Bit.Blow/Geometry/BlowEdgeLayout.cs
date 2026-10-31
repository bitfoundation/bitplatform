namespace Bit.Blow;

/// <summary>
/// Where nodes and edges are drawn, decided the same way by the canvas and the SVG export - which promises to show what
/// is on screen: where a nested node sits, which node each end of an edge is drawn to when it is folded into a
/// collapsed group, and which nodes a routed edge goes round.
/// </summary>
internal static class BlowEdgeLayout
{
    /// <summary>
    /// How far past its two nodes a routed edge looks for nodes in its way. Only what is near counts: the grid the
    /// router searches grows with the square of the obstacles it sees.
    /// </summary>
    public const double ObstacleReach = 150;

    /// <summary>
    /// How many parents a walk up a chain that does not watch for cycles follows before it gives up - finding the
    /// collapsed group an end is folded into, or the groups an edge runs inside. <see cref="Nest"/> watches for them,
    /// and follows a chain however long it is.
    /// </summary>
    public const int MaxParentDepth = 50;

    /// <summary>
    /// Where a node sits once its parents are summed, how deep it is nested, and whether it - or a node it is nested
    /// in - is hidden.
    /// </summary>
    /// <param name="Position">Its position point in flow space: its position summed up its parents, origin not applied.</param>
    /// <param name="Depth">How many parents it has, 0 for a root.</param>
    /// <param name="Hidden">True when it, or any node it is nested in, is hidden.</param>
    public readonly record struct Nesting(BlowXYPosition Position, int Depth, bool Hidden);

    /// <summary>
    /// Resolves every node's <see cref="Nesting"/> into <paramref name="resolved"/>, keyed by id, in list order. Each
    /// node is resolved once and remembered, walking up without recursion, so a chain of any length costs one step per
    /// node and a node is placed the same whatever order its chain is listed in. A link that cannot be followed makes
    /// the node a root, and is passed to <paramref name="broken"/> with whether it is a cycle: a parent not in
    /// <paramref name="index"/>, or one that leads back round to a node already on the walk - a cycle, cut where the
    /// walk finds it.
    /// </summary>
    public static void Nest(IEnumerable<BlowNode> nodes, IReadOnlyDictionary<string, BlowNode> index,
        Dictionary<string, Nesting> resolved, Action<BlowNode, bool>? broken = null)
    {
        var chain = new List<BlowNode>();
        var onChain = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            chain.Clear();
            onChain.Clear();
            Nesting? above = null;
            for (var current = node; ;)
            {
                if (resolved.TryGetValue(current.Id, out var known))
                {
                    above = known;
                    break;
                }
                chain.Add(current);
                onChain.Add(current.Id);
                if (current.ParentId is not { } pid) break;
                if (!index.TryGetValue(pid, out var parent))
                {
                    broken?.Invoke(current, false);
                    break;
                }
                if (onChain.Contains(parent.Id))
                {
                    broken?.Invoke(current, true);
                    break;
                }
                current = parent;
            }

            // Back down the chain, the root (or the first node already known) first.
            for (var i = chain.Count - 1; i >= 0; i--)
            {
                var n = chain[i];
                above = above is { } a
                    ? new Nesting(new BlowXYPosition(a.Position.X + n.Position.X, a.Position.Y + n.Position.Y), a.Depth + 1, a.Hidden || n.Hidden)
                    : new Nesting(n.Position, 0, n.Hidden);
                resolved[n.Id] = above.Value;
            }
        }
    }

    /// <summary>
    /// The node an edge's end is drawn to: the node itself when it is shown, else - when it is folded into a collapsed
    /// node - the nearest collapsed ancestor that is shown, with <paramref name="moved"/> set. The node itself, not
    /// shown, when neither; null when the id names no node.
    /// </summary>
    public static BlowNode? DisplayedEnd(IReadOnlyDictionary<string, BlowNode> index, string nodeId, Func<BlowNode, bool> shown,
        out bool moved)
    {
        moved = false;
        if (!index.TryGetValue(nodeId, out var node)) return null;
        if (shown(node)) return node;

        var current = node;
        for (var guard = 0; guard < MaxParentDepth && current.ParentId is { } pid && index.TryGetValue(pid, out var parent); guard++)
        {
            if (parent.Collapsed && shown(parent))
            {
                moved = true;
                return parent;
            }
            current = parent;
        }
        return node;
    }

    /// <summary>The area a routed edge between two nodes' boxes looks for nodes in its way.</summary>
    public static BlowRect ObstacleArea(BlowRect source, BlowRect target)
    {
        var left = Math.Min(source.Left, target.Left) - ObstacleReach;
        var top = Math.Min(source.Top, target.Top) - ObstacleReach;
        return new BlowRect(left, top,
            Math.Max(source.Right, target.Right) + ObstacleReach - left,
            Math.Max(source.Bottom, target.Bottom) + ObstacleReach - top);
    }

    /// <summary>
    /// What a routed edge goes round: the boxes among <paramref name="candidates"/> that meet
    /// <paramref name="area"/> - its own two nodes included, so it does not cut back across them - less the groups
    /// either end sits in, at any depth, which it has to run inside. In the order of the candidates.
    /// </summary>
    public static List<BlowRect> Obstacles(IEnumerable<(string Id, BlowRect Rect)> candidates, BlowRect area,
        BlowNode source, BlowNode target, IReadOnlyDictionary<string, BlowNode> index)
    {
        var containers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var end in new[] { source, target })
        {
            var current = end;
            for (var guard = 0; guard < MaxParentDepth && current.ParentId is { } pid && index.TryGetValue(pid, out var parent)
                 && containers.Add(pid); guard++)
            {
                current = parent;
            }
        }

        var obstacles = new List<BlowRect>();
        foreach (var (id, rect) in candidates)
        {
            if (containers.Contains(id)) continue;
            if (rect.Left < area.Right && rect.Right > area.Left && rect.Top < area.Bottom && rect.Bottom > area.Top) obstacles.Add(rect);
        }
        return obstacles;
    }
}
