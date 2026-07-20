
namespace Bit.Bmotion;
/// <summary>
/// Pure-C# easing functions. Ported from the original JS implementation.
/// Cached delegates avoid re-allocation for common easing types.
/// </summary>
internal static class BmEaseFunctions
{
    // ── Pre-built delegates for common easings ────────────────────────────────
    private static readonly Func<double, double> _easeIn    = CubicBezier(0.42, 0, 1, 1);
    private static readonly Func<double, double> _easeOut   = CubicBezier(0, 0, 0.58, 1);
    private static readonly Func<double, double> _easeInOut = CubicBezier(0.42, 0, 0.58, 1);
    private static readonly Func<double, double> _backIn    = CubicBezier(0.31455, -0.37755, 0.69245, 1.37755);
    private static readonly Func<double, double> _backOut   = CubicBezier(0.33915, 0, 0.68085, 1.4);
    private static readonly Func<double, double> _backInOut = CubicBezier(0.68987, -0.45, 0.32, 1.45);

    /// <summary>Returns an easing function for the given transition config.</summary>
    public static Func<double, double> Get(BmotionTransitionConfig config)
    {
        // A stepped easing overrides both the named preset and any cubic-bezier.
        if (config.StepCount > 0) return Steps(config.StepCount, config.StepJump);

        if (config.EaseCubicBezier is { Length: 4 } cb &&
            double.IsFinite(cb[0]) && double.IsFinite(cb[1]) && double.IsFinite(cb[2]) && double.IsFinite(cb[3]))
            return CubicBezier(cb[0], cb[1], cb[2], cb[3]);

        return Get(config.Ease);
    }

    /// <summary>
    /// Builds the per-segment easing array for a keyframe sequence with
    /// <paramref name="segments"/> segments. <c>config.Eases</c> entries map one-to-one onto
    /// segments (the last entry repeats when the array is shorter); without them every segment
    /// shares the config's single easing.
    /// </summary>
    public static Func<double, double>[] GetSegmentEases(BmotionTransitionConfig config, int segments)
    {
        var eases = new Func<double, double>[segments];
        if (config.Eases is { Length: > 0 } perSegment)
        {
            for (int i = 0; i < segments; i++)
                eases[i] = Get(perSegment[Math.Min(i, perSegment.Length - 1)]);
        }
        else
        {
            var global = Get(config);
            for (int i = 0; i < segments; i++)
                eases[i] = global;
        }
        return eases;
    }

    /// <summary>Returns the easing function for a named preset.</summary>
    public static Func<double, double> Get(BmEase ease)
    {
        return ease switch
        {
            BmEase.Linear    => t => t,
            BmEase.In    => _easeIn,
            BmEase.Out   => _easeOut,
            BmEase.InOut => _easeInOut,
            BmEase.CircIn    => t => 1 - Math.Sqrt(1 - t * t),
            BmEase.CircOut   => t => Math.Sqrt(1 - (t - 1) * (t - 1)),
            BmEase.CircInOut => t => t < 0.5
                ? (1 - Math.Sqrt(1 - 4 * t * t)) / 2
                : (Math.Sqrt(1 - Math.Pow(2 * t - 2, 2)) + 1) / 2,
            BmEase.BackIn    => _backIn,
            BmEase.BackOut   => _backOut,
            BmEase.BackInOut => _backInOut,
            BmEase.Anticipate => t => t < 0.5
                ? _backIn(t * 2) / 2
                : _easeOut(t * 2 - 1) / 2 + 0.5,

            // ── Sine ──
            BmEase.SineIn    => t => 1 - Math.Cos((t * Math.PI) / 2),
            BmEase.SineOut   => t => Math.Sin((t * Math.PI) / 2),
            BmEase.SineInOut => t => -(Math.Cos(Math.PI * t) - 1) / 2,

            // ── Quad (power 2) ──
            BmEase.QuadIn    => t => t * t,
            BmEase.QuadOut   => t => 1 - (1 - t) * (1 - t),
            BmEase.QuadInOut => t => t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2,

            // ── Quart (power 4) ──
            BmEase.QuartIn    => t => t * t * t * t,
            BmEase.QuartOut   => t => 1 - Math.Pow(1 - t, 4),
            BmEase.QuartInOut => t => t < 0.5 ? 8 * t * t * t * t : 1 - Math.Pow(-2 * t + 2, 4) / 2,

            // ── Quint (power 5) ──
            BmEase.QuintIn    => t => t * t * t * t * t,
            BmEase.QuintOut   => t => 1 - Math.Pow(1 - t, 5),
            BmEase.QuintInOut => t => t < 0.5 ? 16 * t * t * t * t * t : 1 - Math.Pow(-2 * t + 2, 5) / 2,

            // ── Expo ──
            BmEase.ExpoIn    => t => t <= 0 ? 0 : Math.Pow(2, 10 * t - 10),
            BmEase.ExpoOut   => t => t >= 1 ? 1 : 1 - Math.Pow(2, -10 * t),
            BmEase.ExpoInOut => t => t <= 0 ? 0 : t >= 1 ? 1
                : t < 0.5 ? Math.Pow(2, 20 * t - 10) / 2 : (2 - Math.Pow(2, -20 * t + 10)) / 2,

            // ── Elastic (oscillating overshoot) ──
            BmEase.ElasticIn => t => t <= 0 ? 0 : t >= 1 ? 1
                : -Math.Pow(2, 10 * t - 10) * Math.Sin((10 * t - 10.75) * ElasticC4),
            BmEase.ElasticOut => t => t <= 0 ? 0 : t >= 1 ? 1
                : Math.Pow(2, -10 * t) * Math.Sin((10 * t - 0.75) * ElasticC4) + 1,
            BmEase.ElasticInOut => t => t <= 0 ? 0 : t >= 1 ? 1
                : t < 0.5
                    ? -(Math.Pow(2, 20 * t - 10) * Math.Sin((20 * t - 11.125) * ElasticC5)) / 2
                    : (Math.Pow(2, -20 * t + 10) * Math.Sin((20 * t - 11.125) * ElasticC5)) / 2 + 1,

            // ── Bounce ──
            BmEase.BounceIn    => t => 1 - BounceOut(1 - t),
            BmEase.BounceOut   => BounceOut,
            BmEase.BounceInOut => t => t < 0.5
                ? (1 - BounceOut(1 - 2 * t)) / 2
                : (1 + BounceOut(2 * t - 1)) / 2,

            _                => _easeOut,
        };
    }

