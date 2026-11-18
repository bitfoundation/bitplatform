using System.Numerics;

namespace Bit.Bink;

/// <summary>A node of an element, in the element's local coordinates.</summary>
/// <param name="Position">Where the node is, in local coordinates; the element's transform maps it into the document.</param>
/// <param name="Kind">An anchor or a control point.</param>
public readonly record struct BinkNode(Vector2 Position, BinkNodeKind Kind);
