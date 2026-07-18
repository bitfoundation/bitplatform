namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// Golden tests pinning the exact transform/CSS output of <see cref="BmProps.ToCssStyleString"/>
/// and <see cref="BmProps.ToCssStyleDictionary"/>. These lock current behavior so the
/// single-source-of-truth refactor (plan item 1.4) can route both paths through
/// <see cref="BmotionTransformComposer"/> without drifting.
/// </summary>
[TestClass]
public class BmPropsCompositionGoldenTests
{
    // ── ToCssStyleString: samples the FIRST keyframe ──────────────────────────

    [TestMethod]
    public void StyleString_Translate2D()
        => Assert.AreEqual("transform:translate(10px,20px);",
            new BmProps { X = 10, Y = 20 }.ToCssStyleString());

    [TestMethod]
    public void StyleString_Translate3D()
        => Assert.AreEqual("transform:translate3d(10px,20px,30px);",
            new BmProps { X = 10, Y = 20, Z = 30 }.ToCssStyleString());

    [TestMethod]
    public void StyleString_UniformScale()
        => Assert.AreEqual("transform:scale(2);", new BmProps { Scale = 2 }.ToCssStyleString());

    [TestMethod]
    public void StyleString_ScaleXY()
        => Assert.AreEqual("transform:scaleX(1.5) scaleY(0.5);",
            new BmProps { ScaleX = 1.5, ScaleY = 0.5 }.ToCssStyleString());

    [TestMethod]
    public void StyleString_RotateZAlias_FallsBackToRotate()
        => Assert.AreEqual("transform:rotate(45deg);", new BmProps { Rotate = 45 }.ToCssStyleString());

    [TestMethod]
    public void StyleString_RotateZWins()
        => Assert.AreEqual("transform:rotate(90deg);",
            new BmProps { Rotate = 45, RotateZ = 90 }.ToCssStyleString());

    [TestMethod]
    public void StyleString_FullOrdering_PerspectiveFirst()
        => Assert.AreEqual(
            "transform:perspective(500px) translate(100px,50px) scale(1.5) rotate(45deg) rotateX(30deg) rotateY(60deg) skewX(15deg) skewY(10deg);",
            new BmProps
            {
                Perspective = 500, X = 100, Y = 50, Scale = 1.5, Rotate = 45,
                RotateX = 30, RotateY = 60, SkewX = 15, SkewY = 10,
            }.ToCssStyleString());

    [TestMethod]
    public void StyleString_FirstKeyframeSampled()
        => Assert.AreEqual("transform:translate(1px,0px);",
            new BmProps { X = new BmKeyframes([1, 2, 3]) }.ToCssStyleString());

    [TestMethod]
    public void StyleString_IncludesOpacityAndOrigin()
    {
        var css = new BmProps { X = 5, Opacity = 0.5, OriginX = 0, OriginY = 0.5 }.ToCssStyleString();
        StringAssert.Contains(css, "transform:translate(5px,0px);");
        StringAssert.Contains(css, "transform-origin:0% 50%;");
        StringAssert.Contains(css, "opacity:0.5;");
    }

    // ── ToCssStyleDictionary: samples the LAST keyframe ───────────────────────

    [TestMethod]
    public void StyleDict_Translate2D()
    {
        var d = new BmProps { X = 10, Y = 20 }.ToCssStyleDictionary();
        Assert.AreEqual("translate(10px,20px)", d["transform"]);
    }

    [TestMethod]
    public void StyleDict_FullOrdering_PerspectiveFirst()
    {
        var d = new BmProps
        {
            Perspective = 500, X = 100, Y = 50, Scale = 1.5, Rotate = 45,
            RotateX = 30, RotateY = 60, SkewX = 15, SkewY = 10,
        }.ToCssStyleDictionary();
        Assert.AreEqual(
            "perspective(500px) translate(100px,50px) scale(1.5) rotate(45deg) rotateX(30deg) rotateY(60deg) skewX(15deg) skewY(10deg)",
            d["transform"]);
    }

    [TestMethod]
    public void StyleDict_LastKeyframeSampled()
    {
        var d = new BmProps { X = new BmKeyframes([1, 2, 3]) }.ToCssStyleDictionary();
        Assert.AreEqual("translate(3px,0px)", d["transform"]);
    }

    [TestMethod]
    public void StyleDict_ScaleXY()
    {
        var d = new BmProps { ScaleX = 1.5, ScaleY = 0.5 }.ToCssStyleDictionary();
        Assert.AreEqual("scaleX(1.5) scaleY(0.5)", d["transform"]);
    }

    // ── SVG `d` is an attribute, not a CSS property ───────────────────────────

    [TestMethod]
    public void StyleString_ExcludesSvgD()
    {
        // `d` in an inline style string is invalid CSS (needs a path() wrapper); it must not appear.
        // Other string props are unaffected.
        var css = new BmProps { D = "M0 0 L10 10", Left = "5px" }.ToCssStyleString();
        StringAssert.Contains(css, "left:5px;");
        Assert.IsFalse(css.Contains("d:"), $"d must be omitted from the inline style string: {css}");
    }

    [TestMethod]
    public void StyleDict_IncludesSvgD_ForSetAttribute()
    {
        // The instant-set dictionary keeps `d`; JS applies it via setAttribute (_svgGeomAttrs).
        var d = new BmProps { D = "M0 0 L10 10" }.ToCssStyleDictionary();
        Assert.AreEqual("M0 0 L10 10", d["d"]);
    }
}
