namespace Bit.Blow;

/// <summary>Which edges a traversal follows from a node.</summary>
public enum BlowTraversalDirection
{
    /// <summary>Along edges leaving the node, towards its targets.</summary>
    Outgoing,

    /// <summary>Against edges arriving at the node, towards its sources.</summary>
    Incoming,

    /// <summary>Along every edge attached to the node, ignoring direction.</summary>
    Both,
}
