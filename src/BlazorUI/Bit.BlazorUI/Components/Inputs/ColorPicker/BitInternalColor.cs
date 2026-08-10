using System;
using System.Linq;
using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// The color model behind the <see cref="BitColorPicker"/>: it reads the CSS notations a consumer can
/// hand the component, keeps the RGB, HSV and alpha views of the color in step with each other, and
/// writes the color back out in any of the notations the picker supports.
/// </summary>
/// <remarks>
/// HSV - not RGB - is the authoritative state here. A picker is driven in HSV, and the conversion to
/// RGB is lossy at the edges of that space: every fully black color is the same RGB triplet whatever
/// hue it was reached from, and so is every fully desaturated one. Deriving the HSV back from the RGB
/// on every change would therefore snap the hue to red the moment the user drags into a corner, which
/// is the classic "the rainbow slider jumped" bug. Keeping the hue and saturation the user chose means
/// dragging out of a corner returns to the color they left.
/// </remarks>
internal sealed class BitInternalColor
{
    private double _hue;
    private double _saturation;
    private double _value = 1;
    private double _alpha = 1;



    public byte R { get; private set; } = 255;
    public byte G { get; private set; } = 255;
    public byte B { get; private set; } = 255;

    /// <summary>
    /// The alpha channel, from 0 (fully transparent) to 1 (fully opaque). Values outside that range
    /// are clamped into it, so an out-of-range binding can never produce an invalid color string.
    /// </summary>
    public double A
    {
        get => _alpha;
        set => _alpha = ClampUnit(value);
    }

    /// <summary>
    /// Whether the string this color was last parsed from carried an alpha of its own - an <c>rgba()</c>,
    /// an <c>hsla()</c>, an eight-digit hexadecimal or <c>transparent</c> - rather than taking the alpha it
    /// was parsed with. It is how a color read from a string can be told apart from one that only names a
    /// hue, which is what lets a preset be compared against the picker on its RGB alone.
    /// </summary>
    public bool HasParsedAlpha { get; private set; }

    /// <summary>
    /// The color as hue (0-360), saturation and value (both 0-1).
    /// </summary>
    public (double Hue, double Saturation, double Value) Hsv => (_hue, _saturation, _value);

    /// <summary>
    /// The color as hue (0-360), saturation and lightness (both 0-1). HSL shares its hue with HSV but
    /// not its saturation: the same color reads as a different number in each model.
    /// </summary>
    public (double Hue, double Saturation, double Lightness) Hsl
    {
        get
        {
            var lightness = _value * (1 - _saturation / 2);
            var saturation = (lightness == 0 || lightness == 1)
                                ? 0
                                : (_value - lightness) / Math.Min(lightness, 1 - lightness);

            return (_hue, saturation, lightness);
        }
    }

    public string Hex => FormattableString.Invariant($"#{R:X2}{G:X2}{B:X2}");

    public string HexAlpha => FormattableString.Invariant($"#{R:X2}{G:X2}{B:X2}{AlphaByte:X2}");

    public string Rgb => FormattableString.Invariant($"rgb({R},{G},{B})");

    public string Rgba => FormattableString.Invariant($"rgba({R},{G},{B},{FormatAlpha(_alpha)})");

    public string HslString
    {
        get
        {
            var (h, s, l) = Hsl;
            return FormattableString.Invariant($"hsl({RoundDegrees(h)},{RoundPercent(s)}%,{RoundPercent(l)}%)");
        }
    }

    public string HslaString
    {
        get
        {
            var (h, s, l) = Hsl;
            return FormattableString.Invariant($"hsla({RoundDegrees(h)},{RoundPercent(s)}%,{RoundPercent(l)}%,{FormatAlpha(_alpha)})");
        }
    }

    public string HsvString => FormattableString.Invariant($"hsv({RoundDegrees(_hue)},{RoundPercent(_saturation)}%,{RoundPercent(_value)}%)");

    public string HsvaString => FormattableString.Invariant($"hsva({RoundDegrees(_hue)},{RoundPercent(_saturation)}%,{RoundPercent(_value)}%,{FormatAlpha(_alpha)})");

    /// <summary>
    /// The alpha channel as the 0-255 byte the eight-digit hexadecimal notation carries.
    /// </summary>
    public byte AlphaByte => (byte)Math.Round(_alpha * 255);



    public BitInternalColor() { }

    public BitInternalColor(string? color, double alpha = 1.0)
    {
        Parse(color, alpha);
    }

    public BitInternalColor(double hue, double saturation, double value, double alpha)
    {
        Update(hue, saturation, value, alpha);
    }

    public BitInternalColor(byte red = 255, byte green = 255, byte blue = 255, double alpha = 1.0)
    {
        A = alpha;

        SetRgb(red, green, blue);
    }



