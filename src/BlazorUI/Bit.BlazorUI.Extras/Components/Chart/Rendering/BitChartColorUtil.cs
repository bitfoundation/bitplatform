using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>Color helpers (parse hex/rgb/rgba, apply alpha, build palettes).</summary>
public static class BitChartColorUtil
{
    /// <summary>The Chart.js-style default palette.</summary>
    public static readonly string[] DefaultPalette =
    {
        "#36a2eb", // blue
        "#ff6384", // red
        "#4bc0c0", // teal
        "#ff9f40", // orange
        "#9966ff", // purple
        "#ffcd56", // yellow
        "#c9cbcf", // grey
        "#2ecc71", // green
        "#e74c3c", // dark red
        "#34495e"  // navy
    };

    public static string Palette(int i) => DefaultPalette[((i % DefaultPalette.Length) + DefaultPalette.Length) % DefaultPalette.Length];

    /// <summary>Returns the color with the given alpha (0-1), parsing hex/rgb/rgba.</summary>
    public static string WithAlpha(string color, double alpha)
    {
        if (TryParse(color, out var r, out var g, out var b, out _))
            return $"rgba({r},{g},{b},{alpha.ToString("0.###", CultureInfo.InvariantCulture)})";
        return color;
    }

    /// <summary>Lighten/darken a color by a factor (-1..1; positive lightens).</summary>
    public static string Adjust(string color, double factor)
    {
        if (!TryParse(color, out var r, out var g, out var b, out var a)) return color;
        if (factor >= 0)
        {
            r = (int)(r + (255 - r) * factor);
            g = (int)(g + (255 - g) * factor);
            b = (int)(b + (255 - b) * factor);
        }
        else
        {
            var f = 1 + factor;
            r = (int)(r * f); g = (int)(g * f); b = (int)(b * f);
        }
        return $"rgba({Clamp(r)},{Clamp(g)},{Clamp(b)},{a.ToString("0.###", CultureInfo.InvariantCulture)})";
    }

    private static int Clamp(int v) => Math.Max(0, Math.Min(255, v));

    public static bool TryParse(string color, out int r, out int g, out int b, out double a)
    {
        r = g = b = 0; a = 1;
        if (string.IsNullOrWhiteSpace(color)) return false;
        color = color.Trim();

        if (color.StartsWith('#'))
        {
            var hex = color[1..];
            if (hex.Length == 3)
            {
                r = Convert.ToInt32($"{hex[0]}{hex[0]}", 16);
                g = Convert.ToInt32($"{hex[1]}{hex[1]}", 16);
                b = Convert.ToInt32($"{hex[2]}{hex[2]}", 16);
                return true;
            }
            if (hex.Length == 6 || hex.Length == 8)
            {
                r = Convert.ToInt32(hex.Substring(0, 2), 16);
                g = Convert.ToInt32(hex.Substring(2, 2), 16);
                b = Convert.ToInt32(hex.Substring(4, 2), 16);
                if (hex.Length == 8)
                    a = Convert.ToInt32(hex.Substring(6, 2), 16) / 255.0;
                return true;
            }
            return false;
        }

        if (color.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
        {
            var open = color.IndexOf('(');
            var close = color.IndexOf(')');
            if (open < 0 || close < 0) return false;
            var parts = color[(open + 1)..close].Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 3) return false;
            if (!int.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out r)) return false;
            if (!int.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out g)) return false;
            if (!int.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out b)) return false;
            if (parts.Length >= 4) double.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out a);
            return true;
        }

        return false;
    }
}
