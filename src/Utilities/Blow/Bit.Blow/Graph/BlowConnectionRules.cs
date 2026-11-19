namespace Bit.Blow;

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
