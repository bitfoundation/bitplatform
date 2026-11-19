namespace Bit.Blow;

/// <summary>Settings for <see cref="BlowLayout.Force"/>.</summary>
public sealed class BlowForceLayoutOptions : BlowLayoutOptions
{
    /// <inheritdoc/>
    public override void Arrange(IReadOnlyList<BlowNode> nodes, IReadOnlyList<BlowEdge> edges) => BlowLayout.Force(nodes, edges, this);

    /// <summary>How far apart connected nodes settle, roughly, centre to centre, in flow units.</summary>
    public double IdealEdgeLength { get; set; } = 180;

    /// <summary>Simulation steps. More settle a large graph better, at a cost that grows with each step.</summary>
    public int Iterations { get; set; } = 300;

    /// <summary>
    /// How far off a group of nodes has to be for its push to be taken as one heavy node's, in a graph of more than
    /// <see cref="BlowLayout.ExactRepulsionLimit"/> nodes: a group is taken as one when its width is less than this
    /// fraction of its distance. Higher is faster and coarser; 0 pushes every pair apart exactly, at a cost that grows
    /// with the square of the node count. The Barnes-Hut θ.
    /// </summary>
    public double Theta { get; set; } = 0.9;

    /// <summary>The least gap left between any two nodes once the simulation is done, in flow units.</summary>
    public double NodeGap { get; set; } = 20;

    /// <summary>Start from where the nodes are now rather than from a spiral, to tidy an arrangement rather than replace it.</summary>
    public bool StartFromCurrentPositions { get; set; }

    /// <summary>Nodes that must not move; the rest arrange around them. With any pinned, nothing is moved to the origin.</summary>
    public Func<BlowNode, bool>? IsPinned { get; set; }
}
