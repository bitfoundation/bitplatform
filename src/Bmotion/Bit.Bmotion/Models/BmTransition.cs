namespace Bit.Bmotion;

/// <summary>
/// Controls how values transition from one state to another. Use one of the concrete
/// types - <see cref="BmTween"/>, <see cref="BmSpring"/> or <see cref="BmInertia"/> -
/// or the terse factories <see cref="Bm.Tween"/>, <see cref="Bm.Spring"/> and
/// <see cref="Bm.Inertia"/>.
/// </summary>
public abstract class BmTransition
{
    /// <summary>Delay before the animation starts, in seconds.</summary>
    public double Delay { get; set; }

    /// <summary>Repeat configuration. E.g. <c>Repeat = 3</c> or <c>Repeat = BmRepeat.Mirror()</c>.</summary>
    public BmRepeat? Repeat { get; set; }

    /// <summary>
    /// Per-property transition overrides, e.g.
    /// <c>Properties = new() { ["opacity"] = Bm.Tween(0.1) }</c>.
    /// Keys use the engine's camelCase property names ("x", "opacity", "backgroundColor", …).
    /// </summary>
    public Dictionary<string, BmTransition>? Properties { get; set; }

    // ── Orchestration (applies when this transition sits on a variants container) ──

    /// <summary>Seconds to stagger each child's animation start (variants only).</summary>
    public double? StaggerChildren { get; set; }

    /// <summary>Seconds to delay the first child's animation start (variants only).</summary>
    public double? DelayChildren { get; set; }

    /// <summary>
    /// Called on every animation frame with the latest interpolated value.
    /// Supported for single-value numeric animations.
    /// </summary>
    public Action<double>? OnUpdate { get; set; }

    /// <summary>Lowers this transition into the internal flat engine configuration.</summary>
    internal BmotionTransitionConfig ToConfig()
    {
        var c = CreateConfig();
        c.Delay = Delay;
        if (Repeat is { } r)
        {
            c.RepeatInfinite = r.IsForever;
            c.Repeat = r.IsForever ? 0 : r.Count;
            c.RepeatType = r.Type;
            c.RepeatDelay = r.Delay;
        }
        c.StaggerChildren = StaggerChildren;
        c.DelayChildren = DelayChildren;
        c.OnUpdate = OnUpdate;
        if (Properties is { Count: > 0 })
        {
            c.Properties = new Dictionary<string, BmotionTransitionConfig>(Properties.Count, StringComparer.Ordinal);
            foreach (var (key, value) in Properties)
            {
                if (value is null)
                    throw new InvalidOperationException($"Per-property transition override for '{key}' must not be null.");
                c.Properties[key] = value.ToConfig();
            }
        }
        return c;
    }

    private protected abstract BmotionTransitionConfig CreateConfig();

    // ── Value equality (used for render change detection) ─────────────────────

    /// <summary>
    /// Structural comparison so a transition recreated inline on every render
    /// (<c>Transition="Bm.Spring()"</c>) doesn't count as a parameter change.
    /// </summary>
    internal virtual bool ValueEquals(BmTransition other)
    {
        if (Delay != other.Delay) return false;
        if (!Nullable.Equals(Repeat, other.Repeat)) return false;
        if (StaggerChildren != other.StaggerChildren || DelayChildren != other.DelayChildren) return false;
        // OnUpdate is deliberately excluded: inline callbacks are recreated on every render and
        // must not read as a transition change (an animation picks up the freshest delegate when
        // it actually starts).
        return PropertiesEqual(Properties, other.Properties);
    }

    internal static bool AreEquivalent(BmTransition? a, BmTransition? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        if (a.GetType() != b.GetType()) return false;
        return a.ValueEquals(b);
    }

    private static bool PropertiesEqual(Dictionary<string, BmTransition>? a, Dictionary<string, BmTransition>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null || a.Count != b.Count) return false;
        foreach (var (key, value) in a)
        {
            if (!b.TryGetValue(key, out var otherValue)) return false;
            if (!AreEquivalent(value, otherValue)) return false;
        }
        return true;
    }

    private protected static bool ArraysEqual(double[]? a, double[]? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return a.AsSpan().SequenceEqual(b);
    }
}

/// <summary>Duration- and easing-based interpolation between values.</summary>
public sealed class BmTween : BmTransition
{
    /// <summary>Duration in seconds. Default: 0.3.</summary>
    public double Duration { get; set; } = 0.3;

    /// <summary>Named easing preset. Default: <see cref="BmEase.Out"/>.</summary>
    public BmEase Ease { get; set; } = BmEase.Out;

    /// <summary>
    /// Custom cubic-bezier as [x1, y1, x2, y2]; overrides <see cref="Ease"/> when set.
    /// X coordinates must be within [0, 1].
    /// </summary>
    public double[]? Bezier { get; set; }

    /// <summary>
    /// Progress offsets (0-1) for each keyframe value; evenly distributed when omitted.
    /// Length must match the keyframe array being animated.
    /// </summary>
    public double[]? Times { get; set; }

