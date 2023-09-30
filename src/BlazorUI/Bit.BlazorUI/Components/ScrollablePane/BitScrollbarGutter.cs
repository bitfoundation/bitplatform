namespace Bit.BlazorUI;

public enum BitScrollbarGutter
{
    /// <summary>
    /// The initial value. Classic scrollbars create a gutter when overflow is scroll, or when overflow is auto and the box is overflowing. Overlay scrollbars do not consume space.
    /// </summary>
    Auto,

    /// <summary>
    /// When using classic scrollbars, the gutter will be present if overflow is auto, scroll, or hidden even if the box is not overflowing.When using overlay scrollbars, the gutter will not be present.
    /// </summary>
    Stable,

    ///<summary>
    /// If a gutter would be present on one of the inline start/end edges of the box, another will be present on the opposite edge as well.
    /// </summary>
    BothEdges

}