    /// <summary>
    /// Writes the color in the requested notation.
    /// </summary>
    public string ToString(BitColorFormat format) => format switch
    {
        BitColorFormat.HexAlpha => HexAlpha,
        BitColorFormat.Rgb => Rgb,
        BitColorFormat.Rgba => Rgba,
        BitColorFormat.Hsl => HslString,
        BitColorFormat.Hsla => HslaString,
        BitColorFormat.Hsv => HsvString,
        BitColorFormat.Hsva => HsvaString,
        _ => Hex
    };

    public override string ToString() => Rgba;

    /// <summary>
    /// Moves the color to a point in the HSV space, which is what every gesture on the picker itself
    /// ultimately does.
    /// </summary>
    public void Update(double hue, double saturation, double value, double alpha)
    {
        A = alpha;

        _hue = NormalizeHue(hue);
        _saturation = ClampUnit(saturation);
        _value = ClampUnit(value);

        (R, G, B) = ToRgb(_hue, _saturation, _value);
    }

    /// <summary>
    /// Moves the color to an RGB triplet, re-deriving the HSV from it while keeping the hue and the
    /// saturation the color already had wherever the new triplet leaves them undefined - black has no
    /// hue of its own, and neither does any shade of grey.
    /// </summary>
    public void SetRgb(byte red, byte green, byte blue)
    {
        // A triplet that has not moved names no new hue either: it is the color this instance is already
        // on, so the HSV it is already on is the more precise answer of the two. Re-deriving it instead
        // would round the hue to whatever the eight-bit channels can express - a step of several degrees
        // in a dark or washed-out color - and re-reading the same value repeatedly, which is what a bound
        // picker does on every frame of a drag, would walk it further each time.
        if (red == R && green == G && blue == B) return;

        R = red;
        G = green;
        B = blue;

        CalculateHsv();
    }

    /// <summary>
    /// Converts a point in the HSV space to its RGB triplet.
    /// </summary>
    /// <remarks>
    /// Saturation and value are accepted either as fractions (0-1) or as percentages (0-100), since a
    /// consumer reading "saturation" naturally reaches for one or the other.
    /// </remarks>
    public static (byte R, byte G, byte B) ToRgb(double hue, double saturation, double value)
    {
        if (value > 1) value /= 100;
        if (saturation > 1) saturation /= 100;

        hue = NormalizeHue(hue);
        saturation = ClampUnit(saturation);
        value = ClampUnit(value);

        var c = value * saturation;
        var x = c * (1 - Math.Abs(hue / 60 % 2 - 1));
        var m = value - c;

        (double r, double g, double b) color =
              hue < 60 ? (c, x, 0)
            : hue < 120 ? (x, c, 0)
            : hue < 180 ? (0, c, x)
            : hue < 240 ? (0, x, c)
            : hue < 300 ? (x, 0, c)
            : (c, 0, x);

        // Rounded rather than truncated: flooring loses up to a whole level on every channel, which is
        // both a visible shift on dark colors and enough to stop a hex value from surviving a round trip
        // through the HSV space it is edited in.
        return (ToByte(color.r + m), ToByte(color.g + m), ToByte(color.b + m));
    }

    /// <summary>
    /// Reads the notation a color string is written in, which is how the picker keeps answering in the
    /// same notation it was given. Returns null for anything it cannot recognize.
    /// </summary>
    public static BitColorFormat? DetectFormat(string? color)
    {
        if (color.HasNoValue()) return null;

        var text = color!.Trim();

        if (text.StartsWith('#'))
        {
            return text.Length is 5 or 9 ? BitColorFormat.HexAlpha : BitColorFormat.Hex;
        }

        var name = FunctionName(text);

        if (name is null) return null;

        var hasAlphaArgument = SplitArguments(text).Length > 3;

        return name switch
        {
            "rgba" => BitColorFormat.Rgba,
            "rgb" => hasAlphaArgument ? BitColorFormat.Rgba : BitColorFormat.Rgb,
            "hsla" => BitColorFormat.Hsla,
            "hsl" => hasAlphaArgument ? BitColorFormat.Hsla : BitColorFormat.Hsl,
            "hsva" or "hsba" => BitColorFormat.Hsva,
            "hsv" or "hsb" => hasAlphaArgument ? BitColorFormat.Hsva : BitColorFormat.Hsv,
            _ => null
        };
    }

    /// <summary>
    /// Reports whether a string is a color this model can read, without disturbing any existing state.
    /// </summary>
    public static bool IsValid(string? color) => new BitInternalColor().TryParse(color, out _);



