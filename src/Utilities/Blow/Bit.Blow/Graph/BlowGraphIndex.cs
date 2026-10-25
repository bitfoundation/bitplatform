namespace Bit.Blow;

/// <summary>
/// Lookups over a graph built once: nodes and edges by id, and each node's incoming and outgoing edges. Every
/// query that would otherwise scan the edge list per node - the algorithms in <see cref="BlowGraphAlgorithms"/>,
/// validation, connection rules - reads it instead, so a whole-graph question costs O(nodes + edges) rather than
/// O(nodes x edges).
/// </summary>
/// <remarks>
/// <para>
/// The index is a snapshot: it does not follow later changes to the lists it was built from. Build one per
/// question, or once per batch of questions about the same graph.
/// </para>
/// <para>
/// Invalid input is tolerated rather than thrown on, as the canvas tolerates it: the first node or edge with a
/// given id is the one indexed, and an edge naming a node that is not in the list is left out of the adjacency
/// (it is still in <see cref="Edges"/> and <see cref="DanglingEdges"/>).
/// </para>
/// </remarks>
public sealed class BlowGraphIndex
{
    private static readonly IReadOnlyList<BlowEdge> NoEdges = [];

    private readonly Dictionary<string, BlowNode> _nodes;
    private readonly Dictionary<string, BlowEdge> _edges;
    private readonly Dictionary<string, List<BlowEdge>> _outgoing;
    private readonly Dictionary<string, List<BlowEdge>> _incoming;

    /// <summary>Indexes the given graph.</summary>
    public BlowGraphIndex(IEnumerable<BlowNode> nodes, IEnumerable<BlowEdge> edges)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);

        var nodeList = new List<BlowNode>();
        _nodes = new Dictionary<string, BlowNode>(StringComparer.Ordinal);
        foreach (var n in nodes)
        {
            if (_nodes.TryAdd(n.Id, n)) nodeList.Add(n);
        }
        Nodes = nodeList;

        var edgeList = new List<BlowEdge>();
        var dangling = new List<BlowEdge>();
        _edges = new Dictionary<string, BlowEdge>(StringComparer.Ordinal);
        _outgoing = new Dictionary<string, List<BlowEdge>>(StringComparer.Ordinal);
        _incoming = new Dictionary<string, List<BlowEdge>>(StringComparer.Ordinal);
        foreach (var e in edges)
        {
            if (!_edges.TryAdd(e.Id, e)) continue;
            edgeList.Add(e);

            if (!_nodes.ContainsKey(e.Source) || !_nodes.ContainsKey(e.Target))
            {
                dangling.Add(e);
                continue;
            }

            Add(_outgoing, e.Source, e);
            Add(_incoming, e.Target, e);
        }
        Edges = edgeList;
        DanglingEdges = dangling;

        static void Add(Dictionary<string, List<BlowEdge>> map, string key, BlowEdge edge)
        {
            if (!map.TryGetValue(key, out var list)) map[key] = list = [];
            list.Add(edge);
        }
    }

    /// <summary>The indexed nodes, in their original order, without later duplicates of an id.</summary>
    public IReadOnlyList<BlowNode> Nodes { get; }

    /// <summary>The indexed edges, in their original order, without later duplicates of an id.</summary>
    public IReadOnlyList<BlowEdge> Edges { get; }

    /// <summary>The edges whose source or target names a node that is not in the graph.</summary>
    public IReadOnlyList<BlowEdge> DanglingEdges { get; }

    /// <summary>The node with the given id, or null.</summary>
    public BlowNode? GetNode(string id) => _nodes.GetValueOrDefault(id);

    /// <summary>The edge with the given id, or null.</summary>
    public BlowEdge? GetEdge(string id) => _edges.GetValueOrDefault(id);

    /// <summary>True when a node with the given id is in the graph.</summary>
    public bool ContainsNode(string id) => _nodes.ContainsKey(id);

    /// <summary>The edges leaving the node, in edge-list order.</summary>
    public IReadOnlyList<BlowEdge> Outgoing(string nodeId) => _outgoing.TryGetValue(nodeId, out var list) ? list : NoEdges;

    /// <summary>The edges arriving at the node, in edge-list order.</summary>
    public IReadOnlyList<BlowEdge> Incoming(string nodeId) => _incoming.TryGetValue(nodeId, out var list) ? list : NoEdges;

    /// <summary>
    /// The edges attached to one handle of a node: leaving it for a source handle, arriving at it for a target.
    /// A missing handle id and an empty one name the same, unnamed handle.
    /// </summary>
    public IEnumerable<BlowEdge> EdgesAt(string nodeId, BlowHandleType type, string? handleId)
    {
        var edges = type == BlowHandleType.Source ? Outgoing(nodeId) : Incoming(nodeId);
        foreach (var e in edges)
        {
            var at = type == BlowHandleType.Source ? e.SourceHandle : e.TargetHandle;
            if (SameHandle(at, handleId)) yield return e;
        }
    }

    /// <summary>How many edges are attached to one handle of a node.</summary>
    public int CountAt(string nodeId, BlowHandleType type, string? handleId)
    {
        var count = 0;
        foreach (var _ in EdgesAt(nodeId, type, handleId)) count++;
        return count;
    }

    /// <summary>The nodes with an edge into the given node, each once, in edge-list order.</summary>
    public IReadOnlyList<BlowNode> Predecessors(string nodeId) => Distinct(Incoming(nodeId), e => e.Source);

    /// <summary>The nodes the given node has an edge into, each once, in edge-list order.</summary>
    public IReadOnlyList<BlowNode> Successors(string nodeId) => Distinct(Outgoing(nodeId), e => e.Target);

    private List<BlowNode> Distinct(IReadOnlyList<BlowEdge> edges, Func<BlowEdge, string> end)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<BlowNode>();
        foreach (var e in edges)
        {
            var id = end(e);
            if (seen.Add(id) && _nodes.TryGetValue(id, out var node)) result.Add(node);
        }
        return result;
    }

    internal static bool SameHandle(string? a, string? b)
        => string.IsNullOrEmpty(a) ? string.IsNullOrEmpty(b) : a == b;
}
