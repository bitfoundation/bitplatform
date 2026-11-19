namespace Bit.Blow;

/// <summary>
/// The result of <see cref="BlowGraphAlgorithms.TopologicalSort(BlowGraphIndex)"/>: the nodes that could be put
/// in dependency order, and those that could not because they are on a cycle or downstream of one.
/// </summary>
/// <param name="Order">Every node after all the nodes with an edge into it. Ties keep the original node order.</param>
/// <param name="Unordered">The nodes left over because a cycle blocks them, in original node order.</param>
public sealed record BlowTopologicalOrder(IReadOnlyList<BlowNode> Order, IReadOnlyList<BlowNode> Unordered)
{
    /// <summary>True when every node could be ordered, which is when the graph has no cycle.</summary>
    public bool IsAcyclic => Unordered.Count == 0;
}