    /// <summary>
    /// Per-segment easing for keyframe sequences: <c>Eases[i]</c> eases the segment from keyframe
    /// <c>i</c> to keyframe <c>i+1</c> (one fewer entry than keyframes; the last entry repeats when
    /// the array is shorter). Overrides <see cref="Ease"/> segment-by-segment when set.
    /// </summary>
    public BmEase[]? Eases { get; set; }

    private protected override BmotionTransitionConfig CreateConfig() => new()
    {
        Type = BmotionTransitionType.Tween,
        Duration = Duration,
        Ease = Ease,
        EaseCubicBezier = Bezier,
        Times = Times,
        Eases = Eases,
    };

    internal override bool ValueEquals(BmTransition other)
        => other is BmTween t
           && Duration == t.Duration && Ease == t.Ease
           && ArraysEqual(Bezier, t.Bezier) && ArraysEqual(Times, t.Times)
           && EasesEqual(Eases, t.Eases)
           && base.ValueEquals(other);

    private static bool EasesEqual(BmEase[]? a, BmEase[]? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return a.AsSpan().SequenceEqual(b);
    }
}

/// <summary>Physics-based spring driven by stiffness, damping and mass - or, more intuitively,
/// by <see cref="Bounce"/> and <see cref="Duration"/>.</summary>
public sealed class BmSpring : BmTransition
{
    /// <summary>Spring stiffness. Higher = snappier. Default: 100.</summary>
    public double Stiffness { get; set; } = 100;

    /// <summary>Damping coefficient. Higher = less oscillation. Default: 10.</summary>
    public double Damping { get; set; } = 10;

    /// <summary>Virtual mass. Higher = slower acceleration. Default: 1.</summary>
    public double Mass { get; set; } = 1;

    /// <summary>Initial velocity (units/s). Default: 0.</summary>
    public double Velocity { get; set; }

    /// <summary>Minimum speed (units/s) considered at rest. Default: 0.01.</summary>
    public double RestSpeed { get; set; } = 0.01;

    /// <summary>Minimum distance from target considered at rest. Default: 0.01.</summary>
    public double RestDelta { get; set; } = 0.01;

    /// <summary>
    /// Bounciness (0 = no bounce, 1 = very bouncy). When set (or when <see cref="Duration"/>
    /// is set), stiffness and damping are derived automatically.
    /// </summary>
    public double? Bounce { get; set; }

    /// <summary>
    /// The visual time (seconds) the spring appears to take to reach its target
    /// (bounce may continue after). Combines with <see cref="Bounce"/> (default 0.25)
    /// to configure the spring without physics parameters.
    /// </summary>
    public double? Duration { get; set; }

    private protected override BmotionTransitionConfig CreateConfig()
    {
        var c = new BmotionTransitionConfig
        {
            Type = BmotionTransitionType.Spring,
            Stiffness = Stiffness,
            Damping = Damping,
            Mass = Mass,
            Velocity = Velocity,
            RestSpeed = RestSpeed,
            RestDelta = RestDelta,
        };
        // A duration-based spring derives stiffness/damping from bounce + visual duration.
        // Setting either of the intuitive parameters opts into that model.
        if (Duration.HasValue || Bounce.HasValue)
        {
            c.Bounce = Bounce ?? 0.25;
            c.VisualDuration = Duration ?? 0.5;
        }
        return c;
    }

    internal override bool ValueEquals(BmTransition other)
        => other is BmSpring s
           && Stiffness == s.Stiffness && Damping == s.Damping && Mass == s.Mass
           && Velocity == s.Velocity && RestSpeed == s.RestSpeed && RestDelta == s.RestDelta
           && Bounce == s.Bounce && Duration == s.Duration
           && base.ValueEquals(other);
}

/// <summary>Velocity-based deceleration that coasts to a stop (e.g. momentum after a drag).</summary>
public sealed class BmInertia : BmTransition
{
    /// <summary>Velocity at the start of deceleration. Default: 0.</summary>
    public double Velocity { get; set; }

    /// <summary>Exponential decay time constant in milliseconds. Default: 700.</summary>
    public double TimeConstant { get; set; } = 700;

    /// <summary>Multiplier for the projected coast distance. Default: 0.8.</summary>
    public double Power { get; set; } = 0.8;

    /// <summary>Minimum distance from target that counts as at rest. Default: 0.5.</summary>
    public double RestDelta { get; set; } = 0.5;

    /// <summary>Optional lower bound for the coast target.</summary>
    public double? Min { get; set; }

    /// <summary>Optional upper bound for the coast target.</summary>
    public double? Max { get; set; }

    private protected override BmotionTransitionConfig CreateConfig() => new()
    {
        Type = BmotionTransitionType.Inertia,
        InertiaVelocity = Velocity,
        TimeConstant = TimeConstant,
        Power = Power,
        InertiaRestDelta = RestDelta,
        InertiaMin = Min,
        InertiaMax = Max,
    };

    internal override bool ValueEquals(BmTransition other)
        => other is BmInertia i
           && Velocity == i.Velocity && TimeConstant == i.TimeConstant && Power == i.Power
           && RestDelta == i.RestDelta && Min == i.Min && Max == i.Max
           && base.ValueEquals(other);
}
