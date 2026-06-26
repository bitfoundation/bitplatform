namespace Bit.Bmotion;
/// <summary>Controls how a value transitions from one state to another.</summary>
public class BmotionTransitionConfig
{
    // ── Type ─────────────────────────────────────────────────────────────────
    /// <summary>Animation driver: Tween, Spring, or Inertia. Default: Tween.</summary>
    public BmotionTransitionType Type { get; set; } = BmotionTransitionType.Tween;

    // ── Tween ─────────────────────────────────────────────────────────────────
    /// <summary>Duration in seconds. Default: 0.3.</summary>
    public double Duration { get; set; } = 0.3;

    /// <summary>Delay before animation starts, in seconds. Default: 0.</summary>
    public double Delay { get; set; } = 0;

    /// <summary>Named easing preset. See <see cref="BmotionEasing"/>. Default: EaseOut.</summary>
    public BmotionEasing Ease { get; set; } = BmotionEasing.EaseOut;

    /// <summary>
    /// Custom cubic-bezier as [x1, y1, x2, y2]. Overrides <see cref="Ease"/> when set.
    /// Must be either <c>null</c> or an array of exactly 4 finite values.
    /// </summary>
    public double[]? EaseCubicBezier
    {
        get => _easeCubicBezier is null ? null : (double[])_easeCubicBezier.Clone();
        set
        {
            var validated = ValidateCubicBezier(value);
            _easeCubicBezier = validated is null ? null : (double[])validated.Clone();
        }
    }
    private double[]? _easeCubicBezier;

    private static double[]? ValidateCubicBezier(double[]? value)
    {
        if (value is null) return null;
        if (value.Length != 4 || !value.All(double.IsFinite))
            throw new ArgumentException(
                "EaseCubicBezier must be null or an array of exactly 4 finite values [x1, y1, x2, y2].",
                nameof(value));
        // The control-point X coordinates must stay within [0, 1] so the bezier's x(t) curve is
        // monotonic. Outside that range x(t) can fold back on itself, and the Newton-Raphson solver
        // in BmotionEasingFunctions can then converge to the wrong root (a visibly broken easing).
        if (value[0] is < 0 or > 1 || value[2] is < 0 or > 1)
            throw new ArgumentException(
                "EaseCubicBezier X coordinates (x1, x2) must be within [0, 1]; only Y may overshoot.",
                nameof(value));
        return value;
    }

    // ── Repeat ────────────────────────────────────────────────────────────────
    /// <summary>
    /// Number of times to repeat. Setting <see cref="RepeatInfinite"/> (preferred) or the legacy
    /// sentinel <c>int.MaxValue</c> makes the animation repeat forever.
    /// </summary>
    public int Repeat { get; set; } = 0;

    /// <summary>
    /// When <c>true</c> the animation repeats forever, regardless of <see cref="Repeat"/>.
    /// Prefer this over the legacy <c>Repeat = int.MaxValue</c> sentinel.
    /// </summary>
    public bool RepeatInfinite { get; set; }

    /// <summary>True when this transition repeats forever (via flag or legacy sentinel).</summary>
    internal bool IsInfiniteRepeat => RepeatInfinite || Repeat == int.MaxValue;

    /// <summary>How to repeat: Loop, Mirror (ping-pong), or Reverse.</summary>
    public BmotionRepeatType RepeatType { get; set; } = BmotionRepeatType.Loop;

    /// <summary>Delay between repetitions, in seconds.</summary>
    public double RepeatDelay { get; set; } = 0;

    // ── Keyframes ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Progress offsets (0–1) for each keyframe value. Length must match value array.
    /// If omitted the frames are evenly distributed. When set, values must be finite, within
    /// <c>[0, 1]</c> and in monotonically ascending order.
    /// </summary>
    public double[]? Times
    {
        get => _times is null ? null : (double[])_times.Clone();
        set
        {
            var validated = ValidateTimes(value);
            _times = validated is null ? null : (double[])validated.Clone();
        }
    }
    private double[]? _times;

