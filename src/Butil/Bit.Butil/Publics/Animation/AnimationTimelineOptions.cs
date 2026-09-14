namespace Bit.Butil;

/// <summary>
/// Drives an animation from a scroll position instead of from the clock - a
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ScrollTimeline">ScrollTimeline</see>
/// or a <see href="https://developer.mozilla.org/en-US/docs/Web/API/ViewTimeline">ViewTimeline</see>.
/// </summary>
/// <remarks>
/// With one of these in play the animation's progress <b>is</b> the scroll progress, so
/// <see cref="AnimationOptions.Duration"/>, <see cref="AnimationOptions.Delay"/> and
/// <see cref="AnimationOptions.EndDelay"/> stop meaning anything and are not sent - use
/// <see cref="RangeStart"/> and <see cref="RangeEnd"/> to say which part of the scroll the animation
/// occupies.
/// <br/>
/// A reading-progress bar is a scroll timeline; an element that fades in as it comes into view is a
/// view timeline.
/// </remarks>
public class AnimationTimelineOptions
{
    /// <summary>
    /// <c>"scroll"</c> to follow a scroller's position, or <c>"view"</c> to follow an element's
    /// passage through the scrollport.
    /// </summary>
    public string Type { get; set; } = "scroll";

    /// <summary>
    /// Which axis drives it: <c>"block"</c> (the default), <c>"inline"</c>, <c>"x"</c> or <c>"y"</c>.
    /// </summary>
    public string Axis { get; set; } = "block";

    /// <summary>
    /// Where in the scroll range the animation starts, in the CSS syntax of the <c>animation-range</c>
    /// property - <c>"entry 0%"</c>, <c>"cover 20%"</c>, <c>"contain"</c>. Empty means the start of
    /// the range.
    /// </summary>
    public string RangeStart { get; set; } = string.Empty;

    /// <summary>Where the animation ends, same syntax as <see cref="RangeStart"/>. Empty means the end of the range.</summary>
    public string RangeEnd { get; set; } = string.Empty;
}
