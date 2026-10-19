namespace Bit.Blow;

/// <summary>Which edge (or axis) a set of nodes is lined up on.</summary>
public enum BlowAlignEdge
{
    /// <summary>Left edges line up with the leftmost node.</summary>
    Left,

    /// <summary>Horizontal centres line up with the centre of the group's bounding box.</summary>
    CenterX,

    /// <summary>Right edges line up with the rightmost node.</summary>
    Right,

    /// <summary>Top edges line up with the topmost node.</summary>
    Top,

    /// <summary>Vertical centres line up with the centre of the group's bounding box.</summary>
    CenterY,

    /// <summary>Bottom edges line up with the bottommost node.</summary>
    Bottom,
}

/// <summary>
/// Lines up and spaces out a set of nodes - the align and distribute buttons every drawing tool
/// has, and the fastest way to turn a hand-placed diagram into a tidy one.
/// </summary>
/// <remarks>
/// Like <see cref="BlowLayout"/>, every method here writes to <see cref="BlowNode.Position"/> and
/// nothing else. A node's position is relative to its parent, so align a set of siblings (or a
/// set of root nodes) rather than mixing depths in one call; nodes of different parents would be
/// lined up in coordinate spaces that do not share an origin. Nodes that are hidden, or whose
/// <see cref="BlowNode.Draggable"/> is false, are left alone - the same things the user could not
/// have moved by hand.
/// </remarks>
public static class BlowAlign
{
    /// <summary>Lines the given nodes up on one edge or axis.</summary>
    /// <returns>The nodes that actually moved.</returns>
    public static IReadOnlyList<BlowNode> Align(IEnumerable<BlowNode> nodes, BlowAlignEdge edge)
        => Align(nodes, edge, BlowXYPosition.Zero);

    /// <summary>
    /// Lines the given nodes up on one edge or axis, on a canvas whose nodes are positioned by
    /// <paramref name="nodeOrigin"/> rather than by their top-left corner.
    /// </summary>
    /// <param name="nodes">The nodes to line up.</param>
    /// <param name="edge">Which edge or axis to line them up on.</param>
    /// <param name="nodeOrigin">
    /// The canvas's <c>NodeOrigin</c>, used for nodes that do not set their own. Aligning works on
    /// the nodes' boxes, so it has to know which point of the box a position names.
    /// </param>
    /// <returns>The nodes that actually moved.</returns>
    public static IReadOnlyList<BlowNode> Align(IEnumerable<BlowNode> nodes, BlowAlignEdge edge, BlowXYPosition nodeOrigin)
    {
        var movable = Movable(nodes);
        if (movable.Count < 2) return [];

        var bounds = Bounds(movable, nodeOrigin);
        var moved = new List<BlowNode>();

        foreach (var node in movable)
        {
            var size = node.EffectiveSize;
            var before = node.Position;
            var box = TopLeft(node, nodeOrigin);

            var target = edge switch
            {
                BlowAlignEdge.Left => box with { X = bounds.Left },
                BlowAlignEdge.CenterX => box with { X = bounds.Left + (bounds.Width - size.Width) / 2 },
                BlowAlignEdge.Right => box with { X = bounds.Right - size.Width },
                BlowAlignEdge.Top => box with { Y = bounds.Top },
                BlowAlignEdge.CenterY => box with { Y = bounds.Top + (bounds.Height - size.Height) / 2 },
                _ => box with { Y = bounds.Bottom - size.Height },
            };

            node.Position = new BlowXYPosition(before.X + target.X - box.X, before.Y + target.Y - box.Y);
            if (node.Position != before) moved.Add(node);
        }

        return moved;
    }

    /// <summary>
    /// Spaces the given nodes evenly across the horizontal span they already occupy. The leftmost
    /// and rightmost stay put and everything between them is redistributed, so distributing does
    /// not move the group.
    /// </summary>
    /// <param name="nodes">The nodes to space out.</param>
    /// <param name="gap">
    /// When given, the exact gap to leave between neighbours instead of filling the existing
    /// span. The leftmost node still stays put, so the group grows or shrinks to the right.
    /// </param>
    /// <returns>The nodes that actually moved.</returns>
    public static IReadOnlyList<BlowNode> DistributeHorizontally(IEnumerable<BlowNode> nodes, double? gap = null)
        => Distribute(nodes, gap, horizontal: true, BlowXYPosition.Zero);

    /// <summary>Spaces the given nodes evenly down the vertical span they already occupy.</summary>
    /// <inheritdoc cref="DistributeHorizontally"/>
    public static IReadOnlyList<BlowNode> DistributeVertically(IEnumerable<BlowNode> nodes, double? gap = null)
        => Distribute(nodes, gap, horizontal: false, BlowXYPosition.Zero);