    private static double[]? ValidateTimes(double[]? value)
    {
        if (value is null) return null;
        // These feed the keyframe interpolation segment math; non-finite, out-of-range or
        // non-monotonic offsets produce negative/zero segment lengths and NaN output, so reject
        // them up front (matches the validation in the keyframe drivers).
        for (int i = 0; i < value.Length; i++)
        {
            if (!double.IsFinite(value[i]))
                throw new ArgumentException("Times values must be finite.", nameof(value));
            if (value[i] < 0 || value[i] > 1)
                throw new ArgumentException("Times values must be within the range [0, 1].", nameof(value));
            if (i > 0 && value[i] < value[i - 1])
                throw new ArgumentException("Times values must be in monotonically ascending order.", nameof(value));
        }
        return value;
    }

    // ── Spring ────────────────────────────────────────────────────────────────
    /// <summary>Spring stiffness (N/m). Higher = snappier. Default: 100.</summary>
    public double Stiffness { get; set; } = 100;

    /// <summary>Damping coefficient. Higher = less oscillation. Default: 10.</summary>
    public double Damping { get; set; } = 10;

    /// <summary>Virtual mass. Higher = slower acceleration. Default: 1.</summary>
    public double Mass { get; set; } = 1;

    /// <summary>Initial velocity for the spring (units/s). Default: 0.</summary>
    public double Velocity { get; set; } = 0;

    /// <summary>Minimum speed (units/s) considered at rest. Default: 0.01.</summary>
    public double RestSpeed { get; set; } = 0.01;

    /// <summary>Minimum distance from target considered at rest. Default: 0.01.</summary>
    public double RestDelta { get; set; } = 0.01;

    /// <summary>
    /// Bounciness of a duration-based spring (0 = critically damped, 1 = very bouncy).
    /// When set together with <see cref="Duration"/> or <see cref="VisualDuration"/>,
    /// stiffness and damping are derived automatically (overriding their values).
    /// </summary>
    public double? Bounce { get; set; }

    /// <summary>
    /// The visual time (in seconds) the spring will take to appear to reach its target.
    /// Works together with <see cref="Bounce"/> for intuitive spring configuration.
    /// Overrides <see cref="Duration"/> when computing spring parameters.
    /// </summary>
    public double? VisualDuration { get; set; }

    // ── Inertia ───────────────────────────────────────────────────────────────
    /// <summary>Velocity at the start of deceleration. Default: 0.</summary>
    public double InertiaVelocity { get; set; } = 0;

    /// <summary>Exponential decay time constant in ms. Default: 700.</summary>
    public double TimeConstant { get; set; } = 700;

    /// <summary>Multiplier for the projected distance. Default: 0.8.</summary>
    public double Power { get; set; } = 0.8;

    /// <summary>Minimum distance from target that counts as at rest. Default: 0.5.</summary>
    public double InertiaRestDelta { get; set; } = 0.5;

    /// <summary>Optional lower bound for the inertia target.</summary>
    public double? InertiaMin { get; set; }

    /// <summary>Optional upper bound for the inertia target.</summary>
    public double? InertiaMax { get; set; }

    // ── Orchestration (for Variants) ──────────────────────────────────────────
    /// <summary>
    /// Seconds to stagger each child's animation start. Works in Variant transitions.
    /// </summary>
    public double? StaggerChildren { get; set; }

    /// <summary>Seconds to delay the first child's animation start.</summary>
    public double? DelayChildren { get; set; }

    // ── Per-property overrides ────────────────────────────────────────────────
    /// <summary>
    /// Override transition for specific properties, e.g.
    /// <c>Properties = new { ["opacity"] = new BmotionTransitionConfig { Duration = 0.1 } }</c>
    /// </summary>
    public Dictionary<string, BmotionTransitionConfig>? Properties { get; set; }

