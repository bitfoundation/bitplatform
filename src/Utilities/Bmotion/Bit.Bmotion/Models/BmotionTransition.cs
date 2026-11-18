namespace Bit.Bmotion;

/// <summary>
/// Controls how values transition from one state to another. Use one of the concrete
/// types - <see cref="BmotionTween"/>, <see cref="BmotionSpring"/> or <see cref="BmotionInertia"/> -
/// or the terse factories <see cref="Bm.Tween"/>, <see cref="Bm.Spring"/> and
/// <see cref="Bm.Inertia"/>.
/// </summary>
public abstract class BmotionTransition
{
    /// <summary>
    /// Delay before the animation starts, in seconds. A negative delay starts it that far into its motion; a spring or
    /// inertia plays the skipped time frame by frame, at most its last ten minutes, so one still moving ten minutes in
    /// (no damping, damping so heavy it creeps to rest, or repeating forever) lands where it is ten minutes in rather
    /// than where the whole delay would take it.
    /// </summary>
    public double Delay { get; set; }

    /// <summary>
    /// Color space for interpolating color properties. <c>null</c> inherits the global
    /// <c>&lt;BmotionConfig&gt;</c> setting (or sRGB when none). See <see cref="BmotionColorSpace"/>.
    /// </summary>
    public BmotionColorSpace? ColorSpace { get; set; }

    /// <summary>Repeat configuration. E.g. <c>Repeat = 3</c> or <c>Repeat = BmotionRepeat.Mirror()</c>.</summary>
    public BmotionRepeat? Repeat { get; set; }

    /// <summary>
    /// Per-property transition overrides, e.g.
    /// <c>Properties = new() { ["opacity"] = Bm.Tween(0.1) }</c>.
    /// Keys use the engine's camelCase property names ("x", "opacity", "backgroundColor", …).
    /// </summary>
    public Dictionary<string, BmotionTransition>? Properties { get; set; }

    // ── Orchestration (applies when this transition sits on a variants container) ──

    /// <summary>Seconds to stagger each child's animation start (variants only).</summary>
    public double? StaggerChildren { get; set; }

    /// <summary>Seconds to delay the first child's animation start (variants only).</summary>
    public double? DelayChildren { get; set; }

    /// <summary>
    /// Orders this element's own animation against its children's (variants only) - motion.dev's
    /// <c>when</c>. <see cref="BmotionWhen.BeforeChildren"/> pushes every child back by this
    /// transition's own duration, on top of <see cref="DelayChildren"/>;
    /// <see cref="BmotionWhen.AfterChildren"/> delays this element until the cascade has finished.
    /// </summary>
    public BmotionWhen When { get; set; } = BmotionWhen.Together;

    /// <summary>
    /// Full stagger generator for the children of a variants container - motion.dev's
    /// <c>delayChildren: stagger(...)</c>. It supersedes the flat <see cref="StaggerChildren"/>
    /// interval and is what lets the cascade radiate from the last element, from the centre, or
    /// across a grid:
    /// <code>
    /// Bm.Tween(0.3, childStagger: Bm.Stagger(0.06, from: BmotionStaggerFrom.Center))
    /// Bm.Tween(0.3, childStagger: Bm.Stagger(0.04, grid: (cols: 6, rows: 4)))
    /// </code>
    /// <see cref="DelayChildren"/> still adds on top, as does the stagger's own start delay.
    /// </summary>
    public BmotionStagger? ChildStagger { get; set; }

    /// <summary>
    /// Bends the straight line between this animation's start and end point into a curve -
    /// motion.dev's <c>transition.path</c>. Build one with <see cref="Bm.Arc"/>:
    /// <code>
    /// Bm.Tween(0.8, path: Bm.Arc(strength: 0.8, rotate: 1))
    /// </code>
    /// Applies when the target carries both <c>x</c> and <c>y</c> as single values; otherwise the
    /// animation runs in a straight line as usual. See <see cref="BmotionArc"/>.
    /// </summary>
    public BmotionArc? Path { get; set; }

    /// <summary>
    /// Called on every animation frame with the latest interpolated value.
    /// Supported for single-value numeric animations.
    /// </summary>
    public Action<double>? OnUpdate { get; set; }

