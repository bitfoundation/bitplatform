namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// Every color syntax a stylesheet or <c>getComputedStyle</c> can hand the engine must parse: a color it can't
/// read snaps instead of animating, and an implicit starting value read off the page comes back in whatever
/// syntax the browser serialises it in.
/// </summary>
[TestClass]
public class ColorParsingTests
{
    private static void AssertRgba(string css, double r, double g, double b, double a, double tolerance = 1)
    {
        var channels = BmotionColorInterpolator.Parse(css);
        Assert.IsNotNull(channels, $"'{css}' did not parse.");
        Assert.AreEqual(r, channels[0], tolerance, $"red of '{css}'");
        Assert.AreEqual(g, channels[1], tolerance, $"green of '{css}'");
        Assert.AreEqual(b, channels[2], tolerance, $"blue of '{css}'");
        Assert.AreEqual(a, channels[3], 0.005, $"alpha of '{css}'");
    }

    [TestMethod]
    [DataRow("red", 255, 0, 0)]
    [DataRow("RebeccaPurple", 102, 51, 153)]
    [DataRow("cornflowerblue", 100, 149, 237)]
    [DataRow("white", 255, 255, 255)]
    public void NamedColors(string css, int r, int g, int b) => AssertRgba(css, r, g, b, 1);

    [TestMethod]
    public void Transparent_IsTransparentBlack() => AssertRgba("transparent", 0, 0, 0, 0);

    [TestMethod]
    [DataRow("rgb(255 128 0)", 255, 128, 0, 1)]
    [DataRow("rgb(255 128 0 / 0.5)", 255, 128, 0, 0.5)]
    [DataRow("rgb(255 128 0 / 25%)", 255, 128, 0, 0.25)]
    [DataRow("rgb(100% 50% 0%)", 255, 127.5, 0, 1)]
    [DataRow("rgba(255, 128, 0, 0.5)", 255, 128, 0, 0.5)]
    [DataRow("rgb(255, 128, 0)", 255, 128, 0, 1)]
    [DataRow("rgb(100%, 0%, 0%)", 255, 0, 0, 1)]
    public void Rgb_LegacyAndModernSyntax(string css, double r, double g, double b, double a) => AssertRgba(css, r, g, b, a);

    [TestMethod]
    [DataRow("hsl(120 100% 50%)", 0, 255, 0, 1)]
    [DataRow("hsl(120deg 100% 50% / 0.5)", 0, 255, 0, 0.5)]
    [DataRow("hsl(0.5turn 100% 50%)", 0, 255, 255, 1)]
    [DataRow("hsla(240, 100%, 50%, 0.25)", 0, 0, 255, 0.25)]
    public void Hsl_LegacyAndModernSyntaxWithHueUnits(string css, double r, double g, double b, double a) => AssertRgba(css, r, g, b, a);

    [TestMethod]
    public void Hwb_MixesWhiteAndBlackIntoTheHue()
    {
        AssertRgba("hwb(0 0% 0%)", 255, 0, 0, 1);
        AssertRgba("hwb(0 50% 50%)", 127.5, 127.5, 127.5, 1);
        AssertRgba("hwb(120 20% 0%)", 51, 255, 51, 1);
    }

    [TestMethod]
    public void Oklab_AndOklch_ConvertToSrgb()
    {
        // References from the CSS Color 4 conversion code.
        AssertRgba("oklab(1 0 0)", 255, 255, 255, 1);
        AssertRgba("oklab(0 0 0)", 0, 0, 0, 1);
        AssertRgba("oklch(0.627955 0.257683 29.2339)", 255, 0, 0, 1, tolerance: 1.5);
        AssertRgba("oklch(62.7955% 0.257683 29.2339deg / 50%)", 255, 0, 0, 0.5, tolerance: 1.5);
        AssertRgba("oklab(0.452014 -0.032457 -0.311528)", 0, 0, 255, 1, tolerance: 1.5);
    }

    [TestMethod]
    public void Lab_AndLch_ConvertThroughD50()
    {
        AssertRgba("lab(100 0 0)", 255, 255, 255, 1, tolerance: 1.5);
        AssertRgba("lab(54.2905 80.8049 69.891)", 255, 0, 0, 1, tolerance: 1.5);
        AssertRgba("lch(54.2905 106.837 40.8526)", 255, 0, 0, 1, tolerance: 1.5);
    }

    [TestMethod]
    public void ColorFunction_SrgbLinearAndDisplayP3()
    {
        AssertRgba("color(srgb 1 0.5 0)", 255, 127.5, 0, 1);
        AssertRgba("color(srgb-linear 1 1 1)", 255, 255, 255, 1);
        // P3 red is outside sRGB: it clamps to sRGB red.
        AssertRgba("color(display-p3 1 0 0)", 255, 0, 0, 1, tolerance: 1);
        AssertRgba("color(display-p3 1 1 1 / 0.5)", 255, 255, 255, 0.5);
    }

    [TestMethod]
    public void OutOfGamutColors_ClampInsteadOfProducingNaN()
    {
        var channels = BmotionColorInterpolator.Parse("oklch(0.7 0.4 140)")!;
        Assert.IsTrue(channels.All(double.IsFinite));
        Assert.IsTrue(channels.Take(3).All(c => c is >= 0 and <= 255));
    }

    [TestMethod]
    [DataRow("currentColor")]
    [DataRow("notacolor")]
    [DataRow("rgb(1, 2)")]
    [DataRow("rgb(1 2 3 /)")]
    [DataRow("color(rec2020 1 0 0)")]
    [DataRow("#12")]
    [DataRow("10px")]
    public void Unparseable_ReturnsNull(string css) => Assert.IsNull(BmotionColorInterpolator.Parse(css));

    [TestMethod]
    public void StringMixer_InterpolatesNamedAndModernColorsInsideComplexValues()
    {
        var shadow = BmotionStringMixer.TryCreateMix("0 0 8px red", "0 0 16px blue");
        Assert.IsNotNull(shadow);
        Assert.AreEqual("0 0 12px rgba(128,0,128,1)", shadow(0.5));

        var modern = BmotionStringMixer.TryCreateMix("drop-shadow(0 0 4px rgb(0 0 0 / 0))", "drop-shadow(0 0 4px rgb(0 0 0 / 1))");
        Assert.IsNotNull(modern);
        Assert.AreEqual("drop-shadow(0 0 4px rgba(0,0,0,0.5))", modern(0.5));
    }

    [TestMethod]
    public void StringMixer_StillRejectsDifferentKeywords()
        => Assert.IsNull(BmotionStringMixer.TryCreateMix("0 0 8px red inset", "0 0 8px red"));
}
