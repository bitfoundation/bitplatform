namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// Tests for SVG shape morphing (plan item 3.2). Same-structure <c>d</c> paths interpolate through
/// the string mixer; incompatible paths return null (the driver then snaps to the target).
/// </summary>
[TestClass]
public class PathMorphTests
{
    [TestMethod]
    public void SameStructurePaths_MorphControlPoints()
    {
        var mix = BmotionStringMixer.TryCreateMix("M0 0 L10 10", "M10 10 L20 20");
        Assert.IsNotNull(mix);
        Assert.AreEqual("M0 0 L10 10", mix!(0));
        Assert.AreEqual("M5 5 L15 15", mix(0.5));
        Assert.AreEqual("M10 10 L20 20", mix(1));
    }

    [TestMethod]
    public void CubicBezierPaths_MorphAllControlPoints()
    {
        var mix = BmotionStringMixer.TryCreateMix(
            "M0 0 C10 0 20 10 30 10",
            "M0 0 C20 0 40 20 60 20");
        Assert.IsNotNull(mix);
        Assert.AreEqual("M0 0 C15 0 30 15 45 15", mix!(0.5));
    }

    [TestMethod]
    public void IncompatiblePaths_ReturnNull_SoTheDriverSnaps()
    {
        // Different command structure (L vs C) → different literal skeletons → not mixable.
        Assert.IsNull(BmotionStringMixer.TryCreateMix("M0 0 L10 10", "M0 0 C1 1 2 2 3 3"));
        // Different number of segments → not mixable.
        Assert.IsNull(BmotionStringMixer.TryCreateMix("M0 0 L10 10", "M0 0 L10 10 L20 20"));
    }

    [TestMethod]
    public void DProp_FlowsToEngineDictionary()
    {
        var single = Bm.To(d: "M0 0 L10 10").ToJsDictionary();
        Assert.AreEqual("M0 0 L10 10", single["d"]);

        var frames = Bm.To(d: new BmStringKeyframes("M0 0", "M10 10")).ToJsDictionary();
        CollectionAssert.AreEqual(new[] { "M0 0", "M10 10" }, (string[])frames["d"]!);
    }

    [TestMethod]
    public void DProp_ParticipatesInValueEquality()
    {
        Assert.IsFalse(Bm.To(d: "M0 0").ValueEquals(Bm.To(d: "M1 1")));
        Assert.IsTrue(Bm.To(d: "M0 0").ValueEquals(Bm.To(d: "M0 0")));
    }
}