    /// <summary>
    /// Called on every animation frame with the latest interpolated value.
    /// Supported for single-value numeric animations.
    /// </summary>
    public Action<double>? OnUpdate { get; set; }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a deep copy of this configuration. Used internally when the library
    /// needs to derive a variant of a transition (e.g. applying a global
    /// <see cref="BmotionConfigContext.TransitionSpeed"/> scale or a stagger delay)
    /// without mutating or partially losing the original's fields.
    /// </summary>
    public BmotionTransitionConfig Clone() => new()
    {
        Type = Type,
        Duration = Duration,
        Delay = Delay,
        Ease = Ease,
        // Read the backing field directly: the EaseCubicBezier getter already returns a defensive
        // clone, so cloning it again here would allocate a redundant second copy.
        EaseCubicBezier = _easeCubicBezier is null ? null : (double[])_easeCubicBezier.Clone(),
        Repeat = Repeat,
        RepeatInfinite = RepeatInfinite,
        RepeatType = RepeatType,
        RepeatDelay = RepeatDelay,
        Times = _times is null ? null : (double[])_times.Clone(),
        Stiffness = Stiffness,
        Damping = Damping,
        Mass = Mass,
        Velocity = Velocity,
        RestSpeed = RestSpeed,
        RestDelta = RestDelta,
        Bounce = Bounce,
        VisualDuration = VisualDuration,
        InertiaVelocity = InertiaVelocity,
        TimeConstant = TimeConstant,
        Power = Power,
        InertiaRestDelta = InertiaRestDelta,
        InertiaMin = InertiaMin,
        InertiaMax = InertiaMax,
        StaggerChildren = StaggerChildren,
        DelayChildren = DelayChildren,
        Properties = CloneProperties(Properties),
        OnUpdate = OnUpdate,
    };

    private static Dictionary<string, BmotionTransitionConfig>? CloneProperties(
        Dictionary<string, BmotionTransitionConfig>? source)
    {
        if (source is null) return null;
        // Preserve the source's key comparer so a custom (e.g. case-insensitive) lookup semantic
        // survives the clone instead of silently reverting to the default ordinal comparer.
        var copy = new Dictionary<string, BmotionTransitionConfig>(source.Count, source.Comparer);
        foreach (var kv in source)
        {
            // The dictionary's value type is non-nullable, so a null per-property override violates
            // the contract and would surface as a NullReferenceException downstream. Reject it here
            // with a clear message instead of propagating the null via the null-forgiving operator.
            if (kv.Value is null)
                throw new ArgumentException(
                    $"Per-property transition override for '{kv.Key}' must not be null.", nameof(source));
            copy[kv.Key] = kv.Value.Clone();
        }
        return copy;
    }

    // ── Factory helpers ───────────────────────────────────────────────────────
    public static BmotionTransitionConfig Spring(double stiffness = 100, double damping = 10, double mass = 1)
        => new() { Type = BmotionTransitionType.Spring, Stiffness = stiffness, Damping = damping, Mass = mass };

    /// <summary>
    /// Duration-based spring using intuitive <paramref name="bounce"/> (0 = no bounce, 1 = very bouncy)
    /// and <paramref name="duration"/> parameters. Stiffness and damping are derived automatically.
    /// </summary>
    public static BmotionTransitionConfig BounceSpring(double duration = 0.5, double bounce = 0.25, double mass = 1)
    {
        var (stiffness, damping) = SpringFromBounce(duration, bounce, mass);
        return new()
        {
            Type = BmotionTransitionType.Spring,
            Duration = duration,
            Bounce = bounce,
            VisualDuration = duration,
            Stiffness = stiffness,
            Damping = damping,
            Mass = mass,
        };
    }

    public static BmotionTransitionConfig Tween(double duration = 0.3, BmotionEasing ease = BmotionEasing.EaseOut)
        => new() { Type = BmotionTransitionType.Tween, Duration = duration, Ease = ease };

    public static BmotionTransitionConfig Inertia(double velocity = 0, double timeConstant = 700)
        => new() { Type = BmotionTransitionType.Inertia, InertiaVelocity = velocity, TimeConstant = timeConstant };

    /// <summary>
    /// Derives <c>(stiffness, damping)</c> from Framer-Motion-compatible <paramref name="bounce"/>
    /// (0–1) and <paramref name="visualDuration"/> parameters.
    /// </summary>
    internal static (double stiffness, double damping) SpringFromBounce(
        double visualDuration, double bounce, double mass = 1)
    {
        double b = Math.Clamp(bounce, 0.0, 1.0);
        double omega0 = (2.0 * Math.PI) / Math.Max(visualDuration, 0.001);
        // damping ratio: 0 → fully elastic (bounce=1), 1 → critically damped (bounce=0)
        double zeta = b < 0.05 ? 1.0 : Math.Sqrt(1.0 - Math.Pow(b, 2.0 / 3.0));
        return (Math.Max(omega0 * omega0 * mass, 0.001), Math.Max(2.0 * zeta * omega0 * mass, 0.001));
    }
}
