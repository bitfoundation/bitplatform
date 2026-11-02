namespace Bit.Blow;

/// <summary>Why a prospective connection was refused. <see cref="None"/> means it was not.</summary>
public enum BlowConnectionRejection
{
    /// <summary>The connection is allowed.</summary>
    None,

    /// <summary>One of the two nodes is not in the graph.</summary>
    MissingNode,

    /// <summary>One of the two nodes does not take connections: its <c>Connectable</c> is off, or it is hidden.</summary>
    NotConnectable,

    /// <summary>In strict mode, both handles are sources or both are targets.</summary>
    SameHandleType,

    /// <summary>The connection would join a node to itself, which is not allowed - or would join a handle to itself.</summary>
    SelfConnection,

    /// <summary>An edge with the same source, target and handles already exists.</summary>
    Duplicate,

    /// <summary>The two ports carry data types that are not compatible.</summary>
    IncompatibleTypes,

    /// <summary>The source end's port already has as many edges as its <c>MaxConnections</c> allows.</summary>
    SourceFull,

    /// <summary>The target end's port already has as many edges as its <c>MaxConnections</c> allows.</summary>
    TargetFull,

    /// <summary>The connection would close a cycle, and cycles are prevented.</summary>
    Cycle,

    /// <summary>An application rule - the canvas's or a handle's <c>IsValidConnection</c> - returned false.</summary>
    RejectedByRule,

    /// <summary>
    /// The connection kept changing while <c>OnBeforeConnect</c> was deciding on it - the end of a reconnected edge that
    /// stays put moved again and again - so it was dropped without an answer that covers the wiring it would make.
    /// </summary>
    Unsettled,
}

/// <summary>The verdict on one prospective connection.</summary>
/// <param name="Rejection">Why it was refused, or <see cref="BlowConnectionRejection.None"/>.</param>
public readonly record struct BlowConnectionCheck(BlowConnectionRejection Rejection)
{
    /// <summary>The verdict for an allowed connection.</summary>
    public static readonly BlowConnectionCheck Valid = new(BlowConnectionRejection.None);

    /// <summary>True when the connection is allowed.</summary>
    public bool IsValid => Rejection == BlowConnectionRejection.None;
}

/// <summary>The rules a connection has to pass. The canvas builds one from its own parameters.</summary>
public sealed class BlowConnectionPolicy
{
    /// <summary>Whether only a source may join a target (<see cref="BlowConnectionMode.Strict"/>, the default) or any handle any other.</summary>
    public BlowConnectionMode Mode { get; init; } = BlowConnectionMode.Strict;

    /// <summary>Whether a node may be joined to itself, through two different handles. Off by default.</summary>
    public bool AllowSelfConnections { get; init; }

    /// <summary>Whether a second edge with the same source, target and handles is refused. On by default.</summary>
    public bool PreventDuplicates { get; init; } = true;

    /// <summary>
    /// Whether a connection that would close a directed cycle is refused - for pipelines, dependency graphs and
    /// anything else that has to stay acyclic. A self-connection is a cycle. Off by default.
    /// </summary>
    public bool PreventCycles { get; init; }

    /// <summary>
    /// Decides whether data of the source port's <see cref="BlowPort.DataType"/> may flow into the target port's.
    /// Null uses <see cref="BlowConnectionRules.AreTypesCompatible"/>.
    /// </summary>
    public Func<string?, string?, bool>? TypeCompatibility { get; init; }

    /// <summary>An application rule over the connection that would be made; false refuses it.</summary>
    public Func<BlowConnection, bool>? IsValidConnection { get; init; }

    /// <summary>
    /// Finds the port standing for a handle, given its node, the handle type and the handle id. Null uses the
    /// node's declared <see cref="BlowNode.Ports"/>.
    /// </summary>
    public Func<BlowNode, BlowHandleType, string?, BlowPort?>? ResolvePort { get; init; }

    /// <summary>The policy with every default: strict, no self-connections, no duplicates, cycles allowed.</summary>
    public static readonly BlowConnectionPolicy Default = new();
}

