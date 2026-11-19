namespace Bit.BevTools;

public sealed class BevToolsComponentTreeNode
{
    public required BevToolsComponentRecord Record { get; init; }

    public int Depth { get; set; }

    public List<BevToolsComponentTreeNode> Children { get; } = [];

    /// <summary>Total renders of this node and all descendants.</summary>
    public int SubtreeRenders { get; set; }

    public double SubtreeRenderMs { get; set; }
}
