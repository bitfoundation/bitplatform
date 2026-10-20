namespace Bit.Blow;

/// <summary>
/// A serializable snapshot of the flow state, returned by <c>BlowCanvas.ToObject()</c>.
/// Mirrors React Flow's <c>toObject()</c> result.
/// </summary>
public sealed record BlowSnapshot(
    IReadOnlyList<BlowNode> Nodes,
    IReadOnlyList<BlowEdge> Edges,
    BlowViewport Viewport)
{
    /// <summary>
    /// A copy of the snapshot with its own node and edge instances, sharing nothing mutable with
    /// this one.
    /// </summary>
    /// <remarks>
    /// Restoring a snapshot hands its nodes to the live graph, where the user goes on editing
    /// them - and a node is mutable, so those edits would reach back into the snapshot that is
    /// still holding it. That matters most for a history: undo twice to the same step and the
    /// second one would restore whatever the first had since been dragged into.
    /// </remarks>
    public BlowSnapshot Detach() => new(
        Nodes.Select(n => n.Clone()).ToList(),
        Edges.Select(e => e.Clone()).ToList(),
        Viewport);
}
