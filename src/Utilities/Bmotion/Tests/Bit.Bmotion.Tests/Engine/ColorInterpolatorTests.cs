
namespace Bit.Bmotion.Tests.Engine;

[TestClass]
public class ColorInterpolatorTests
{
    // ── LooksLikeColor ────────────────────────────────────────────────────────

    [TestMethod]
    [DataRow("#ff0000", true)]
    [DataRow("#fff", true)]
    [DataRow("rgb(255,0,0)", true)]
    [DataRow("rgba(255,0,0,1)", true)]
    [DataRow("hsl(0,100%,50%)", true)]
    [DataRow("red", false)]
    [DataRow("transparent", false)]
    [DataRow("", false)]
    [DataRow(null, false)]
    public void LooksLikeColor_ReturnsExpected(string? value, bool expected)
    {
        Assert.AreEqual(expected, BmotionColorInterpolator.LooksLikeColor(value));
    }

    // ── Lerp - boundary conditions ────────────────────────────────────────────

    [TestMethod]
    public void Lerp_AtT0_ReturnsFromColor()
    {
        Assert.AreEqual("rgba(0,0,0,1)", BmotionColorInterpolator.Lerp("#000000", "#ffffff", 0.0));
    }

    [TestMethod]
    public void Lerp_AtT1_ReturnsToColor()
    {
        Assert.AreEqual("rgba(255,255,255,1)", BmotionColorInterpolator.Lerp("#000000", "#ffffff", 1.0));
    }

    [TestMethod]
    public void Lerp_AtMidpoint_InterpolatesChannels()
    {
        Assert.AreEqual("rgba(128,128,128,1)", BmotionColorInterpolator.Lerp("#000000", "#ffffff", 0.5));
    }

    // ── Hex format parsing ────────────────────────────────────────────────────

    [TestMethod]
    public void Lerp_ShorthandHex_Expands()
    {
        Assert.AreEqual("rgba(128,128,128,1)", BmotionColorInterpolator.Lerp("#000", "#fff", 0.5));
    }

    [TestMethod]
    public void Lerp_ShorthandHexWithAlpha_ParsesAlpha()
    {
        // #000f → [0,0,0,alpha=1.0]; #fff0 → [255,255,255,alpha=0.0]
        var result = BmotionColorInterpolator.Lerp("#000f", "#fff0", 0.5);
        Assert.AreEqual("rgba(128,128,128,0.5)", result);
    }

    [TestMethod]
    public void Lerp_FullHex_MixesChannels()
    {
        // red + blue at 0.5 → rgba(128,0,128,1)
        Assert.AreEqual("rgba(128,0,128,1)", BmotionColorInterpolator.Lerp("#ff0000", "#0000ff", 0.5));
    }

    // ── rgb/rgba format parsing ───────────────────────────────────────────────

    [TestMethod]
    public void Lerp_RgbFormat_AtT1_ReturnsToColor()
    {
        Assert.AreEqual("rgba(100,200,100,1)", BmotionColorInterpolator.Lerp("rgb(0,0,0)", "rgb(100,200,100)", 1.0));
    }

    [TestMethod]
    public void Lerp_RgbaFormat_InterpolatesAlpha()
    {
        Assert.AreEqual("rgba(0,0,0,0.5)", BmotionColorInterpolator.Lerp("rgba(0,0,0,0)", "rgba(0,0,0,1)", 0.5));
    }

    // ── Invalid input ─────────────────────────────────────────────────────────

    [TestMethod]
    public void Lerp_UnparsableFrom_ReturnsFallbackToValue()
    {
        // When 'from' can't be parsed, returns the raw 'to' string unchanged
        Assert.AreEqual("#ff0000", BmotionColorInterpolator.Lerp("notacolor", "#ff0000", 0.5));
    }

    [TestMethod]
    public void Lerp_UnparsableTo_ReturnsFallbackToValue()
    {
        Assert.AreEqual("notacolor", BmotionColorInterpolator.Lerp("#ff0000", "notacolor", 0.5));
    }
}
