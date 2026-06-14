using Bit.Bmotion.Models;

namespace Bit.Bmotion.Engine;

/// <summary>
/// Pure-C# easing functions. Ported from the original JS implementation.
/// Cached delegates avoid re-allocation for common easing types.
/// </summary>
internal static class EasingFunctions
{
    // ── Pre-built delegates for common easings ────────────────────────────────
    private static readonly Func<double, double> _easeIn    = CubicBezier(0.42, 0, 1, 1);
    private static readonly Func<double, double> _easeOut   = CubicBezier(0, 0, 0.58, 1);
    private static readonly Func<double, double> _easeInOut = CubicBezier(0.42, 0, 0.58, 1);
    private static readonly Func<double, double> _backIn    = CubicBezier(0.31455, -0.37755, 0.69245, 1.37755);
    private static readonly Func<double, double> _backOut   = CubicBezier(0.33915, 0, 0.68085, 1.4);
    private static readonly Func<double, double> _backInOut = CubicBezier(0.68987, -0.45, 0.32, 1.45);

    /// <summary>Returns an easing function for the given transition config.</summary>
    public static Func<double, double> Get(TransitionConfig config)
    {
        if (config.EaseCubicBezier is { Length: 4 } cb)
            return CubicBezier(cb[0], cb[1], cb[2], cb[3]);

        return config.Ease switch
        {
            Easing.Linear    => t => t,
            Easing.EaseIn    => _easeIn,
            Easing.EaseOut   => _easeOut,
            Easing.EaseInOut => _easeInOut,
            Easing.CircIn    => t => 1 - Math.Sqrt(1 - t * t),
            Easing.CircOut   => t => Math.Sqrt(1 - (t - 1) * (t - 1)),
            Easing.CircInOut => t => t < 0.5
                ? (1 - Math.Sqrt(1 - 4 * t * t)) / 2
                : (Math.Sqrt(1 - Math.Pow(2 * t - 2, 2)) + 1) / 2,
            Easing.BackIn    => _backIn,
            Easing.BackOut   => _backOut,
            Easing.BackInOut => _backInOut,
            Easing.Anticipate => t => t < 0.5
                ? _backIn(t * 2) / 2
                : _easeOut(t * 2 - 1) / 2 + 0.5,
            _                => _easeOut,
        };
    }

    /// <summary>Returns a CSS easing string for use with the Web Animations API (FLIP).</summary>
    public static string ToCssString(TransitionConfig? config)
    {
        if (config == null) return "ease";
        if (config.EaseCubicBezier is { Length: 4 } cb)
            return $"cubic-bezier({cb[0]},{cb[1]},{cb[2]},{cb[3]})";
        return config.Ease switch
        {
            Easing.Linear    => "linear",
            Easing.EaseIn    => "ease-in",
            Easing.EaseOut   => "ease-out",
            Easing.EaseInOut => "ease-in-out",
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
