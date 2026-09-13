namespace Bit.BlazorUI;

/// <summary>
/// Everything the browser side of a <see cref="BitSwiper"/> is driven with.
/// </summary>
/// <remarks>
/// It is handed over as a whole, both at setup and on every change, so the two sides can never end up
/// disagreeing about one of these while agreeing about the rest.
/// </remarks>
internal class BitSwiperOptions
{
    /// <summary>
    /// Whether the swiper scrolls up and down instead of left and right.
    /// </summary>
    public bool Vertical { get; set; }

    /// <summary>
    /// Whether dragging the swiper with the mouse is turned off.
    /// </summary>
    public bool NoDrag { get; set; }

    /// <summary>
    /// Whether the swiper navigates with the wheel of the mouse.
    /// </summary>
    public bool Wheel { get; set; }

    /// <summary>
    /// Whether the swiper responds to anything at all.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Whether the swiper settles on an item instead of stopping wherever the scrolling ran out.
    /// </summary>
    public bool Snap { get; set; }

    /// <summary>
    /// Where an item comes to rest within the swiper: 0 at its start, 0.5 in its middle, 1 at its end.
    /// </summary>
    public double Align { get; set; }

    /// <summary>
    /// The duration (in seconds) of the moves the swiper makes itself.
    /// </summary>
    public double Duration { get; set; }

    /// <summary>
    /// The distance (in pixels) the pointer has to travel before a press turns into a drag.
    /// </summary>
    public int Threshold { get; set; }

    /// <summary>
    /// The number of items a single navigation moves.
    /// </summary>
    public int ScrollCount { get; set; }

    /// <summary>
    /// The zero based index of the item the swiper is laid out on, read only while it is being set up.
    /// </summary>
    public int Start { get; set; }
}
