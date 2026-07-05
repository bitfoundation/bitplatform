namespace Bit.Bmotion;

/// <summary>
/// A declarative multi-step animation timeline, motion.dev's <c>animate([...])</c> sequences:
/// <code>
/// var seq = new BmSequence()
///     .Add("#box", Bm.To(x: 100), Bm.Tween(0.5))
///     .Add("#box", Bm.To(y: 50), Bm.Tween(0.3), at: "-0.1")   // overlap previous end by 0.1s
///     .Label("burst")
///     .Add(".dot", Bm.To(scale: [1, 1.4, 1]), at: "burst");   // at a named label
/// await Motion.RunAsync(seq);
/// </code>
/// <para>
/// The <c>at</c> parameter positions a segment on the timeline:
/// <list type="bullet">
///   <item><description><c>null</c> - after the previous segment ends (default)</description></item>
///   <item><description><c>"+0.5"</c> / <c>"-0.2"</c> - relative to the previous segment's end</description></item>
///   <item><description><c>"&lt;"</c> / <c>"&lt;0.3"</c> - at (or offset from) the previous segment's start</description></item>
///   <item><description><c>"1.5"</c> - absolute seconds from the sequence start</description></item>
///   <item><description>any other string - a label declared with <see cref="Label"/></description></item>
/// </list>
/// Segment durations for springs and inertia are estimates (their real settling time is
/// physics-dependent); use explicit <c>at</c> offsets when exact alignment matters.
/// </para>
/// </summary>
public sealed class BmSequence
{
    internal sealed record Segment(string Selector, BmProps Target, BmTransition? Transition, double Start);

    private readonly List<Segment> _segments = new();
    private readonly Dictionary<string, double> _labels = new(StringComparer.Ordinal);
    private double _prevStart;
    private double _prevEnd;
    private double _cursor; // where the next default-positioned segment starts

    internal IReadOnlyList<Segment> Segments => _segments;

    /// <summary>Declares a named point at the current end of the timeline.</summary>
    public BmSequence Label(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        _labels[name] = _cursor;
        return this;
    }

    /// <summary>Adds a segment animating all elements matching <paramref name="selector"/>.</summary>
    public BmSequence Add(string selector, BmProps target, BmTransition? transition = null, string? at = null)
    {
        if (string.IsNullOrWhiteSpace(selector))
            throw new ArgumentException("Selector must not be null or whitespace.", nameof(selector));
        ArgumentNullException.ThrowIfNull(target);

        double start = ResolveStart(at);
        double duration = EstimateDuration(target.Transition ?? transition);

        _segments.Add(new Segment(selector, target, target.Transition ?? transition, start));
        _prevStart = start;
        _prevEnd = start + duration;
        _cursor = Math.Max(_cursor, _prevEnd);
        return this;
    }

    private double ResolveStart(string? at)
    {
        if (at is null) return _cursor;
        at = at.Trim();

        if (at.StartsWith('+') || at.StartsWith('-'))
        {
            if (!double.TryParse(at, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double offset) || !double.IsFinite(offset))
                throw new ArgumentException($"Invalid relative 'at' offset: '{at}'.", nameof(at));
            return Math.Max(0, _prevEnd + offset);
        }

        if (at.StartsWith('<'))
        {
            var rest = at[1..];
            double offset = 0;
            if (rest.Length > 0 &&
                (!double.TryParse(rest, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out offset) || !double.IsFinite(offset)))
                throw new ArgumentException($"Invalid 'at' offset from previous start: '{at}'.", nameof(at));
            return Math.Max(0, _prevStart + offset);
        }

        if (double.TryParse(at, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double absolute))
        {
            if (!double.IsFinite(absolute) || absolute < 0)
                throw new ArgumentException($"Absolute 'at' time must be a finite, non-negative number: '{at}'.", nameof(at));
            return absolute;
        }

        if (_labels.TryGetValue(at, out double label)) return label;
        throw new ArgumentException($"Unknown 'at' label: '{at}'. Declare it with .Label(\"{at}\") first.", nameof(at));
    }

    /// <summary>
    /// Estimated visual length of a segment, used to place default-positioned successors.
    /// Tweens are exact; springs use their visual duration (or a 0.8 s estimate);
    /// inertia decays are approximated as three time constants.
    /// </summary>
    private static double EstimateDuration(BmTransition? transition) => transition switch
    {
        BmTween tween => tween.Delay + tween.Duration,
        BmSpring spring => spring.Delay + (spring.Duration ?? 0.8),
        BmInertia inertia => inertia.Delay + inertia.TimeConstant * 3 / 1000.0,
        _ => 0.3, // the default tween duration
    };
}
