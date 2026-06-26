
namespace Bit.Bmotion.Tests.Engine;

[TestClass]
public class TransformComposerTests
{
    // ── IsTransformProp ───────────────────────────────────────────────────────

    [TestMethod]
    [DataRow("x", true)]
    [DataRow("y", true)]
    [DataRow("z", true)]
    [DataRow("rotate", true)]
    [DataRow("rotateX", true)]
    [DataRow("rotateY", true)]
    [DataRow("rotateZ", true)]
    [DataRow("scaleX", true)]
    [DataRow("scaleY", true)]
    [DataRow("scale", true)]
    [DataRow("skewX", true)]
    [DataRow("skewY", true)]
    [DataRow("perspective", true)]
    [DataRow("X", true)]      // case-insensitive
    [DataRow("SCALE", true)]
    [DataRow("opacity", false)]
    [DataRow("width", false)]
    [DataRow("color", false)]
    [DataRow("backgroundColor", false)]
    public void IsTransformProp_ReturnsExpected(string key, bool expected)
    {
        Assert.AreEqual(expected, BmotionTransformComposer.IsTransformProp(key));
    }

    // ── Build - case-insensitive keys ─────────────────────────────────────────

    [TestMethod]
    public void Build_MixedCaseKeys_ComposesTransform()
    {
        // The engine stores transform components in a case-insensitive dictionary (matching
        // IsTransformProp's OrdinalIgnoreCase contract), so mixed-case keys identified as valid
        // transform props must still compose end-to-end rather than being silently dropped.
        var t = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["X"] = 10,
            ["SCALE"] = 2.0,
        };
        var result = BmotionTransformComposer.Build(t);

        StringAssert.Contains(result, "translate(10px,0px)");
        StringAssert.Contains(result, "scale(2)");
    }

    // ── Build - empty/identity ────────────────────────────────────────────────

    [TestMethod]
    public void Build_EmptyDict_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, BmotionTransformComposer.Build([]));
    }

    [TestMethod]
    public void Build_AllIdentityValues_ReturnsEmpty()
    {
        var t = new Dictionary<string, double> { ["x"] = 0, ["y"] = 0, ["rotate"] = 0, ["scale"] = 1 };
        Assert.AreEqual(string.Empty, BmotionTransformComposer.Build(t));
    }

    // ── Translate ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void Build_Translation2D_ReturnsTranslate()
    {
        var t = new Dictionary<string, double> { ["x"] = 10, ["y"] = 20 };
        Assert.AreEqual("translate(10px,20px)", BmotionTransformComposer.Build(t));
    }

    [TestMethod]
    public void Build_Translation3D_ReturnsTranslate3d()
    {
        var t = new Dictionary<string, double> { ["x"] = 10, ["y"] = 20, ["z"] = 30 };
        Assert.AreEqual("translate3d(10px,20px,30px)", BmotionTransformComposer.Build(t));
    }

    [TestMethod]
    public void Build_OnlyX_ReturnsTranslateWithZeroY()
    {
        var t = new Dictionary<string, double> { ["x"] = 50 };
        Assert.AreEqual("translate(50px,0px)", BmotionTransformComposer.Build(t));
    }

    // ── Scale ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Build_UniformScale_ReturnsScale()
    {
        var t = new Dictionary<string, double> { ["scale"] = 2.0 };
        Assert.AreEqual("scale(2)", BmotionTransformComposer.Build(t));
    }

    [TestMethod]
    public void Build_ScaleXOnly_ReturnsScaleX()
    {
        var t = new Dictionary<string, double> { ["scaleX"] = 1.5 };
        Assert.AreEqual("scaleX(1.5)", BmotionTransformComposer.Build(t));
    }

    [TestMethod]
    public void Build_ScaleYOnly_ReturnsScaleY()
    {
        var t = new Dictionary<string, double> { ["scaleY"] = 0.5 };
        Assert.AreEqual("scaleY(0.5)", BmotionTransformComposer.Build(t));
    }

    [TestMethod]
    public void Build_ScaleXAtIdentity_OmitsScaleX()
    {
        // scaleX=1 is identity → omitted; y=10 is non-zero → translate present
        var t = new Dictionary<string, double> { ["scaleX"] = 1.0, ["y"] = 10 };
        Assert.AreEqual("translate(0px,10px)", BmotionTransformComposer.Build(t));
    }

    // ── Rotate ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Build_Rotate_ReturnsRotateDeg()
    {
        var t = new Dictionary<string, double> { ["rotate"] = 45 };
        Assert.AreEqual("rotate(45deg)", BmotionTransformComposer.Build(t));
    }

    [TestMethod]
    public void Build_RotateZAlias_ReturnsRotateDeg()
    {
        var t = new Dictionary<string, double> { ["rotateZ"] = 90 };
        Assert.AreEqual("rotate(90deg)", BmotionTransformComposer.Build(t));
    }

    [TestMethod]
    public void Build_RotateX_ReturnsRotateXDeg()
    {
        var t = new Dictionary<string, double> { ["rotateX"] = 30 };
        Assert.AreEqual("rotateX(30deg)", BmotionTransformComposer.Build(t));
    }

    [TestMethod]
    public void Build_RotateY_ReturnsRotateYDeg()
    {
        var t = new Dictionary<string, double> { ["rotateY"] = 60 };
        Assert.AreEqual("rotateY(60deg)", BmotionTransformComposer.Build(t));
    }

    // ── Skew ──────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Build_SkewX_ReturnsSkewXDeg()
    {
        var t = new Dictionary<string, double> { ["skewX"] = 15 };
        Assert.AreEqual("skewX(15deg)", BmotionTransformComposer.Build(t));
    }

    [TestMethod]
    public void Build_SkewY_ReturnsSkewYDeg()
    {
        var t = new Dictionary<string, double> { ["skewY"] = 10 };
        Assert.AreEqual("skewY(10deg)", BmotionTransformComposer.Build(t));
    }

    // ── Perspective ───────────────────────────────────────────────────────────

    [TestMethod]
    public void Build_Perspective_AppearsFirst()
    {
        var t = new Dictionary<string, double> { ["perspective"] = 500, ["x"] = 10 };
        var result = BmotionTransformComposer.Build(t);
        StringAssert.StartsWith(result, "perspective(500px)");
        StringAssert.Contains(result, "translate(10px,0px)");
    }

    // ── Combined / ordering ───────────────────────────────────────────────────

    [TestMethod]
    public void Build_Combined_PreservesOrder()
    {
        var t = new Dictionary<string, double>
        {
            ["x"] = 100,
            ["y"] = 50,
            ["scale"] = 1.5,
            ["rotate"] = 45,
        };
        var result = BmotionTransformComposer.Build(t);

        StringAssert.Contains(result, "translate(100px,50px)");
        StringAssert.Contains(result, "scale(1.5)");
        StringAssert.Contains(result, "rotate(45deg)");
        // Order: translate → scale → rotate
        Assert.IsTrue(result.IndexOf("translate") < result.IndexOf("scale"));
        Assert.IsTrue(result.IndexOf("scale") < result.IndexOf("rotate"));
    }
}
