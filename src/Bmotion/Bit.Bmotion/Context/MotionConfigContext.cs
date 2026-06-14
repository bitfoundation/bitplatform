using Bit.Bmotion.Models;

namespace Bit.Bmotion.Context;

/// <summary>
/// Cascaded by <see cref="Components.MotionConfig"/> to set library-wide defaults.
/// </summary>
public class MotionConfigContext
{
    /// <summary>Global default transition applied when no individual transition is set.</summary>
    public TransitionConfig? DefaultTransition { get; set; }

    /// <summary>
    /// When true, all animations are skipped (useful for accessibility / reduced-motion).
    /// If null the library respects the OS prefers-reduced-motion media query automatically.
    /// </summary>
    public bool? ReduceMotion { get; set; }

    /// <summary>
    /// Scale factor applied to all animation durations. 0 = instant, 2 = half speed
    /// (durations are multiplied by this factor). Default: 1.
    /// </summary>
    public double TransitionSpeed { get; set; } = 1.0;
}
