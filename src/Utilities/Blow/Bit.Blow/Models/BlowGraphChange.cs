namespace Bit.Blow;

/// <summary>
/// One completed edit of the graph, raised through <c>BlowCanvas.OnGraphChanged</c>: what kind it was, where it came
/// from, and the elements it touched. Raised once per edit - at the end of a drag, not on every move - which makes
/// it the place to persist, to validate, or to send an operation to a collaboration server.
/// </summary>
/// <param name="Kind">What kind of edit it was.</param>
/// <param name="Origin">Where it came from.</param>
public sealed record BlowGraphChange(BlowChangeKind Kind, BlowChangeOrigin Origin)
{
    /// <summary>
    /// The nodes the edit added, moved, resized or touched - for <see cref="BlowChangeKind.Replaced"/>, every node of
    /// the new graph. The live instances.
    /// </summary>
    public IReadOnlyList<BlowNode> Nodes { get; init; } = [];

    /// <summary>The edges the edit added, reconnected or touched. The live instances.</summary>
    public IReadOnlyList<BlowEdge> Edges { get; init; } = [];

    /// <summary>The nodes the edit removed, as they were when they went.</summary>
    public IReadOnlyList<BlowNode> RemovedNodes { get; init; } = [];

    /// <summary>The edges the edit removed, including those that went with a removed node.</summary>
    public IReadOnlyList<BlowEdge> RemovedEdges { get; init; } = [];

    /// <summary>
    /// A short code for the edit - <c>move</c>, <c>resize</c>, <c>connect</c>, <c>reconnect</c>, <c>delete</c>,
    /// <c>add</c>, <c>paste</c>, <c>duplicate</c>, <c>cut</c>, <c>align</c>, <c>distribute</c>, <c>group</c>,
    /// <c>ungroup</c>, <c>order</c>, <c>load</c>, <c>restore</c>, <c>undo</c>, <c>redo</c> - or the label given to
    /// <c>EditAsync</c>. The same code labels the edit's step in the built-in history.
    /// </summary>
    public string? Label { get; init; }
}