    /// <summary>
    /// Reads a CSS color into this instance, falling back to opaque white when the string is not a color
    /// this model understands - the same white an unconfigured picker starts on.
    /// </summary>
    /// <param name="color">The color string.</param>
    /// <param name="alpha">
    /// The alpha to apply when the string does not carry one of its own. A string that does carry one -
    /// an <c>rgba()</c>, an <c>hsla()</c> or an eight-digit hex - always wins, since it is the more
    /// specific answer of the two.
    /// </param>
    /// <returns>Whether the string was recognized.</returns>
    public bool Parse(string? color, double alpha = 1.0)
    {
        A = alpha;

        if (TryParse(color, out var parsedAlpha) is false)
        {
            ResetColor();
            A = alpha;
            HasParsedAlpha = false;
            return false;
        }

        HasParsedAlpha = parsedAlpha.HasValue;

        if (parsedAlpha.HasValue)
        {
            A = parsedAlpha.Value;
        }

        return true;
    }

    /// <summary>
    /// The single reader behind both <see cref="Parse"/> and <see cref="IsValid"/>: it writes the RGB
    /// (and, for the HSL/HSV notations, the exact hue and saturation the string names) into this
    /// instance and reports the alpha the string carried, if any.
    /// </summary>
    private bool TryParse(string? color, out double? alpha)
    {
        alpha = null;

        if (color.HasNoValue()) return false;

        var text = color!.Trim();

        if (text.Equals("transparent", StringComparison.OrdinalIgnoreCase))
        {
            SetRgb(0, 0, 0);
            alpha = 0;
            return true;
        }

        if (text.StartsWith('#')) return TryParseHex(text[1..], out alpha);

        var name = FunctionName(text);

        if (name is null)
        {
            if (BitCssNamedColors.TryGet(text, out var named) is false) return false;

            SetRgb((byte)((named >> 16) & 255), (byte)((named >> 8) & 255), (byte)(named & 255));
            return true;
        }

        var args = SplitArguments(text);

        if (args.Length < 3) return false;

        if (args.Length > 3 && TryParseAlpha(args[3], out var parsedAlpha))
        {
            alpha = parsedAlpha;
        }

        return name switch
        {
            "rgb" or "rgba" => TryParseRgbArguments(args),
            "hsl" or "hsla" => TryParseHslArguments(args),
            "hsv" or "hsva" or "hsb" or "hsba" => TryParseHsvArguments(args),
            _ => false
        };
    }

    private bool TryParseHex(string digits, out double? alpha)
    {
        alpha = null;

        if (digits.Length is not (3 or 4 or 6 or 8)) return false;

        foreach (var c in digits)
        {
            if (Uri.IsHexDigit(c) is false) return false;
        }

        // The short forms name each channel with a single digit that stands for the doubled pair, so
        // "#f0c" is "#ff00cc" - expanded here rather than special-cased in every channel below.
        if (digits.Length is 3 or 4)
        {
            digits = string.Concat(digits.Select(c => new string(c, 2)));
        }

        SetRgb(ParseHexPair(digits, 0), ParseHexPair(digits, 2), ParseHexPair(digits, 4));

        if (digits.Length == 8)
        {
            alpha = ParseHexPair(digits, 6) / 255d;
        }

        return true;
    }

    private bool TryParseRgbArguments(string[] args)
    {
        if (TryParseChannel(args[0], 255, out var r) is false) return false;
        if (TryParseChannel(args[1], 255, out var g) is false) return false;
        if (TryParseChannel(args[2], 255, out var b) is false) return false;

        SetRgb(ToByte(r / 255), ToByte(g / 255), ToByte(b / 255));

        return true;
    }

    private bool TryParseHslArguments(string[] args)
    {
        if (TryParseHue(args[0], out var h) is false) return false;
        if (TryParseChannel(args[1], 1, out var s) is false) return false;
        if (TryParseChannel(args[2], 1, out var l) is false) return false;

        s = ClampUnit(s);
        l = ClampUnit(l);

        // HSL and HSV share a hue but not a saturation, so the pair is converted rather than copied.
        var v = l + s * Math.Min(l, 1 - l);

        Update(h, v == 0 ? 0 : 2 * (1 - l / v), v, _alpha);

        return true;
    }

    private bool TryParseHsvArguments(string[] args)
    {
        if (TryParseHue(args[0], out var h) is false) return false;
        if (TryParseChannel(args[1], 1, out var s) is false) return false;
        if (TryParseChannel(args[2], 1, out var v) is false) return false;

        Update(h, s, v, _alpha);

        return true;
    }

    private void ResetColor()
    {
        _hue = 0;
        _saturation = 0;
        _value = 1;
        _alpha = 1;

        R = 255;
        G = 255;
        B = 255;
    }

