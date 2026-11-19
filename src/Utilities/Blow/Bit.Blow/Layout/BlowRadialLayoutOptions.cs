namespace Bit.Blow;

/// <summary>Settings for <see cref="BlowLayout.Radial"/>.</summary>
public sealed class BlowRadialLayoutOptions : BlowLayoutOptions
{
    /// <inheritdoc/>
    public override void Arrange(IReadOnlyList<BlowNode> nodes, IReadOnlyList<BlowEdge> edges) => BlowLayout.Radial(nodes, edges, this);

    /// <summary>
    /// Which nodes are the roots. Null takes the nodes nothing leads to. One root goes in the centre; several share
    /// the first ring around an empty centre.
    /// </summary>
    public Func<BlowNode, bool>? IsRoot { get; set; }

    /// <summary>The least gap between one ring's nodes and the next ring's, in flow units.</summary>
    public double RingGap { get; set; } = 60;

    /// <summary>The least gap between neighbours on a ring, in flow units.</summary>
    public double NodeGap { get; set; } = 20;

    /// <summary>Where the first subtree starts, in degrees clockwise from twelve o'clock.</summary>
    public double StartAngle { get; set; }
}
