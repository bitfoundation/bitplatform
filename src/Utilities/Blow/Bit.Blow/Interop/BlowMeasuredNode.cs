namespace Bit.Blow;

/// <summary>One node as measured in the DOM, with sizes already divided back out of the zoom.</summary>
/// <param name="Id">The node's id, read from its <c>data-id</c> attribute.</param>
/// <param name="Width">Rendered width in flow units.</param>
/// <param name="Height">Rendered height in flow units.</param>
/// <param name="Handles">The handles found inside the node.</param>
public sealed record BlowMeasuredNode(string Id, double Width, double Height, BlowMeasuredHandle[] Handles);
