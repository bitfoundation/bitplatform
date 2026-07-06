
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
            _                => _easeOut,
        };
    }

    /// <summary>Returns a CSS easing string for use with the Web Animations API (FLIP).</summary>
    public static string ToCssString(BmotionTransitionConfig? config)
    {
        if (config == null) return "ease";
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
