using Bit.Bmotion.Tests.TestInfra;

namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// A length animated between two units (<c>"100px"</c> → <c>"50%"</c>, <c>"2rem"</c> → <c>"40vw"</c>) interpolates
/// the way CSS does - through <c>calc()</c> of both ends - instead of snapping to its target: on the frame loop, and
/// in the browser, which is given the two ends and interpolates them so itself.
/// </summary>
[TestClass]
public class MixedUnitLengthTests
{
    private static Dictionary<string, object?> Values(params (string Key, object Value)[] values)
        => values.ToDictionary(v => v.Key, v => (object?)v.Value);

    [TestMethod]
    public void TheMix_IsCalcOfBothEnds_WeightedByProgress()
    {
        var mix = BmotionElementAnimationState.MixedLengthMix("100px", "50%")!;

        Assert.AreEqual("100px", mix(0), "the ends are the authored strings");
        Assert.AreEqual("50%", mix(1));
        Assert.AreEqual("calc(50px + 25%)", mix(0.5));
        Assert.AreEqual("calc(75px + 12.5%)", mix(0.25));
        Assert.AreEqual("calc(-10px + 55%)", mix(1.1), "a spring's overshoot carries on past the end");
    }

    [TestMethod]
    public void TheMix_TakesAnyLengthUnit_AndABareZero()
    {
        Assert.AreEqual("calc(1rem + 20vw)", BmotionElementAnimationState.MixedLengthMix("2rem", "40vw")!(0.5));
        Assert.AreEqual("calc(0px + 25%)", BmotionElementAnimationState.MixedLengthMix("0", "50%")!(0.5));
        Assert.AreEqual("calc(6pt + 1.5em)", BmotionElementAnimationState.MixedLengthMix(" 12pt ", "3em")!(0.5));
    }

    [TestMethod]
    public void TheMix_IsOnlyForLengthsInDifferentUnits()
    {
        Assert.IsNull(BmotionElementAnimationState.MixedLengthMix("100px", "200px"), "one unit: plain numbers");
        Assert.IsNull(BmotionElementAnimationState.MixedLengthMix("1.5", "20px"), "a bare number other than 0 is no length");
        Assert.IsNull(BmotionElementAnimationState.MixedLengthMix("90deg", "50%"), "an angle is no length");
        Assert.IsNull(BmotionElementAnimationState.MixedLengthMix("auto", "50%"));
        Assert.IsNull(BmotionElementAnimationState.MixedLengthMix("calc(10px + 5%)", "50%"));
    }

    [TestMethod]
    public async Task OnTheFrameLoop_ALengthBetweenUnits_GoesThroughCalc_AndLandsOnTheTarget()
    {
        var interop = new FakeBmotionInterop { WebAnimationsSupported = false };
        await using var engine = new BmotionAnimationEngine(interop);
        engine.RegisterElement("el", Values(("width", "100px")));

        await engine.AnimateToAsync("el", Values(("width", "50%")), Bm.Tween(0.2, BmEase.Linear).ToConfig());

        var written = new List<string>();
        for (int i = 0; i <= 16; i++)
            if (engine.ComputeFrame(i * 16.0)?.GetValueOrDefault("el")?.TryGetValue("width", out var v) == true) written.Add(v);
        Assert.IsTrue(written.Any(w => w.StartsWith("calc(", StringComparison.Ordinal) && w.Contains("px + ") && w.EndsWith("%)")),
            $"it plays between the two: {string.Join(" | ", written)}");
        Assert.AreEqual("50%", written[^1], "and lands on the authored target");
    }

    [TestMethod]
    public async Task InTheBrowser_ALengthBetweenUnits_IsGivenBothEnds()
    {
        foreach (var inProcess in new[] { true, false })
        {
            var interop = new FakeBmotionInterop { IsInProcess = inProcess, SupportsLinearEasing = true };
            await using var engine = new BmotionAnimationEngine(interop);
            engine.RegisterElement("el", Values(("width", "100px")));

            await engine.AnimateToAsync("el", Values(("width", "50%")), Bm.Tween(0.3).ToConfig());

            var keyframes = (Dictionary<string, object>[])interop.ArgsOf("playWaapiAnimation").Last()[2]!;
            var mode = inProcess ? "WebAssembly" : "Server";
            // On Blazor Server the browser starts from what the page shows (an implicit first keyframe).
            Assert.AreEqual(inProcess ? 2 : 1, keyframes.Length, $"the browser interpolates between the ends itself ({mode})");
            if (inProcess) Assert.AreEqual("100px", keyframes[0]["width"]);
            Assert.AreEqual("50%", keyframes[^1]["width"], mode);
            interop.FinishWaapi();
            Assert.AreEqual("50%", engine.GetDiagnostics().Single(d => d.Id == "el").StringValues["width"]);
        }
    }
}