    // Elastic period constants (easings.net): 2π/3 for in/out, 2π/4.5 for in-out.
    private const double ElasticC4 = 2 * Math.PI / 3;
    private const double ElasticC5 = 2 * Math.PI / 4.5;

    /// <summary>The classic four-segment "bounce out" curve (easings.net).</summary>
    private static double BounceOut(double t)
    {
        const double n1 = 7.5625, d1 = 2.75;
        if (t < 1 / d1) return n1 * t * t;
        if (t < 2 / d1) { t -= 1.5 / d1; return n1 * t * t + 0.75; }
        if (t < 2.5 / d1) { t -= 2.25 / d1; return n1 * t * t + 0.9375; }
        t -= 2.625 / d1;
        return n1 * t * t + 0.984375;
    }

    /// <summary>
    /// A stepped easing matching CSS <c>steps(count, jumpTerm)</c>. Produces a staircase from 0→1.
    /// </summary>
    public static Func<double, double> Steps(int count, BmStepJump jump)
    {
        int n = Math.Max(1, count);
        return t =>
        {
            if (t <= 0) t = 0;
            else if (t >= 1) t = 1;

            double v = jump switch
            {
                BmStepJump.Start => Math.Ceiling(t * n) / n,
                BmStepJump.None => n > 1 ? Math.Min(Math.Floor(t * n), n - 1) / (n - 1) : 0,
                BmStepJump.Both => (Math.Floor(Math.Min(t, 0.9999999999) * n) + 1) / (n + 1),
                _ => Math.Floor(t * n) / n, // End (jump-end)
            };
            // Every jump-term lands exactly on 1 at the end of the interval.
            if (t >= 1) v = 1;
            return Math.Clamp(v, 0, 1);
        };
    }

    /// <summary>
    /// Whether the easing has a CSS representation that <b>exactly</b> reproduces the runtime curve
    /// (so it can be shipped to the compositor via <see cref="ToCssString"/> without drift). Only
    /// <c>steps()</c>, an explicit cubic-bezier, and the presets whose runtime function IS that CSS
    /// keyword/bezier qualify. Curves that <see cref="ToCssString"/> serializes as an <i>approximate</i>
    /// cubic-bezier (Circ*, Anticipate, Sine/Quad/Quart/Quint/Expo) are NOT faithful - the runtime
    /// uses exact math there, so they must be sampled to <c>linear()</c> or run on rAF instead of
    /// offloading a subtly-wrong curve. Elastic/bounce have no CSS form at all.
    /// </summary>
    public static bool HasFaithfulCssEasing(BmotionTransitionConfig config)
    {
        // A Length-4 EaseCubicBezier is always finite: the setter (ValidateCubicBezier) rejects any
        // non-finite / wrong-length array, so ToCssString emits it verbatim. Steps are exact too.
        if (config.StepCount > 0 || config.EaseCubicBezier is { Length: 4 }) return true;
        // Only these presets are byte-for-byte reproduced by ToCssString: the linear/ease* keywords
        // and Back* (whose runtime delegate is literally the same cubic-bezier ToCssString emits).
        return config.Ease is
            BmEase.Linear or BmEase.In or BmEase.Out or BmEase.InOut or
            BmEase.BackIn or BmEase.BackOut or BmEase.BackInOut;
    }

