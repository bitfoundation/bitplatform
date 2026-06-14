namespace Bit.Bmotion.Engine;

/// <summary>
/// Pure-C# RGBA color parsing and linear interpolation.
/// Handles #hex, rgb(), rgba(), hsl(), and hsla() formats.
/// </summary>
internal static class ColorInterpolator
{
    /// <summary>Linearly interpolates between two CSS color strings at progress <paramref name="t"/> (0–1).</summary>
    public static string Lerp(string from, string to, double t)
    {
        var f = Parse(from);
        var tt = Parse(to);
        if (f == null || tt == null) return to;

        int r = (int)Math.Round(f[0] + (tt[0] - f[0]) * t);
        int g = (int)Math.Round(f[1] + (tt[1] - f[1]) * t);
        int b = (int)Math.Round(f[2] + (tt[2] - f[2]) * t);
        double a = f[3] + (tt[3] - f[3]) * t;
        return $"rgba({r},{g},{b},{a:G4})";
    }

    /// <summary>Returns true if the CSS string looks like a color value.</summary>
    public static bool LooksLikeColor(string? value)
        => value != null &&
           (value.StartsWith('#') ||
            value.StartsWith("rgb", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("hsl", StringComparison.OrdinalIgnoreCase));

    // ── Internal ──────────────────────────────────────────────────────────────

    private static double[]? Parse(string c)
    {
        if (string.IsNullOrEmpty(c)) return null;

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
        var m = System.Text.RegularExpressions.Regex.Match(
            c, @"rgba?\(\s*([\d.]+)\s*,\s*([\d.]+)\s*,\s*([\d.]+)(?:\s*,\s*([\d.]+))?\s*\)");
        if (m.Success)
        {
            return
            [
                double.Parse(m.Groups[1].Value),
                double.Parse(m.Groups[2].Value),
                double.Parse(m.Groups[3].Value),
                m.Groups[4].Success ? double.Parse(m.Groups[4].Value) : 1.0,
            ];
        }

        // hsl() / hsla()
        var mh = System.Text.RegularExpressions.Regex.Match(
            c, @"hsla?\(\s*([\d.]+)\s*,\s*([\d.]+)%?\s*,\s*([\d.]+)%?(?:\s*,\s*([\d.]+))?\s*\)");
        if (mh.Success)
        {
            double h2  = double.Parse(mh.Groups[1].Value);
            double s2  = double.Parse(mh.Groups[2].Value) / 100.0;
            double l2  = double.Parse(mh.Groups[3].Value) / 100.0;
            double a2  = mh.Groups[4].Success ? double.Parse(mh.Groups[4].Value) : 1.0;
            var rgb2 = HslToRgb(h2, s2, l2);
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
