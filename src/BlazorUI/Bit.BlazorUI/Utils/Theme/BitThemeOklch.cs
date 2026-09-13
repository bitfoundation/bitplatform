using System;

namespace Bit.BlazorUI;

/// <summary>
/// Minimal OKLCH ⇄ sRGB conversion for theme color derivation (Björn Ottosson's OKLab, D65).
/// OKLab lightness is perceptually uniform, so equal L steps read as equal lightness changes for
/// every hue - unlike HSV value, which under-shifts bright saturated hues (yellow) and over-shifts
/// dark ones. Out-of-gamut results are mapped back to sRGB by reducing chroma at constant
/// lightness and hue, the same strategy CSS <c>oklch()</c> rendering effectively applies.
/// </summary>
internal static class BitThemeOklch
{
    /// <summary>Converts an 8-bit sRGB color to OKLCH (L in [0,1], C ≥ 0, H in degrees [0,360)).</summary>
    internal static (double L, double C, double H) FromRgb(byte r8, byte g8, byte b8)
    {
        var (l, a, b) = LinearSrgbToOklab(Linearize(r8 / 255.0), Linearize(g8 / 255.0), Linearize(b8 / 255.0));

        var c = Math.Sqrt((a * a) + (b * b));
        var h = Math.Atan2(b, a) * 180.0 / Math.PI;
        if (h < 0) h += 360.0;

        return (l, c, h);
    }

    /// <summary>
    /// Converts OKLCH to 8-bit sRGB. Lightness is clamped to [0,1]; a chroma that sRGB cannot
    /// represent at that lightness/hue is reduced (binary search, constant L and H) until the
    /// color fits the gamut, so hue and perceived lightness are preserved over saturation.
    /// </summary>
    internal static (byte R, byte G, byte B) ToRgb(double l, double c, double h)
    {
        l = Math.Clamp(l, 0.0, 1.0);
        c = Math.Max(c, 0.0);

        if (TryToLinearSrgb(l, c, h, out var rgb) is false)
        {
            // c = 0 is achromatic (pure L gray) and always inside the gamut, so the search space
            // [0, c] is guaranteed to contain a representable chroma; 24 halvings bring the
            // interval below any 8-bit quantization step.
            double lo = 0.0, hi = c;
            for (var i = 0; i < 24; i++)
            {
                var mid = (lo + hi) / 2.0;
                if (TryToLinearSrgb(l, mid, h, out var candidate))
                {
                    lo = mid;
                    rgb = candidate;
                }
                else
                {
                    hi = mid;
                }
            }
        }

        return (ToByte(rgb.R), ToByte(rgb.G), ToByte(rgb.B));
    }

    /// <summary>Formats gamut-mapped OKLCH as an uppercase <c>#RRGGBB</c> hex string.</summary>
    internal static string ToHex(double l, double c, double h)
    {
        var (r, g, b) = ToRgb(l, c, h);
        return FormattableString.Invariant($"#{r:X2}{g:X2}{b:X2}");
    }

    private static (double L, double A, double B) LinearSrgbToOklab(double r, double g, double b)
    {
        var l = Math.Cbrt((0.4122214708 * r) + (0.5363325363 * g) + (0.0514459929 * b));
        var m = Math.Cbrt((0.2119034982 * r) + (0.6806995451 * g) + (0.1073969566 * b));
        var s = Math.Cbrt((0.0883024619 * r) + (0.2817188376 * g) + (0.6299787005 * b));

        return ((0.2104542553 * l) + (0.7936177850 * m) - (0.0040720468 * s),
                (1.9779984951 * l) - (2.4285922050 * m) + (0.4505937099 * s),
                (0.0259040371 * l) + (0.7827717662 * m) - (0.8086757660 * s));
    }

    private static bool TryToLinearSrgb(double l, double c, double h, out (double R, double G, double B) rgb)
    {
        var hRad = h * Math.PI / 180.0;
        var a = c * Math.Cos(hRad);
        var b = c * Math.Sin(hRad);

        var l_ = l + (0.3963377774 * a) + (0.2158037573 * b);
        var m_ = l - (0.1055613458 * a) - (0.0638541728 * b);
        var s_ = l - (0.0894841775 * a) - (1.2914855480 * b);

        var l3 = l_ * l_ * l_;
        var m3 = m_ * m_ * m_;
        var s3 = s_ * s_ * s_;

        rgb = ((+4.0767416621 * l3) - (3.3077115913 * m3) + (0.2309699292 * s3),
               (-1.2684380046 * l3) + (2.6097574011 * m3) - (0.3413193965 * s3),
               (-0.0041960863 * l3) - (0.7034186147 * m3) + (1.7076147010 * s3));

        // Small negative/overshoot values are rounding noise, not genuine gamut misses; treat
        // anything within this epsilon as inside so the chroma search doesn't desaturate a color
        // that would round to a representable 8-bit value anyway.
        const double epsilon = 1e-6;
        return rgb.R >= -epsilon && rgb.R <= 1 + epsilon
            && rgb.G >= -epsilon && rgb.G <= 1 + epsilon
            && rgb.B >= -epsilon && rgb.B <= 1 + epsilon;
    }

    private static double Linearize(double channel)
    {
        return channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
    }

    private static byte ToByte(double linearChannel)
    {
        var clamped = Math.Clamp(linearChannel, 0.0, 1.0);
        var srgb = clamped <= 0.0031308 ? clamped * 12.92 : (1.055 * Math.Pow(clamped, 1.0 / 2.4)) - 0.055;
        return (byte)Math.Clamp(Math.Round(srgb * 255.0), 0.0, 255.0);
    }
}
