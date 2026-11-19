namespace Bit.Blow;

/// <summary>
/// How box selection decides whether a node is selected.
/// </summary>
public enum BlowSelectionMode
{
    /// <summary>A node is selected if the selection box overlaps it at all.</summary>
    Partial,

    /// <summary>A node is selected only if it is fully enclosed by the selection box.</summary>
    Full
}
