namespace Bit.Blow;

/// <summary>
/// Whether a handle is a connection source or target (or can be both).
/// </summary>
public enum BlowHandleType
{
    /// <summary>A handle connections are drawn from: the source end of an edge.</summary>
    Source,

    /// <summary>A handle connections are dropped on: the target end of an edge.</summary>
    Target
}
