using System.Globalization;

namespace Bit.Bink;

/// <summary>
/// An 8-bit-per-channel RGBA color. Immutable and allocation-free.
/// </summary>
/// <param name="R">Red channel (0-255).</param>
/// <param name="G">Green channel (0-255).</param>
/// <param name="B">Blue channel (0-255).</param>
/// <param name="A">Alpha channel (0-255, 255 = opaque).</param>
public readonly record struct BinkColor(byte R, byte G, byte B, byte A = 255)
{
    /// <summary>Opaque black.</summary>
    public static readonly BinkColor Black = new(0, 0, 0);

    /// <summary>Opaque white.</summary>
    public static readonly BinkColor White = new(255, 255, 255);

    /// <summary>Fully transparent black.</summary>
    public static readonly BinkColor Transparent = new(0, 0, 0, 0);

    /// <summary>True when the color is fully transparent.</summary>
    public bool IsTransparent => A == 0;

    /// <summary>True when the color is fully opaque.</summary>
    public bool IsOpaque => A == 255;

    /// <summary>Alpha as a 0..1 value.</summary>
    public float Alpha => A / 255f;

    /// <summary>Returns the color with a different alpha (0..1).</summary>
    public BinkColor WithAlpha(float alpha) => this with { A = ToByte(alpha) };

    /// <summary>Creates a color from a packed 0xAARRGGBB value.</summary>
    public static BinkColor FromArgb(uint argb)
        => new((byte)(argb >> 16), (byte)(argb >> 8), (byte)argb, (byte)(argb >> 24));

    /// <summary>Creates a color from 0..1 floating point channels.</summary>
    public static BinkColor FromScRgb(float r, float g, float b, float a = 1f)
        => new(ToByte(r), ToByte(g), ToByte(b), ToByte(a));

    private static byte ToByte(float v) => (byte)Math.Clamp(MathF.Round(v * 255f), 0, 255);

    /// <summary>Packs the color as 0xAARRGGBB.</summary>
    public uint ToArgb() => ((uint)A << 24) | ((uint)R << 16) | ((uint)G << 8) | B;

    /// <summary>
    /// Returns a CSS color string: <c>#rrggbb</c> when opaque, otherwise <c>#rrggbbaa</c>.
    /// The output only ever contains <c>#</c> and hex digits, so it is safe to embed in CSS, SVG and JSON.
    /// </summary>
    public string ToCss()
        => A == 255
            ? string.Create(7, this, static (span, c) => { span[0] = '#'; Hex(span, 1, c.R); Hex(span, 3, c.G); Hex(span, 5, c.B); })
            : string.Create(9, this, static (span, c) => { span[0] = '#'; Hex(span, 1, c.R); Hex(span, 3, c.G); Hex(span, 5, c.B); Hex(span, 7, c.A); });

    private static void Hex(Span<char> span, int offset, byte value)
    {
        const string digits = "0123456789abcdef";
        span[offset] = digits[value >> 4];
        span[offset + 1] = digits[value & 0xF];
    }

    /// <summary>
    /// Parses a CSS hex color (<c>#rgb</c>, <c>#rgba</c>, <c>#rrggbb</c>, <c>#rrggbbaa</c>), an
    /// <c>rgb()</c>/<c>rgba()</c> functional notation, or one of a small set of named colors.
    /// </summary>
    /// <exception cref="FormatException">The value is not a supported color.</exception>
    public static BinkColor Parse(string value)
        => TryParse(value, out BinkColor color)
            ? color
            : throw new FormatException($"\"{value}\" is not a valid color. Use #rgb, #rgba, #rrggbb, #rrggbbaa or rgb()/rgba().");

    /// <summary>Tries to parse a color. See <see cref="Parse"/> for supported syntaxes.</summary>
    public static bool TryParse(string? value, out BinkColor color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        ReadOnlySpan<char> s = value.AsSpan().Trim();

        if (s[0] == '#')
        {
            s = s[1..];
            switch (s.Length)
            {
                case 3:
                case 4:
                {
                    if (!TryNibble(s[0], out int r) || !TryNibble(s[1], out int g) || !TryNibble(s[2], out int b)) return false;
                    int a = 15;
                    if (s.Length == 4 && !TryNibble(s[3], out a)) return false;
                    color = new BinkColor((byte)(r * 17), (byte)(g * 17), (byte)(b * 17), (byte)(a * 17));
                    return true;
                }
                case 6:
                case 8:
                {
                    if (!TryByte(s, 0, out byte r) || !TryByte(s, 2, out byte g) || !TryByte(s, 4, out byte b)) return false;
                    byte a = 255;
                    if (s.Length == 8 && !TryByte(s, 6, out a)) return false;
                    color = new BinkColor(r, g, b, a);
                    return true;
                }
                default:
                    return false;
            }
        }

        if (s.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
        {
            int open = s.IndexOf('(');
            int close = s.LastIndexOf(')');
            if (open < 0 || close < open) return false;
            ReadOnlySpan<char> inner = s[(open + 1)..close];
            Span<float> parts = stackalloc float[4];
            parts[3] = 1f;
            int count = 0;
            while (!inner.IsEmpty)
            {
                int comma = inner.IndexOf(',');
                ReadOnlySpan<char> part = (comma < 0 ? inner : inner[..comma]).Trim();
                inner = comma < 0 ? default : inner[(comma + 1)..];
                if (count >= 4) return false;
                if (part.EndsWith("%", StringComparison.Ordinal))
                {
                    if (!float.TryParse(part[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out float pct)) return false;
                    parts[count] = count == 3 ? pct / 100f : pct * 2.55f;
                }
                else if (!float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out parts[count]))
                {
                    return false;
                }
                count++;
            }
            if (count < 3) return false;
            color = new BinkColor(ToByte(parts[0] / 255f), ToByte(parts[1] / 255f), ToByte(parts[2] / 255f), ToByte(parts[3]));
            return true;
        }

        return TryParseNamed(s, out color);
    }

    private static bool TryParseNamed(ReadOnlySpan<char> name, out BinkColor color)
    {
        color = default;
        if (name.Equals("black", StringComparison.OrdinalIgnoreCase)) { color = Black; return true; }
        if (name.Equals("white", StringComparison.OrdinalIgnoreCase)) { color = White; return true; }
        if (name.Equals("transparent", StringComparison.OrdinalIgnoreCase)) { color = Transparent; return true; }
        if (name.Equals("red", StringComparison.OrdinalIgnoreCase)) { color = new BinkColor(255, 0, 0); return true; }
        if (name.Equals("green", StringComparison.OrdinalIgnoreCase)) { color = new BinkColor(0, 128, 0); return true; }
        if (name.Equals("blue", StringComparison.OrdinalIgnoreCase)) { color = new BinkColor(0, 0, 255); return true; }
        if (name.Equals("yellow", StringComparison.OrdinalIgnoreCase)) { color = new BinkColor(255, 255, 0); return true; }
        if (name.Equals("orange", StringComparison.OrdinalIgnoreCase)) { color = new BinkColor(255, 165, 0); return true; }
        if (name.Equals("gray", StringComparison.OrdinalIgnoreCase) || name.Equals("grey", StringComparison.OrdinalIgnoreCase)) { color = new BinkColor(128, 128, 128); return true; }
        return false;
    }

    private static bool TryNibble(char c, out int value)
    {
        value = c switch
        {
            >= '0' and <= '9' => c - '0',
            >= 'a' and <= 'f' => c - 'a' + 10,
            >= 'A' and <= 'F' => c - 'A' + 10,
            _ => -1,
        };
        return value >= 0;
    }

    private static bool TryByte(ReadOnlySpan<char> s, int offset, out byte value)
    {
        value = 0;
        if (!TryNibble(s[offset], out int hi) || !TryNibble(s[offset + 1], out int lo)) return false;
        value = (byte)((hi << 4) | lo);
        return true;
    }

    /// <summary>Linear interpolation between two colors.</summary>
    public static BinkColor Lerp(BinkColor a, BinkColor b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new BinkColor(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t),
            (byte)(a.A + (b.A - a.A) * t));
    }

    /// <inheritdoc />
    public override string ToString() => ToCss();
}