/// <summary>
/// The rules deciding whether two handles may be connected, in one place and with no UI: the canvas consults
/// them on every frame of a connection drag, and an application can call them to validate a connection it is
/// about to make from code, on a server, or in a test.
/// </summary>
/// <remarks>
/// <para>
/// The checks run in a fixed order, and the first that fails names the <see cref="BlowConnectionRejection"/>:
/// missing node, connectable, handle types (strict mode), self-connection, duplicate, data types, the source's
/// limit, the target's limit, cycles, and finally the application's own rule - so the most fundamental reason is
/// always the one reported, and the application's rule only ever sees connections that are otherwise allowed.
/// </para>
/// <para>
/// A drag offers one starting handle to many candidates, so <see cref="Begin"/> does the work that depends only on
/// the starting handle once - the existing wiring, the connection counts, the nodes a cycle would come back
/// through - and each <see cref="BlowConnectionSession.Check"/> after that costs a handful of lookups.
/// </para>
/// </remarks>
public static class BlowConnectionRules
{
    /// <summary>
    /// Checks one explicit connection from a source handle to a target handle against the graph it would join.
    /// </summary>
    public static BlowConnectionCheck Check(
        IEnumerable<BlowNode> nodes, IEnumerable<BlowEdge> edges, BlowConnection connection, BlowConnectionPolicy? policy = null)
        => Check(new BlowGraphIndex(nodes, edges), connection, policy);

    /// <inheritdoc cref="Check(IEnumerable{BlowNode}, IEnumerable{BlowEdge}, BlowConnection, BlowConnectionPolicy?)"/>
    public static BlowConnectionCheck Check(BlowGraphIndex graph, BlowConnection connection, BlowConnectionPolicy? policy = null)
    {
        if (connection.Source is null || connection.Target is null) return new(BlowConnectionRejection.MissingNode);

        return Begin(graph, connection.Source, connection.SourceHandle, BlowHandleType.Source, policy)
            .Check(connection.Target, connection.TargetHandle, BlowHandleType.Target);
    }

    /// <summary>
    /// Prepares to check connections from one starting handle - the handle a drag begins on, or the fixed end of an
    /// edge being reconnected.
    /// </summary>
    /// <param name="graph">The graph the connection would join.</param>
    /// <param name="nodeId">The node of the starting handle.</param>
    /// <param name="handleId">The starting handle's id, or null for the node's unnamed handle of its type.</param>
    /// <param name="handleType">Whether the starting handle is a source or a target.</param>
    /// <param name="policy">The rules; null for <see cref="BlowConnectionPolicy.Default"/>.</param>
    /// <param name="reconnecting">
    /// An edge being moved to a new handle. It is left out of the duplicate check, the connection counts and the
    /// cycle check, since it is the edge the connection replaces.
    /// </param>
    public static BlowConnectionSession Begin(BlowGraphIndex graph, string nodeId, string? handleId,
        BlowHandleType handleType, BlowConnectionPolicy? policy = null, BlowEdge? reconnecting = null)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(nodeId);
        return new BlowConnectionSession(graph, nodeId, handleId, handleType, policy ?? BlowConnectionPolicy.Default, reconnecting);
    }

    /// <summary>
    /// The built-in data-type rule: data may flow between two ports when either declares no type (null, empty or
    /// <c>"*"</c>), or when the comma-separated type lists share a type, ignoring case. So <c>"number"</c> joins
    /// <c>"Number"</c> and <c>"int,number"</c>, and <c>"*"</c> joins anything.
    /// </summary>
    public static bool AreTypesCompatible(string? sourceType, string? targetType)
    {
        if (IsAny(sourceType) || IsAny(targetType)) return true;

        foreach (var s in Split(sourceType!))
        {
            foreach (var t in Split(targetType!))
            {
                if (string.Equals(s, t, StringComparison.OrdinalIgnoreCase)) return true;
            }
        }
        return false;

        static bool IsAny(string? type) => string.IsNullOrWhiteSpace(type) || type.Trim() == "*";

        static IEnumerable<string> Split(string types)
            => types.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}

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
            if (_from is null || !_from.Connectable || _from.Hidden) return false;
            return !IsFull(_from, FromType, FromHandleId);
        }
    }

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

        if (!_from.Connectable || _from.Hidden || !to.Connectable || to.Hidden)
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
