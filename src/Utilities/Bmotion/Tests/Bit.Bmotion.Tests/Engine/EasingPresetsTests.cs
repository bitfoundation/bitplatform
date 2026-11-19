namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// Tests for the expanded easing presets (plan item 2.2): Sine/Quad/Quart/Quint/Expo/Elastic/Bounce
/// and stepped easings, plus their compositor (CSS/linear) representations.
/// </summary>
[TestClass]
public class EasingPresetsTests
{
    private const double Tol = 1e-6;

    private static Func<double, double> Ease(BmotionEase e) => BmotionEaseFunctions.Get(e);

    [TestMethod]
    [DataRow(BmotionEase.SineIn)]
    [DataRow(BmotionEase.SineOut)]
    [DataRow(BmotionEase.SineInOut)]
    [DataRow(BmotionEase.QuadIn)]
    [DataRow(BmotionEase.QuadOut)]
    [DataRow(BmotionEase.QuadInOut)]
    [DataRow(BmotionEase.QuartIn)]
    [DataRow(BmotionEase.QuartOut)]
    [DataRow(BmotionEase.QuartInOut)]
    [DataRow(BmotionEase.QuintIn)]
    [DataRow(BmotionEase.QuintOut)]
    [DataRow(BmotionEase.QuintInOut)]
    [DataRow(BmotionEase.ExpoIn)]
    [DataRow(BmotionEase.ExpoOut)]
    [DataRow(BmotionEase.ExpoInOut)]
    [DataRow(BmotionEase.ElasticIn)]
    [DataRow(BmotionEase.ElasticOut)]
    [DataRow(BmotionEase.ElasticInOut)]
    [DataRow(BmotionEase.BounceIn)]
    [DataRow(BmotionEase.BounceOut)]
    [DataRow(BmotionEase.BounceInOut)]
    public void AllPresets_HitEndpoints(BmotionEase e)
    {
        var f = Ease(e);
        Assert.AreEqual(0, f(0), Tol, $"{e} at 0");
        Assert.AreEqual(1, f(1), Tol, $"{e} at 1");
    }

    [TestMethod]
    public void PowerCurves_MatchReferenceValues()
    {
        Assert.AreEqual(0.25, Ease(BmotionEase.QuadIn)(0.5), Tol);
        Assert.AreEqual(0.75, Ease(BmotionEase.QuadOut)(0.5), Tol);
        Assert.AreEqual(0.0625, Ease(BmotionEase.QuartIn)(0.5), Tol);
        Assert.AreEqual(0.03125, Ease(BmotionEase.QuintIn)(0.5), Tol);
        Assert.AreEqual(0.5, Ease(BmotionEase.SineInOut)(0.5), Tol);
    }

    [TestMethod]
    public void Elastic_And_Bounce_OvershootWithinBounds()
    {
        // Bounce is always within [0,1]; elastic overshoots but starts/ends clamped.
        for (double t = 0; t <= 1.0001; t += 0.05)
        {
            var b = Ease(BmotionEase.BounceOut)(t);
            Assert.IsTrue(b >= -Tol && b <= 1 + Tol, $"bounce {t}={b}");
        }
        // ElasticOut overshoots above 1 somewhere in the middle.
        var peak = 0.0;
        for (double t = 0; t <= 1; t += 0.01) peak = Math.Max(peak, Ease(BmotionEase.ElasticOut)(t));
        Assert.IsTrue(peak > 1.0, "elastic-out should overshoot past 1");
    }

    // ── Steps ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Steps_JumpEnd_Staircase()
    {
        var f = BmotionEaseFunctions.Steps(4, BmotionStepJump.End);
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
        var f = BmotionEaseFunctions.Steps(4, BmotionStepJump.Start);
        Assert.AreEqual(0, f(0), Tol);
        Assert.AreEqual(0.25, f(0.01), Tol);
        Assert.AreEqual(1, f(1), Tol);
    }

    [TestMethod]
    public void Steps_JumpNone_ReachesBothEnds()
    {
        var f = BmotionEaseFunctions.Steps(5, BmotionStepJump.None);
        Assert.AreEqual(0, f(0), Tol);
        Assert.AreEqual(0.25, f(0.25), Tol); // floor(1.25)/4 = 1/4
        Assert.AreEqual(1, f(1), Tol);
    }

    [TestMethod]
    public void Steps_JumpBoth_OffsetsBothEnds()
    {
        var f = BmotionEaseFunctions.Steps(4, BmotionStepJump.Both);
        Assert.AreEqual(0.2, f(0), Tol);   // 1/(4+1)
        Assert.AreEqual(1, f(1), Tol);
    }

