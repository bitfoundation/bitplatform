using System;
using System.Globalization;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ColorPicker;

[TestClass]
public class BitColorTests
{
    [TestMethod, DataRow("#5D0914")]
    public void BitColorHexToRgbTest(string color)
    {
        var bitColor = new BitInternalColor(color);

        Assert.AreEqual(93, bitColor.R);
        Assert.AreEqual(9, bitColor.G);
        Assert.AreEqual(20, bitColor.B);
        Assert.AreEqual(1, bitColor.A);
    }

    [TestMethod, DataRow("#5D0914", 0.6)]
    public void BitColorHexToRgbaTest(string color, double alpha)
    {
        var bitColor = new BitInternalColor(color, alpha);

        Assert.AreEqual(93, bitColor.R);
        Assert.AreEqual(9, bitColor.G);
        Assert.AreEqual(20, bitColor.B);
        Assert.AreEqual(bitColor.A, alpha);
    }

    [TestMethod, DataRow("rgb(93,9,20)")]
    public void BitColorRgbToHexTest(string color)
    {
        var bitColor = new BitInternalColor(color);

        Assert.AreEqual("#5D0914", bitColor.Hex);
        Assert.AreEqual(1, bitColor.A);
    }

    [TestMethod, DataRow("rgb(93,9,20)", 0.6)]
    public void BitColorRgbaToHexTest(string color, double alpha)
    {
        var bitColor = new BitInternalColor(color, alpha);

        Assert.AreEqual("#5D0914", bitColor.Hex);
        Assert.AreEqual(alpha, bitColor.A);
    }

    [TestMethod, DataRow((byte)93, (byte)9, (byte)20, 0.6)]
    public void BitColorSetRgbaTest(byte red, byte green, byte blue, double alpha)
    {
        var bitColor = new BitInternalColor(red, green, blue, alpha);

        Assert.AreEqual("#5D0914", bitColor.Hex);
        Assert.AreEqual(alpha, bitColor.A);
    }

    [TestMethod, DataRow("#5d0914")]
    public void BitColorHexToHsvTest(string color)
    {
        var bitColor = new BitInternalColor(color);

        Assert.AreEqual(352.14, Math.Round(bitColor.Hsv.Hue, 2));
        Assert.AreEqual(0.90, Math.Round(bitColor.Hsv.Saturation, 2));
        Assert.AreEqual(0.36, Math.Round(bitColor.Hsv.Value, 2));
        Assert.AreEqual(1, bitColor.A);
    }

    [TestMethod, DataRow(352.143, 0.903, 0.365, 0.9)]
    public void BitColorHsvToHexTest(double hue, double saturation, double value, double alpha)
    {
        var bitColor = new BitInternalColor(hue, saturation, value, alpha);

        Assert.AreEqual("#5D0914", bitColor.Hex);
    }



    // The short hexadecimal forms name each channel with a single digit standing for the doubled pair,
    // so "#f0c" has to expand to "#ff00cc" rather than being read as a single 12-bit number.
    [TestMethod,
        DataRow("#FFF", (byte)255, (byte)255, (byte)255, 1.0),
        DataRow("#000", (byte)0, (byte)0, (byte)0, 1.0),
        DataRow("#f0c", (byte)255, (byte)0, (byte)204, 1.0),
        DataRow("#5D0914", (byte)93, (byte)9, (byte)20, 1.0),
        DataRow("#f0c8", (byte)255, (byte)0, (byte)204, 0.53),
        DataRow("#5D091499", (byte)93, (byte)9, (byte)20, 0.6),
        DataRow("#FF0000FF", (byte)255, (byte)0, (byte)0, 1.0),
        DataRow("  #5D0914  ", (byte)93, (byte)9, (byte)20, 1.0)
    ]
    public void BitColorShouldParseEveryHexLength(string color, byte r, byte g, byte b, double alpha)
    {
        var bitColor = new BitInternalColor(color);

        Assert.AreEqual(r, bitColor.R);
        Assert.AreEqual(g, bitColor.G);
        Assert.AreEqual(b, bitColor.B);
        Assert.AreEqual(alpha, Math.Round(bitColor.A, 2));
    }