    /// <summary>
    /// Returns a copy of this transition with <see cref="Delay"/> replaced - the way to reuse one
    /// configured transition across staggered elements without mutating the shared instance:
    /// <code>
    /// var t = Bm.Spring(bounce: 0.3, duration: 0.6);
    /// // …then per element: t.WithDelay(i * 0.05)
    /// </code>
    /// </summary>
    public BmotionTransition WithDelay(double delay)
    {
        var copy = CloneTransition();
        copy.Delay = delay;
        return copy;
    }

    /// <summary>Creates a copy of this transition, including every base-class field.</summary>
    private protected abstract BmotionTransition CloneTransition();

    /// <summary>Copies the fields declared on <see cref="BmotionTransition"/> onto <paramref name="target"/>.</summary>
    private protected void CopyBaseTo(BmotionTransition target)
    {
        target.Delay = Delay;
        target.ColorSpace = ColorSpace;
        target.Repeat = Repeat;
        target.StaggerChildren = StaggerChildren;
        target.DelayChildren = DelayChildren;
        target.When = When;
        target.ChildStagger = ChildStagger;
        target.Path = Path;
        target.OnUpdate = OnUpdate;
        // Per-property overrides are immutable from this type's perspective (ToConfig only reads
        // them), so a shallow copy of the map is enough to keep the copies independent.
        target.Properties = Properties is null ? null : new Dictionary<string, BmotionTransition>(Properties);
    }

    /// <summary>Lowers this transition into the internal flat engine configuration.</summary>
    internal BmotionTransitionConfig ToConfig() => ToConfig(null);

    /// <summary>
    /// Lowers this transition, inheriting <paramref name="ambientColorSpace"/> (the enclosing
    /// <c>&lt;BmotionConfig&gt;</c> setting) wherever this transition didn't set its own
    /// <see cref="ColorSpace"/>. The resolved space then cascades as the ambient for nested
    /// per-property configs, so a per-property color override doesn't reset to sRGB.
    /// </summary>
    internal BmotionTransitionConfig ToConfig(BmotionColorSpace? ambientColorSpace)
    {
        var c = CreateConfig();
        c.Delay = Delay;
        // Explicit per-transition space wins; otherwise inherit the ambient (global) space.
        var resolvedColorSpace = ColorSpace ?? ambientColorSpace;
        if (resolvedColorSpace is BmotionColorSpace cs) c.ColorSpace = cs;
        if (Repeat is { } r)
        {
            c.RepeatInfinite = r.IsForever;
            c.Repeat = r.IsForever ? 0 : r.Count;
            c.RepeatType = r.Type;
            c.RepeatDelay = r.Delay;
        }
        c.StaggerChildren = StaggerChildren;
        c.DelayChildren = DelayChildren;
        c.When = When;
        c.Path = Path;
        c.OnUpdate = OnUpdate;
        if (Properties is { Count: > 0 })
        {
            c.Properties = new Dictionary<string, BmotionTransitionConfig>(Properties.Count, StringComparer.Ordinal);
            foreach (var (key, value) in Properties)
            {
                if (value is null)
                    throw new InvalidOperationException($"Per-property transition override for '{key}' must not be null.");
                c.Properties[key] = value.ToConfig(resolvedColorSpace);
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
    internal virtual bool ValueEquals(BmotionTransition other)
    {
        if (Delay != other.Delay) return false;
        if (ColorSpace != other.ColorSpace) return false;
        if (!Nullable.Equals(Repeat, other.Repeat)) return false;
        if (StaggerChildren != other.StaggerChildren || DelayChildren != other.DelayChildren) return false;
        if (When != other.When) return false;
        if (!BmotionStagger.AreEquivalent(ChildStagger, other.ChildStagger)) return false;
        if (!BmotionArc.AreEquivalent(Path, other.Path)) return false;
        // OnUpdate is deliberately excluded: inline callbacks are recreated on every render and
        // must not read as a transition change (an animation picks up the freshest delegate when
        // it actually starts).
        return PropertiesEqual(Properties, other.Properties);
    }

    internal static bool AreEquivalent(BmotionTransition? a, BmotionTransition? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        if (a.GetType() != b.GetType()) return false;
        return a.ValueEquals(b);
    }

    private static bool PropertiesEqual(Dictionary<string, BmotionTransition>? a, Dictionary<string, BmotionTransition>? b)
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
