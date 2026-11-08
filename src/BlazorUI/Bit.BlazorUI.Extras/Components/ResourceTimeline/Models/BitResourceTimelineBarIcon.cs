namespace Bit.BlazorUI;

/// <summary>
/// A small custom icon or image rendered next to a <see cref="BitResourceTimelineAllocation"/> bar.
/// Multiple icons can be attached to a single bar and placed at different
/// positions. Icons are purely decorative (not selectable). Each icon is
/// scaled to fit inside a square box (see <see cref="Size"/>) while preserving
/// its aspect ratio.
/// </summary>
public class BitResourceTimelineBarIcon
{
    /// <summary>
    /// Image source: an absolute or app-relative URL (for example
    /// <c>"/icons/warning.png"</c>) or a data URI (for example
    /// <c>"data:image/svg+xml;base64,…"</c>). Required.
    /// </summary>
    public required string Source { get; set; }

    /// <summary>Where the icon is anchored relative to the bar. Defaults to <see cref="BitResourceTimelineBarIconPosition.Start"/>.</summary>
    public BitResourceTimelineBarIconPosition Position { get; set; } = BitResourceTimelineBarIconPosition.Start;

    /// <summary>
    /// Draws the icon within the bar instead of outside it, with
    /// <see cref="Position"/> aligning it against the matching edge:
    /// <c>Start</c>/<c>End</c> just inside the start/end edge, <c>Above</c>/<c>Below</c>
    /// just inside the top/bottom edge. Has no effect on
    /// <see cref="BitResourceTimelineBarIconPosition.Center"/>, which always draws
    /// inside. An icon larger than the bar overhangs it, so give bars carrying one a
    /// <see cref="BitResourceTimelineAllocation.Height"/> that fits.
    /// </summary>
    public bool Inside { get; set; }

    /// <summary>
    /// Length of the square box (in pixels) the icon is drawn within. The image
    /// is scaled to fit while keeping its aspect ratio. Falls back to the
    /// renderer's default icon size when null or non-positive.
    /// </summary>
    public int? Size { get; set; }
}
