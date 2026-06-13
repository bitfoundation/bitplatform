using Bit.Bmotion.Engine;
using Bit.Bmotion.Models;

namespace Bit.Bmotion.Tests.Engine;

[TestClass]
public class EasingFunctionsTests
{
    [TestMethod]
    public void Get_Linear_ReturnsLinearFunction()
    {
        var fn = EasingFunctions.Get(new TransitionConfig { Ease = Easing.Linear });

        Assert.AreEqual(0.0, fn(0.0), 1e-5);
        Assert.AreEqual(0.5, fn(0.5), 1e-5);
        Assert.AreEqual(1.0, fn(1.0), 1e-5);
    }

    [TestMethod]
    [DataRow((int)Easing.EaseIn)]
    [DataRow((int)Easing.EaseOut)]
    [DataRow((int)Easing.EaseInOut)]
    [DataRow((int)Easing.CircIn)]
    [DataRow((int)Easing.CircOut)]
    [DataRow((int)Easing.CircInOut)]
    [DataRow((int)Easing.BackIn)]
    [DataRow((int)Easing.BackOut)]
    [DataRow((int)Easing.BackInOut)]
    [DataRow((int)Easing.Anticipate)]
    public void Get_AllEasings_BoundaryConditions(int easing)
    {
        var fn = EasingFunctions.Get(new TransitionConfig { Ease = (Easing)easing });

        Assert.AreEqual(0.0, fn(0.0), 1e-3);
        Assert.AreEqual(1.0, fn(1.0), 1e-3);
    }

    [TestMethod]
    public void Get_EaseOut_FasterAtStart()
    {
        // ease-out is faster early: at 25% of time, more than 25% of progress
        var fn = EasingFunctions.Get(new TransitionConfig { Ease = Easing.EaseOut });
        Assert.IsTrue(fn(0.25) > 0.25);
    }

    [TestMethod]
    public void Get_EaseIn_SlowerAtStart()
    {
        // ease-in is slower early: at 25% of time, less than 25% of progress
        var fn = EasingFunctions.Get(new TransitionConfig { Ease = Easing.EaseIn });
        Assert.IsTrue(fn(0.25) < 0.25);
    }

    [TestMethod]
    public void Get_EaseInOut_SymmetricAtMidpoint()
    {
        var fn = EasingFunctions.Get(new TransitionConfig { Ease = Easing.EaseInOut });
        Assert.AreEqual(0.5, fn(0.5), 1e-2);
    }

    [TestMethod]
    public void Get_CircIn_CorrectValueAtMidpoint()
    {
        var fn = EasingFunctions.Get(new TransitionConfig { Ease = Easing.CircIn });
        double expected = 1 - Math.Sqrt(1 - 0.5 * 0.5);
        Assert.AreEqual(expected, fn(0.5), 1e-5);
    }

    [TestMethod]
    public void Get_CircOut_CorrectValueAtMidpoint()
    {
        var fn = EasingFunctions.Get(new TransitionConfig { Ease = Easing.CircOut });
        double expected = Math.Sqrt(1 - (0.5 - 1) * (0.5 - 1));
        Assert.AreEqual(expected, fn(0.5), 1e-5);
    }

    [TestMethod]
    public void Get_CustomCubicBezier_OverridesNamedEase()
    {
        // A (0,0,1,1) cubic-bezier approximates linear
        var config = new TransitionConfig { EaseCubicBezier = [0, 0, 1, 1] };
        var fn = EasingFunctions.Get(config);

        Assert.AreEqual(0.0, fn(0.0), 1e-5);
        Assert.AreEqual(1.0, fn(1.0), 1e-5);
        // Mid-point should be close to 0.5
        Assert.AreEqual(0.5, fn(0.5), 1e-1);
    }

    // ── ToCssString ──────────────────────────────────────────────────────────

    [TestMethod]
    public void ToCssString_Null_ReturnsEase()
    {
        Assert.AreEqual("ease", EasingFunctions.ToCssString(null));
    }

    [TestMethod]
    [DataRow((int)Easing.Linear, "linear")]
    [DataRow((int)Easing.EaseIn, "ease-in")]
    [DataRow((int)Easing.EaseOut, "ease-out")]
    [DataRow((int)Easing.EaseInOut, "ease-in-out")]
    [DataRow((int)Easing.CircIn, "ease")]
    [DataRow((int)Easing.BackOut, "ease")]
    [DataRow((int)Easing.Anticipate, "ease")]
    public void ToCssString_NamedEasing_ReturnsCorrectString(int easing, string expected)
    {
        var config = new TransitionConfig { Ease = (Easing)easing };
        Assert.AreEqual(expected, EasingFunctions.ToCssString(config));
    }

    [TestMethod]
    public void ToCssString_CubicBezier_ReturnsCubicBezierString()
    {
        var config = new TransitionConfig { EaseCubicBezier = [0.1, 0.2, 0.3, 0.4] };
        Assert.AreEqual("cubic-bezier(0.1,0.2,0.3,0.4)", EasingFunctions.ToCssString(config));
    }

    // ── CubicBezier factory ───────────────────────────────────────────────────

    [TestMethod]
    public void CubicBezier_AtZero_ReturnsZero()
    {
        var fn = EasingFunctions.CubicBezier(0.42, 0, 0.58, 1);
        Assert.AreEqual(0.0, fn(0.0), 1e-5);
    }

    [TestMethod]
    public void CubicBezier_AtOne_ReturnsOne()
    {
        var fn = EasingFunctions.CubicBezier(0.42, 0, 0.58, 1);
        Assert.AreEqual(1.0, fn(1.0), 1e-5);
    }

    [TestMethod]
    public void CubicBezier_Linear_ApproximatesT()
    {
        // (0,0,1,1) is the identity cubic-bezier - should approximate t at all points
        var fn = EasingFunctions.CubicBezier(0.0, 0.0, 1.0, 1.0);
        Assert.AreEqual(0.5, fn(0.5), 1e-1);
    }
}
