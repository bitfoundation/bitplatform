namespace Bit.Bmotion.Tests.Models;

/// <summary>Tests for CSS motion-path props (plan item 3.2, motion-path half).</summary>
[TestClass]
public class MotionPathTests
{
    [TestMethod]
    public void OffsetPathAndDistance_FlowToEngineDictionary()
    {
        var d = Bm.To(offsetPath: "path('M0,0 L100,100')", offsetDistance: "50%").ToJsDictionary();
        Assert.AreEqual("path('M0,0 L100,100')", d["offsetPath"]);
        Assert.AreEqual("50%", d["offsetDistance"]);
    }

    [TestMethod]
    public void OffsetDistance_Keyframes_AnimateThroughEngine()
    {
        var d = Bm.To(offsetDistance: new BmStringKeyframes("0%", "100%")).ToJsDictionary();
        CollectionAssert.AreEqual(new[] { "0%", "100%" }, (string[])d["offsetDistance"]!);
    }

    [TestMethod]
    public void InitialStyle_EmitsDashCaseOffsetProps()
    {
        var css = Bm.To(offsetPath: "path('M0,0 L10,10')", offsetDistance: "0%").ToCssStyleString();
        StringAssert.Contains(css, "offset-path:path('M0,0 L10,10');");
        StringAssert.Contains(css, "offset-distance:0%;");
    }
}
