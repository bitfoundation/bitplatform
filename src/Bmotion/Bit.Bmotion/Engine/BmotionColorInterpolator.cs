namespace Bit.Bmotion;
/// <summary>
/// Pure-C# RGBA color parsing and linear interpolation.
/// Handles #hex, rgb(), rgba(), hsl(), and hsla() formats.
/// </summary>
internal static partial class BmotionColorInterpolator
{
    // Source-generated regexes: faster than the static Regex cache and trim-safe (no runtime
    // IL emit), which matters because this assembly is built with IsTrimmable=true.
    [System.Text.RegularExpressions.GeneratedRegex(
        @"^rgba?\(\s*([\d.]+)\s*,\s*([\d.]+)\s*,\s*([\d.]+)(?:\s*,\s*([\d.]+))?\s*\)$",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase)]
    private static partial System.Text.RegularExpressions.Regex RgbRegex();

    [System.Text.RegularExpressions.GeneratedRegex(
        @"^hsla?\(\s*([\d.]+)\s*,\s*([\d.]+)%?\s*,\s*([\d.]+)%?(?:\s*,\s*([\d.]+))?\s*\)$",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase)]
    private static partial System.Text.RegularExpressions.Regex HslRegex();
    /// <summary>
    /// Linearly interpolates between two CSS color strings at progress <paramref name="t"/> (0–1).
    /// <para>
    /// Interpolation is performed per-channel in the sRGB color space (matching Framer Motion's
    /// default). This is fast and predictable, but mixing complementary colors can pass through a
    /// desaturated mid-point (e.g. blue→yellow briefly looks grey). A perceptual space such as
    /// OKLab would avoid that at extra cost; sRGB is intentional here for parity and performance.
    /// </para>
    /// </summary>
    public static string Lerp(string from, string to, double t)
    {
        var f = Parse(from);
        var tt = Parse(to);
        if (f == null || tt == null) return to;
        return Lerp(f, tt, t);
    }

    /// <summary>
    /// Interpolates between two pre-parsed RGBA channel arrays (as produced by <see cref="Parse"/>).
    /// Drivers parse their colors once up-front and call this each tick, avoiding the per-frame
    /// regex/parse cost of the string overload.
    /// </summary>
    public static string Lerp(double[] from, double[] to, double t)
        => Lerp(from, to, t, BmColorSpace.Srgb);

    /// <summary>
    /// Interpolates between two pre-parsed RGBA channel arrays in the given color space. sRGB mixes
    /// per-channel; OKLab converts to a perceptual space first so mid-points stay saturated.
    /// </summary>
    public static string Lerp(double[] from, double[] to, double t, BmColorSpace space)
    {
        if (space == BmColorSpace.Oklab)
            return LerpOklab(from, to, t);

        int r = (int)Math.Round(Math.Clamp(from[0] + (to[0] - from[0]) * t, 0, 255));
        int g = (int)Math.Round(Math.Clamp(from[1] + (to[1] - from[1]) * t, 0, 255));
        int b = (int)Math.Round(Math.Clamp(from[2] + (to[2] - from[2]) * t, 0, 255));
        double a = Math.Clamp(from[3] + (to[3] - from[3]) * t, 0, 1);
        return $"rgba({r},{g},{b},{BmotionCssFormat.Num(a, "G4")})";
    }

    // ── OKLab perceptual interpolation (Björn Ottosson) ───────────────────────

    private static string LerpOklab(double[] from, double[] to, double t)
    {
        var (l1, a1, b1) = RgbToOklab(from[0], from[1], from[2]);
        var (l2, a2, b2) = RgbToOklab(to[0], to[1], to[2]);
        double l = l1 + (l2 - l1) * t;
        double aa = a1 + (a2 - a1) * t;
        double bb = b1 + (b2 - b1) * t;
        double alpha = Math.Clamp(from[3] + (to[3] - from[3]) * t, 0, 1);

        var (r, g, b) = OklabToRgb(l, aa, bb);
        return $"rgba({r},{g},{b},{BmotionCssFormat.Num(alpha, "G4")})";
    }

    private static (double L, double A, double B) RgbToOklab(double r255, double g255, double b255)
    {
        double r = LinearizeSrgb(r255 / 255.0);
        double g = LinearizeSrgb(g255 / 255.0);
        double b = LinearizeSrgb(b255 / 255.0);

        double l = 0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b;
        double m = 0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b;
        double s = 0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b;

        double l_ = Math.Cbrt(l), m_ = Math.Cbrt(m), s_ = Math.Cbrt(s);

        return (
            0.2104542553 * l_ + 0.7936177850 * m_ - 0.0040720468 * s_,
            1.9779984951 * l_ - 2.4285922050 * m_ + 0.4505937099 * s_,
            0.0259040371 * l_ + 0.7827717662 * m_ - 0.8086757660 * s_);
    }

