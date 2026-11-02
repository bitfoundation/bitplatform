namespace Bit.Bortable.Samples.Core;

/// <summary>An entry of the Tree page: a name, and the list of entries under it, which is a list of its own.</summary>
public sealed class TreeNode
{
    public required string Name { get; init; }

    public List<TreeNode> Children { get; init; } = [];
}
