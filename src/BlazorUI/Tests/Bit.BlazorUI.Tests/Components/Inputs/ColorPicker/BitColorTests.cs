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
        DataRow("oklch(0.7 0.1 200)")
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
        DataRow("hsva(1,2%,3%,0.5)", BitColorFormat.Hsva)
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
        DataRow("oklch(0.7 0.1 200)")
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
    // "saturation" naturally reaches for one or the other.
    [TestMethod]
    public void BitColorToRgbShouldAcceptFractionsAndPercentages()
    {
        Assert.AreEqual(BitInternalColor.ToRgb(120, 1, 1), BitInternalColor.ToRgb(120, 100, 100));
        Assert.AreEqual(((byte)0, (byte)255, (byte)0), BitInternalColor.ToRgb(120, 1, 1));
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
