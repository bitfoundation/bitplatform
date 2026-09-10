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

    /// <summary>Expands one hex digit into a byte (the #rgb short form).</summary>
    private static bool Nibble(char c, out int value)
    {
        if (!int.TryParse(stackalloc char[] { c }, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
        {
            value = 0;
            return false;
        }
        value = value * 17;
        return true;
    }

    private static bool Byte(ReadOnlySpan<char> pair, out int value)
        => int.TryParse(pair, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);

    public static bool TryParse(string color, out int r, out int g, out int b, out double a)
    {
        r = g = b = 0; a = 1;
        if (string.IsNullOrWhiteSpace(color)) return false;
        color = color.Trim();

        if (color.StartsWith('#'))
        {
            // Parsed rather than converted: a stray character in a color string is a typo, not a reason
            // for the whole chart to throw out of its render.
            var hex = color.AsSpan(1);
            if (hex.Length == 3 || hex.Length == 4)
            {
                if (!Nibble(hex[0], out r) || !Nibble(hex[1], out g) || !Nibble(hex[2], out b)) return false;
                if (hex.Length == 4)
                {
                    if (!Nibble(hex[3], out var na)) return false;
                    a = na / 255.0;
                }
                return true;
            }
            if (hex.Length == 6 || hex.Length == 8)
            {
                if (!Byte(hex[..2], out r) || !Byte(hex.Slice(2, 2), out g) || !Byte(hex.Slice(4, 2), out b)) return false;
                if (hex.Length == 8)
                {
                    if (!Byte(hex.Slice(6, 2), out var ba)) return false;
                    a = ba / 255.0;
                }
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
