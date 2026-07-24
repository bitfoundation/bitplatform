namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// Tests for the individual CSS transform-property emit mode (plan item 2.1 core:
/// <see cref="BmotionTransformComposer.BuildIndividual"/>).
/// </summary>
[TestClass]
public class IndividualTransformTests
{
    [TestMethod]
    public void Translate_ScaleUniform_Rotate_EmitAsIndividualProps()
    {
        var d = BmotionTransformComposer.BuildIndividual(new()
        {
            ["x"] = 10, ["y"] = 20, ["scale"] = 2, ["rotate"] = 45,
        });
        Assert.AreEqual("10px 20px", d["translate"]);
        Assert.AreEqual("2", d["scale"]);
        Assert.AreEqual("45deg", d["rotate"]);
        Assert.IsFalse(d.ContainsKey("transform")); // nothing needs composing
    }

    [TestMethod]
    public void Translate3D_UsesThreeComponents()
    {
        var d = BmotionTransformComposer.BuildIndividual(new() { ["x"] = 1, ["y"] = 2, ["z"] = 3 });
        Assert.AreEqual("1px 2px 3px", d["translate"]);
    }

    [TestMethod]
    public void PerAxisScale_EmitsBothFactors()
    {
        var d = BmotionTransformComposer.BuildIndividual(new() { ["scaleX"] = 1.5, ["scaleY"] = 0.5 });
        Assert.AreEqual("1.5 0.5", d["scale"]);
    }

    [TestMethod]
    public void NonIndividualComponents_StayComposedInTransform()
    {
        var d = BmotionTransformComposer.BuildIndividual(new()
        {
            ["x"] = 10, ["skewX"] = 15, ["rotateY"] = 30, ["perspective"] = 500,
        });
        Assert.AreEqual("10px 0px", d["translate"]);
        // rotateY/skewX/perspective have no individual property → composed, perspective first.
        Assert.AreEqual("perspective(500px) rotateY(30deg) skewX(15deg)", d["transform"]);
    }

    [TestMethod]
    public void IdentityComponents_AreOmitted()
    {
        var d = BmotionTransformComposer.BuildIndividual(new() { ["x"] = 0, ["scale"] = 1, ["rotate"] = 0 });
        Assert.AreEqual(0, d.Count);
    }

    [TestMethod]
    [DataRow("x", true)]
    [DataRow("scale", true)]
    [DataRow("rotate", true)]
    [DataRow("rotateZ", true)]
    [DataRow("skewX", false)]
    [DataRow("rotateX", false)]
    [DataRow("perspective", false)]
    public void IsIndividualProp_Classifies(string key, bool expected)
        => Assert.AreEqual(expected, BmotionTransformComposer.IsIndividualProp(key));
}
