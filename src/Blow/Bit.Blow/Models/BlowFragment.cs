namespace Bit.Blow;

/// <summary>
/// A self-contained piece of a graph: some nodes and the edges that run between them. What a
/// copy produces and a paste consumes.
/// </summary>
/// <remarks>
/// A fragment never carries an edge with an endpoint outside its own node set - an edge to a
/// node that is not there is not part of the thing being copied, and pasting one would produce
/// a dangling edge.
/// </remarks>
public sealed record BlowFragment(IReadOnlyList<BlowNode> Nodes, IReadOnlyList<BlowEdge> Edges)
{
    /// <summary>A fragment with nothing in it.</summary>
    public static readonly BlowFragment Empty = new([], []);

    /// <summary>True when there is nothing to paste.</summary>
    public bool IsEmpty => Nodes.Count == 0 && Edges.Count == 0;
}