    /// <summary>Returns a CSS easing string for use with the Web Animations API (FLIP).</summary>
    public static string ToCssString(BmotionTransitionConfig? config)
    {
        if (config == null) return "ease";
        if (config.StepCount > 0)
        {
            // steps(1,jump-none) is invalid CSS (jump-none needs >= 2 steps). For a single step it
            // is behaviourally identical to jump-end (hold 0, then snap to 1 at the end), which is
            // exactly what Steps(1, None) produces at runtime - so normalize it rather than emit
            // a string the browser rejects (which would throw when passed to element.animate).
            var jump = config.StepJump switch
            {
                BmStepJump.Start => "jump-start",
                BmStepJump.None when config.StepCount >= 2 => "jump-none",
                BmStepJump.None => "jump-end",
                BmStepJump.Both => "jump-both",
                _ => "jump-end",
            };
            return $"steps({config.StepCount},{jump})";
        }
        if (config.EaseCubicBezier is { Length: 4 } cb &&
            double.IsFinite(cb[0]) && double.IsFinite(cb[1]) && double.IsFinite(cb[2]) && double.IsFinite(cb[3]))
            return $"cubic-bezier({BmotionCssFormat.Num(cb[0])},{BmotionCssFormat.Num(cb[1])},{BmotionCssFormat.Num(cb[2])},{BmotionCssFormat.Num(cb[3])})";
        return config.Ease switch
        {
            BmEase.Linear    => "linear",
            BmEase.In    => "ease-in",
            BmEase.Out   => "ease-out",
            BmEase.InOut => "ease-in-out",
            // Circ* have no CSS keyword - map to their closest cubic-bezier approximations.
            BmEase.CircIn    => "cubic-bezier(0.55,0,1,0.45)",
            BmEase.CircOut   => "cubic-bezier(0,0.55,0.45,1)",
            BmEase.CircInOut => "cubic-bezier(0.85,0,0.15,1)",
            // Back* map exactly to the cubic-beziers used by Get(...).
            BmEase.BackIn    => "cubic-bezier(0.31455,-0.37755,0.69245,1.37755)",
            BmEase.BackOut   => "cubic-bezier(0.33915,0,0.68085,1.4)",
            BmEase.BackInOut => "cubic-bezier(0.68987,-0.45,0.32,1.45)",
            // Anticipate has no CSS equivalent; use the backIn curve as the nearest fallback.
            BmEase.Anticipate => "cubic-bezier(0.31455,-0.37755,0.69245,1.37755)",
            // Power curves → standard cubic-bezier approximations (easings.net).
            BmEase.SineIn    => "cubic-bezier(0.12,0,0.39,0)",
            BmEase.SineOut   => "cubic-bezier(0.61,1,0.88,1)",
            BmEase.SineInOut => "cubic-bezier(0.37,0,0.63,1)",
            BmEase.QuadIn    => "cubic-bezier(0.11,0,0.5,0)",
            BmEase.QuadOut   => "cubic-bezier(0.5,1,0.89,1)",
            BmEase.QuadInOut => "cubic-bezier(0.45,0,0.55,1)",
            BmEase.QuartIn    => "cubic-bezier(0.5,0,0.75,0)",
            BmEase.QuartOut   => "cubic-bezier(0.25,1,0.5,1)",
            BmEase.QuartInOut => "cubic-bezier(0.76,0,0.24,1)",
            BmEase.QuintIn    => "cubic-bezier(0.64,0,0.78,0)",
            BmEase.QuintOut   => "cubic-bezier(0.22,1,0.36,1)",
            BmEase.QuintInOut => "cubic-bezier(0.83,0,0.17,1)",
            BmEase.ExpoIn    => "cubic-bezier(0.7,0,0.84,0)",
            BmEase.ExpoOut   => "cubic-bezier(0.16,1,0.3,1)",
            BmEase.ExpoInOut => "cubic-bezier(0.87,0,0.13,1)",
            // Elastic/Bounce have no faithful CSS curve; sampled to linear() for the compositor
            // (see HasFaithfulCssEasing). This keyword is only a last-resort fallback.
            _                => "ease",
        };
    }

    /// <summary>Constructs a cubic-bezier easing function via Newton-Raphson iteration.</summary>
    public static Func<double, double> CubicBezier(double x1, double y1, double x2, double y2)
    {
        return t =>
        {
            if (t <= 0) return 0;
            if (t >= 1) return 1;
            double u = t;
            for (int i = 0; i < 10; i++)
            {
                double bx = 3 * u * (1 - u) * (1 - u) * x1 + 3 * u * u * (1 - u) * x2 + u * u * u - t;
                // True derivative dx/du of the cubic-bezier x(u):
                //   3(1-u)²·x1 + 6(1-u)u·(x2-x1) + 3u²·(1-x2)
                double dbx = 3 * (1 - u) * (1 - u) * x1
                           + 6 * (1 - u) * u * (x2 - x1)
                           + 3 * u * u * (1 - x2);
                if (Math.Abs(dbx) < 1e-8) break;
                u -= bx / dbx;
                u = Math.Max(0, Math.Min(1, u));
            }
            return 3 * u * (1 - u) * (1 - u) * y1 + 3 * u * u * (1 - u) * y2 + u * u * u;
        };
    }
}
