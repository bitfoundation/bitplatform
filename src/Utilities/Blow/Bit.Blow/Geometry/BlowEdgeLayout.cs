namespace Bit.Blow;

/// <summary>
/// Where an edge is drawn, decided the same way by the canvas and the SVG export - which promises to show what is on
/// screen: which node each end is drawn to when it is folded into a collapsed group, and which nodes a routed edge
/// goes round.
/// </summary>
internal static class BlowEdgeLayout
{
    /// <summary>
    /// How far past its two nodes a routed edge looks for nodes in its way. Only what is near counts: the grid the
    /// router searches grows with the square of the obstacles it sees.
    /// </summary>
    public const double ObstacleReach = 150;

    /// <summary>
    /// The node an edge's end is drawn to: the node itself when it is shown, else - when it is folded into a collapsed
    /// node - the nearest collapsed ancestor that is shown, with <paramref name="moved"/> set. The node itself, not
    /// shown, when neither; null when the id names no node.
    /// </summary>
    public static BlowNode? DisplayedEnd(IReadOnlyDictionary<string, BlowNode> index, string nodeId, Func<BlowNode, bool> shown,
        int maxDepth, out bool moved)
    {
        moved = false;
        if (!index.TryGetValue(nodeId, out var node)) return null;
        if (shown(node)) return node;

        var current = node;
        for (var guard = 0; guard < maxDepth && current.ParentId is { } pid && index.TryGetValue(pid, out var parent); guard++)
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
        BlowNode source, BlowNode target, IReadOnlyDictionary<string, BlowNode> index, int maxDepth)
    {
        var containers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var end in new[] { source, target })
        {
            var current = end;
            for (var guard = 0; guard < maxDepth && current.ParentId is { } pid && index.TryGetValue(pid, out var parent)
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
