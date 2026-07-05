
namespace Bit.Bmotion;
/// <summary>
/// Cascaded by <see cref="BmotionConfig"/> to set library-wide defaults.
/// </summary>
internal class BmotionConfigContext
{
    /// <summary>Global default transition applied when no individual transition is set.</summary>
    public BmTransition? DefaultTransition { get; set; }

    /// <summary>
    /// When true, all animations are skipped (useful for accessibility / reduced-motion).
    /// If null the library respects the OS prefers-reduced-motion media query automatically.
    /// </summary>
    public bool? ReduceMotion { get; set; }

    /// <summary>
    /// Playback rate applied to all animations: 2 = twice as fast, 0.5 = half speed,
    /// 0 = instant (durations are divided by this rate). Default: 1.
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
