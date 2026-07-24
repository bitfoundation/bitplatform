
namespace Bit.Bmotion.Tests.Engine;

[TestClass]
public class EasingFunctionsTests
{
    [TestMethod]
    public void Get_Linear_ReturnsLinearFunction()
    {
        var fn = BmEaseFunctions.Get(new BmotionTransitionConfig { Ease = BmEase.Linear });

        Assert.AreEqual(0.0, fn(0.0), 1e-5);
        Assert.AreEqual(0.5, fn(0.5), 1e-5);
        Assert.AreEqual(1.0, fn(1.0), 1e-5);
    }

    [TestMethod]
    [DataRow((int)BmEase.In)]
    [DataRow((int)BmEase.Out)]
    [DataRow((int)BmEase.InOut)]
    [DataRow((int)BmEase.CircIn)]
    [DataRow((int)BmEase.CircOut)]
    [DataRow((int)BmEase.CircInOut)]
    [DataRow((int)BmEase.BackIn)]
    [DataRow((int)BmEase.BackOut)]
    [DataRow((int)BmEase.BackInOut)]
    [DataRow((int)BmEase.Anticipate)]
    public void Get_AllEasings_BoundaryConditions(int easing)
    {
        var fn = BmEaseFunctions.Get(new BmotionTransitionConfig { Ease = (BmEase)easing });

        Assert.AreEqual(0.0, fn(0.0), 1e-3);
        Assert.AreEqual(1.0, fn(1.0), 1e-3);
    }

    [TestMethod]
    public void Get_EaseOut_FasterAtStart()
    {
        // ease-out is faster early: at 25% of time, more than 25% of progress
        var fn = BmEaseFunctions.Get(new BmotionTransitionConfig { Ease = BmEase.Out });
        Assert.IsTrue(fn(0.25) > 0.25);
    }

    [TestMethod]
    public void Get_EaseIn_SlowerAtStart()
    {
        // ease-in is slower early: at 25% of time, less than 25% of progress
        var fn = BmEaseFunctions.Get(new BmotionTransitionConfig { Ease = BmEase.In });
        Assert.IsTrue(fn(0.25) < 0.25);
    }

    [TestMethod]
    public void Get_EaseInOut_SymmetricAtMidpoint()
    {
        var fn = BmEaseFunctions.Get(new BmotionTransitionConfig { Ease = BmEase.InOut });
        Assert.AreEqual(0.5, fn(0.5), 1e-2);
    }

    [TestMethod]
    public void Get_CircIn_CorrectValueAtMidpoint()
    {
        var fn = BmEaseFunctions.Get(new BmotionTransitionConfig { Ease = BmEase.CircIn });
        double expected = 1 - Math.Sqrt(1 - 0.5 * 0.5);
        Assert.AreEqual(expected, fn(0.5), 1e-5);
    }

    [TestMethod]
    public void Get_CircOut_CorrectValueAtMidpoint()
    {
        var fn = BmEaseFunctions.Get(new BmotionTransitionConfig { Ease = BmEase.CircOut });
        double expected = Math.Sqrt(1 - (0.5 - 1) * (0.5 - 1));
        Assert.AreEqual(expected, fn(0.5), 1e-5);
    }

    [TestMethod]
    public void Get_CustomCubicBezier_OverridesNamedEase()
    {
        // A (0,0,1,1) cubic-bezier approximates linear
        var config = new BmotionTransitionConfig { EaseCubicBezier = [0, 0, 1, 1] };
        var fn = BmEaseFunctions.Get(config);

        Assert.AreEqual(0.0, fn(0.0), 1e-5);
        Assert.AreEqual(1.0, fn(1.0), 1e-5);
        // Mid-point should be close to 0.5
        Assert.AreEqual(0.5, fn(0.5), 1e-1);
    }

    // ── ToCssString ──────────────────────────────────────────────────────────

    [TestMethod]
    public void ToCssString_Null_ReturnsEase()
    {
        Assert.AreEqual("ease", BmEaseFunctions.ToCssString(null));
    }

    [TestMethod]
    [DataRow((int)BmEase.Linear, "linear")]
    [DataRow((int)BmEase.In, "ease-in")]
    [DataRow((int)BmEase.Out, "ease-out")]
    [DataRow((int)BmEase.InOut, "ease-in-out")]
    [DataRow((int)BmEase.CircIn, "cubic-bezier(0.55,0,1,0.45)")]
    [DataRow((int)BmEase.BackOut, "cubic-bezier(0.33915,0,0.68085,1.4)")]
    [DataRow((int)BmEase.Anticipate, "cubic-bezier(0.31455,-0.37755,0.69245,1.37755)")]
    public void ToCssString_NamedEasing_ReturnsCorrectString(int easing, string expected)
    {
        var config = new BmotionTransitionConfig { Ease = (BmEase)easing };
        Assert.AreEqual(expected, BmEaseFunctions.ToCssString(config));
    }

    [TestMethod]
    public void ToCssString_CubicBezier_ReturnsCubicBezierString()
    {
        var config = new BmotionTransitionConfig { EaseCubicBezier = [0.1, 0.2, 0.3, 0.4] };
        Assert.AreEqual("cubic-bezier(0.1,0.2,0.3,0.4)", BmEaseFunctions.ToCssString(config));
    }

    // ── CubicBezier factory ───────────────────────────────────────────────────

    [TestMethod]
    public void CubicBezier_AtZero_ReturnsZero()
    {
        var fn = BmEaseFunctions.CubicBezier(0.42, 0, 0.58, 1);
        Assert.AreEqual(0.0, fn(0.0), 1e-5);
    }

    [TestMethod]
    public void CubicBezier_AtOne_ReturnsOne()
    {
        var fn = BmEaseFunctions.CubicBezier(0.42, 0, 0.58, 1);
        Assert.AreEqual(1.0, fn(1.0), 1e-5);
    }

    [TestMethod]
    public void CubicBezier_Linear_ApproximatesT()
    {
        // (0,0,1,1) is the identity cubic-bezier - should approximate t at all points
        var fn = BmEaseFunctions.CubicBezier(0.0, 0.0, 1.0, 1.0);
        Assert.AreEqual(0.5, fn(0.5), 1e-1);
    }
}
