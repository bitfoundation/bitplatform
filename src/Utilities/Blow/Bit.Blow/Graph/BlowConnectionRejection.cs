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
