namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// Tests for the expanded easing presets (plan item 2.2): Sine/Quad/Quart/Quint/Expo/Elastic/Bounce
/// and stepped easings, plus their compositor (CSS/linear) representations.
/// </summary>
[TestClass]
public class EasingPresetsTests
{
    private const double Tol = 1e-6;

    private static Func<double, double> Ease(BmEase e) => BmEaseFunctions.Get(e);

    [TestMethod]
    [DataRow(BmEase.SineIn)]
    [DataRow(BmEase.SineOut)]
    [DataRow(BmEase.SineInOut)]
    [DataRow(BmEase.QuadIn)]
    [DataRow(BmEase.QuadOut)]
    [DataRow(BmEase.QuadInOut)]
    [DataRow(BmEase.QuartIn)]
    [DataRow(BmEase.QuartOut)]
    [DataRow(BmEase.QuartInOut)]
    [DataRow(BmEase.QuintIn)]
    [DataRow(BmEase.QuintOut)]
    [DataRow(BmEase.QuintInOut)]
    [DataRow(BmEase.ExpoIn)]
    [DataRow(BmEase.ExpoOut)]
    [DataRow(BmEase.ExpoInOut)]
    [DataRow(BmEase.ElasticIn)]
    [DataRow(BmEase.ElasticOut)]
    [DataRow(BmEase.ElasticInOut)]
    [DataRow(BmEase.BounceIn)]
    [DataRow(BmEase.BounceOut)]
    [DataRow(BmEase.BounceInOut)]
    public void AllPresets_HitEndpoints(BmEase e)
    {
        var f = Ease(e);
        Assert.AreEqual(0, f(0), Tol, $"{e} at 0");
        Assert.AreEqual(1, f(1), Tol, $"{e} at 1");
    }

    [TestMethod]
    public void PowerCurves_MatchReferenceValues()
    {
        Assert.AreEqual(0.25, Ease(BmEase.QuadIn)(0.5), Tol);
        Assert.AreEqual(0.75, Ease(BmEase.QuadOut)(0.5), Tol);
        Assert.AreEqual(0.0625, Ease(BmEase.QuartIn)(0.5), Tol);
        Assert.AreEqual(0.03125, Ease(BmEase.QuintIn)(0.5), Tol);
        Assert.AreEqual(0.5, Ease(BmEase.SineInOut)(0.5), Tol);
    }

    [TestMethod]
    public void Elastic_And_Bounce_OvershootWithinBounds()
    {
        // Bounce is always within [0,1]; elastic overshoots but starts/ends clamped.
        for (double t = 0; t <= 1.0001; t += 0.05)
        {
            var b = Ease(BmEase.BounceOut)(t);
            Assert.IsTrue(b >= -Tol && b <= 1 + Tol, $"bounce {t}={b}");
        }
        // ElasticOut overshoots above 1 somewhere in the middle.
        var peak = 0.0;
        for (double t = 0; t <= 1; t += 0.01) peak = Math.Max(peak, Ease(BmEase.ElasticOut)(t));
        Assert.IsTrue(peak > 1.0, "elastic-out should overshoot past 1");
    }

    // ── Steps ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Steps_JumpEnd_Staircase()
    {
        var f = BmEaseFunctions.Steps(4, BmStepJump.End);
        Assert.AreEqual(0, f(0), Tol);
        Assert.AreEqual(0, f(0.24), Tol);
        Assert.AreEqual(0.25, f(0.25), Tol);
        Assert.AreEqual(0.5, f(0.5), Tol);
        Assert.AreEqual(0.75, f(0.99), Tol);
        Assert.AreEqual(1, f(1), Tol);
    }

    [TestMethod]
    public void Steps_JumpStart_JumpsImmediately()
    {
        var f = BmEaseFunctions.Steps(4, BmStepJump.Start);
        Assert.AreEqual(0, f(0), Tol);
        Assert.AreEqual(0.25, f(0.01), Tol);
        Assert.AreEqual(1, f(1), Tol);
    }

    [TestMethod]
    public void Steps_JumpNone_ReachesBothEnds()
    {
        var f = BmEaseFunctions.Steps(5, BmStepJump.None);
        Assert.AreEqual(0, f(0), Tol);
        Assert.AreEqual(0.25, f(0.25), Tol); // floor(1.25)/4 = 1/4
        Assert.AreEqual(1, f(1), Tol);
    }

