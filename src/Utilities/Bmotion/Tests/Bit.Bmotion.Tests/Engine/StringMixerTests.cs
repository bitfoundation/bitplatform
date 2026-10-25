namespace Bit.Bmotion.Tests.Engine;

[TestClass]
public class StringMixerTests
{
    [TestMethod]
    public void Mix_SingleFunction_InterpolatesNumber()
    {
        var mix = BmotionStringMixer.TryCreateMix("blur(0px)", "blur(8px)");

        Assert.IsNotNull(mix);
        Assert.AreEqual("blur(0px)", mix(0));
        Assert.AreEqual("blur(4px)", mix(0.5));
        Assert.AreEqual("blur(8px)", mix(1));
    }

    [TestMethod]
    public void Mix_MultiPartFilter_InterpolatesEveryNumber()
    {
        var mix = BmotionStringMixer.TryCreateMix(
            "blur(0px) brightness(1)", "blur(10px) brightness(1.5)");

        Assert.IsNotNull(mix);
        Assert.AreEqual("blur(5px) brightness(1.25)", mix(0.5));
    }

    [TestMethod]
    public void Mix_BoxShadowWithColor_InterpolatesNumbersAndColor()
    {
        var mix = BmotionStringMixer.TryCreateMix(
            "0px 0px 0px rgba(0,0,0,0)", "0px 10px 20px rgba(0,0,0,0.5)");

        Assert.IsNotNull(mix);
        Assert.AreEqual("0px 5px 10px rgba(0,0,0,0.25)", mix(0.5));
    }

    [TestMethod]
    public void Mix_HexColors_InterpolateAsColors()
    {
        var mix = BmotionStringMixer.TryCreateMix(
            "linear-gradient(#000000, #ffffff)", "linear-gradient(#646464, #ffffff)");

        Assert.IsNotNull(mix);
        Assert.AreEqual("linear-gradient(rgba(50,50,50,1), rgba(255,255,255,1))", mix(0.5));
    }

    [TestMethod]
    public void Mix_NumbersExtrapolate_ColorsClamp()
    {
        var numMix = BmotionStringMixer.TryCreateMix("blur(0px)", "blur(10px)");
        Assert.IsNotNull(numMix);
        Assert.AreEqual("blur(15px)", numMix(1.5)); // spring overshoot flows through

        var colorMix = BmotionStringMixer.TryCreateMix("rgba(0,0,0,1)", "rgba(100,100,100,1)");
        Assert.IsNotNull(colorMix);
        Assert.AreEqual("rgba(100,100,100,1)", colorMix(1.5)); // colors clamp at the target
    }

    [TestMethod]
    public void Mix_MismatchedShapes_ReturnsNull()
    {
        // Different function names (literal skeletons differ).
        Assert.IsNull(BmotionStringMixer.TryCreateMix("blur(4px)", "brightness(2)"));
        // Different units.
        Assert.IsNull(BmotionStringMixer.TryCreateMix("100px", "50%"));
        // Different token counts.
        Assert.IsNull(BmotionStringMixer.TryCreateMix("blur(1px)", "blur(1px) brightness(2)"));
        // Number vs color token kinds.
        Assert.IsNull(BmotionStringMixer.TryCreateMix("4", "#ff0000"));
        // No tokens at all.
        Assert.IsNull(BmotionStringMixer.TryCreateMix("auto", "none"));
    }

    [TestMethod]
    public void Mix_EmptyInputs_ReturnsNull()
    {
        Assert.IsNull(BmotionStringMixer.TryCreateMix("", "blur(4px)"));
        Assert.IsNull(BmotionStringMixer.TryCreateMix("blur(4px)", ""));
    }
}
