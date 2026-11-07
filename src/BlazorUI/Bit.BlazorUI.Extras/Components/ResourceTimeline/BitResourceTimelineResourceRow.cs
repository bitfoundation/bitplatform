namespace Bit.BlazorUI;

/// <summary>
/// One row of the resource column as the rendering engine of the <see cref="BitResourceTimeline"/> reports it, in
/// display order, for the overlay that renders the <see cref="BitResourceTimeline.ResourceTemplate"/>.
/// </summary>
/// <param name="Id">The id of the resource the row renders.</param>
/// <param name="Name">The display name of the resource.</param>
/// <param name="Depth">The depth of the resource in the hierarchy; 0 for a root row.</param>
/// <param name="HasChildren">Whether the row is a group, a resource with child resources.</param>
/// <param name="Collapsed">Whether the group is collapsed.</param>
/// <param name="Height">
/// The height of the row in pixels, which grows when overlapping bars stack; 0 when not known. Kept as fractional as the
/// engine measured it: the rows of the overlay stack on one another as the engine's do, so a height rounded off here
/// would put every row below it a little further from the one it labels.
/// </param>
internal sealed record BitResourceTimelineResourceRow(string Id, string Name, int Depth, bool HasChildren, bool Collapsed, double Height);
