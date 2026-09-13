namespace Bit.Butil;

/// <summary>
/// Where the element should come to rest inside the scrolling box - along the block axis
/// (vertically, in a horizontal writing mode) or the inline axis.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollIntoView">Element.scrollIntoView()</see>
/// </summary>
public enum ScrollLogicalPosition
{
    /// <summary>Align the element's start edge with the container's start edge. The default.</summary>
    Start,

    /// <summary>Centre the element in the container.</summary>
    Center,

    /// <summary>Align the element's end edge with the container's end edge.</summary>
    End,

    /// <summary>Scroll as little as possible - nothing at all if the element is already in view.</summary>
    Nearest
}
