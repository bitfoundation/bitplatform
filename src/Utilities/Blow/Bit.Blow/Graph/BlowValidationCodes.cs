namespace Bit.Blow;

/// <summary>The <see cref="BlowValidationIssue.Code"/> values of the built-in rules.</summary>
public static class BlowValidationCodes
{
    /// <summary>Two or more nodes share an id.</summary>
    public const string DuplicateNodeId = BlowErrorCode.DuplicateNodeId;

    /// <summary>Two or more edges share an id.</summary>
    public const string DuplicateEdgeId = BlowErrorCode.DuplicateEdgeId;

    /// <summary>An edge names a source or target node that is not in the graph.</summary>
    public const string MissingEdgeEndpoint = BlowErrorCode.MissingEdgeEndpoint;

    /// <summary>A node names a parent that is not in the graph.</summary>
    public const string MissingParentNode = BlowErrorCode.MissingParentNode;

    /// <summary>Following parents from a node leads back to it.</summary>
    public const string ParentCycle = BlowErrorCode.ParentCycle;

    /// <summary>An edge names a handle that its node, which declares ports, does not declare.</summary>
    public const string UnknownPort = "unknown-port";

    /// <summary>A port has fewer edges than its <c>MinConnections</c>.</summary>
    public const string TooFewConnections = "too-few-connections";

    /// <summary>A port has more edges than its <c>MaxConnections</c>.</summary>
    public const string TooManyConnections = "too-many-connections";

    /// <summary>An edge joins two ports whose data types are not compatible.</summary>
    public const string IncompatibleTypes = "incompatible-types";

    /// <summary>The graph has a cycle where it must not.</summary>
    public const string Cycle = "cycle";

    /// <summary>A node has no edges at all.</summary>
    public const string IsolatedNode = "isolated-node";

    /// <summary>A node cannot be reached from any start node.</summary>
    public const string UnreachableNode = "unreachable-node";

    /// <summary>A node's type is not registered (<see cref="BlowNodeRegistry.ValidationRule"/>).</summary>
    public const string UnknownNodeType = "unknown-node-type";

    /// <summary>A node's type is deprecated (<see cref="BlowNodeDefinition.Deprecated"/>).</summary>
    public const string DeprecatedNodeType = "deprecated-node-type";
}