    [TestMethod]
    public void Steps_JumpBoth_OffsetsBothEnds()
    {
        var f = BmEaseFunctions.Steps(4, BmStepJump.Both);
        Assert.AreEqual(0.2, f(0), Tol);   // 1/(4+1)
        Assert.AreEqual(1, f(1), Tol);
    }

    [TestMethod]
    public void Steps_ConfiguredViaTween_OverridesEase()
    {
        var config = Bm.Tween(steps: 3, stepJump: BmStepJump.End).ToConfig();
        Assert.AreEqual(3, config.StepCount);
        var f = BmEaseFunctions.Get(config);
        Assert.AreEqual(0, f(0), Tol);
        Assert.AreEqual(1.0 / 3, f(0.4), Tol);
    }

    // ── CSS / compositor representation ───────────────────────────────────────

    [TestMethod]
    public void ToCssString_Steps_EmitsCssSteps()
    {
        Assert.AreEqual("steps(3,jump-end)", BmEaseFunctions.ToCssString(Bm.Tween(steps: 3).ToConfig()));
        Assert.AreEqual("steps(3,jump-start)", BmEaseFunctions.ToCssString(Bm.Tween(steps: 3, stepJump: BmStepJump.Start).ToConfig()));
    }

    [TestMethod]
    public void ToCssString_SingleStepJumpNone_NormalizesToJumpEnd()
    {
        // steps(1,jump-none) is invalid CSS; a single step is identical to jump-end at runtime.
        Assert.AreEqual("steps(1,jump-end)",
            BmEaseFunctions.ToCssString(Bm.Tween(steps: 1, stepJump: BmStepJump.None).ToConfig()));
        // >= 2 steps keep jump-none (valid CSS).
        Assert.AreEqual("steps(2,jump-none)",
            BmEaseFunctions.ToCssString(Bm.Tween(steps: 2, stepJump: BmStepJump.None).ToConfig()));
    }

    [TestMethod]
    public void ToCssString_PowerCurves_EmitCubicBezier()
    {
        Assert.AreEqual("cubic-bezier(0.37,0,0.63,1)",
            BmEaseFunctions.ToCssString(Bm.Tween(ease: BmEase.SineInOut).ToConfig()));
    }

    [TestMethod]
    public void HasFaithfulCssEasing_TrueOnlyForExactCssCurves()
    {
        // Faithful: keyword/back presets, explicit bezier, and steps (ToCssString reproduces them exactly).
        Assert.IsTrue(BmEaseFunctions.HasFaithfulCssEasing(Bm.Tween(ease: BmEase.Linear).ToConfig()));
        Assert.IsTrue(BmEaseFunctions.HasFaithfulCssEasing(Bm.Tween(ease: BmEase.InOut).ToConfig()));
        Assert.IsTrue(BmEaseFunctions.HasFaithfulCssEasing(Bm.Tween(ease: BmEase.BackOut).ToConfig()));
        Assert.IsTrue(BmEaseFunctions.HasFaithfulCssEasing(Bm.Tween(bezier: [0.1, 0.2, 0.3, 0.4]).ToConfig()));
        Assert.IsTrue(BmEaseFunctions.HasFaithfulCssEasing(Bm.Tween(steps: 4).ToConfig()));

        // NOT faithful: these serialize as APPROXIMATE cubic-beziers while the runtime uses exact
        // math, so offloading their CSS curve would drift from the rAF result.
        Assert.IsFalse(BmEaseFunctions.HasFaithfulCssEasing(Bm.Tween(ease: BmEase.SineInOut).ToConfig()));
        Assert.IsFalse(BmEaseFunctions.HasFaithfulCssEasing(Bm.Tween(ease: BmEase.QuartOut).ToConfig()));
        Assert.IsFalse(BmEaseFunctions.HasFaithfulCssEasing(Bm.Tween(ease: BmEase.ExpoIn).ToConfig()));
        Assert.IsFalse(BmEaseFunctions.HasFaithfulCssEasing(Bm.Tween(ease: BmEase.CircInOut).ToConfig()));
        Assert.IsFalse(BmEaseFunctions.HasFaithfulCssEasing(Bm.Tween(ease: BmEase.Anticipate).ToConfig()));
        Assert.IsFalse(BmEaseFunctions.HasFaithfulCssEasing(Bm.Tween(ease: BmEase.ElasticOut).ToConfig()));
        Assert.IsFalse(BmEaseFunctions.HasFaithfulCssEasing(Bm.Tween(ease: BmEase.BounceInOut).ToConfig()));
    }
}
