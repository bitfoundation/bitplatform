namespace Bit.Blow;

/// <summary>Settings for <see cref="BlowLayout.Tree"/>.</summary>
public sealed class BlowTreeLayoutOptions : BlowLayoutOptions
{
    /// <inheritdoc/>
    public override void Arrange(IReadOnlyList<BlowNode> nodes, IReadOnlyList<BlowEdge> edges) => BlowLayout.Tree(nodes, edges, this);

    /// <summary>Which way the tree grows from its roots.</summary>
    public BlowLayoutDirection Direction { get; set; } = BlowLayoutDirection.TopToBottom;

    /// <summary>Gap between one level of the tree and the next, in flow units.</summary>
    public double LevelGap { get; set; } = 60;

    /// <summary>Gap between neighbouring nodes and subtrees on one level, in flow units.</summary>
    public double NodeGap { get; set; } = 30;

    /// <summary>
    /// Whether to turn each node's handles to face the way the tree grows; see
    /// <see cref="BlowLayeredLayoutOptions.AlignHandlesToDirection"/>.
    /// </summary>
    public bool AlignHandlesToDirection { get; set; } = true;
}
