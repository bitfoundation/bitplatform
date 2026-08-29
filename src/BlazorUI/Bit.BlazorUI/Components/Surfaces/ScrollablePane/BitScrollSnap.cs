namespace Bit.BlazorUI;

/// <summary>
/// How strictly a <see cref="BitScrollablePane"/> comes to rest on the snap positions of its content.
/// </summary>
/// <remarks>
/// This is the strictness half of the CSS <c>scroll-snap-type</c> property. Snapping only happens where
/// the content says where it may happen, which is what <see cref="BitScrollablePane.SnapAlign"/> - or a
/// <c>scroll-snap-align</c> of the consumer's own - puts on the items.
/// </remarks>
public enum BitScrollSnap
{
    /// <summary>
    /// The pane does not snap, which is the initial value.
    /// </summary>
    None,

    /// <summary>
    /// The pane snaps to a position only when it comes to rest near one, so a scroll can still be left
    /// anywhere between two items.
    /// </summary>
    Proximity,

    /// <summary>
    /// The pane always comes to rest on a snap position, which is what a carousel or a row of pages wants.
    /// </summary>
    Mandatory
}
