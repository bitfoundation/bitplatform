namespace Bit.Blow;

/// <summary>
/// Where a <c>BlowNodeToolbar</c> sits along the side of the node it is anchored to.
/// </summary>
public enum BlowToolbarAlign
{
    /// <summary>Centred on the side (the default).</summary>
    Center,

    /// <summary>Flush with the leading corner: the left edge for a top/bottom toolbar, the top edge for a left/right one.</summary>
    Start,

    /// <summary>Flush with the trailing corner.</summary>
    End,
}