    private void CalculateHsv()
    {
        var r = R / 255.0;
        var g = G / 255.0;
        var b = B / 255.0;

        var maxC = Math.Max(r, Math.Max(g, b));
        var minC = Math.Min(r, Math.Min(g, b));

        _value = maxC;

        if (maxC == 0)
        {
            // Black names no hue and no saturation, so both are left as they were: dragging back up out
            // of the bottom of the picker returns to the color the user came from.
            _saturation = 0;
            return;
        }

        var delta = maxC - minC;

        _saturation = delta / maxC;

        // A shade of grey names no hue either, for the same reason.
        if (delta == 0) return;

        double h;

        if (maxC == r)
        {
            h = (g - b) / delta;
        }
        else if (maxC == g)
        {
            h = (b - r) / delta + 2;
        }
        else // if (maxC == b)
        {
            h = (r - g) / delta + 4;
        }

        _hue = NormalizeHue(h * 60);
    }



    /// <summary>
    /// The name of a functional notation - the "rgb" of "rgb(255, 0, 0)" - or null when the string is
    /// not one.
    /// </summary>
    private static string? FunctionName(string text)
    {
        var open = text.IndexOf('(');

        if (open <= 0 || text.EndsWith(')') is false) return null;

        return text[..open].Trim().ToLowerInvariant();
    }

    /// <summary>
    /// The arguments of a functional notation, in either the legacy comma-separated form or the modern
    /// space-separated one whose alpha is introduced by a slash.
    /// </summary>
    private static string[] SplitArguments(string text)
    {
        var open = text.IndexOf('(');

        if (open < 0 || text.EndsWith(')') is false) return [];

        return text[(open + 1)..^1]
                .Replace('/', ' ')
                .Split([',', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Reads one channel, which CSS lets be written either as a plain number or as a percentage of the
    /// channel's own range.
    /// </summary>
    private static bool TryParseChannel(string text, double max, out double value)
    {
        value = 0;

        text = text.Trim();

        if (text.EndsWith('%'))
        {
            if (TryParseNumber(text[..^1], out var percent) is false) return false;

            value = percent / 100 * max;
            return true;
        }

        if (TryParseNumber(text, out var number) is false) return false;

        value = number;
        return true;
    }

    /// <summary>
    /// Reads a hue, which CSS also lets be written with an angle unit.
    /// </summary>
    private static bool TryParseHue(string text, out double hue)
    {
        hue = 0;

        text = text.Trim();

        double factor = 1;

        if (text.EndsWith("deg", StringComparison.OrdinalIgnoreCase)) text = text[..^3];
        else if (text.EndsWith("grad", StringComparison.OrdinalIgnoreCase)) { text = text[..^4]; factor = 0.9; }
        else if (text.EndsWith("rad", StringComparison.OrdinalIgnoreCase)) { text = text[..^3]; factor = 180 / Math.PI; }
        else if (text.EndsWith("turn", StringComparison.OrdinalIgnoreCase)) { text = text[..^4]; factor = 360; }

        if (TryParseNumber(text, out var number) is false) return false;

        hue = number * factor;
        return true;
    }

    private static bool TryParseAlpha(string text, out double alpha)
    {
        if (TryParseChannel(text, 1, out alpha) is false) return false;

        alpha = ClampUnit(alpha);
        return true;
    }

    /// <summary>
    /// Every number in a CSS color is read in the invariant culture, because that is the culture CSS
    /// itself is written in: a "0.5" alpha must not become unparsable under a comma-decimal locale.
    /// </summary>
    private static bool TryParseNumber(string text, out double value)
    {
        return double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static byte ParseHexPair(string digits, int index)
    {
        return (byte)(Uri.FromHex(digits[index]) * 16 + Uri.FromHex(digits[index + 1]));
    }

    /// <summary>
    /// Wraps a hue onto the color wheel, so that -30 and 330 name the same red and 360 names the same
    /// red as 0 does.
    /// </summary>
    private static double NormalizeHue(double hue)
    {
        if (double.IsNaN(hue) || double.IsInfinity(hue)) return 0;

        hue %= 360;

        return hue < 0 ? hue + 360 : hue;
    }

    private static double ClampUnit(double value)
    {
        if (double.IsNaN(value)) return 0;

        return Math.Clamp(value, 0, 1);
    }

    private static byte ToByte(double value) => (byte)Math.Clamp(Math.Round(value * 255), 0, 255);

    private static double RoundDegrees(double value) => Math.Round(value, 2);

    private static double RoundPercent(double value) => Math.Round(value * 100, 2);

    /// <summary>
    /// The alpha of a color string, kept short: an opaque color reads "1" rather than "1.00", and the
    /// two decimals below that are as fine as the picker's own alpha slider can be dragged.
    /// </summary>
    private static string FormatAlpha(double alpha) => Math.Round(alpha, 2).ToString("0.##", CultureInfo.InvariantCulture);
}
