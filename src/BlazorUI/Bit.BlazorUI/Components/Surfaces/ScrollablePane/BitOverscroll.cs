namespace Bit.BlazorUI;

/// <summary>
/// What the browser does with a scroll that has already reached the edge of a <see cref="BitScrollablePane"/>.
/// </summary>
/// <remarks>
/// This is the CSS <c>overscroll-behavior</c> property. It decides two things at once: whether the scroll
/// carries on into whatever scrolls behind the pane once the pane itself has nowhere left to go (scroll
/// chaining), and whether the platform's own overscroll affordance - the rubber band, the pull to refresh,
/// the navigation swipe - is still offered inside it.
/// </remarks>
public enum BitOverscroll
{
    /// <summary>
    /// The initial value: the scroll carries on into the nearest scrolling ancestor once the pane has
    /// reached its edge, and the platform's own overscroll affordance is kept.
    /// </summary>
    Auto,

    /// <summary>
    /// The scroll stops at the edge of the pane instead of carrying on into the page behind it, while the
    /// platform's own overscroll affordance inside the pane is kept.
    /// </summary>
    Contain,

    /// <summary>
    /// Like <see cref="Contain"/>, and the platform's own overscroll affordance is suppressed as well, so
    /// the pane neither bounces nor triggers a pull to refresh at its edges.
    /// </summary>
    None
}
