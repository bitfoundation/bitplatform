namespace Bit.Blow;

/// <summary>An edge present in both graphs with something about it changed.</summary>
/// <param name="Before">The edge as it was.</param>
/// <param name="After">The edge as it is.</param>
/// <param name="Properties">The names of the <see cref="BlowEdge"/> properties that differ: "Target", "Label", "MarkerEnd", ...</param>
public sealed record BlowEdgeChange(BlowEdge Before, BlowEdge After, IReadOnlyList<string> Properties);