    // Both the legacy comma-separated syntax and the modern space-separated one - whose alpha is
    // introduced by a slash - are valid CSS, and percentages are valid wherever a number is.
    [TestMethod,
        DataRow("rgb(93,9,20)", (byte)93, (byte)9, (byte)20, 1.0),
        DataRow("rgb(93, 9, 20)", (byte)93, (byte)9, (byte)20, 1.0),
        DataRow("rgba(93,9,20,0.6)", (byte)93, (byte)9, (byte)20, 0.6),
        DataRow("rgb(93 9 20)", (byte)93, (byte)9, (byte)20, 1.0),
        DataRow("rgb(93 9 20 / 0.6)", (byte)93, (byte)9, (byte)20, 0.6),
        DataRow("rgb(93 9 20 / 60%)", (byte)93, (byte)9, (byte)20, 0.6),
        DataRow("rgba(93 9 20 / 60%)", (byte)93, (byte)9, (byte)20, 0.6),
        DataRow("rgb(100%, 0%, 0%)", (byte)255, (byte)0, (byte)0, 1.0),
        DataRow("RGB(93,9,20)", (byte)93, (byte)9, (byte)20, 1.0)
    ]
    public void BitColorShouldParseEveryRgbSyntax(string color, byte r, byte g, byte b, double alpha)
    {
        var bitColor = new BitInternalColor(color);

        Assert.AreEqual(r, bitColor.R);
        Assert.AreEqual(g, bitColor.G);
        Assert.AreEqual(b, bitColor.B);
        Assert.AreEqual(alpha, Math.Round(bitColor.A, 2));
    }

    [TestMethod,
        DataRow("hsl(0,100%,50%)", "#FF0000", 1.0),
        DataRow("hsl(120, 100%, 50%)", "#00FF00", 1.0),
        DataRow("hsla(240,100%,50%,0.5)", "#0000FF", 0.5),
        DataRow("hsl(240 100% 50% / 50%)", "#0000FF", 0.5),
        DataRow("hsl(0,0%,100%)", "#FFFFFF", 1.0),
        DataRow("hsl(0,0%,0%)", "#000000", 1.0),
        DataRow("hsl(0.5turn,100%,50%)", "#00FFFF", 1.0),
        DataRow("hsl(180deg,100%,50%)", "#00FFFF", 1.0)
    ]
    public void BitColorShouldParseHsl(string color, string hex, double alpha)
    {
        var bitColor = new BitInternalColor(color);

        Assert.AreEqual(hex, bitColor.Hex);
        Assert.AreEqual(alpha, Math.Round(bitColor.A, 2));
    }

    // HSV is the model the picker itself is driven in, and "hsb" is the name the same model goes by in
    // design tools, so both spellings are accepted.
    [TestMethod,
        DataRow("hsv(0,100%,100%)", "#FF0000", 1.0),
        DataRow("hsb(120,100%,100%)", "#00FF00", 1.0),
        DataRow("hsva(240,100%,100%,0.4)", "#0000FF", 0.4),
        DataRow("hsv(0,0%,50%)", "#808080", 1.0)
    ]
    public void BitColorShouldParseHsv(string color, string hex, double alpha)
    {
        var bitColor = new BitInternalColor(color);

        Assert.AreEqual(hex, bitColor.Hex);
        Assert.AreEqual(alpha, Math.Round(bitColor.A, 2));
    }

    // HWB names a hue and then how much white and how much black are stirred into it. A pair that adds up
    // to more than the whole leaves no room for the hue, and CSS says the answer is the grey the two make.
    [TestMethod,
        DataRow("hwb(0 0% 0%)", "#FF0000", 1.0),
        DataRow("hwb(120 0% 0%)", "#00FF00", 1.0),
        DataRow("hwb(0 100% 0%)", "#FFFFFF", 1.0),
        DataRow("hwb(0 0% 100%)", "#000000", 1.0),
        DataRow("hwb(0 50% 50%)", "#808080", 1.0),
        DataRow("hwb(0 80% 80%)", "#808080", 1.0)
    ]
    public void BitColorShouldParseHwb(string color, string hex, double alpha)
    {
        var bitColor = new BitInternalColor(color);

        Assert.AreEqual(hex, bitColor.Hex);
        Assert.AreEqual(alpha, Math.Round(bitColor.A, 2));
    }

