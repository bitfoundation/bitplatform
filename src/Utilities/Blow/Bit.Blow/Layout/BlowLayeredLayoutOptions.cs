namespace Bit.Blow;

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
