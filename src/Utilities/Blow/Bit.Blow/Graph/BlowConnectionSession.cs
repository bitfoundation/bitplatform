namespace Bit.Blow;

/// <summary>
/// The connection rules bound to one starting handle, ready to check any number of candidate handles cheaply.
/// Built by <see cref="BlowConnectionRules.Begin"/>; a snapshot of the graph at that moment.
/// </summary>
public sealed class BlowConnectionSession
{
    private readonly BlowGraphIndex _graph;
    private readonly BlowConnectionPolicy _policy;
    private readonly BlowEdge? _reconnecting;
    private readonly BlowNode? _from;
    private readonly HashSet<string> _existing = new(StringComparer.Ordinal);
    private readonly BlowHandleCounts _counts;
    private HashSet<string>? _cycleClosers;

    internal BlowConnectionSession(BlowGraphIndex graph, string nodeId, string? handleId, BlowHandleType handleType,
        BlowConnectionPolicy policy, BlowEdge? reconnecting)
    {
        _graph = graph;
        _policy = policy;
        _reconnecting = reconnecting;
        _from = graph.GetNode(nodeId);
        FromNodeId = nodeId;
        FromHandleId = string.IsNullOrEmpty(handleId) ? null : handleId;
        FromType = handleType;

        foreach (var e in graph.Edges)
        {
            if (!ReferenceEquals(e, reconnecting)) _existing.Add(Key(e.Source, e.SourceHandle, e.Target, e.TargetHandle));
        }
        _counts = new BlowHandleCounts(graph.Edges, except: reconnecting);
    }

    /// <summary>The node the connection starts from.</summary>
    public string FromNodeId { get; }

    /// <summary>The id of the handle the connection starts from, or null for the node's unnamed handle.</summary>
    public string? FromHandleId { get; }

    /// <summary>Whether the starting handle is a source or a target.</summary>
    public BlowHandleType FromType { get; }

    /// <summary>
    /// Whether anything at all can be connected from the starting handle: its node exists, takes connections,
    /// and its port is not already at its limit. A handle that cannot start is drawn as such and ignores presses.
    /// </summary>
    public bool CanStart
    {
        get
        {
            if (_from is null || !_from.Connectable || !IsShown(_from)) return false;
            return !IsFull(_from, FromType, FromHandleId);
        }
    }

    /// <summary>
    /// Whether a node is on show: neither it nor any node it is nested in is hidden. A node inside a hidden or collapsed
    /// one is out of sight, and takes no connections any more than a hidden one does.
    /// </summary>
    private bool IsShown(BlowNode node) => BlowEdgeLayout.IsShown(node, _graph.GetNode);

    /// <summary>
    /// The connection completing on the given handle would make, normalised so <c>Source</c> is the source end.
    /// </summary>
    public BlowConnection Describe(string nodeId, string? handleId, BlowHandleType handleType)
        => FromType == BlowHandleType.Source
            ? new BlowConnection(FromNodeId, nodeId, FromHandleId, handleId)
            : new BlowConnection(nodeId, FromNodeId, handleId, FromHandleId);

