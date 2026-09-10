namespace Bit.BlazorUI;

/// <summary>Where a link opens, i.e. the <c>target</c> attribute it is rendered with.</summary>
public enum BitMarkdownLinkTarget
{
    /// <summary>No <c>target</c> at all: the link opens in the same browsing context.</summary>
    Self,

    /// <summary>Opens in a new tab or window (<c>_blank</c>).</summary>
    Blank,

    /// <summary>Opens in the parent browsing context (<c>_parent</c>).</summary>
    Parent,

    /// <summary>Opens in the topmost browsing context (<c>_top</c>).</summary>
    Top
}