    private static (int R, int G, int B) OklabToRgb(double L, double A, double B)
    {
        double l_ = L + 0.3963377774 * A + 0.2158037573 * B;
        double m_ = L - 0.1055613458 * A - 0.0638541728 * B;
        double s_ = L - 0.0894841775 * A - 1.2914855480 * B;

        double l = l_ * l_ * l_, m = m_ * m_ * m_, s = s_ * s_ * s_;

        double r = +4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s;
        double g = -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s;
        double b = -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s;

        return (
            (int)Math.Round(Math.Clamp(DelinearizeSrgb(r) * 255, 0, 255)),
            (int)Math.Round(Math.Clamp(DelinearizeSrgb(g) * 255, 0, 255)),
            (int)Math.Round(Math.Clamp(DelinearizeSrgb(b) * 255, 0, 255)));
    }

    private static double LinearizeSrgb(double c)
        => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

    private static double DelinearizeSrgb(double c)
        => c <= 0.0031308 ? c * 12.92 : 1.055 * Math.Pow(c, 1.0 / 2.4) - 0.055;

    /// <summary>Returns true if the CSS string looks like a color value.</summary>
    public static bool LooksLikeColor(string? value)
    {
        if (value is null) return false;
        // Trim first so padded inputs like " #fff " or " rgb(...) " match, consistent with Parse.
        var v = value.AsSpan().Trim();
        return v.StartsWith("#") ||
               v.StartsWith("rgb", StringComparison.OrdinalIgnoreCase) ||
               v.StartsWith("hsl", StringComparison.OrdinalIgnoreCase);
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses a CSS color into its <c>[r, g, b, a]</c> channels (0–255 for RGB, 0–1 for alpha),
    /// or returns <c>null</c> when the string is not a recognised color. Exposed to drivers so
    /// they can parse keyframes once instead of on every tick.
    /// </summary>
    internal static double[]? Parse(string c)
    {
        if (string.IsNullOrWhiteSpace(c)) return null;
        // Trim surrounding whitespace so padded inputs like " #fff " or " rgb(...) " still match the
        // prefix checks and regexes below instead of falling through to the unparseable-null path.
        c = c.Trim();

        if (c.StartsWith('#'))
        {
            var h = c[1..];
            // Expand shorthand #rgb → #rrggbb, #rgba → #rrggbbaa
            if (h.Length == 3 || h.Length == 4)
                h = string.Concat(h.Select(ch => $"{ch}{ch}"));
            if (h.Length is not (6 or 8)) return null;
            if (!TryHex(h[..2], out int r) ||
                !TryHex(h[2..4], out int g) ||
                !TryHex(h[4..6], out int b))
                return null;
            double alpha = 1.0;
            if (h.Length == 8)
            {
                if (!TryHex(h[6..8], out int a)) return null;
                alpha = a / 255.0;
            }
            return [r, g, b, alpha];
        }

        // rgb() / rgba()
        var m = RgbRegex().Match(c);
        if (m.Success)
        {
            // Use TryParse so malformed numerics like "1..2" fall back to null (and the Lerp
            // string overload then returns the target) instead of throwing a FormatException.
            if (!BmotionCssFormat.TryParse(m.Groups[1].Value, out double mr) ||
                !BmotionCssFormat.TryParse(m.Groups[2].Value, out double mg) ||
                !BmotionCssFormat.TryParse(m.Groups[3].Value, out double mb))
                return null;
            double ma = 1.0;
            if (m.Groups[4].Success && !BmotionCssFormat.TryParse(m.Groups[4].Value, out ma))
                return null;
            return [mr, mg, mb, ma];
        }

        // hsl() / hsla()
        var mh = HslRegex().Match(c);
        if (mh.Success)
        {
            if (!BmotionCssFormat.TryParse(mh.Groups[1].Value, out double h2) ||
                !BmotionCssFormat.TryParse(mh.Groups[2].Value, out double s2raw) ||
                !BmotionCssFormat.TryParse(mh.Groups[3].Value, out double l2raw))
                return null;
            double a2 = 1.0;
            if (mh.Groups[4].Success && !BmotionCssFormat.TryParse(mh.Groups[4].Value, out a2))
                return null;
            var rgb2 = HslToRgb(h2, s2raw / 100.0, l2raw / 100.0);
            return [rgb2[0], rgb2[1], rgb2[2], a2];
        }

        return null;
    }

    private static bool TryHex(string s, out int value)
        => int.TryParse(s, System.Globalization.NumberStyles.HexNumber,
            System.Globalization.CultureInfo.InvariantCulture, out value);

    private static double[] HslToRgb(double h, double s, double l)
    {
        h = ((h % 360) + 360) % 360; // normalise to 0-360
        double c = (1 - Math.Abs(2 * l - 1)) * s;
        double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        double m = l - c / 2;
        double r, g, b;
        if      (h < 60)  { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else              { r = c; g = 0; b = x; }
        return [(r + m) * 255, (g + m) * 255, (b + m) * 255];
    }
}
