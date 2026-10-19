namespace Bit.Bmotion.Tests.Models;

/// <summary>Tests for the wider animatable property surface (plan item 2.4).</summary>
[TestClass]
public class PropertySurfaceTests
{
    [TestMethod]
    public void TypedLayoutProps_FlowToEngineDictionary()
    {
        var d = Bm.To(top: "20px", left: "0", gap: "2rem", letterSpacing: "0.1em").ToJsDictionary();
        Assert.AreEqual("20px", d["top"]);
        Assert.AreEqual("0", d["left"]);
        Assert.AreEqual("2rem", d["gap"]);
        Assert.AreEqual("0.1em", d["letterSpacing"]);
    }

    [TestMethod]
    public void GenericCssBag_DashCaseKey_BecomesCamelCaseEngineKey()
    {
        var d = Bm.To(css: new() { ["background-position"] = "0 0", ["gap"] = "1rem" }).ToJsDictionary();
        Assert.AreEqual("0 0", d["backgroundPosition"]);
        Assert.AreEqual("1rem", d["gap"]);
    }

    [TestMethod]
    public void GenericCssBag_CustomProperty_PassesThroughUnchanged()
    {
        var d = Bm.To(css: new() { ["--my-var"] = "5px" }).ToJsDictionary();
        Assert.AreEqual("5px", d["--my-var"]);
    }

    [TestMethod]
    public void InitialStyle_EmitsDashCaseForExtendedProps()
    {
        var css = Bm.To(top: "20px", gap: "2rem", css: new() { ["letterSpacing"] = "0.1em" }).ToCssStyleString();
        StringAssert.Contains(css, "top:20px;");
        StringAssert.Contains(css, "gap:2rem;");
        StringAssert.Contains(css, "letter-spacing:0.1em;");
    }

    [TestMethod]
    public void InstantSet_EmitsCamelCaseForExtendedProps()
    {
        var d = Bm.To(letterSpacing: "0.1em", css: new() { ["background-size"] = "cover" }).ToCssStyleDictionary();
        Assert.AreEqual("0.1em", d["letterSpacing"]);
        Assert.AreEqual("cover", d["backgroundSize"]);
    }

    [TestMethod]
    [DataRow("background-position", "backgroundPosition")]
    [DataRow("gap", "gap")]
    [DataRow("letterSpacing", "letterSpacing")]
    [DataRow("--my-var", "--my-var")]
    public void ToCamelCase_Converts(string input, string expected)
        => Assert.AreEqual(expected, BmProps.ToCamelCase(input));

    [TestMethod]
    [DataRow("letterSpacing", "letter-spacing")]
    [DataRow("gap", "gap")]
    [DataRow("--my-var", "--my-var")]
    public void ToDashCase_Converts(string input, string expected)
        => Assert.AreEqual(expected, BmProps.ToDashCase(input));

    [TestMethod]
    public void ValueEquals_DetectsExtendedPropChange()
    {
        Assert.IsFalse(Bm.To(gap: "1rem").ValueEquals(Bm.To(gap: "2rem")));
        Assert.IsTrue(Bm.To(gap: "1rem").ValueEquals(Bm.To(gap: "1rem")));
        Assert.IsFalse(Bm.To(css: new() { ["gap"] = "1rem" }).ValueEquals(Bm.To(css: new() { ["gap"] = "2rem" })));
    }
}
