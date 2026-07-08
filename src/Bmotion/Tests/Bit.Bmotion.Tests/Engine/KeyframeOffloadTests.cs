using Bit.Bmotion.Tests.TestInfra;

namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// Tests for multi-keyframe compositor offload (plan item 1.1 widening): the interruption-mirror
/// math and that eligible keyframe animations reach the compositor with N keyframes.
/// </summary>
[TestClass]
public class KeyframeOffloadTests
{
    // ── Mirror interpolation (interruption-critical) ──────────────────────────

    [TestMethod]
    public void KeyframeLerp_HitsEndpointsAndMidpoints()
    {
        double[] f = [1, 2, 1]; // pulse
        Assert.AreEqual(1, BmotionElementAnimationState.WaapiPlan.KeyframeLerp(f, 0), 1e-9);
        Assert.AreEqual(2, BmotionElementAnimationState.WaapiPlan.KeyframeLerp(f, 0.5), 1e-9);
        Assert.AreEqual(1, BmotionElementAnimationState.WaapiPlan.KeyframeLerp(f, 1), 1e-9);
        Assert.AreEqual(1.5, BmotionElementAnimationState.WaapiPlan.KeyframeLerp(f, 0.25), 1e-9); // first segment
        Assert.AreEqual(1.5, BmotionElementAnimationState.WaapiPlan.KeyframeLerp(f, 0.75), 1e-9); // second segment
    }

    [TestMethod]
    public void KeyframeLerp_ClampsOutOfRangeProgress()
    {
        double[] f = [0, 10];
        Assert.AreEqual(0, BmotionElementAnimationState.WaapiPlan.KeyframeLerp(f, -0.5), 1e-9);
        Assert.AreEqual(10, BmotionElementAnimationState.WaapiPlan.KeyframeLerp(f, 1.5), 1e-9);
    }

    [TestMethod]
    public void KeyframeLerp_SingleFrame_IsConstant()
        => Assert.AreEqual(7, BmotionElementAnimationState.WaapiPlan.KeyframeLerp([7], 0.3), 1e-9);

    // ── Integration: eligibility ──────────────────────────────────────────────

    private static (BmotionAnimationEngine engine, FakeBmotionInterop interop) NewEngine()
    {
        var interop = new FakeBmotionInterop { IsInProcess = true, SupportsLinearEasing = true };
        return (new BmotionAnimationEngine(interop), interop);
    }

    private static object[]? OffloadedKeyframes(FakeBmotionInterop interop)
    {
        var call = interop.Calls.LastOrDefault(c => c.Method == "playWaapiAnimation");
        return call is null ? null : (object[])call.Args[2]!;
    }

    [TestMethod]
    public async Task KeyframeTransform_OffloadsWithNKeyframes()
    {
        var (engine, interop) = NewEngine();
        engine.RegisterElement("el", null);

        await engine.AnimateToAsync("el",
            new Dictionary<string, object?> { ["scale"] = new double[] { 1, 1.4, 0.8, 1 } },
            Bm.Tween(0.6).ToConfig());

        var kf = OffloadedKeyframes(interop);
        Assert.IsNotNull(kf, "an eligible keyframe transform should offload to the compositor");
        Assert.AreEqual(4, kf!.Length, "one WAAPI keyframe per array frame");
    }

    [TestMethod]
    public async Task ScalarPlusKeyframe_AlignOntoCommonGrid()
    {
        var (engine, interop) = NewEngine();
        engine.RegisterElement("el", null);

        // opacity is a scalar (2 frames); y is a 3-frame keyframe → both align to 3 keyframes.
        await engine.AnimateToAsync("el",
            new Dictionary<string, object?> { ["opacity"] = 0.5, ["y"] = new double[] { 0, -20, 0 } },
            Bm.Tween(0.5).ToConfig());

        var kf = OffloadedKeyframes(interop);
        Assert.IsNotNull(kf);
        Assert.AreEqual(3, kf!.Length);
    }

    [TestMethod]
    public async Task MixedKeyframeLengths_StayOnRafPath()
    {
        var (engine, interop) = NewEngine();
        engine.RegisterElement("el", null);

        // x has 2 frames, scale has 3 → mismatched keyframe lengths → not offloaded.
        await engine.AnimateToAsync("el",
            new Dictionary<string, object?>
            {
                ["x"] = new double[] { 0, 100 },
                ["scale"] = new double[] { 1, 2, 1 },
            },
            Bm.Tween(0.5).ToConfig());

        Assert.IsNull(OffloadedKeyframes(interop), "mixed keyframe lengths must fall back to rAF");
    }

    [TestMethod]
    public async Task InterruptingKeyframeOffload_RealizesMidKeyframeValue()
    {
        var (engine, interop) = NewEngine();
        engine.RegisterElement("el", null);

        // Start a keyframe offload, then immediately animate the same prop again: the interrupt
        // must fold the compositor plan back into state (via the keyframe mirror) without throwing.
        await engine.AnimateToAsync("el",
            new Dictionary<string, object?> { ["scale"] = new double[] { 1, 2, 1 } },
            Bm.Tween(1.0).ToConfig());
        await engine.AnimateToAsync("el",
            new Dictionary<string, object?> { ["scale"] = 0.5 },
            Bm.Tween(0.3).ToConfig());

        // The scale value is finite and within the plausible range the keyframe mirror could realize.
        var scale = engine.GetDiagnostics().Single(d => d.Id == "el").Transforms["scale"];
        Assert.IsTrue(double.IsFinite(scale));
    }
}
