namespace Bit.Bmotion;

/// <summary>DOM bounding rect returned by <c>getBoundingRect</c> in JS.</summary>
public sealed class BmotionBoundingRect
{
    public double X      { get; set; }
    public double Y      { get; set; }
    public double Width  { get; set; }
    public double Height { get; set; }
    public double Top    { get; set; }
    public double Left   { get; set; }
}

/// <summary>
/// How a layout (FLIP) measurement should be taken, so a snapshot and the measurement it is
/// compared against describe the same coordinate space. See <c>Bmotion.LayoutScroll</c> and
/// <c>Bmotion.LayoutRoot</c>.
/// </summary>
/// <param name="TrackScroll">
/// Add the nearest scrollable ancestor's scroll offset, so scrolling that container between the
/// two measurements isn't read as the element having moved.
/// </param>
/// <param name="FixedRoot">
/// Measure in viewport coordinates rather than document coordinates - correct for a
/// <c>position: fixed</c> element, which stays put while the page scrolls underneath it.
/// </param>
public readonly record struct BmotionMeasureOptions(bool TrackScroll, bool FixedRoot)
{
    /// <summary>Document-relative measurement with no container tracking (the default).</summary>
    public static BmotionMeasureOptions Default => default;

    /// <summary>Whether this is the default measurement, so callers can skip sending options at all.</summary>
    internal bool IsDefault => !TrackScroll && !FixedRoot;
}
