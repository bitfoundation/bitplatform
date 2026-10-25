using System.Text.Json.Nodes;

namespace Bit.Blow;

/// <summary>
/// A graph as it is saved: its nodes and edges, where it was being looked at, and what the application wants to keep
/// with it. What <see cref="BlowSerializer"/> writes and reads.
/// </summary>
/// <remarks>
/// Only persistent state is saved. What the canvas maintains for itself - measured sizes and handle anchors, drag and
/// resize state, resolved absolute positions - is worked out again when the graph is drawn, and selection is left out
/// unless <see cref="BlowSerializerOptions.IncludeSelection"/> asks for it.
/// </remarks>
public sealed class BlowDocument
{
    /// <summary>The nodes.</summary>
    public List<BlowNode> Nodes { get; set; } = [];

    /// <summary>The edges.</summary>
    public List<BlowEdge> Edges { get; set; } = [];

    /// <summary>Where the graph was being looked at, or null to leave the viewport as it is when loading.</summary>
    public BlowViewport? Viewport { get; set; }

    /// <summary>An identifier for the document, for the application's own bookkeeping. Optional.</summary>
    public string? Id { get; set; }

    /// <summary>
    /// The application's own version of what it keeps in this document - the shape of its node data, say. Saved as
    /// written and compared with <see cref="BlowSerializerOptions.CurrentVersion"/> on loading, where the migrations
    /// bring an older document up to date. Independent of Blow's own schema version, which the serializer manages.
    /// </summary>
    public int Version { get; set; }

    /// <summary>Anything else the application wants saved with the graph: a title, an author, settings.</summary>
    public JsonObject? Metadata { get; set; }

    /// <summary>A document holding a snapshot's nodes, edges and viewport (the instances themselves, not copies).</summary>
    public static BlowDocument FromSnapshot(BlowSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new BlowDocument { Nodes = [.. snapshot.Nodes], Edges = [.. snapshot.Edges], Viewport = snapshot.Viewport };
    }

    /// <summary>The document's graph as a snapshot, ready for <c>BlowCanvas.RestoreAsync</c>.</summary>
    public BlowSnapshot ToSnapshot() => new(Nodes, Edges, Viewport ?? BlowViewport.Identity);
}
