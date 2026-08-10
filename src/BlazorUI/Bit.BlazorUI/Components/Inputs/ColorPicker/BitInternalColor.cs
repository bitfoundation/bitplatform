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
    /// The color as hue (0-360), whiteness and blackness (both 0-1). HWB shares its hue with HSV and is
    /// the pair of "how much white" and "how much black" is mixed into it.
    /// </summary>
    public (double Hue, double Whiteness, double Blackness) Hwb => (_hue, _value * (1 - _saturation), 1 - _value);

    // CSS only defines the space-separated syntax for hwb(), and has no hwba() function at all - the
    // alpha goes into hwb() itself, after a slash.
    public string HwbString
    {
        get
        {
            var (h, w, b) = Hwb;
            return FormattableString.Invariant($"hwb({RoundDegrees(h)} {RoundPercent(w)}% {RoundPercent(b)}%)");
        }
    }

    public string HwbaString
    {
        get
        {
            var (h, w, b) = Hwb;
            return FormattableString.Invariant($"hwb({RoundDegrees(h)} {RoundPercent(w)}% {RoundPercent(b)}% / {FormatAlpha(_alpha)})");
        }
    }

    /// <summary>
    /// The color as Oklab lightness (0-1) and the two opponent axes a and b (roughly -0.4 to 0.4).
    /// </summary>
    public (double Lightness, double A, double B) Oklab => ToOklab(R, G, B);

    /// <summary>
    /// The color as Oklab lightness (0-1), chroma (0 to about 0.4) and hue (0-360) - the polar form of
    /// <see cref="Oklab"/>, and the one modern design tokens are usually written in.
    /// </summary>
    public (double Lightness, double Chroma, double Hue) Oklch
    {
        get
        {
            var (l, a, b) = Oklab;

            var chroma = Math.Sqrt(a * a + b * b);
            // A color with no chroma has no hue to report either, and atan2(0,0) would answer zero
            // rather than the hue the color was reached from.
            var hue = chroma < 1e-6 ? _hue : NormalizeHue(Math.Atan2(b, a) * 180 / Math.PI);

            return (l, chroma, hue);
        }
    }

    public string OklabString
    {
        get
        {
            var (l, a, b) = Oklab;
            return FormattableString.Invariant($"oklab({RoundOk(l)} {RoundOk(a)} {RoundOk(b)})");
        }
    }

    public string OklabaString
    {
        get
        {
            var (l, a, b) = Oklab;
            return FormattableString.Invariant($"oklab({RoundOk(l)} {RoundOk(a)} {RoundOk(b)} / {FormatAlpha(_alpha)})");
        }
    }

    public string OklchString
    {
        get
        {
            var (l, c, h) = Oklch;
            return FormattableString.Invariant($"oklch({RoundOk(l)} {RoundOk(c)} {RoundDegrees(h)})");
        }
    }

    public string OklchaString
    {
        get
        {
            var (l, c, h) = Oklch;
            return FormattableString.Invariant($"oklch({RoundOk(l)} {RoundOk(c)} {RoundDegrees(h)} / {FormatAlpha(_alpha)})");
        }
    }

    /// <summary>
    /// The color said in words - "light vibrant blue", "dark gray" - which is what a screen reader can
    /// announce about a gradient that carries no text of its own. A number triplet tells a sighted user
    /// nothing they cannot already see and a blind one nothing at all; a name tells both which color the
    /// picker has landed on.
    /// </summary>
    /// <remarks>
    /// Built from three parts, in the shape Adobe arrived at for their own color components: how light
    /// the color is, how much color there is in it, and which color it is. Naming the hue alone would
    /// call navy, sky and slate all "blue".
    /// </remarks>
    public string ColorDescription
    {
        get
        {
            var (hue, saturation, lightness) = Hsl;

            if (lightness <= 0.04) return "black";
            if (lightness >= 0.96 && saturation <= 0.12) return "white";

            // Below this there is not enough color left to name a hue by: what is left is the lightness.
            if (saturation <= 0.07)
            {
                return lightness < 0.35 ? "dark gray" : lightness > 0.7 ? "light gray" : "gray";
            }

            var name = HueName(hue, lightness, saturation);

            var lightnessName = lightness switch
            {
                < 0.2 => "very dark ",
                < 0.4 => "dark ",
                > 0.85 => "very light ",
                > 0.68 => "light ",
                _ => string.Empty
            };

            var chromaName = saturation switch
            {
                < 0.25 => "grayish ",
                < 0.5 => "pale ",
                > 0.85 => "vibrant ",
                _ => string.Empty
            };

            // "brown" already says dark orange, so the modifier that would repeat it is dropped.
            if (name == "brown") return chromaName + name;

            return lightnessName + chromaName + name;
        }
    }

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
        BitColorFormat.Hwb => HwbString,
        BitColorFormat.Hwba => HwbaString,
        BitColorFormat.Oklab => OklabString,
        BitColorFormat.Oklaba => OklabaString,
        BitColorFormat.Oklch => OklchString,
        BitColorFormat.Oklcha => OklchaString,
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
            "hwb" => hasAlphaArgument ? BitColorFormat.Hwba : BitColorFormat.Hwb,
            "oklab" => hasAlphaArgument ? BitColorFormat.Oklaba : BitColorFormat.Oklab,
            "oklch" => hasAlphaArgument ? BitColorFormat.Oklcha : BitColorFormat.Oklch,
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

        // The color() notation names its color space in the first argument, which shifts every channel -
        // and the alpha with them - one place along, so it is read before the shared reader below.
        if (name == "color") return TryParseColorFunction(args, out alpha);

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
            "hwb" or "hwba" => TryParseHwbArguments(args),
            "oklab" => TryParseOklabArguments(args),
            "oklch" => TryParseOklchArguments(args),
            "lab" => TryParseLabArguments(args),
            "lch" => TryParseLchArguments(args),
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

    /// <summary>
    /// HWB names a hue and then how much white and how much black are mixed into it, which is the same
    /// wheel HSV turns on: the blackness is what is left of the value, and the whiteness eats the
    /// saturation. A pair that adds up to more than the whole leaves no room for the hue at all, and CSS
    /// says the result is the grey the two of them make between themselves.
    /// </summary>
    private bool TryParseHwbArguments(string[] args)
    {
        if (TryParseHue(args[0], out var h) is false) return false;
        if (TryParseChannel(args[1], 1, out var w) is false) return false;
        if (TryParseChannel(args[2], 1, out var b) is false) return false;

        w = ClampUnit(w);
        b = ClampUnit(b);

        if (w + b >= 1)
        {
            var grey = w + b == 0 ? 0 : w / (w + b);

            Update(h, 0, grey, _alpha);

            return true;
        }

        var value = 1 - b;

        Update(h, value == 0 ? 0 : 1 - w / value, value, _alpha);

        return true;
    }

    private bool TryParseOklabArguments(string[] args)
    {
        if (TryParseChannel(args[0], 1, out var l) is false) return false;
        if (TryParseChannel(args[1], OklabAxisRange, out var a) is false) return false;
        if (TryParseChannel(args[2], OklabAxisRange, out var b) is false) return false;

        SetFromOklab(l, a, b);

        return true;
    }

    private bool TryParseOklchArguments(string[] args)
    {
        if (TryParseChannel(args[0], 1, out var l) is false) return false;
        if (TryParseChannel(args[1], OklabAxisRange, out var c) is false) return false;
        if (TryParseHue(args[2], out var h) is false) return false;

        c = Math.Max(c, 0);
        h = NormalizeHue(h);

        SetFromOklab(l, c * Math.Cos(h * Math.PI / 180), c * Math.Sin(h * Math.PI / 180));

        return true;
    }

    /// <summary>
    /// CIE Lab, which CSS measures against the D50 white point. It is read but never written: the picker
    /// answers in Oklab where a perceptual notation is wanted, since Oklab is the one that actually holds
    /// its hue when the lightness is moved.
    /// </summary>
    private bool TryParseLabArguments(string[] args)
    {
        if (TryParseChannel(args[0], 100, out var l) is false) return false;
        if (TryParseChannel(args[1], LabAxisRange, out var a) is false) return false;
        if (TryParseChannel(args[2], LabAxisRange, out var b) is false) return false;

        SetFromLab(l, a, b);

        return true;
    }

    private bool TryParseLchArguments(string[] args)
    {
        if (TryParseChannel(args[0], 100, out var l) is false) return false;
        if (TryParseChannel(args[1], LchChromaRange, out var c) is false) return false;
        if (TryParseHue(args[2], out var h) is false) return false;

        c = Math.Max(c, 0);
        h = NormalizeHue(h);

        SetFromLab(l, c * Math.Cos(h * Math.PI / 180), c * Math.Sin(h * Math.PI / 180));

        return true;
    }

    /// <summary>
    /// The <c>color()</c> notation, which names its color space first. Only the sRGB spaces are read:
    /// a wider one would have to be gamut-mapped into the sRGB the picker itself works in, and answering
    /// with a color that is not the one asked for is worse than not recognizing it.
    /// </summary>
    private bool TryParseColorFunction(string[] args, out double? alpha)
    {
        alpha = null;

        if (args.Length < 4) return false;

        var space = args[0].ToLowerInvariant();

        if (space is not ("srgb" or "srgb-linear")) return false;

        if (TryParseChannel(args[1], 1, out var r) is false) return false;
        if (TryParseChannel(args[2], 1, out var g) is false) return false;
        if (TryParseChannel(args[3], 1, out var b) is false) return false;

        if (args.Length > 4 && TryParseAlpha(args[4], out var parsedAlpha))
        {
            alpha = parsedAlpha;
        }

        if (space == "srgb-linear")
        {
            r = FromLinear(r);
            g = FromLinear(g);
            b = FromLinear(b);
        }

        SetRgb(ToByte(r), ToByte(g), ToByte(b));

        return true;
    }

    /// <summary>
    /// Moves the color to a point in the Oklab space, which is a point in the sRGB space once it has been
    /// converted - the hue the picker turns on is derived from that, since Oklab measures its own hue on
    /// a differently spaced wheel and the two numbers would not agree.
    /// </summary>
    private void SetFromOklab(double lightness, double a, double b)
    {
        var (r, g, bl) = OklabToRgb(lightness, a, b);

        SetRgb(r, g, bl);
    }

    private void SetFromLab(double lightness, double a, double b)
    {
        var (r, g, bl) = LabToRgb(lightness, a, b);

        SetRgb(r, g, bl);
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

    /// <summary>
    /// The last step of every conversion into an eight-bit channel. NaN is answered with 0 rather than
    /// left to the cast: Math.Clamp passes it through - every comparison against it is false - and casting
    /// it to a byte is undefined, so a color parsed from a notation carrying "NaN" would land on whatever
    /// the platform happens to produce.
    /// </summary>
    private static byte ToByte(double value)
    {
        if (double.IsNaN(value)) return 0;

        return (byte)Math.Clamp(Math.Round(value * 255), 0, 255);
    }

    private static double RoundDegrees(double value) => Math.Round(value, 2);

    private static double RoundPercent(double value) => Math.Round(value * 100, 2);

    /// <summary>
    /// The Oklab channels are written to four decimals: they run from 0 to 1 rather than to 255, so the
    /// same visual precision costs more digits than an RGB channel does.
    /// </summary>
    private static double RoundOk(double value) => Math.Round(value, 4);

    /// <summary>
    /// What CSS calls 100% on the a and b axes of <c>oklab()</c> - and on the chroma of <c>oklch()</c> -
    /// so that a percentage and a plain number name the same color.
    /// </summary>
    private const double OklabAxisRange = 0.4;

    /// <summary>What CSS calls 100% on the a and b axes of <c>lab()</c>.</summary>
    private const double LabAxisRange = 125;

    /// <summary>What CSS calls 100% on the chroma of <c>lch()</c>.</summary>
    private const double LchChromaRange = 150;

    /// <summary>
    /// The name a hue goes by, in the bands the names actually cover: they are not twelve equal slices,
    /// since the eye gives yellow a far narrower band than it gives green or blue.
    /// </summary>
    private static string HueName(double hue, double lightness, double saturation)
    {
        // Dark, saturated orange has a name of its own, and calling it "dark orange" would be a worse
        // answer than the one everybody already uses.
        if (hue is >= 10 and < 45 && lightness < 0.35 && saturation > 0.2) return "brown";

        return hue switch
        {
            < 10 => "red",
            < 45 => "orange",
            < 70 => "yellow",
            < 165 => "green",
            < 195 => "cyan",
            < 255 => "blue",
            < 285 => "purple",
            < 315 => "magenta",
            < 345 => "pink",
            _ => "red"
        };
    }



    // The sRGB transfer function and its inverse. Every conversion that is defined in terms of light
    // rather than of an eight-bit channel - Oklab among them - starts by undoing it.
    private static double ToLinear(double channel)
    {
        return channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
    }

    private static double FromLinear(double channel)
    {
        return channel <= 0.0031308 ? channel * 12.92 : 1.055 * Math.Pow(channel, 1 / 2.4) - 0.055;
    }

    /// <summary>
    /// Converts an sRGB triplet to Oklab, through the cone response the space is built on. The matrices
    /// are Björn Ottosson's, which is what the CSS Color 4 specification points at.
    /// </summary>
    private static (double L, double A, double B) ToOklab(byte red, byte green, byte blue)
    {
        var r = ToLinear(red / 255.0);
        var g = ToLinear(green / 255.0);
        var b = ToLinear(blue / 255.0);

        var l = Math.Cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b);
        var m = Math.Cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b);
        var s = Math.Cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b);

        return (0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s,
                1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s,
                0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s);
    }

    /// <summary>
    /// Converts a point in the Oklab space back to sRGB. Oklab is larger than sRGB, so a point can name a
    /// color no screen can show; the channels are clamped into the gamut rather than refused, which is
    /// what the browsers themselves do for an out-of-range <c>oklch()</c>.
    /// </summary>
    private static (byte R, byte G, byte B) OklabToRgb(double lightness, double a, double b)
    {
        var l = Cube(lightness + 0.3963377774 * a + 0.2158037573 * b);
        var m = Cube(lightness - 0.1055613458 * a - 0.0638541728 * b);
        var s = Cube(lightness - 0.0894841775 * a - 1.2914855480 * b);

        var red = 4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s;
        var green = -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s;
        var blue = -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s;

        return (ToByte(FromLinear(Math.Clamp(red, 0, 1))),
                ToByte(FromLinear(Math.Clamp(green, 0, 1))),
                ToByte(FromLinear(Math.Clamp(blue, 0, 1))));
    }

    /// <summary>
    /// Converts a point in the CIE Lab space to sRGB, the long way round that CSS defines it: Lab is
    /// measured against the D50 white point, so it becomes D50 XYZ, is adapted to the D65 white point
    /// screens are measured against, and only then becomes light a monitor can emit.
    /// </summary>
    private static (byte R, byte G, byte B) LabToRgb(double lightness, double a, double b)
    {
        const double epsilon = 216 / 24389d;
        const double kappa = 24389 / 27d;

        var fy = (lightness + 16) / 116;
        var fx = fy + a / 500;
        var fz = fy - b / 200;

        var xr = Cube(fx) > epsilon ? Cube(fx) : (116 * fx - 16) / kappa;
        var yr = lightness > kappa * epsilon ? Cube(fy) : lightness / kappa;
        var zr = Cube(fz) > epsilon ? Cube(fz) : (116 * fz - 16) / kappa;

        // The D50 white point CSS measures lab() against.
        var x = xr * 0.3457 / 0.3585;
        var y = yr;
        var z = zr * (1 - 0.3457 - 0.3585) / 0.3585;

        // Bradford adaptation from D50 to the D65 that sRGB is defined in.
        var x65 = 0.9554734527042182 * x - 0.023098536874261423 * y + 0.0632593086610217 * z;
        var y65 = -0.028369706963208136 * x + 1.0099954580058226 * y + 0.021041398966943008 * z;
        var z65 = 0.012314001688319899 * x - 0.020507696433477912 * y + 1.3303659366080753 * z;

        var red = 3.2409699419045226 * x65 - 1.537383177570094 * y65 - 0.4986107602930034 * z65;
        var green = -0.9692436362808796 * x65 + 1.8759675015077202 * y65 + 0.04155505740717559 * z65;
        var blue = 0.05563007969699366 * x65 - 0.20397695888897652 * y65 + 1.0569715142428786 * z65;

        return (ToByte(FromLinear(Math.Clamp(red, 0, 1))),
                ToByte(FromLinear(Math.Clamp(green, 0, 1))),
                ToByte(FromLinear(Math.Clamp(blue, 0, 1))));
    }

    private static double Cube(double value) => value * value * value;

    /// <summary>
    /// The alpha of a color string, kept short: an opaque color reads "1" rather than "1.00", and the
    /// two decimals below that are as fine as the picker's own alpha slider can be dragged.
    /// </summary>
    private static string FormatAlpha(double alpha) => Math.Round(alpha, 2).ToString("0.##", CultureInfo.InvariantCulture);
}
