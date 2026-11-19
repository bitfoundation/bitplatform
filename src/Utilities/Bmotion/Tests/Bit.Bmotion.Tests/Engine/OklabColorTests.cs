namespace Bit.Bmotion.Tests.Engine;

/// <summary>Tests for perceptual OKLab color interpolation (plan item 3.1).</summary>
[TestClass]
public class OklabColorTests
{
    private static double[] Rgb(int r, int g, int b, double a = 1) => [r, g, b, a];

    private static (int R, int G, int B) ParseRgb(string rgba)
    {
        var inner = rgba[rgba.IndexOf('(')..].Trim('(', ')');
        var parts = inner.Split(',');
        return (int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
    }

    [TestMethod]
    public void Oklab_Endpoints_MatchInput()
    {
        var start = BmotionColorInterpolator.Lerp(Rgb(0, 0, 255), Rgb(255, 255, 0), 0, BmotionColorSpace.Oklab);
        var end = BmotionColorInterpolator.Lerp(Rgb(0, 0, 255), Rgb(255, 255, 0), 1, BmotionColorSpace.Oklab);
        var (sr, sg, sb) = ParseRgb(start);
        var (er, eg, eb) = ParseRgb(end);
        // Round-trip through OKLab should reproduce the endpoints within rounding tolerance.
        Assert.IsTrue(Math.Abs(sr - 0) <= 1 && Math.Abs(sg - 0) <= 1 && Math.Abs(sb - 255) <= 1, $"start={start}");
        Assert.IsTrue(Math.Abs(er - 255) <= 1 && Math.Abs(eg - 255) <= 1 && Math.Abs(eb - 0) <= 1, $"end={end}");
    }

    [TestMethod]
    public void Oklab_BlueToYellow_MidpointStaysSaturated()
    {
        // sRGB blue→yellow mid is a muddy grey (all channels near 128). OKLab keeps it saturated,
        // so the mid-point must NOT be near-grey (channels should differ substantially).
        var srgbMid = ParseRgb(BmotionColorInterpolator.Lerp(Rgb(0, 0, 255), Rgb(255, 255, 0), 0.5, BmotionColorSpace.Srgb));
        var oklabMid = ParseRgb(BmotionColorInterpolator.Lerp(Rgb(0, 0, 255), Rgb(255, 255, 0), 0.5, BmotionColorSpace.Oklab));

        int SrgbSpread = Math.Max(srgbMid.R, Math.Max(srgbMid.G, srgbMid.B)) - Math.Min(srgbMid.R, Math.Min(srgbMid.G, srgbMid.B));
        int OklabSpread = Math.Max(oklabMid.R, Math.Max(oklabMid.G, oklabMid.B)) - Math.Min(oklabMid.R, Math.Min(oklabMid.G, oklabMid.B));

        // sRGB midpoint is near-grey (low channel spread); OKLab is more colorful (higher spread).
        Assert.IsTrue(OklabSpread > SrgbSpread, $"oklab spread {OklabSpread} should exceed srgb {SrgbSpread}");
    }

    [TestMethod]
    public void Oklab_SameColor_IsStable()
    {
        var mid = ParseRgb(BmotionColorInterpolator.Lerp(Rgb(100, 150, 200), Rgb(100, 150, 200), 0.5, BmotionColorSpace.Oklab));
        Assert.IsTrue(Math.Abs(mid.R - 100) <= 1 && Math.Abs(mid.G - 150) <= 1 && Math.Abs(mid.B - 200) <= 1);
    }

    [TestMethod]
    public void Srgb_RemainsDefault()
    {
        // The 3-arg overload keeps sRGB behavior for back-compat.
        var mid = BmotionColorInterpolator.Lerp(Rgb(0, 0, 0), Rgb(255, 255, 255), 0.5);
        Assert.AreEqual("rgba(128,128,128,1)", mid);
    }

    [TestMethod]
    public void Tween_ColorSpace_FlowsToConfig()
    {
        var config = Bm.Tween(colorSpace: BmotionColorSpace.Oklab).ToConfig();
        Assert.AreEqual(BmotionColorSpace.Oklab, config.ColorSpace);

        var defaultConfig = Bm.Tween().ToConfig();
        Assert.AreEqual(BmotionColorSpace.Srgb, defaultConfig.ColorSpace);
    }

    [TestMethod]
    public void AmbientColorSpace_CascadesToTopLevelAndPerProperty()
    {
        var t = new BmotionTween
        {
            Properties = new()
            {
                ["backgroundColor"] = new BmotionTween(),                         // inherits ambient
                ["color"] = new BmotionTween { ColorSpace = BmotionColorSpace.Srgb },  // explicit wins
            },
        };

        var config = t.ToConfig(BmotionColorSpace.Oklab);

        Assert.AreEqual(BmotionColorSpace.Oklab, config.ColorSpace, "top-level inherits the ambient space");
        Assert.AreEqual(BmotionColorSpace.Oklab, config.Properties!["backgroundColor"].ColorSpace,
            "a per-property override without its own space inherits the ambient - not sRGB");
        Assert.AreEqual(BmotionColorSpace.Srgb, config.Properties!["color"].ColorSpace,
            "an explicit per-property space is preserved");
    }

    [TestMethod]
    public void NoAmbientColorSpace_KeepsSrgbDefaultEverywhere()
    {
        var t = new BmotionTween { Properties = new() { ["backgroundColor"] = new BmotionTween() } };

        var config = t.ToConfig();

        Assert.AreEqual(BmotionColorSpace.Srgb, config.ColorSpace);
        Assert.AreEqual(BmotionColorSpace.Srgb, config.Properties!["backgroundColor"].ColorSpace);
    }

    [TestMethod]
    public void ExplicitTopLevelColorSpace_CascadesToPerProperty()
    {
        // A parent transition's explicit space is the ambient for its per-property children.
        var t = new BmotionTween
        {
            ColorSpace = BmotionColorSpace.Oklab,
            Properties = new() { ["backgroundColor"] = new BmotionTween() },
        };

        var config = t.ToConfig();

        Assert.AreEqual(BmotionColorSpace.Oklab, config.Properties!["backgroundColor"].ColorSpace);
    }
}
