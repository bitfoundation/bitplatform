namespace Bit.Blow;

/// <summary>Settings for <see cref="BlowLayout.Circle"/>.</summary>
public sealed class BlowCircleLayoutOptions : BlowLayoutOptions
{
    /// <inheritdoc/>
    public override void Arrange(IReadOnlyList<BlowNode> nodes, IReadOnlyList<BlowEdge> edges) => BlowLayout.Circle(nodes, this);

    /// <summary>
    /// Radius of the ring in flow units. When null it is derived from the node sizes so
    /// neighbours do not overlap.
    /// </summary>
    public double? Radius { get; set; }

    /// <summary>Where the first node sits, in degrees clockwise from twelve o'clock.</summary>
    public double StartAngle { get; set; }

    /// <summary>Minimum gap between neighbours on the ring, used when deriving the radius.</summary>
    public double NodeGap { get; set; } = 30;
}
