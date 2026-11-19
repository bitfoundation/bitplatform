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
