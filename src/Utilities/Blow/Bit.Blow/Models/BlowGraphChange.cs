namespace Bit.Blow;

/// <summary>What kind of edit a <see cref="BlowGraphChange"/> describes.</summary>
public enum BlowChangeKind
{
    /// <summary>Nodes or edges were added: an <c>AddNodes</c> or <c>AddEdges</c> call, a paste, a duplicate.</summary>
    Added,

    /// <summary>Nodes or edges were removed, with the edges and children that went with them.</summary>
    Removed,

    /// <summary>Nodes moved: a drag, a keyboard nudge, align or distribute.</summary>
    Moved,

    /// <summary>A node was resized.</summary>
    Resized,

    /// <summary>A connection made a new edge.</summary>
    Connected,

    /// <summary>An existing edge was moved to another handle.</summary>
    Reconnected,

    /// <summary>
    /// The whole graph was replaced - restored from a snapshot, loaded from a document, or stepped back or forward in
    /// the history.
    /// </summary>
    Replaced,

    /// <summary>Several changes made together through <c>BlowCanvas.EditAsync</c>, or nodes grouped, ungrouped or reordered.</summary>
    Edited,
}

/// <summary>Where a <see cref="BlowGraphChange"/> came from.</summary>
public enum BlowChangeOrigin
{
    /// <summary>The user, with a pointer gesture or a key on the canvas.</summary>
    User,

    /// <summary>The application, through the canvas's API.</summary>
    Api,

    /// <summary>The built-in history: an undo or a redo.</summary>
    History,

    /// <summary>
    /// Somewhere else - another user's edit applied by a collaboration adapter, a server push. Passed to
    /// <c>BlowCanvas.EditAsync</c> by the code applying it, so a listener that broadcasts local changes can tell
    /// the ones not to send back.
    /// </summary>
    Remote,
}

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
