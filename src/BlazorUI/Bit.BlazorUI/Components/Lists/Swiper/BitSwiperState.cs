namespace Bit.BlazorUI;

/// <summary>
/// Where a <see cref="BitSwiper"/> stands, as reported from the browser whenever it actually changes.
/// </summary>
/// <remarks>
/// Everything about the geometry of the swiper is measured in the browser, since the sizes of the items
/// (and of the box they scroll in) only exist there. The swiper is told about it once per change rather
/// than once per scroll event, so dragging it across a screenful costs a handful of round trips instead
/// of one per frame.
/// </remarks>
public class BitSwiperState
{
    /// <summary>
    /// The zero based index of the item the swiper is currently standing on.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// The zero based index of the screenful (page) the swiper is currently showing.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// The number of screenfuls (pages) the items of the swiper take up.
    /// </summary>
    public int PagesCount { get; set; }

    /// <summary>
    /// Whether the swiper is scrolled all the way to its start.
    /// </summary>
    public bool AtStart { get; set; }

    /// <summary>
    /// Whether the swiper is scrolled all the way to its end.
    /// </summary>
    public bool AtEnd { get; set; }

    /// <summary>
    /// Whether the items of the swiper take up more room than the swiper itself, so there is anything to scroll at all.
    /// </summary>
    public bool Scrollable { get; set; }

    /// <summary>
    /// The size (in pixels) of the box the items scroll in, along the axis the swiper scrolls on.
    /// </summary>
    public double Viewport { get; set; }
}