    // The whiteness and the blackness survive the trip into the HSV the picker turns on and back out of it,
    // which is what lets an hwb() binding be edited and answered in the same numbers it arrived in.
    [TestMethod]
    public void BitColorShouldRoundTripHwb()
    {
        var bitColor = new BitInternalColor("hwb(210deg 20% 30% / 0.5)");

        var (hue, whiteness, blackness) = bitColor.Hwb;

        Assert.AreEqual(210, Math.Round(hue, 6));
        Assert.AreEqual(0.2, Math.Round(whiteness, 6));
        Assert.AreEqual(0.3, Math.Round(blackness, 6));
        Assert.AreEqual(0.5, bitColor.A);
        Assert.AreEqual("hwb(210 20% 30% / 0.5)", bitColor.HwbaString);
    }

    // Oklab and its polar form Oklch are the color spaces modern design tokens are written in - a Tailwind
    // palette is a list of oklch() - so a value handed straight from one has to be a color the picker reads.
    [TestMethod,
        DataRow("oklch(0.62796 0.25768 29.234)", "#FF0000", 1.0),
        DataRow("oklch(0.86644 0.29483 142.4953)", "#00FF00", 1.0),
        DataRow("oklch(0.45201 0.31321 264.052)", "#0000FF", 1.0),
        DataRow("oklch(1 0 0)", "#FFFFFF", 1.0),
        DataRow("oklch(0 0 0)", "#000000", 1.0),
        DataRow("oklch(100% 0 0)", "#FFFFFF", 1.0),
        DataRow("oklch(0.62796 0.25768 29.234 / 0.4)", "#FF0000", 0.4),
        DataRow("oklab(0.62796 0.22486 0.12585)", "#FF0000", 1.0),
        DataRow("oklab(0.86644 -0.23389 0.1795)", "#00FF00", 1.0),
        DataRow("oklab(1 0 0 / 25%)", "#FFFFFF", 0.25)
    ]
    public void BitColorShouldParseOklabAndOklch(string color, string hex, double alpha)
    {
        var bitColor = new BitInternalColor(color);

        Assert.AreEqual(hex, bitColor.Hex);
        Assert.AreEqual(alpha, Math.Round(bitColor.A, 2));
    }

    // CIE Lab and its polar form are read too, which completes the CSS Color 4 set. They are read but
    // never written: where a perceptual notation is wanted the picker answers in Oklab, which is the one
    // that actually holds its hue when the lightness is moved.
    [TestMethod,
        DataRow("lab(100% 0 0)", "#FFFFFF", 1.0),
        DataRow("lab(0% 0 0)", "#000000", 1.0),
        DataRow("lab(54.29% 80.8 69.89)", "#FF0000", 1.0),
        DataRow("lab(87.82% -79.27 80.99)", "#00FF00", 1.0),
        DataRow("lab(54.29 80.8 69.89 / 0.4)", "#FF0000", 0.4),
        DataRow("lch(54.29% 106.84 40.85)", "#FF0000", 1.0),
        DataRow("lch(100% 0 0)", "#FFFFFF", 1.0),
        DataRow("lch(54.29% 106.84 40.85 / 25%)", "#FF0000", 0.25)
    ]
    public void BitColorShouldParseLabAndLch(string color, string hex, double alpha)
    {
        var bitColor = new BitInternalColor(color);

        Assert.AreEqual(hex, bitColor.Hex);
        Assert.AreEqual(alpha, Math.Round(bitColor.A, 2));
    }

    // A color named outside the sRGB gamut is clamped into it rather than refused, which is what the
    // browsers themselves do with an out-of-range oklch().
    [TestMethod]
    public void BitColorShouldClampAnOutOfGamutOklch()
    {
        Assert.AreEqual("#FF0000", new BitInternalColor("oklch(0.7 0.5 29.234)").Hex);
        Assert.AreEqual("#000000", new BitInternalColor("oklch(-1 0 0)").Hex);
        Assert.AreEqual("#FFFFFF", new BitInternalColor("oklch(2 0 0)").Hex);
    }

