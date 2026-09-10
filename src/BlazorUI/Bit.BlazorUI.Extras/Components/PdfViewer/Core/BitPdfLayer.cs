// Optional content (layers): the /OCProperties groups a document declares, which a
// reader is allowed to turn on and off - CAD drawings, map overlays, multilingual
// artwork and watermarks all ship as layers.

namespace Bit.BlazorUI;

/// <summary>An optional-content group (a layer) declared by the document.</summary>
public sealed class BitPdfLayer
{
    /// <summary>
    /// The identity of the group: a stable key derived from the indirect reference
    /// that names it. Content marked with this group is what the layer switches.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>The group's display name (<c>/Name</c>).</summary>
    public required string Name { get; init; }

    /// <summary>
    /// Whether the document's default configuration shows the layer. The viewer
    /// starts from this and the reader takes it from there.
    /// </summary>
    public bool VisibleByDefault { get; init; }
}
