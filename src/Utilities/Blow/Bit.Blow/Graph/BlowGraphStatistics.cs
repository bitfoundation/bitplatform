namespace Bit.Blow;

/// <summary>Counts that describe a graph's shape at a glance.</summary>
/// <param name="NodeCount">Nodes in the graph.</param>
/// <param name="EdgeCount">Edges between nodes in the graph (dangling edges are counted separately).</param>
/// <param name="RootCount">Nodes with no incoming edge.</param>
/// <param name="LeafCount">Nodes with no outgoing edge.</param>
/// <param name="IsolatedCount">Nodes with no edge at all.</param>
/// <param name="ComponentCount">Separate pieces the graph falls into, ignoring edge direction.</param>
/// <param name="SelfLoopCount">Edges from a node to itself.</param>
/// <param name="MaxInDegree">The most incoming edges any one node has.</param>
/// <param name="MaxOutDegree">The most outgoing edges any one node has.</param>
/// <param name="IsAcyclic">True when the graph has no directed cycle (self-loops included).</param>
/// <param name="DanglingEdgeCount">Edges naming a node that is not in the graph.</param>
public sealed record BlowGraphStatistics(
    int NodeCount, int EdgeCount, int RootCount, int LeafCount, int IsolatedCount, int ComponentCount,
    int SelfLoopCount, int MaxInDegree, int MaxOutDegree, bool IsAcyclic, int DanglingEdgeCount);
