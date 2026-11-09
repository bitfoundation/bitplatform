using System.Globalization;

namespace Bit.Bheadless;

/// <summary>A channel of a <see cref="BitBhColor"/>, and the range a <see cref="BitBhColorSlider"/> moves it in.</summary>
public enum BitBhColorChannel
{
    /// <summary>Hue, 0–360 degrees.</summary>
    Hue,

    /// <summary>HSB / HSV saturation, 0–100%.</summary>
    Saturation,

    /// <summary>HSB / HSV brightness (value), 0–100%.</summary>
    Brightness,

    /// <summary>HSL lightness, 0–100%.</summary>
    Lightness,

    /// <summary>Red, 0–255.</summary>
    Red,

    /// <summary>Green, 0–255.</summary>
    Green,

    /// <summary>Blue, 0–255.</summary>
    Blue,

    /// <summary>Opacity, 0–100%.</summary>
    Alpha,
}

/// <summary>
/// An immutable color, kept in HSB (hue, saturation, brightness) and alpha so a hue survives a trip through gray (a
/// color picker's hue doesn't jump to red when its saturation reaches 0), with conversions to and from RGB, HSL and
/// CSS text (<c>#rrggbb</c>, <c>#rgb</c>, <c>#rrggbbaa</c>, <c>rgb()</c>, <c>hsl()</c>).
/// </summary>
/// <param name="Hue">0–360.</param>
/// <param name="Saturation">HSB saturation, 0–1.</param>
/// <param name="Brightness">HSB brightness, 0–1.</param>
/// <param name="Alpha">0–1.</param>
public readonly record struct BitBhColor(double Hue, double Saturation, double Brightness, double Alpha = 1)
{
    /// <summary>Opaque black.</summary>
    public static BitBhColor Black => new(0, 0, 0);

    /// <summary>Opaque white.</summary>
    public static BitBhColor White => new(0, 0, 1);

    /// <summary>The color from RGB channels (0–255) and alpha (0–1).</summary>
    public static BitBhColor FromRgb(double red, double green, double blue, double alpha = 1)
    {
        var r = Math.Clamp(red, 0, 255) / 255;
        var g = Math.Clamp(green, 0, 255) / 255;
        var b = Math.Clamp(blue, 0, 255) / 255;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;
        double hue = 0;
        if (delta > 0)
        {
            if (max == r) hue = 60 * (((g - b) / delta) % 6);
            else if (max == g) hue = 60 * ((b - r) / delta + 2);
            else hue = 60 * ((r - g) / delta + 4);
        }
        if (hue < 0) hue += 360;
        return new BitBhColor(hue, max == 0 ? 0 : delta / max, max, Math.Clamp(alpha, 0, 1));
    }

    /// <summary>The color from HSL (hue 0–360, saturation and lightness 0–1) and alpha.</summary>
    public static BitBhColor FromHsl(double hue, double saturation, double lightness, double alpha = 1)
    {
        saturation = Math.Clamp(saturation, 0, 1);
        lightness = Math.Clamp(lightness, 0, 1);
        var brightness = lightness + saturation * Math.Min(lightness, 1 - lightness);
        var s = brightness == 0 ? 0 : 2 * (1 - lightness / brightness);
        return new BitBhColor(Wrap(hue), s, brightness, Math.Clamp(alpha, 0, 1));
    }

    /// <summary>Red, green and blue (0–255, not rounded).</summary>
    public (double Red, double Green, double Blue) ToRgb()
    {
        var (h, s, v) = (Hue, Saturation, Brightness);
        double F(double n)
        {
            var k = (n + h / 60) % 6;
            return v - v * s * Math.Max(0, Math.Min(k, Math.Min(4 - k, 1)));
        }
        return (F(5) * 255, F(3) * 255, F(1) * 255);
    }

    /// <summary>HSL saturation and lightness (0–1).</summary>
    public (double Hue, double Saturation, double Lightness) ToHsl()
    {
        var lightness = Brightness * (1 - Saturation / 2);
        var s = lightness is 0 or 1 ? 0 : (Brightness - lightness) / Math.Min(lightness, 1 - lightness);
        return (Hue, s, lightness);
    }

    /// <summary>The value of one channel, in the channel's own range (<see cref="BitBhColorChannel"/>).</summary>
    public double Get(BitBhColorChannel channel)
    {
        var (r, g, b) = ToRgb();
        return channel switch
        {
            BitBhColorChannel.Hue => Hue,
            BitBhColorChannel.Saturation => Saturation * 100,
            BitBhColorChannel.Brightness => Brightness * 100,
            BitBhColorChannel.Lightness => ToHsl().Lightness * 100,
            BitBhColorChannel.Red => r,
            BitBhColorChannel.Green => g,
            BitBhColorChannel.Blue => b,
            _ => Alpha * 100,
        };
    }

    /// <summary>The color with one channel set (in the channel's own range).</summary>
    public BitBhColor With(BitBhColorChannel channel, double value)
    {
        var (r, g, b) = ToRgb();
        switch (channel)
        {
            case BitBhColorChannel.Hue: return this with { Hue = Wrap(value) };
            case BitBhColorChannel.Saturation: return this with { Saturation = Math.Clamp(value / 100, 0, 1) };
            case BitBhColorChannel.Brightness: return this with { Brightness = Math.Clamp(value / 100, 0, 1) };
            case BitBhColorChannel.Lightness:
                var hsl = ToHsl();
                return FromHsl(Hue, hsl.Saturation, value / 100, Alpha) with { Hue = Hue };
            case BitBhColorChannel.Red: return KeepHue(FromRgb(value, g, b, Alpha));
            case BitBhColorChannel.Green: return KeepHue(FromRgb(r, value, b, Alpha));
            case BitBhColorChannel.Blue: return KeepHue(FromRgb(r, g, value, Alpha));
            default: return this with { Alpha = Math.Clamp(value / 100, 0, 1) };
        }
    }

    // A gray has no hue of its own: it keeps the one it had.
    private BitBhColor KeepHue(BitBhColor color) => color.Saturation == 0 || color.Brightness == 0 ? color with { Hue = Hue } : color;

    /// <summary>The range of a channel: (min, max, step).</summary>
    public static (double Min, double Max, double Step) RangeOf(BitBhColorChannel channel) => channel switch
    {
        BitBhColorChannel.Hue => (0, 360, 1),
        BitBhColorChannel.Red or BitBhColorChannel.Green or BitBhColorChannel.Blue => (0, 255, 1),
        _ => (0, 100, 1),
    };

    /// <summary>The CSS hex form: <c>#rrggbb</c>, or <c>#rrggbbaa</c> when not opaque (or when <paramref name="alpha"/> asks).</summary>
    public string ToHex(bool? alpha = null)
    {
        var (r, g, b) = ToRgb();
        var hex = $"#{(int)Math.Round(r):x2}{(int)Math.Round(g):x2}{(int)Math.Round(b):x2}";
        return alpha ?? Alpha < 1 ? $"{hex}{(int)Math.Round(Alpha * 255):x2}" : hex;
    }

    /// <summary>The CSS <c>rgb()</c> form, with alpha when not opaque.</summary>
    public string ToRgbString()
    {
        var (r, g, b) = ToRgb();
        var i = CultureInfo.InvariantCulture;
        return Alpha < 1
            ? string.Create(i, $"rgb({Math.Round(r)} {Math.Round(g)} {Math.Round(b)} / {Math.Round(Alpha, 2)})")
            : string.Create(i, $"rgb({Math.Round(r)} {Math.Round(g)} {Math.Round(b)})");
    }

    /// <summary>The CSS <c>hsl()</c> form, with alpha when not opaque.</summary>
    public string ToHslString()
    {
        var (h, s, l) = ToHsl();
        var i = CultureInfo.InvariantCulture;
        return Alpha < 1
            ? string.Create(i, $"hsl({Math.Round(h)} {Math.Round(s * 100)}% {Math.Round(l * 100)}% / {Math.Round(Alpha, 2)})")
            : string.Create(i, $"hsl({Math.Round(h)} {Math.Round(s * 100)}% {Math.Round(l * 100)}%)");
    }

    /// <inheritdoc />
    public override string ToString() => ToHex();

    /// <summary>
    /// Reads <c>#rgb</c>, <c>#rgba</c>, <c>#rrggbb</c>, <c>#rrggbbaa</c> (the <c>#</c> optional), <c>rgb()</c> / <c>rgba()</c> and
    /// <c>hsl()</c> / <c>hsla()</c> (commas or spaces, alpha after a slash or a fourth comma).
    /// </summary>
    public static bool TryParse(string? text, out BitBhColor color)
    {
        color = Black;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var s = text.Trim().ToLowerInvariant();
        try
        {
            if (s.StartsWith("rgb", StringComparison.Ordinal) || s.StartsWith("hsl", StringComparison.Ordinal))
                return TryParseFunction(s, out color);
            var hex = s.TrimStart('#');
            if (hex.Length is 3 or 4) hex = string.Concat(hex.Select(c => $"{c}{c}"));
            if (hex.Length is not (6 or 8) || !hex.All(Uri.IsHexDigit)) return false;
            int H(int at) => int.Parse(hex.AsSpan(at, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            color = FromRgb(H(0), H(2), H(4), hex.Length == 8 ? H(6) / 255.0 : 1);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>Parses like <see cref="TryParse"/>, throwing a <see cref="FormatException"/> for text that isn't a color.</summary>
    public static BitBhColor Parse(string text)
        => TryParse(text, out var color) ? color : throw new FormatException($"'{text}' is not a color.");

    private static bool TryParseFunction(string s, out BitBhColor color)
    {
        color = Black;
        var open = s.IndexOf('(');
        var close = s.LastIndexOf(')');
        if (open < 0 || close < open) return false;
        var parts = s[(open + 1)..close].Replace('/', ' ').Replace(',', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is < 3 or > 4) return false;
        static double Num(string p, double percentOf) => p.EndsWith('%')
            ? double.Parse(p[..^1], CultureInfo.InvariantCulture) / 100 * percentOf
            : double.Parse(p.TrimEnd('d', 'e', 'g'), CultureInfo.InvariantCulture);
        var alpha = parts.Length == 4 ? Num(parts[3], 1) : 1;
        color = s.StartsWith("rgb", StringComparison.Ordinal)
            ? FromRgb(Num(parts[0], 255), Num(parts[1], 255), Num(parts[2], 255), alpha)
            : FromHsl(Num(parts[0], 360), Num(parts[1], 1) > 1 ? Num(parts[1], 1) / 100 : Num(parts[1], 1), Num(parts[2], 1) > 1 ? Num(parts[2], 1) / 100 : Num(parts[2], 1), alpha);
        return true;
    }

    private static double Wrap(double hue)
    {
        // 360 stays 360: a hue slider's End is the last stop of the wheel, not the first.
        if (hue is >= 0 and <= 360) return hue;
        hue %= 360;
        return hue < 0 ? hue + 360 : hue;
    }
}
