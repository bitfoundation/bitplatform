
namespace Bit.Bmotion;
/// <summary>
/// Cascaded by <see cref="BmotionConfig"/> to set library-wide defaults.
/// </summary>
public class BmotionConfigContext
{
    /// <summary>Global default transition applied when no individual transition is set.</summary>
    public BmotionTransitionConfig? DefaultTransition { get; set; }

    /// <summary>
    /// When true, all animations are skipped (useful for accessibility / reduced-motion).
    /// If null the library respects the OS prefers-reduced-motion media query automatically.
    /// </summary>
    public bool? ReduceMotion { get; set; }

    /// <summary>
    /// Scale factor applied to all animation durations. 0 = instant, 2 = half speed
    /// (durations are multiplied by this factor). Default: 1.
    /// <para>
    /// Negative and non-finite (NaN/Infinity) values are coerced to <c>0</c> rather than throwing,
    /// so a bad binding can never crash a render. This matches <see cref="BmotionConfig"/>'s behaviour.
    /// </para>
    /// </summary>
    public double TransitionSpeed
    {
        get => _transitionSpeed;
        set => _transitionSpeed = double.IsFinite(value) && value >= 0 ? value : 0;
    }
    private double _transitionSpeed = 1.0;
}
