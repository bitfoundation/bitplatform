namespace Bit.Bmotion.Models;

/// <summary>Controls how a value transitions from one state to another.</summary>
public class TransitionConfig
{
    // ── Type ─────────────────────────────────────────────────────────────────
    /// <summary>Animation driver: Tween, Spring, or Inertia. Default: Tween.</summary>
    public TransitionType Type { get; set; } = TransitionType.Tween;

    // ── Tween ─────────────────────────────────────────────────────────────────
    /// <summary>Duration in seconds. Default: 0.3.</summary>
    public double Duration { get; set; } = 0.3;

    /// <summary>Delay before animation starts, in seconds. Default: 0.</summary>
    public double Delay { get; set; } = 0;

    /// <summary>Named easing preset. See <see cref="Easing"/>. Default: EaseOut.</summary>
    public Easing Ease { get; set; } = Easing.EaseOut;

    /// <summary>
    /// Custom cubic-bezier as [x1, y1, x2, y2]. Overrides <see cref="Ease"/> when set.
    /// </summary>
    public double[]? EaseCubicBezier { get; set; }

    // ── Repeat ────────────────────────────────────────────────────────────────
    /// <summary>Number of times to repeat. Set to <c>int.MaxValue</c> for infinite.</summary>
    public int Repeat { get; set; } = 0;

    /// <summary>How to repeat: Loop, Mirror (ping-pong), or Reverse.</summary>
    public RepeatType RepeatType { get; set; } = RepeatType.Loop;

    /// <summary>Delay between repetitions, in seconds.</summary>
    public double RepeatDelay { get; set; } = 0;

    // ── Keyframes ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Progress offsets (0–1) for each keyframe value. Length must match value array.
    /// If omitted the frames are evenly distributed.
    /// </summary>
    public double[]? Times { get; set; }

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

    /// <summary>Order relative to parent: Default (in parallel), BeforeChildren, AfterChildren.</summary>
    public WhenType When { get; set; } = WhenType.Default;

    // ── Per-property overrides ────────────────────────────────────────────────
    /// <summary>
    /// Override transition for specific properties, e.g.
    /// <c>Properties = new { ["opacity"] = new TransitionConfig { Duration = 0.1 } }</c>
    /// </summary>
    public Dictionary<string, TransitionConfig>? Properties { get; set; }

    /// <summary>
    /// Called on every animation frame with the latest interpolated value.
    /// Supported for single-value numeric animations.
    /// </summary>
    public Action<double>? OnUpdate { get; set; }

    // ── Helpers ───────────────────────────────────────────────────────────────
    internal object ToJsObject()
    {
        var d = new Dictionary<string, object?>
        {
            ["type"] = Type.ToString().ToLowerInvariant(),
            ["duration"] = Duration,
            ["delay"] = Delay,
            ["ease"] = EaseCubicBezier != null ? (object)EaseCubicBezier : EasingToJs(Ease),
            ["repeat"] = Repeat == int.MaxValue ? "Infinity" : (object)Repeat,
            ["repeatType"] = RepeatType.ToString().ToLowerInvariant(),
            ["repeatDelay"] = RepeatDelay,
            ["stiffness"] = Stiffness,
            ["damping"] = Damping,
            ["mass"] = Mass,
            ["velocity"] = Velocity,
            ["restSpeed"] = RestSpeed,
            ["restDelta"] = RestDelta,
            ["inertiaVelocity"] = InertiaVelocity,
            ["timeConstant"] = TimeConstant,
            ["power"] = Power,
            ["inertiaRestDelta"] = InertiaRestDelta,
        };

        if (Times != null) d["times"] = Times;
        if (StaggerChildren.HasValue) d["staggerChildren"] = StaggerChildren.Value;
        if (DelayChildren.HasValue) d["delayChildren"] = DelayChildren.Value;
        if (When != WhenType.Default) d["when"] = When.ToString().ToLowerInvariant();
        if (InertiaMin.HasValue) d["inertiaMin"] = InertiaMin.Value;
        if (InertiaMax.HasValue) d["inertiaMax"] = InertiaMax.Value;

        if (Properties != null)
        {
            var props = new Dictionary<string, object?>();
            foreach (var kv in Properties)
                props[kv.Key] = kv.Value.ToJsObject();
            d["properties"] = props;
        }

        return d;
    }

    private static string EasingToJs(Easing e) => e switch
    {
        Easing.Linear => "linear",
        Easing.EaseIn => "easeIn",
        Easing.EaseOut => "easeOut",
        Easing.EaseInOut => "easeInOut",
        Easing.CircIn => "circIn",
        Easing.CircOut => "circOut",
        Easing.CircInOut => "circInOut",
        Easing.BackIn => "backIn",
        Easing.BackOut => "backOut",
        Easing.BackInOut => "backInOut",
        Easing.Anticipate => "anticipate",
        _ => "easeOut"
    };

    /// <summary>
    /// Creates a deep copy of this configuration. Used internally when the library
    /// needs to derive a variant of a transition (e.g. applying a global
    /// <see cref="Context.MotionConfigContext.TransitionSpeed"/> scale or a stagger delay)
    /// without mutating or partially losing the original's fields.
    /// </summary>
    public TransitionConfig Clone() => new()
    {
        Type = Type,
        Duration = Duration,
        Delay = Delay,
        Ease = Ease,
        EaseCubicBezier = EaseCubicBezier is null ? null : (double[])EaseCubicBezier.Clone(),
        Repeat = Repeat,
        RepeatType = RepeatType,
        RepeatDelay = RepeatDelay,
        Times = Times is null ? null : (double[])Times.Clone(),
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
        When = When,
        Properties = Properties,
        OnUpdate = OnUpdate,
    };

    // ── Factory helpers ───────────────────────────────────────────────────────
    public static TransitionConfig Spring(double stiffness = 100, double damping = 10, double mass = 1)
        => new() { Type = TransitionType.Spring, Stiffness = stiffness, Damping = damping, Mass = mass };

    /// <summary>
    /// Duration-based spring using intuitive <paramref name="bounce"/> (0 = no bounce, 1 = very bouncy)
    /// and <paramref name="duration"/> parameters. Stiffness and damping are derived automatically.
    /// </summary>
    public static TransitionConfig BounceSpring(double duration = 0.5, double bounce = 0.25, double mass = 1)
    {
        var (stiffness, damping) = SpringFromBounce(duration, bounce, mass);
        return new()
        {
            Type = TransitionType.Spring,
            Duration = duration,
            Bounce = bounce,
            VisualDuration = duration,
            Stiffness = stiffness,
            Damping = damping,
            Mass = mass,
        };
    }

    public static TransitionConfig Tween(double duration = 0.3, Easing ease = Easing.EaseOut)
        => new() { Type = TransitionType.Tween, Duration = duration, Ease = ease };

    public static TransitionConfig Inertia(double velocity = 0, double timeConstant = 700)
        => new() { Type = TransitionType.Inertia, InertiaVelocity = velocity, TimeConstant = timeConstant };

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

// ── Enumerations ──────────────────────────────────────────────────────────────

public enum TransitionType { Tween, Spring, Inertia, Keyframes }

public enum Easing
{
    Linear,
    EaseIn, EaseOut, EaseInOut,
    CircIn, CircOut, CircInOut,
    BackIn, BackOut, BackInOut,
    Anticipate
}

public enum RepeatType { Loop, Mirror, Reverse }

public enum WhenType { Default, BeforeChildren, AfterChildren }
