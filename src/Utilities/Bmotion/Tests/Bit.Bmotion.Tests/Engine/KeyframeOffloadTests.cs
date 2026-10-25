using System.Globalization;
using System.Text.RegularExpressions;
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

    // A keyframe sequence plays in the browser exactly as the frame loop would play it - recorded frame by frame -
    // so its easing applies to every segment (motion.dev semantics), not once across the whole sequence.

    private static Dictionary<string, object>[] Recorded(FakeBmotionInterop interop)
        => (Dictionary<string, object>[])interop.Calls.Last(c => c.Method == "playWaapiAnimation").Args[2]!;

    [TestMethod]
    public async Task KeyframeTransform_PlaysInTheBrowser_EasedPerSegment()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el", null);

        await engine.AnimateToAsync("el",
            new Dictionary<string, object?> { ["scale"] = new double[] { 1, 1.4, 0.8, 1 } },
            Bm.Tween(0.6).ToConfig());

        var kf = Recorded(interop);
        // A third of the way in is the end of the first of three segments: exactly its keyframe.
        Assert.AreEqual(1.4, WebAnimationKeyframes.At(kf, 1.0 / 3, WebAnimationKeyframes.Scale), 2e-3);
        // Half-way through that segment it is eased out within it, past the linear mid-point.
        double mid = WebAnimationKeyframes.At(kf, 1.0 / 6, WebAnimationKeyframes.Scale);
        Assert.IsTrue(mid > 1.2 + 0.05, $"the segment eases on its own: {mid}");
        Assert.AreEqual(1.0, WebAnimationKeyframes.Scale(kf[^1]), 1e-9);
    }

    [TestMethod]
    public async Task ScalarBesideAKeyframeSequence_KeepsItsOwnTimeline()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el", new Dictionary<string, object?> { ["opacity"] = 1.0 });

        // opacity is a scalar (eased once over the whole tween); y is a 3-frame sequence (eased per segment).
        await engine.AnimateToAsync("el",
            new Dictionary<string, object?> { ["opacity"] = 0.5, ["y"] = new double[] { 0, -20, 0 } },
            Bm.Tween(0.5, ease: BmEase.Linear).ToConfig());

        var kf = Recorded(interop);
        Assert.AreEqual(0.75, WebAnimationKeyframes.At(kf, 0.5, k => WebAnimationKeyframes.Number(k, "opacity")), 2e-3,
            "linear over the whole tween");
        Assert.AreEqual(-20, WebAnimationKeyframes.At(kf, 0.5, WebAnimationKeyframes.TranslateY), 0.1,
            "the sequence is at its middle keyframe");
    }

    [TestMethod]
    public async Task MixedKeyframeLengths_PlayInTheBrowser_EachOnItsOwnTimeline()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el", null);

        // x has 2 frames, scale has 3 - once a limit of the compositor offload, now just two recorded tracks.
        await engine.AnimateToAsync("el",
            new Dictionary<string, object?>
            {
                ["x"] = new double[] { 0, 100 },
                ["scale"] = new double[] { 1, 2, 1 },
            },
            Bm.Tween(0.5).ToConfig());

        var kf = Recorded(interop);
        Assert.AreEqual("translate(100px,0px)", kf[^1]["transform"]);
        Assert.AreEqual(2, WebAnimationKeyframes.At(kf, 0.5, WebAnimationKeyframes.Scale), 2e-3, "scale peaks half-way, at its middle keyframe");
    }

    [TestMethod]
    public async Task InterruptingKeyframeOffload_RealizesMidKeyframeValue()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el", null);

        // A pulse [1, 2, 1] that is exactly halfway through on the wall clock the mirror samples:
        // a very long duration started half of it ago, so the scheduling jitter between here and
        // the interrupt is a negligible fraction of the timeline. Halfway is the middle keyframe -
        // a value a two-endpoint mirror (from 1 to 1) could never produce.
        const double durationMs = 1_000_000;
        engine.GetState("el")!.AddWaapiPlan(new BmotionElementAnimationState.WaapiPlan
        {
            Token = 4242,
            StartMs = Environment.TickCount64 - (long)(durationMs / 2),
            DelayMs = 0,
            DurationMs = durationMs,
            Progress = [0, 1], // linear
            Values = new() { ["scale"] = (1, 1) },
            Frames = new() { ["scale"] = [1, 2, 1] },
            Iterations = 0,
            Mirror = false,
        });

        // Interrupting the same property must fold the plan back into state through the keyframe
        // mirror, and cancel the browser animation with commit so nothing flashes meanwhile.
        await engine.AnimateToAsync("el",
            new Dictionary<string, object?> { ["scale"] = 0.5 },
            Bm.Tween(0.3).ToConfig());

        var cancel = interop.ArgsOf("cancelWaapiAnimation").Single();
        Assert.AreEqual(4242, cancel[1]);
        Assert.AreEqual(true, cancel[2]);

        // The new offload starts from the realized middle keyframe.
        var keyframes = OffloadedKeyframes(interop)!;
        var from = (string)((Dictionary<string, object>)keyframes[0])["transform"];
        var scale = double.Parse(Regex.Match(from, @"scale\(([^)]+)\)").Groups[1].Value, CultureInfo.InvariantCulture);
        Assert.AreEqual(2.0, scale, 1e-3, $"the interrupted pulse must resume from its peak: {from}");
        Assert.AreEqual(2.0, engine.GetDiagnostics().Single(d => d.Id == "el").Transforms["scale"], 1e-3);
    }
}
