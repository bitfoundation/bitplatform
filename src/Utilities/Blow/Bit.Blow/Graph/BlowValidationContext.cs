namespace Bit.Blow;

/// <summary>What a validation rule is given: the graph as handed over, and an index over it.</summary>
public sealed class BlowValidationContext
{
    internal BlowValidationContext(IReadOnlyList<BlowNode> nodes, IReadOnlyList<BlowEdge> edges, CancellationToken cancellationToken)
    {
        Nodes = nodes;
        Edges = edges;
        Graph = new BlowGraphIndex(nodes, edges);
        CancellationToken = cancellationToken;
    }

    /// <summary>The nodes exactly as given, duplicates included - which is what a rule about duplicates needs.</summary>
    public IReadOnlyList<BlowNode> Nodes { get; }

    /// <summary>The edges exactly as given.</summary>
    public IReadOnlyList<BlowEdge> Edges { get; }

    /// <summary>Lookups and adjacency over the graph, for rules that ask about connections.</summary>
    public BlowGraphIndex Graph { get; }

    /// <summary>Signalled when the caller no longer wants the result; long rules should check it.</summary>
    public CancellationToken CancellationToken { get; }
}
