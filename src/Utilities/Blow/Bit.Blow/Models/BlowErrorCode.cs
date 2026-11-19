namespace Bit.Blow;

/// <summary>The <see cref="BlowError.Code"/> values the canvas raises.</summary>
public static class BlowErrorCode
{
    /// <summary>Two or more nodes share an id; only the first one is reachable.</summary>
    public const string DuplicateNodeId = "duplicate-node-id";

    /// <summary>Two or more edges share an id.</summary>
    public const string DuplicateEdgeId = "duplicate-edge-id";

    /// <summary>An edge names a source or target node that is not in the node list.</summary>
    public const string MissingEdgeEndpoint = "missing-edge-endpoint";

    /// <summary>A node names a parent that is not in the node list, so it is laid out as a root.</summary>
    public const string MissingParentNode = "missing-parent-node";

    /// <summary>
    /// Following a node's parents leads back to it (a node can even name itself). The cycle is broken where it was
    /// found: the node whose parent closes it is laid out as a root.
    /// </summary>
    public const string ParentCycle = "parent-cycle";

    /// <summary>
    /// A node carries two handles of the same type sharing an id (or both without one), so an
    /// edge naming that handle cannot say which it means.
    /// </summary>
    public const string DuplicateHandleId = "duplicate-handle-id";
}
