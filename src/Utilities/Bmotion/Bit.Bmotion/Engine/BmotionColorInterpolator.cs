namespace Bit.Bmotion;
/// <summary>
/// Pure-C# CSS color parsing and linear interpolation. Parses every color syntax a stylesheet or
/// <c>getComputedStyle</c> produces - hex, named colors and <c>transparent</c>, <c>rgb()</c>/<c>hsl()</c> in
/// both the legacy comma and the modern space syntax, <c>hwb()</c>, <c>lab()</c>/<c>lch()</c>,
/// <c>oklab()</c>/<c>oklch()</c> and <c>color(srgb | srgb-linear | display-p3 …)</c> - into sRGB channels,
/// and interpolates them in sRGB or OKLab.
/// </summary>
internal static partial class BmotionColorInterpolator
{
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
    /// per-channel; OKLab converts to a perceptual space first so mid-points stay saturated. Both mix with
    /// premultiplied alpha, as CSS does: a fade from <c>transparent</c> to red is red growing more opaque, not a
    /// darkening through translucent black - and it is what the browser plays, so a browser-played colour
    /// animation and the frame loop agree.
    /// </summary>
    public static string Lerp(double[] from, double[] to, double t, BmColorSpace space)
    {
        double a = Math.Clamp(from[3] + (to[3] - from[3]) * t, 0, 1);
        if (space == BmColorSpace.Oklab)
            return LerpOklab(from, to, t, a);

        int r = (int)Math.Round(Math.Clamp(Unpremultiply(from[0], from[3], to[0], to[3], t, a), 0, 255));
        int g = (int)Math.Round(Math.Clamp(Unpremultiply(from[1], from[3], to[1], to[3], t, a), 0, 255));
        int b = (int)Math.Round(Math.Clamp(Unpremultiply(from[2], from[3], to[2], to[3], t, a), 0, 255));
        return $"rgba({r},{g},{b},{BmotionCssFormat.Num(a, "G4")})";
    }

    // One channel mixed premultiplied: interpolate channel × alpha, divide by the interpolated alpha. A fully
    // transparent result has no colour; its channels fall back to the plain mix.
    private static double Unpremultiply(double c1, double a1, double c2, double a2, double t, double alpha)
        => alpha > 1e-9 ? ((c1 * a1) + (((c2 * a2) - (c1 * a1)) * t)) / alpha : c1 + ((c2 - c1) * t);

    /// <summary>A parsed colour as a legacy <c>rgba()</c> string - which the browser interpolates in sRGB.</summary>
    internal static string ToRgbaCss(double[] c)
        => $"rgba({(int)Math.Round(Math.Clamp(c[0], 0, 255))},{(int)Math.Round(Math.Clamp(c[1], 0, 255))},{(int)Math.Round(Math.Clamp(c[2], 0, 255))},{BmotionCssFormat.Num(Math.Clamp(c[3], 0, 1), "G4")})";

    /// <summary>A parsed colour as an <c>oklab()</c> string - which the browser interpolates in OKLab.</summary>
    internal static string ToOklabCss(double[] c)
    {
        var (l, a, b) = RgbToOklab(c[0], c[1], c[2]);
        return $"oklab({BmotionCssFormat.Num(l)} {BmotionCssFormat.Num(a)} {BmotionCssFormat.Num(b)} / {BmotionCssFormat.Num(Math.Clamp(c[3], 0, 1), "G4")})";
    }

    // ── OKLab perceptual interpolation (Björn Ottosson) ───────────────────────

    private static string LerpOklab(double[] from, double[] to, double t, double alpha)
    {
        var (l1, a1, b1) = RgbToOklab(from[0], from[1], from[2]);
        var (l2, a2, b2) = RgbToOklab(to[0], to[1], to[2]);
        double l = Unpremultiply(l1, from[3], l2, to[3], t, alpha);
        double aa = Unpremultiply(a1, from[3], a2, to[3], t, alpha);
        double bb = Unpremultiply(b1, from[3], b2, to[3], t, alpha);

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
        var (r, g, b) = OklabToLinearRgb(L, A, B);
        return (
            (int)Math.Round(Math.Clamp(DelinearizeSrgb(r) * 255, 0, 255)),
            (int)Math.Round(Math.Clamp(DelinearizeSrgb(g) * 255, 0, 255)),
            (int)Math.Round(Math.Clamp(DelinearizeSrgb(b) * 255, 0, 255)));
    }