    // The color() notation names its color space first, which shifts every channel - and the alpha with
    // them - one place along. Only the sRGB spaces are read: gamut-mapping a wider one would answer with a
    // color that is not the one that was asked for.
    [TestMethod,
        DataRow("color(srgb 1 0 0)", "#FF0000", 1.0),
        DataRow("color(srgb 0 0.5 1)", "#0080FF", 1.0),
        DataRow("color(srgb 1 1 1 / 0.5)", "#FFFFFF", 0.5),
        DataRow("color(srgb 100% 0% 0%)", "#FF0000", 1.0),
        DataRow("color(srgb-linear 1 0 0)", "#FF0000", 1.0)
    ]
    public void BitColorShouldParseTheColorFunction(string color, string hex, double alpha)
    {
        var bitColor = new BitInternalColor(color);

        Assert.AreEqual(hex, bitColor.Hex);
        Assert.AreEqual(alpha, Math.Round(bitColor.A, 2));
    }

    [TestMethod,
        DataRow("red", "#FF0000", 1.0),
        DataRow("Tomato", "#FF6347", 1.0),
        DataRow("REBECCAPURPLE", "#663399", 1.0),
        DataRow("transparent", "#000000", 0.0)
    ]
    public void BitColorShouldParseCssKeywords(string color, string hex, double alpha)
    {
        var bitColor = new BitInternalColor(color);

        Assert.AreEqual(hex, bitColor.Hex);
        Assert.AreEqual(alpha, bitColor.A);
    }

    // An unrecognized string falls back to the opaque white an unconfigured picker starts on rather
    // than throwing, so a half-typed color bound from a text field cannot break the page.
    [TestMethod,
        DataRow(null),
        DataRow(""),
        DataRow("   "),
        DataRow("#12345"),
        DataRow("#GGGGGG"),
        DataRow("rgb(1,2)"),
        DataRow("rgb(a,b,c)"),
        DataRow("hsl(x,1%,1%)"),
        DataRow("not-a-color"),
        DataRow("rgb(1,2,3"),
        DataRow("lch(70% x 200)"),
        DataRow("color(display-p3 1 0 0)")
    ]
    public void BitColorShouldFallBackToWhiteOnInvalidInput(string? color)
    {
        var bitColor = new BitInternalColor(color);

        Assert.AreEqual("#FFFFFF", bitColor.Hex);
        Assert.AreEqual(1, bitColor.A);
        Assert.IsFalse(BitInternalColor.IsValid(color));
    }

    [TestMethod,
        DataRow("#FFF"),
        DataRow("rgb(1,2,3)"),
        DataRow("hsl(1,2%,3%)"),
        DataRow("transparent"),
        DataRow("tomato")
    ]
    public void BitColorShouldReportValidInput(string color)
    {
        Assert.IsTrue(BitInternalColor.IsValid(color));
    }