    /// <summary>Whether a connection from the starting handle to the given one is allowed, and if not, why.</summary>
    public BlowConnectionCheck Check(string nodeId, string? handleId, BlowHandleType handleType)
    {
        handleId = string.IsNullOrEmpty(handleId) ? null : handleId;

        if (_from is null || _graph.GetNode(nodeId) is not { } to) return Reject(BlowConnectionRejection.MissingNode);

        if (!_from.Connectable || !IsShown(_from) || !to.Connectable || !IsShown(to))
            return Reject(BlowConnectionRejection.NotConnectable);

        if (_policy.Mode == BlowConnectionMode.Strict && handleType == FromType)
            return Reject(BlowConnectionRejection.SameHandleType);

        var self = ReferenceEquals(to, _from);
        if (self && (!_policy.AllowSelfConnections || (handleType == FromType && handleId == FromHandleId)))
            return Reject(BlowConnectionRejection.SelfConnection);

        var connection = Describe(nodeId, handleId, handleType);
        if (_policy.PreventDuplicates
            && _existing.Contains(Key(connection.Source!, connection.SourceHandle, connection.Target!, connection.TargetHandle)))
        {
            return Reject(BlowConnectionRejection.Duplicate);
        }

        // The ports, each looked up as the handle it actually is - in loose mode a source handle can end an edge.
        var fromPort = ResolvePort(_from, FromType, FromHandleId);
        var toPort = ResolvePort(to, handleType, handleId);
        var (sourcePort, targetPort) = FromType == BlowHandleType.Source ? (fromPort, toPort) : (toPort, fromPort);

        var compatible = _policy.TypeCompatibility ?? BlowConnectionRules.AreTypesCompatible;
        if (!compatible(sourcePort?.DataType, targetPort?.DataType))
            return Reject(BlowConnectionRejection.IncompatibleTypes);

        var (sourceNode, sourceType, sourceHandle, targetNode, targetType, targetHandle) = FromType == BlowHandleType.Source
            ? (_from, FromType, FromHandleId, to, handleType, handleId)
            : (to, handleType, handleId, _from, FromType, FromHandleId);
        if (IsFull(sourceNode, sourceType, sourceHandle, sourcePort)) return Reject(BlowConnectionRejection.SourceFull);
        if (IsFull(targetNode, targetType, targetHandle, targetPort)) return Reject(BlowConnectionRejection.TargetFull);

        if (_policy.PreventCycles && (self || CycleClosers().Contains(to.Id)))
            return Reject(BlowConnectionRejection.Cycle);

        if (_policy.IsValidConnection is { } rule && !rule(connection))
            return Reject(BlowConnectionRejection.RejectedByRule);

        return BlowConnectionCheck.Valid;
    }

    private static BlowConnectionCheck Reject(BlowConnectionRejection reason) => new(reason);

    private BlowPort? ResolvePort(BlowNode node, BlowHandleType type, string? handleId)
        => _policy.ResolvePort is { } resolve ? resolve(node, type, handleId) : node.FindPort(type, handleId);

    private bool IsFull(BlowNode node, BlowHandleType type, string? handleId)
        => IsFull(node, type, handleId, ResolvePort(node, type, handleId));

    private bool IsFull(BlowNode node, BlowHandleType type, string? handleId, BlowPort? port)
        => port?.MaxConnections is { } max && _counts.At(node.Id, type, handleId, _policy.Mode) >= max;

    /// <summary>
    /// The nodes that, joined to the starting handle, would close a cycle: everything upstream of it when it is the
    /// source end of the new edge (an edge from it back into its own ancestry), everything downstream when it is
    /// the target end. Worked out once, on first need.
    /// </summary>
    private HashSet<string> CycleClosers()
    {
        if (_cycleClosers is not null) return _cycleClosers;

        var closers = new HashSet<string>(StringComparer.Ordinal) { FromNodeId };
        var queue = new Queue<string>();
        queue.Enqueue(FromNodeId);
        var upstream = FromType == BlowHandleType.Source;
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            foreach (var e in upstream ? _graph.Incoming(id) : _graph.Outgoing(id))
            {
                if (ReferenceEquals(e, _reconnecting)) continue;
                var next = upstream ? e.Source : e.Target;
                if (closers.Add(next)) queue.Enqueue(next);
            }
        }
        return _cycleClosers = closers;
    }

    // A unit separator keeps "a" + "b.c" from colliding with "a.b" + "c".
    private static string Key(string source, string? sourceHandle, string target, string? targetHandle)
        => $"{source}\u001f{Normalize(sourceHandle)}\u001f{target}\u001f{Normalize(targetHandle)}";

    private static string Normalize(string? handleId) => handleId ?? string.Empty;
}