    private static (double R, double G, double B) OklabToLinearRgb(double L, double A, double B)
    {
        double l_ = L + 0.3963377774 * A + 0.2158037573 * B;
        double m_ = L - 0.1055613458 * A - 0.0638541728 * B;
        double s_ = L - 0.0894841775 * A - 1.2914855480 * B;

        double l = l_ * l_ * l_, m = m_ * m_ * m_, s = s_ * s_ * s_;

        return (
            +4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
            -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
            -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s);
    }

    private static double LinearizeSrgb(double c)
        => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

    private static double DelinearizeSrgb(double c)
    {
        // Gamma-encode symmetrically for out-of-range linear values (a wide-gamut color converted to sRGB), which
        // are clamped afterwards: Math.Pow of a negative base is NaN.
        double magnitude = Math.Abs(c);
        double encoded = magnitude <= 0.0031308 ? magnitude * 12.92 : 1.055 * Math.Pow(magnitude, 1.0 / 2.4) - 0.055;
        return c < 0 ? -encoded : encoded;
    }

    /// <summary>
    /// Returns true if the CSS string is a color this interpolator can parse - so it can animate - rather than
    /// some other value.
    /// </summary>
    public static bool LooksLikeColor(string? value) => value is not null && Parse(value) is not null;

    // ── Parsing ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses a CSS color into its <c>[r, g, b, a]</c> channels (0–255 for RGB, 0–1 for alpha),
    /// or returns <c>null</c> when the string is not a recognised color. Exposed to drivers so
    /// they can parse keyframes once instead of on every tick. Colors outside the sRGB gamut
    /// (a saturated <c>oklch()</c>, a <c>display-p3</c> color) are clamped into it; the drivers land on the
    /// authored string itself at the end, so only the in-between frames are approximated.
    /// </summary>
    internal static double[]? Parse(string c)
    {
        if (string.IsNullOrWhiteSpace(c)) return null;
        // Trim surrounding whitespace so padded inputs like " #fff " or " rgb(...) " still parse.
        var span = c.AsSpan().Trim();

        if (span[0] == '#') return ParseHex(span[1..]);

        int open = span.IndexOf('(');
        if (open < 0)
        {
            // Keywords: transparent, and the named colors.
            if (span.Equals("transparent", StringComparison.OrdinalIgnoreCase)) return [0, 0, 0, 0];
            return NamedColors.TryGetValue(span.ToString(), out int rgb)
                ? [(rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF, 1]
                : null;
        }

        if (span[^1] != ')') return null;
        var name = span[..open].Trim().ToString().ToLowerInvariant();
        if (!TrySplitArguments(span[(open + 1)..^1], out var args, out var alphaToken)) return null;

        double alpha = 1;
        if (alphaToken is not null && !TryAlpha(alphaToken, out alpha)) return null;

        double[]? rgba = name switch
        {
            "rgb" or "rgba" => ParseRgb(args),
            "hsl" or "hsla" => ParseHsl(args),
            "hwb" => ParseHwb(args),
            "oklab" => ParseOklab(args),
            "oklch" => ParseOklch(args),
            "lab" => ParseLab(args),
            "lch" => ParseLch(args),
            "color" => ParseColorFunction(args),
            _ => null,
        };
        if (rgba is null) return null;
        rgba[3] = alpha;
        return rgba;
    }

    private static double[]? ParseHex(ReadOnlySpan<char> h)
    {
        // Expand shorthand #rgb → #rrggbb, #rgba → #rrggbbaa
        Span<char> full = stackalloc char[8];
        int length;
        if (h.Length is 3 or 4)
        {
            for (int i = 0; i < h.Length; i++) { full[2 * i] = h[i]; full[2 * i + 1] = h[i]; }
            length = h.Length * 2;
        }
        else if (h.Length is 6 or 8)
        {
            h.CopyTo(full);
            length = h.Length;
        }
        else return null;

        if (!TryHex(full[..2], out int r) || !TryHex(full[2..4], out int g) || !TryHex(full[4..6], out int b))
            return null;
        double alpha = 1.0;
        if (length == 8)
        {
            if (!TryHex(full[6..8], out int a)) return null;
            alpha = a / 255.0;
        }
        return [r, g, b, alpha];
    }

    private static bool TryHex(ReadOnlySpan<char> s, out int value)
        => int.TryParse(s, System.Globalization.NumberStyles.HexNumber,
            System.Globalization.CultureInfo.InvariantCulture, out value);

    /// <summary>
    /// Splits a color function's arguments - comma separated (<c>rgb(1, 2, 3, 0.5)</c>) or space separated with an
    /// optional slash alpha (<c>rgb(1 2 3 / 50%)</c>). A fourth comma argument is the legacy alpha.
    /// </summary>
    private static bool TrySplitArguments(ReadOnlySpan<char> inner, out List<string> args, out string? alpha)
    {
        args = new List<string>(4);
        alpha = null;
        var text = inner.ToString();
        int slash = text.IndexOf('/');
        if (slash >= 0)
        {
            alpha = text[(slash + 1)..].Trim();
            if (alpha.Length == 0) return false;
            text = text[..slash];
        }

        var separators = text.Contains(',') ? new[] { ',' } : new[] { ' ', '\t', '\n', '\r' };
        foreach (var part in text.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            args.Add(part);

        // Legacy rgba(r, g, b, a) / hsla(h, s, l, a): the fourth argument is the alpha.
        if (alpha is null && args.Count == 4 && text.Contains(','))
        {
            alpha = args[3];
            args.RemoveAt(3);
        }
        return args.Count >= 3;
    }

    private static bool TryAlpha(string token, out double alpha)
    {
        if (!TryNumber(token, out alpha, out bool percent)) return false;
        alpha = Math.Clamp(percent ? alpha / 100 : alpha, 0, 1);
        return true;
    }

    /// <summary>A number, a percentage (<paramref name="percent"/> set) or the <c>none</c> keyword (zero).</summary>
    private static bool TryNumber(string token, out double value, out bool percent)
    {
        percent = false;
        if (token.Equals("none", StringComparison.OrdinalIgnoreCase)) { value = 0; return true; }
        if (token.EndsWith('%'))
        {
            percent = true;
            return BmotionCssFormat.TryParse(token[..^1], out value);
        }
        return BmotionCssFormat.TryParse(token, out value);
    }

    /// <summary>A hue in degrees: a bare number, or one with a <c>deg</c>, <c>grad</c>, <c>rad</c> or <c>turn</c> unit.</summary>
    private static bool TryHue(string token, out double degrees)
    {
        var t = token.ToLowerInvariant();
        (string Unit, double Scale)[] units = [("deg", 1), ("grad", 0.9), ("rad", 180 / Math.PI), ("turn", 360)];
        foreach (var (unit, scale) in units)
        {
            if (!t.EndsWith(unit, StringComparison.Ordinal)) continue;
            if (!BmotionCssFormat.TryParse(t[..^unit.Length], out degrees)) return false;
            degrees *= scale;
            return true;
        }
        return TryNumber(t, out degrees, out _);
    }

    private static double[]? ParseRgb(List<string> args)
    {
        var rgb = new double[4];
        for (int i = 0; i < 3; i++)
        {
            if (!TryNumber(args[i], out double v, out bool percent)) return null;
            rgb[i] = Math.Clamp(percent ? v * 2.55 : v, 0, 255);
        }
        rgb[3] = 1;
        return rgb;
    }

    private static double[]? ParseHsl(List<string> args)
    {
        if (!TryHue(args[0], out double h) ||
            !TryNumber(args[1], out double s, out _) ||
            !TryNumber(args[2], out double l, out _))
            return null;
        var rgb = HslToRgb(h, Math.Clamp(s / 100.0, 0, 1), Math.Clamp(l / 100.0, 0, 1));
        return [rgb[0], rgb[1], rgb[2], 1];
    }

    private static double[]? ParseHwb(List<string> args)
    {
        if (!TryHue(args[0], out double h) ||
            !TryNumber(args[1], out double w, out _) ||
            !TryNumber(args[2], out double bl, out _))
            return null;
        w = Math.Clamp(w / 100.0, 0, 1);
        bl = Math.Clamp(bl / 100.0, 0, 1);
        if (w + bl >= 1)
        {
            double grey = w / (w + bl) * 255;
            return [grey, grey, grey, 1];
        }
        var pure = HslToRgb(h, 1, 0.5);
        double scale = 1 - w - bl;
        return [pure[0] * scale + (w * 255), pure[1] * scale + (w * 255), pure[2] * scale + (w * 255), 1];
    }

    // oklab(L a b): L is 0-1 (or 0-100%), a/b are ±0.4 at 100%.
    private static double[]? ParseOklab(List<string> args)
    {
        if (!TryNumber(args[0], out double l, out bool lPercent) ||
            !TryNumber(args[1], out double a, out bool aPercent) ||
            !TryNumber(args[2], out double b, out bool bPercent))
            return null;
        if (lPercent) l /= 100;
        if (aPercent) a *= 0.4 / 100;
        if (bPercent) b *= 0.4 / 100;
        return FromLinearRgb(OklabToLinearRgb(l, a, b));
    }

    // oklch(L C H): C is 0.4 at 100%.
    private static double[]? ParseOklch(List<string> args)
    {
        if (!TryNumber(args[0], out double l, out bool lPercent) ||
            !TryNumber(args[1], out double chroma, out bool cPercent) ||
            !TryHue(args[2], out double h))
            return null;
        if (lPercent) l /= 100;
        if (cPercent) chroma *= 0.4 / 100;
        double rad = h * Math.PI / 180;
        return FromLinearRgb(OklabToLinearRgb(l, chroma * Math.Cos(rad), chroma * Math.Sin(rad)));
    }

    // CIE lab(L a b): L is 0-100, a/b are ±125 at 100%; D50 white.
    private static double[]? ParseLab(List<string> args)
    {
        if (!TryNumber(args[0], out double l, out _) ||
            !TryNumber(args[1], out double a, out bool aPercent) ||
            !TryNumber(args[2], out double b, out bool bPercent))
            return null;
        if (aPercent) a *= 1.25;
        if (bPercent) b *= 1.25;
        return FromLinearRgb(LabToLinearRgb(l, a, b));
    }

    // CIE lch(L C H): C is 150 at 100%.
    private static double[]? ParseLch(List<string> args)
    {
        if (!TryNumber(args[0], out double l, out _) ||
            !TryNumber(args[1], out double chroma, out bool cPercent) ||
            !TryHue(args[2], out double h))
            return null;
        if (cPercent) chroma *= 1.5;
        double rad = h * Math.PI / 180;
        return FromLinearRgb(LabToLinearRgb(l, chroma * Math.Cos(rad), chroma * Math.Sin(rad)));
    }

    // color(<space> c1 c2 c3): sRGB, linear sRGB and Display P3, channels 0-1 (or 0-100%).
    private static double[]? ParseColorFunction(List<string> args)
    {
        if (args.Count < 4) return null;
        var channels = new double[3];
        for (int i = 0; i < 3; i++)
        {
            if (!TryNumber(args[i + 1], out double v, out bool percent)) return null;
            channels[i] = percent ? v / 100 : v;
        }
        switch (args[0].ToLowerInvariant())
        {
            case "srgb":
                return [Math.Clamp(channels[0], 0, 1) * 255, Math.Clamp(channels[1], 0, 1) * 255, Math.Clamp(channels[2], 0, 1) * 255, 1];
            case "srgb-linear":
                return FromLinearRgb((channels[0], channels[1], channels[2]));
            case "display-p3":
            {
                // P3 shares sRGB's transfer curve; convert its linear values through XYZ to linear sRGB.
                double r = LinearizeSrgb(channels[0]), g = LinearizeSrgb(channels[1]), b = LinearizeSrgb(channels[2]);
                return FromLinearRgb((
                    1.2249401 * r - 0.2249404 * g,
                    -0.0420569 * r + 1.0420571 * g,
                    -0.0196376 * r - 0.0786361 * g + 1.0982735 * b));
            }
            default:
                return null;
        }
    }

    private static (double R, double G, double B) LabToLinearRgb(double l, double a, double b)
    {
        const double kappa = 24389.0 / 27, epsilon = 216.0 / 24389;
        double fy = (l + 16) / 116, fx = fy + (a / 500), fz = fy - (b / 200);
        double Inverse(double f) => f * f * f > epsilon ? f * f * f : ((116 * f) - 16) / kappa;
        double xD50 = 0.96422 * Inverse(fx);
        double yD50 = l > kappa * epsilon ? Math.Pow((l + 16) / 116, 3) : l / kappa;
        double zD50 = 0.82521 * Inverse(fz);

        // Bradford adaptation D50 → D65, then XYZ → linear sRGB.
        double x = (0.9554734527042182 * xD50) - (0.023098536874261423 * yD50) + (0.0632593086610217 * zD50);
        double y = (-0.028369706963208136 * xD50) + (1.0099954580058226 * yD50) + (0.021041398966943008 * zD50);
        double z = (0.012314001688319899 * xD50) - (0.020507696433477912 * yD50) + (1.3303659366080753 * zD50);
        return (
            (3.2409699419045226 * x) - (1.537383177570094 * y) - (0.4986107602930034 * z),
            (-0.9692436362808796 * x) + (1.8759675015077202 * y) + (0.04155505740717559 * z),
            (0.05563007969699366 * x) - (0.20397695888897652 * y) + (1.0569715142428786 * z));
    }

    private static double[] FromLinearRgb((double R, double G, double B) linear)
        => [
            Math.Clamp(DelinearizeSrgb(linear.R) * 255, 0, 255),
            Math.Clamp(DelinearizeSrgb(linear.G) * 255, 0, 255),
            Math.Clamp(DelinearizeSrgb(linear.B) * 255, 0, 255),
            1,
        ];

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

    /// <summary>Whether <paramref name="word"/> is one of the CSS named colors (or <c>transparent</c>).</summary>
    internal static bool IsNamedColor(string word)
        => word.Equals("transparent", StringComparison.OrdinalIgnoreCase) || NamedColors.ContainsKey(word);

    // The CSS Color 4 named colors, as 0xRRGGBB.
    private static readonly Dictionary<string, int> NamedColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["aliceblue"] = 0xF0F8FF, ["antiquewhite"] = 0xFAEBD7, ["aqua"] = 0x00FFFF, ["aquamarine"] = 0x7FFFD4,
        ["azure"] = 0xF0FFFF, ["beige"] = 0xF5F5DC, ["bisque"] = 0xFFE4C4, ["black"] = 0x000000,
        ["blanchedalmond"] = 0xFFEBCD, ["blue"] = 0x0000FF, ["blueviolet"] = 0x8A2BE2, ["brown"] = 0xA52A2A,
        ["burlywood"] = 0xDEB887, ["cadetblue"] = 0x5F9EA0, ["chartreuse"] = 0x7FFF00, ["chocolate"] = 0xD2691E,
        ["coral"] = 0xFF7F50, ["cornflowerblue"] = 0x6495ED, ["cornsilk"] = 0xFFF8DC, ["crimson"] = 0xDC143C,
        ["cyan"] = 0x00FFFF, ["darkblue"] = 0x00008B, ["darkcyan"] = 0x008B8B, ["darkgoldenrod"] = 0xB8860B,
        ["darkgray"] = 0xA9A9A9, ["darkgreen"] = 0x006400, ["darkgrey"] = 0xA9A9A9, ["darkkhaki"] = 0xBDB76B,
        ["darkmagenta"] = 0x8B008B, ["darkolivegreen"] = 0x556B2F, ["darkorange"] = 0xFF8C00, ["darkorchid"] = 0x9932CC,
        ["darkred"] = 0x8B0000, ["darksalmon"] = 0xE9967A, ["darkseagreen"] = 0x8FBC8F, ["darkslateblue"] = 0x483D8B,
        ["darkslategray"] = 0x2F4F4F, ["darkslategrey"] = 0x2F4F4F, ["darkturquoise"] = 0x00CED1, ["darkviolet"] = 0x9400D3,
        ["deeppink"] = 0xFF1493, ["deepskyblue"] = 0x00BFFF, ["dimgray"] = 0x696969, ["dimgrey"] = 0x696969,
        ["dodgerblue"] = 0x1E90FF, ["firebrick"] = 0xB22222, ["floralwhite"] = 0xFFFAF0, ["forestgreen"] = 0x228B22,
        ["fuchsia"] = 0xFF00FF, ["gainsboro"] = 0xDCDCDC, ["ghostwhite"] = 0xF8F8FF, ["gold"] = 0xFFD700,
        ["goldenrod"] = 0xDAA520, ["gray"] = 0x808080, ["green"] = 0x008000, ["greenyellow"] = 0xADFF2F,
        ["grey"] = 0x808080, ["honeydew"] = 0xF0FFF0, ["hotpink"] = 0xFF69B4, ["indianred"] = 0xCD5C5C,
        ["indigo"] = 0x4B0082, ["ivory"] = 0xFFFFF0, ["khaki"] = 0xF0E68C, ["lavender"] = 0xE6E6FA,
        ["lavenderblush"] = 0xFFF0F5, ["lawngreen"] = 0x7CFC00, ["lemonchiffon"] = 0xFFFACD, ["lightblue"] = 0xADD8E6,
        ["lightcoral"] = 0xF08080, ["lightcyan"] = 0xE0FFFF, ["lightgoldenrodyellow"] = 0xFAFAD2, ["lightgray"] = 0xD3D3D3,
        ["lightgreen"] = 0x90EE90, ["lightgrey"] = 0xD3D3D3, ["lightpink"] = 0xFFB6C1, ["lightsalmon"] = 0xFFA07A,
        ["lightseagreen"] = 0x20B2AA, ["lightskyblue"] = 0x87CEFA, ["lightslategray"] = 0x778899, ["lightslategrey"] = 0x778899,
        ["lightsteelblue"] = 0xB0C4DE, ["lightyellow"] = 0xFFFFE0, ["lime"] = 0x00FF00, ["limegreen"] = 0x32CD32,
        ["linen"] = 0xFAF0E6, ["magenta"] = 0xFF00FF, ["maroon"] = 0x800000, ["mediumaquamarine"] = 0x66CDAA,
        ["mediumblue"] = 0x0000CD, ["mediumorchid"] = 0xBA55D3, ["mediumpurple"] = 0x9370DB, ["mediumseagreen"] = 0x3CB371,
        ["mediumslateblue"] = 0x7B68EE, ["mediumspringgreen"] = 0x00FA9A, ["mediumturquoise"] = 0x48D1CC, ["mediumvioletred"] = 0xC71585,
        ["midnightblue"] = 0x191970, ["mintcream"] = 0xF5FFFA, ["mistyrose"] = 0xFFE4E1, ["moccasin"] = 0xFFE4B5,
        ["navajowhite"] = 0xFFDEAD, ["navy"] = 0x000080, ["oldlace"] = 0xFDF5E6, ["olive"] = 0x808000,
        ["olivedrab"] = 0x6B8E23, ["orange"] = 0xFFA500, ["orangered"] = 0xFF4500, ["orchid"] = 0xDA70D6,
        ["palegoldenrod"] = 0xEEE8AA, ["palegreen"] = 0x98FB98, ["paleturquoise"] = 0xAFEEEE, ["palevioletred"] = 0xDB7093,
        ["papayawhip"] = 0xFFEFD5, ["peachpuff"] = 0xFFDAB9, ["peru"] = 0xCD853F, ["pink"] = 0xFFC0CB,
        ["plum"] = 0xDDA0DD, ["powderblue"] = 0xB0E0E6, ["purple"] = 0x800080, ["rebeccapurple"] = 0x663399,
        ["red"] = 0xFF0000, ["rosybrown"] = 0xBC8F8F, ["royalblue"] = 0x4169E1, ["saddlebrown"] = 0x8B4513,
        ["salmon"] = 0xFA8072, ["sandybrown"] = 0xF4A460, ["seagreen"] = 0x2E8B57, ["seashell"] = 0xFFF5EE,
        ["sienna"] = 0xA0522D, ["silver"] = 0xC0C0C0, ["skyblue"] = 0x87CEEB, ["slateblue"] = 0x6A5ACD,
        ["slategray"] = 0x708090, ["slategrey"] = 0x708090, ["snow"] = 0xFFFAFA, ["springgreen"] = 0x00FF7F,
        ["steelblue"] = 0x4682B4, ["tan"] = 0xD2B48C, ["teal"] = 0x008080, ["thistle"] = 0xD8BFD8,
        ["tomato"] = 0xFF6347, ["turquoise"] = 0x40E0D0, ["violet"] = 0xEE82EE, ["wheat"] = 0xF5DEB3,
        ["white"] = 0xFFFFFF, ["whitesmoke"] = 0xF5F5F5, ["yellow"] = 0xFFFF00, ["yellowgreen"] = 0x9ACD32,
    };
}