    // Every number in a CSS color is written in the invariant culture, so a comma-decimal culture must
    // not turn a "0.6" alpha into an unparsable string in either direction.
    [TestMethod]
    public void BitColorShouldBeCultureInvariant()
    {
        var culture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            var bitColor = new BitInternalColor("rgba(93,9,20,0.6)");

            Assert.AreEqual(0.6, bitColor.A);
            Assert.AreEqual("rgba(93,9,20,0.6)", bitColor.Rgba);
            Assert.AreEqual("#5D091499", bitColor.HexAlpha);
            Assert.AreEqual("hsla(352.14,82.35%,20%,0.6)", bitColor.HslaString);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [TestMethod]
    public void BitColorShouldWriteEveryFormat()
    {
        var bitColor = new BitInternalColor("rgba(255,0,0,0.5)");

        Assert.AreEqual("#FF0000", bitColor.ToString(BitColorFormat.Hex));
        Assert.AreEqual("#FF000080", bitColor.ToString(BitColorFormat.HexAlpha));
        Assert.AreEqual("rgb(255,0,0)", bitColor.ToString(BitColorFormat.Rgb));
        Assert.AreEqual("rgba(255,0,0,0.5)", bitColor.ToString(BitColorFormat.Rgba));
        Assert.AreEqual("hsl(0,100%,50%)", bitColor.ToString(BitColorFormat.Hsl));
        Assert.AreEqual("hsla(0,100%,50%,0.5)", bitColor.ToString(BitColorFormat.Hsla));
        Assert.AreEqual("hsv(0,100%,100%)", bitColor.ToString(BitColorFormat.Hsv));
        Assert.AreEqual("hsva(0,100%,100%,0.5)", bitColor.ToString(BitColorFormat.Hsva));

        // CSS defines hwb() and oklch() with the space-separated syntax only, and has no hwba() or
        // oklcha() function at all - the alpha goes into the notation itself, after a slash.
        Assert.AreEqual("hwb(0 0% 0%)", bitColor.ToString(BitColorFormat.Hwb));
        Assert.AreEqual("hwb(0 0% 0% / 0.5)", bitColor.ToString(BitColorFormat.Hwba));
    }

    // Every notation the picker writes has to be one it can read back, or a bound value would not survive
    // the round trip through the consumer that holds it.
    [TestMethod,
        DataRow("#FF0000"),
        DataRow("#5D0914"),
        DataRow("#123456"),
        DataRow("#ABCDEF"),
        DataRow("#808080"),
        DataRow("#000000"),
        DataRow("#FFFFFF"),
        DataRow("#010203")
    ]
    public void BitColorShouldReadBackEveryFormatItWrites(string hex)
    {
        var source = new BitInternalColor(hex, 0.5);

        foreach (var format in Enum.GetValues<BitColorFormat>())
        {
            var written = source.ToString(format);
            var readBack = new BitInternalColor(written, 0.5);

            Assert.AreEqual(hex, readBack.Hex, $"{format} wrote \"{written}\", which reads back as {readBack.Hex}.");
        }
    }

    [TestMethod,
        DataRow("#FF0000", 0.628, 0.2577, 29.23),
        DataRow("#00FF00", 0.8664, 0.2948, 142.5),
        DataRow("#0000FF", 0.452, 0.3132, 264.05),
        DataRow("#FFFFFF", 1.0, 0.0, 0.0),
        DataRow("#000000", 0.0, 0.0, 0.0)
    ]
    public void BitColorShouldExposeOklch(string hex, double lightness, double chroma, double hue)
    {
        var (l, c, h) = new BitInternalColor(hex).Oklch;

        Assert.AreEqual(lightness, Math.Round(l, 4), 0.001);
        Assert.AreEqual(chroma, Math.Round(c, 4), 0.001);

        // A color with no chroma has no hue to report, so it keeps the one it was reached from - which,
        // for a color parsed out of a grey hexadecimal, is zero.
        Assert.AreEqual(hue, Math.Round(h, 2), 0.05);
    }

    [TestMethod,
        DataRow("#FF0000", 0.0, 0.0),
        DataRow("#FFFFFF", 1.0, 0.0),
        DataRow("#000000", 0.0, 1.0),
        DataRow("#808080", 0.502, 0.498)
    ]
    public void BitColorShouldExposeHwb(string hex, double whiteness, double blackness)
    {
        var (_, w, b) = new BitInternalColor(hex).Hwb;

        Assert.AreEqual(whiteness, Math.Round(w, 3));
        Assert.AreEqual(blackness, Math.Round(b, 3));
    }

    // The picker announces the color it is on by name, since a gradient carries no text a screen reader
    // could read instead and a channel triplet is three numbers nobody can picture.
    [TestMethod,
        DataRow("#FF0000", "vibrant red"),
        DataRow("#000000", "black"),
        DataRow("#FFFFFF", "white"),
        DataRow("#808080", "gray"),
        DataRow("#333333", "dark gray"),
        DataRow("#DDDDDD", "light gray"),
        DataRow("#0000FF", "vibrant blue"),
        DataRow("#B0C4DE", "light pale blue"),
        DataRow("#4D7FB3", "pale blue"),
        DataRow("#00203F", "very dark vibrant blue"),
        DataRow("#00FFFF", "vibrant cyan"),
        DataRow("#FFFF00", "vibrant yellow"),
        DataRow("#8A4B08", "vibrant brown"),
        DataRow("#FF00FF", "vibrant magenta"),
        DataRow("#7E4AE2", "purple")
    ]
    public void BitColorShouldDescribeItself(string hex, string expected)
    {
        Assert.AreEqual(expected, new BitInternalColor(hex).ColorDescription);
    }

    [TestMethod,
        DataRow("#FFFFFF", BitColorFormat.Hex),
        DataRow("#FFF", BitColorFormat.Hex),
        DataRow("#FFFF", BitColorFormat.HexAlpha),
        DataRow("#FFFFFFFF", BitColorFormat.HexAlpha),
        DataRow("rgb(1,2,3)", BitColorFormat.Rgb),
        DataRow("rgb(1 2 3 / 0.5)", BitColorFormat.Rgba),
        DataRow("rgba(1,2,3,0.5)", BitColorFormat.Rgba),
        DataRow("hsl(1,2%,3%)", BitColorFormat.Hsl),
        DataRow("hsla(1,2%,3%,0.5)", BitColorFormat.Hsla),
        DataRow("hsv(1,2%,3%)", BitColorFormat.Hsv),
        DataRow("hsb(1,2%,3%)", BitColorFormat.Hsv),
        DataRow("hsva(1,2%,3%,0.5)", BitColorFormat.Hsva),
        DataRow("hwb(1 2% 3%)", BitColorFormat.Hwb),
        DataRow("hwb(1 2% 3% / 0.5)", BitColorFormat.Hwba),
        DataRow("oklab(0.5 0.1 0.1)", BitColorFormat.Oklab),
        DataRow("oklab(0.5 0.1 0.1 / 0.5)", BitColorFormat.Oklaba),
        DataRow("oklch(0.5 0.1 200)", BitColorFormat.Oklch),
        DataRow("oklch(0.5 0.1 200 / 0.5)", BitColorFormat.Oklcha)
    ]
    public void BitColorShouldDetectTheFormatItWasGiven(string color, BitColorFormat expected)
    {
        Assert.AreEqual(expected, BitInternalColor.DetectFormat(color));
    }

    // A keyword or an unreadable string names no notation, which is what lets the picker fall back to
    // its own default instead of pretending the consumer asked for one.
    [TestMethod,
        DataRow(null),
        DataRow(""),
        DataRow("tomato"),
        DataRow("color(srgb 1 0 0)"),
        DataRow("lab(70% 40 20)"),
        DataRow("lch(70% 40 200)")
    ]
    public void BitColorShouldDetectNoFormatForUnnotatedInput(string? color)
    {
        Assert.IsNull(BitInternalColor.DetectFormat(color));
    }

    // Flooring the conversion loses up to a whole level on every channel, which is both a visible shift
    // on dark colors and enough to stop a hex value from surviving a round trip through HSV.
    [TestMethod,
        DataRow("#808080"),
        DataRow("#5D0914"),
        DataRow("#123456"),
        DataRow("#ABCDEF"),
        DataRow("#010203"),
        DataRow("#FEFDFC")
    ]
    public void BitColorShouldSurviveAHexToHsvRoundTrip(string hex)
    {
        var source = new BitInternalColor(hex);
        var (h, s, v) = source.Hsv;

        var roundTripped = new BitInternalColor(h, s, v, 1);

        Assert.AreEqual(source.Hex, roundTripped.Hex);
    }

    // The hue is an angle on a wheel, so it wraps rather than clamping: -30 and 330 name the same red.
    [TestMethod,
        DataRow(0.0, "#FF0000"),
        DataRow(360.0, "#FF0000"),
        DataRow(720.0, "#FF0000"),
        DataRow(-360.0, "#FF0000"),
        DataRow(-120.0, "#0000FF"),
        DataRow(480.0, "#00FF00")
    ]
    public void BitColorShouldWrapTheHueOntoTheWheel(double hue, string hex)
    {
        var bitColor = new BitInternalColor(hue, 1, 1, 1);

        Assert.AreEqual(hex, bitColor.Hex);
    }

    // Black names no hue and no saturation of its own, and neither does any shade of grey. Deriving them
    // back from the RGB would snap the picker's rainbow slider to red the moment the user drags into a
    // corner, so the ones the color was reached from are kept instead.
    [TestMethod]
    public void BitColorShouldKeepTheHueThroughBlackAndGrey()
    {
        var bitColor = new BitInternalColor(210, 0.8, 0.9, 1);

        bitColor.Update(210, 0.8, 0, 1);
        Assert.AreEqual(210, bitColor.Hsv.Hue);
        Assert.AreEqual("#000000", bitColor.Hex);

        bitColor.Update(210, 0, 0.5, 1);
        Assert.AreEqual(210, bitColor.Hsv.Hue);

        // Coming back out of the corner returns to the color that was left, not to red.
        bitColor.Update(210, 0.8, 0.9, 1);
        Assert.AreEqual("#2E8AE6", bitColor.Hex);
    }

    [TestMethod]
    public void BitColorShouldKeepTheHueWhenSetToAGreyRgb()
    {
        var bitColor = new BitInternalColor(210, 0.8, 0.9, 1);

        bitColor.SetRgb(0, 0, 0);
        Assert.AreEqual(210, bitColor.Hsv.Hue);

        bitColor.SetRgb(128, 128, 128);
        Assert.AreEqual(210, bitColor.Hsv.Hue);
        Assert.AreEqual(0, bitColor.Hsv.Saturation);
    }

    // HSL and HSV share a hue but not a saturation, so the pair has to be converted rather than copied.
    [TestMethod,
        DataRow("#FF0000", 0.0, 1.0, 0.5),
        DataRow("#FFFFFF", 0.0, 0.0, 1.0),
        DataRow("#000000", 0.0, 0.0, 0.0),
        DataRow("#808080", 0.0, 0.0, 0.502)
    ]
    public void BitColorShouldExposeHsl(string hex, double hue, double saturation, double lightness)
    {
        var bitColor = new BitInternalColor(hex);
        var (h, s, l) = bitColor.Hsl;

        Assert.AreEqual(hue, Math.Round(h, 3));
        Assert.AreEqual(saturation, Math.Round(s, 3));
        Assert.AreEqual(lightness, Math.Round(l, 3));
    }

    [TestMethod,
        DataRow(-1.0, 0.0),
        DataRow(0.0, 0.0),
        DataRow(0.5, 0.5),
        DataRow(1.0, 1.0),
        DataRow(2.0, 1.0)
    ]
    public void BitColorShouldClampAlpha(double alpha, double expected)
    {
        var bitColor = new BitInternalColor("#FFFFFF") { A = alpha };

        Assert.AreEqual(expected, bitColor.A);
    }

    // A string that carries its own alpha is the more specific answer of the two, so it wins over the
    // alpha the picker was configured with; one that does not leaves that alpha alone.
    [TestMethod]
    public void BitColorShouldPreferTheAlphaCarriedByTheString()
    {
        Assert.AreEqual(0.25, new BitInternalColor("rgba(1,2,3,0.25)", 0.9).A);
        Assert.AreEqual(0.9, new BitInternalColor("rgb(1,2,3)", 0.9).A);
        Assert.AreEqual(0.6, Math.Round(new BitInternalColor("#5D091499", 0.9).A, 2));
        Assert.AreEqual(0.9, new BitInternalColor("#5D0914", 0.9).A);
    }

    // ToRgb takes saturation and value either as fractions or as percentages, since a consumer reading
    // "saturation" naturally reaches for one or the other. The two ranges overlap at 1, which is read as
    // the fraction - a full 100% - so anything at or below 1 is a fraction and anything above it a
    // percentage.
    [TestMethod]
    public void BitColorToRgbShouldAcceptFractionsAndPercentages()
    {
        Assert.AreEqual(BitInternalColor.ToRgb(120, 1, 1), BitInternalColor.ToRgb(120, 100, 100));
        Assert.AreEqual(((byte)0, (byte)255, (byte)0), BitInternalColor.ToRgb(120, 1, 1));

        Assert.AreEqual(BitInternalColor.ToRgb(120, 0.5, 0.5), BitInternalColor.ToRgb(120, 50, 50));
        Assert.AreEqual(((byte)64, (byte)128, (byte)64), BitInternalColor.ToRgb(120, 50, 50));
    }

    [TestMethod]
    public void BitColorShouldDefaultToOpaqueWhite()
    {
        var bitColor = new BitInternalColor();

        Assert.AreEqual("#FFFFFF", bitColor.Hex);
        Assert.AreEqual("rgb(255,255,255)", bitColor.Rgb);
        Assert.AreEqual("rgba(255,255,255,1)", bitColor.Rgba);
        Assert.AreEqual(1, bitColor.A);
        Assert.AreEqual(255, bitColor.AlphaByte);
    }
}