    /// <summary>
    /// Spaces the given nodes evenly, on a canvas whose nodes are positioned by
    /// <paramref name="nodeOrigin"/> rather than by their top-left corner.
    /// </summary>
    /// <param name="nodes">The nodes to space out.</param>
    /// <param name="horizontal">True to space them along X, false along Y.</param>
    /// <param name="nodeOrigin">The canvas's <c>NodeOrigin</c>, used for nodes that do not set their own.</param>
    /// <param name="gap">When given, the exact gap to leave between neighbours instead of filling the existing span.</param>
    /// <returns>The nodes that actually moved.</returns>
    public static IReadOnlyList<BlowNode> Distribute(
        IEnumerable<BlowNode> nodes, bool horizontal, BlowXYPosition nodeOrigin, double? gap = null)
        => Distribute(nodes, gap, horizontal, nodeOrigin);

    private static IReadOnlyList<BlowNode> Distribute(
        IEnumerable<BlowNode> nodes, double? gap, bool horizontal, BlowXYPosition nodeOrigin)
    {
        var movable = Movable(nodes);
        if (movable.Count < 3 && gap is null) return [];
        if (movable.Count < 2) return [];

        // Where each node's box starts along the axis.
        double Lead(BlowNode n) => horizontal ? TopLeft(n, nodeOrigin).X : TopLeft(n, nodeOrigin).Y;

        // Order by where the nodes already are, so distributing preserves the sequence the user
        // laid out rather than the order the list happens to be in.
        movable.Sort((a, b) => Lead(a).CompareTo(Lead(b)));

        var extents = new double[movable.Count];
        var total = 0.0;
        var end = double.MinValue;
        for (var i = 0; i < movable.Count; i++)
        {
            var size = movable[i].EffectiveSize;
            extents[i] = horizontal ? size.Width : size.Height;
            total += extents[i];
            end = Math.Max(end, Lead(movable[i]) + extents[i]);
        }

        var start = Lead(movable[0]);

        double step;
        if (gap is { } fixedGap)
        {
            step = fixedGap;
        }
        else
        {
            // Whatever the nodes do not themselves occupy, shared out between the gaps. The span
            // runs to the furthest far edge, which need not belong to the node that starts last.
            // When the nodes are wider than the span altogether there is nothing to share, and a
            // negative gap would slide them over each other and out of order, so they are packed
            // edge to edge instead.
            step = Math.Max(0, (end - start - total) / (movable.Count - 1));
        }

        var moved = new List<BlowNode>();
        var cursor = start;
        for (var i = 0; i < movable.Count; i++)
        {
            var node = movable[i];
            var before = node.Position;
            var shift = cursor - Lead(node);
            node.Position = horizontal ? before with { X = before.X + shift } : before with { Y = before.Y + shift };
            if (node.Position != before) moved.Add(node);
            cursor += extents[i] + step;
        }

        return moved;
    }

    /// <summary>The nodes a user could have moved by hand: visible and draggable.</summary>
    private static List<BlowNode> Movable(IEnumerable<BlowNode> nodes)
    {
        var list = new List<BlowNode>();
        foreach (var n in nodes)
        {
            if (n.Hidden || !n.Draggable) continue;
            list.Add(n);
        }
        return list;
    }

    /// <summary>
    /// Bounding box of the nodes in their <em>own</em> coordinate space. Deliberately not
    /// <c>GetNodesBounds</c>, which works in absolute space: what is written back is
    /// <see cref="BlowNode.Position"/>, so the box has to be measured in the same space.
    /// </summary>
    private static BlowRect Bounds(List<BlowNode> nodes, BlowXYPosition nodeOrigin)
    {
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        foreach (var n in nodes)
        {
            var size = n.EffectiveSize;
            var box = TopLeft(n, nodeOrigin);
            minX = Math.Min(minX, box.X);
            minY = Math.Min(minY, box.Y);
            maxX = Math.Max(maxX, box.X + size.Width);
            maxY = Math.Max(maxY, box.Y + size.Height);
        }
        return new BlowRect(minX, minY, maxX - minX, maxY - minY);
    }

    /// <summary>The top-left of the node's box, in the node's own coordinate space.</summary>
    private static BlowXYPosition TopLeft(BlowNode node, BlowXYPosition nodeOrigin)
    {
        var origin = node.Origin ?? nodeOrigin;
        var size = node.EffectiveSize;
        return new BlowXYPosition(node.Position.X - origin.X * size.Width, node.Position.Y - origin.Y * size.Height);
    }
}