    [TestMethod]
    public void Steps_ConfiguredViaTween_OverridesEase()
    {
        var config = Bm.Tween(steps: 3, stepJump: BmotionStepJump.End).ToConfig();
        Assert.AreEqual(3, config.StepCount);
        var f = BmotionEaseFunctions.Get(config);
        Assert.AreEqual(0, f(0), Tol);
        Assert.AreEqual(1.0 / 3, f(0.4), Tol);
    }

    // ── CSS / compositor representation ───────────────────────────────────────

    [TestMethod]
    public void ToCssString_Steps_EmitsCssSteps()
    {
        Assert.AreEqual("steps(3,jump-end)", BmotionEaseFunctions.ToCssString(Bm.Tween(steps: 3).ToConfig()));
        Assert.AreEqual("steps(3,jump-start)", BmotionEaseFunctions.ToCssString(Bm.Tween(steps: 3, stepJump: BmotionStepJump.Start).ToConfig()));
    }

    [TestMethod]
    public void ToCssString_SingleStepJumpNone_NormalizesToJumpEnd()
    {
        // steps(1,jump-none) is invalid CSS; a single step is identical to jump-end at runtime.
        Assert.AreEqual("steps(1,jump-end)",
            BmotionEaseFunctions.ToCssString(Bm.Tween(steps: 1, stepJump: BmotionStepJump.None).ToConfig()));
        // >= 2 steps keep jump-none (valid CSS).
        Assert.AreEqual("steps(2,jump-none)",
            BmotionEaseFunctions.ToCssString(Bm.Tween(steps: 2, stepJump: BmotionStepJump.None).ToConfig()));
    }

    [TestMethod]
    public void ToCssString_PowerCurves_EmitCubicBezier()
    {
        Assert.AreEqual("cubic-bezier(0.37,0,0.63,1)",
            BmotionEaseFunctions.ToCssString(Bm.Tween(ease: BmotionEase.SineInOut).ToConfig()));
    }

    [TestMethod]
    public void HasFaithfulCssEasing_TrueOnlyForExactCssCurves()
    {
        // Faithful: keyword/back presets, explicit bezier, and steps (ToCssString reproduces them exactly).
        Assert.IsTrue(BmotionEaseFunctions.HasFaithfulCssEasing(Bm.Tween(ease: BmotionEase.Linear).ToConfig()));
        Assert.IsTrue(BmotionEaseFunctions.HasFaithfulCssEasing(Bm.Tween(ease: BmotionEase.InOut).ToConfig()));
        Assert.IsTrue(BmotionEaseFunctions.HasFaithfulCssEasing(Bm.Tween(ease: BmotionEase.BackOut).ToConfig()));
        Assert.IsTrue(BmotionEaseFunctions.HasFaithfulCssEasing(Bm.Tween(bezier: [0.1, 0.2, 0.3, 0.4]).ToConfig()));
        Assert.IsTrue(BmotionEaseFunctions.HasFaithfulCssEasing(Bm.Tween(steps: 4).ToConfig()));

        // NOT faithful: these serialize as APPROXIMATE cubic-beziers while the runtime uses exact
        // math, so offloading their CSS curve would drift from the rAF result.
        Assert.IsFalse(BmotionEaseFunctions.HasFaithfulCssEasing(Bm.Tween(ease: BmotionEase.SineInOut).ToConfig()));
        Assert.IsFalse(BmotionEaseFunctions.HasFaithfulCssEasing(Bm.Tween(ease: BmotionEase.QuartOut).ToConfig()));
        Assert.IsFalse(BmotionEaseFunctions.HasFaithfulCssEasing(Bm.Tween(ease: BmotionEase.ExpoIn).ToConfig()));
        Assert.IsFalse(BmotionEaseFunctions.HasFaithfulCssEasing(Bm.Tween(ease: BmotionEase.CircInOut).ToConfig()));
        Assert.IsFalse(BmotionEaseFunctions.HasFaithfulCssEasing(Bm.Tween(ease: BmotionEase.Anticipate).ToConfig()));
        Assert.IsFalse(BmotionEaseFunctions.HasFaithfulCssEasing(Bm.Tween(ease: BmotionEase.ElasticOut).ToConfig()));
        Assert.IsFalse(BmotionEaseFunctions.HasFaithfulCssEasing(Bm.Tween(ease: BmotionEase.BounceInOut).ToConfig()));
    }
}
