namespace Bit.Blow;

/// <summary>Shared settings for every built-in layout.</summary>
public abstract class BlowLayoutOptions : IBlowLayout
{
    /// <inheritdoc/>
    public abstract void Arrange(IReadOnlyList<BlowNode> nodes, IReadOnlyList<BlowEdge> edges);

    /// <summary>Top-left corner of the laid-out block, in the coordinate space of the nodes.</summary>
    public BlowXYPosition Origin { get; set; } = BlowXYPosition.Zero;

    /// <summary>
    /// The canvas's <c>NodeOrigin</c>: which point of a node its <see cref="BlowNode.Position"/>
    /// names. The layouts arrange nodes by their boxes, so on a canvas that positions nodes by
    /// their centre (0.5, 0.5) this has to match, or nodes of different sizes come out shifted
    /// against each other. A node's own <see cref="BlowNode.Origin"/> still wins. Defaults to the
    /// top-left (0, 0).
    /// </summary>
    public BlowXYPosition NodeOrigin { get; set; } = BlowXYPosition.Zero;

    /// <summary>
    /// Size assumed for a node that has neither an explicit size nor a measurement yet. Laying
    /// out before the first measurement is common - it is what you do to a graph you just
    /// loaded - so the fallback decides how good that first arrangement looks.
    /// </summary>
    public BlowDimensions FallbackSize { get; set; } = new(BlowNode.DefaultWidth, BlowNode.DefaultHeight);
}
