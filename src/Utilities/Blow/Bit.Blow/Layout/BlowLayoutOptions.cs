namespace Bit.Blow;

/// <summary>Which way a layered layout flows.</summary>
public enum BlowLayoutDirection
{
    /// <summary>Roots at the top, each layer below the last.</summary>
    TopToBottom,

    /// <summary>Roots on the left, each layer to the right of the last.</summary>
    LeftToRight,

    /// <summary>Roots at the bottom, each layer above the last.</summary>
    BottomToTop,

    /// <summary>Roots on the right, each layer to the left of the last.</summary>
    RightToLeft,
}

/// <summary>
/// A way of arranging nodes: something <c>BlowCanvas.LayoutAsync</c> can apply to the whole graph or to a selection.
/// Every built-in layout's options are one - <see cref="BlowLayeredLayoutOptions"/>, <see cref="BlowTreeLayoutOptions"/>,
/// <see cref="BlowRadialLayoutOptions"/>, <see cref="BlowForceLayoutOptions"/>, <see cref="BlowGridLayoutOptions"/>,
/// <see cref="BlowCircleLayoutOptions"/> - and an application's own layout, or an adapter to an external engine,
/// implements it the same way.
/// </summary>
public interface IBlowLayout
{
    /// <summary>
    /// Sets the <see cref="BlowNode.Position"/> of every node in <paramref name="nodes"/>, and nothing else of
    /// importance. The nodes all share one parent, so their positions are in one coordinate space;
    /// <paramref name="edges"/> are the edges among them.
    /// </summary>
    void Arrange(IReadOnlyList<BlowNode> nodes, IReadOnlyList<BlowEdge> edges);
}

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

/// <summary>Settings for <see cref="BlowLayout.Layered"/>.</summary>
public sealed class BlowLayeredLayoutOptions : BlowLayoutOptions
{
    /// <inheritdoc/>
    public override void Arrange(IReadOnlyList<BlowNode> nodes, IReadOnlyList<BlowEdge> edges) => BlowLayout.Layered(nodes, edges, this);

    /// <summary>Which way the graph flows.</summary>
    public BlowLayoutDirection Direction { get; set; } = BlowLayoutDirection.TopToBottom;

    /// <summary>Gap between one layer and the next, in flow units.</summary>
    public double LayerGap { get; set; } = 80;

    /// <summary>Gap between neighbouring nodes within a layer, in flow units.</summary>
    public double NodeGap { get; set; } = 40;

    /// <summary>
    /// How many ordering sweeps to run to reduce edge crossings. Each sweep is cheap and the
    /// result stops improving quickly; four is a good balance and zero skips the step entirely,
    /// leaving nodes in the order they were given.
    /// </summary>
    public int CrossingReductionPasses { get; set; } = 4;

    /// <summary>
    /// Whether to move each node's handles to face the flow - sources on the trailing side,
    /// targets on the leading one. Without this a left-to-right layout still routes its edges
    /// top-to-bottom, which looks wrong. Only affects nodes drawn by the built-in renderer.
    /// </summary>
    public bool AlignHandlesToDirection { get; set; } = true;
}

/// <summary>Settings for <see cref="BlowLayout.Grid"/>.</summary>
public sealed class BlowGridLayoutOptions : BlowLayoutOptions
{
    /// <inheritdoc/>
    public override void Arrange(IReadOnlyList<BlowNode> nodes, IReadOnlyList<BlowEdge> edges) => BlowLayout.Grid(nodes, this);

    /// <summary>
    /// Number of columns. When null a roughly square grid is used, which is what "just tidy
    /// these up" usually means.
    /// </summary>
    public int? Columns { get; set; }

    /// <summary>Horizontal gap between cells, in flow units.</summary>
    public double ColumnGap { get; set; } = 40;

    /// <summary>Vertical gap between cells, in flow units.</summary>
    public double RowGap { get; set; } = 40;
}

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

/// <summary>Settings for <see cref="BlowLayout.Force"/>.</summary>
public sealed class BlowForceLayoutOptions : BlowLayoutOptions
{
    /// <inheritdoc/>
    public override void Arrange(IReadOnlyList<BlowNode> nodes, IReadOnlyList<BlowEdge> edges) => BlowLayout.Force(nodes, edges, this);

    /// <summary>How far apart connected nodes settle, roughly, centre to centre, in flow units.</summary>
    public double IdealEdgeLength { get; set; } = 180;

    /// <summary>Simulation steps. More settle a large graph better, at a cost that grows with the square of its size.</summary>
    public int Iterations { get; set; } = 300;

    /// <summary>The least gap left between any two nodes once the simulation is done, in flow units.</summary>
    public double NodeGap { get; set; } = 20;

    /// <summary>Start from where the nodes are now rather than from a spiral, to tidy an arrangement rather than replace it.</summary>
    public bool StartFromCurrentPositions { get; set; }

    /// <summary>Nodes that must not move; the rest arrange around them. With any pinned, nothing is moved to the origin.</summary>
    public Func<BlowNode, bool>? IsPinned { get; set; }
}
