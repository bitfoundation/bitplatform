namespace Bit.BlazorUI;

/// <summary>
/// Determines whether scrolling is instant or animates smoothly.
/// </summary>
public enum BitScrollBehavior
{
    /// <summary>
    /// Scrolling should animate smoothly.
    /// </summary>
    Smooth,

    /// <summary>
    /// Scrolling should happen instantly in a single jump.
    /// </summary>
    Instant,

    /// <summary>
    /// Scroll behavior is determined by the computed value of scroll-behavior.
    /// </summary>
    Auto
}
