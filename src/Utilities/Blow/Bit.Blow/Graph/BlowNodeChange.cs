namespace Bit.Blow;

/// <summary>A node present in both graphs with something about it changed.</summary>
/// <param name="Before">The node as it was.</param>
/// <param name="After">The node as it is.</param>
/// <param name="Properties">The names of the <see cref="BlowNode"/> properties that differ: "Position", "Label", "Data", ...</param>
public sealed record BlowNodeChange(BlowNode Before, BlowNode After, IReadOnlyList<string> Properties);
