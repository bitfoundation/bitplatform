using Bit.Bmotion.Tests.TestInfra;

namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// Filters and shadows animate to and from <c>none</c> - the value the page reports for an element without one - by
/// way of the other end's neutral values, as CSS interpolates them, and land on the value that was asked for.
/// </summary>
[TestClass]
public class NeutralNoneTests
{
    private static Dictionary<string, object?> Values(params (string Key, object Value)[] values)
        => values.ToDictionary(v => v.Key, v => (object?)v.Value);

    private static Dictionary<string, object>[] Keyframes(FakeBmotionInterop interop)
        => (Dictionary<string, object>[])interop.ArgsOf("playWaapiAnimation").Last()[2]!;

    private static BmotionElementDiagnostics State(BmotionAnimationEngine engine)
        => engine.GetDiagnostics().Single(d => d.Id == "el");

    [TestMethod]
    [DataRow("filter", "none", "blur(8px) brightness(1.5)", "blur(0px) brightness(1)", "blur(8px) brightness(1.5)")]
    [DataRow("filter", "drop-shadow(2px 4px 6px black)", "none", "drop-shadow(2px 4px 6px black)", "drop-shadow(0px 0px 0px rgba(0,0,0,0))")]
    [DataRow("backdropFilter", "blur(4px) saturate(180%)", "none", "blur(4px) saturate(180%)", "blur(0px) saturate(100%)")]
    [DataRow("boxShadow", "none", "0 4px 12px rgba(0,0,0,.3)", "0px 0px 0px 0px rgba(0,0,0,0)", "0px 4px 12px 0px rgba(0,0,0,.3)")]
    [DataRow("boxShadow", "rgba(0, 0, 0, 0.3) 0px 4px 12px 0px", "0 8px 24px rgba(0,0,0,.4)", "0px 4px 12px 0px rgba(0, 0, 0, 0.3)", "0px 8px 24px 0px rgba(0,0,0,.4)")]
    [DataRow("boxShadow", "0 1px 2px red", "0 1px 2px red, inset 0 0 0 1px blue", "0px 1px 2px 0px red, inset 0px 0px 0px 0px rgba(0,0,0,0)", "0px 1px 2px 0px red, inset 0px 0px 0px 1px blue")]
    [DataRow("boxShadow", "inset 0 1px 2px red", "0 1px 2px red", "inset 0 1px 2px red", "0 1px 2px red")] // inset never interpolates with outset
    [DataRow("textShadow", "1px 1px red", "none", "1px 1px 0px red", "0px 0px 0px rgba(0,0,0,0)")]
    [DataRow("clipPath", "none", "inset(10px)", "none", "inset(10px)")] // no clip isn't inset(0): left alone
    [DataRow("filter", "none", "none", "none", "none")]
    public void NoneEnds_AreTheOtherEndsNeutralValues(string key, string from, string to, string expectedFrom, string expectedTo)
    {
        var (f, t) = BmotionStringMixer.MixEnds(key, from, to);
        Assert.AreEqual(expectedFrom, f);
        Assert.AreEqual(expectedTo, t);
    }

    [TestMethod]
    public async Task FromNone_PlaysInTheBrowser_FromNeutral()
    {
        foreach (var inProcess in new[] { true, false })
        {
            var interop = new FakeBmotionInterop { IsInProcess = inProcess, SupportsLinearEasing = true };
            await using var engine = new BmotionAnimationEngine(interop);
            engine.RegisterElement("el", Values(("filter", "none")));

            await engine.AnimateToAsync("el", Values(("filter", "blur(8px)")), Bm.Tween(0.3).ToConfig());

            var kf = Keyframes(interop);
            Assert.AreEqual("blur(8px)", kf[^1]["filter"]);
            if (inProcess) Assert.AreEqual("blur(0px)", kf[0]["filter"], "it grows out of no blur");
            interop.FinishWaapi();
            Assert.AreEqual("blur(8px)", State(engine).StringValues["filter"]);
        }
    }

    [TestMethod]
    public async Task ToNone_EndsOnNone()
    {
        var interop = new FakeBmotionInterop { IsInProcess = true, SupportsLinearEasing = true };
        await using var engine = new BmotionAnimationEngine(interop);
        engine.RegisterElement("el", Values(("boxShadow", "0 4px 12px rgba(0,0,0,0.3)")));

        await engine.AnimateToAsync("el", Values(("boxShadow", "none")), Bm.Tween(0.3).ToConfig());

        var kf = Keyframes(interop);
        Assert.AreEqual(2, kf.Length, "a timeline, not a one-step snap");
        Assert.AreEqual("0px 0px 0px 0px rgba(0,0,0,0)", kf[^1]["boxShadow"]);
        interop.FinishWaapi();
        var frame = engine.ComputeFrame(0);
        Assert.AreEqual("none", frame!["el"]["boxShadow"], "the authored none is written once it is over");
    }

    [TestMethod]
    public async Task OnTheFrameLoop_FromNone_PassesThroughIntermediateValues()
    {
        var interop = new FakeBmotionInterop { IsInProcess = true, WebAnimationsSupported = false };
        await using var engine = new BmotionAnimationEngine(interop);
        engine.RegisterElement("el", Values(("filter", "none")));

        await engine.AnimateToAsync("el", Values(("filter", "blur(8px)")), Bm.Tween(0.2, BmEase.Linear).ToConfig());

        var written = new List<string>();
        for (int i = 0; i <= 14; i++)
            if (engine.ComputeFrame(i * 16.0)?.GetValueOrDefault("el")?.TryGetValue("filter", out var v) == true) written.Add(v);
        Assert.IsTrue(written.Any(w => w is not "none" and not "blur(0px)" and not "blur(8px)"), string.Join(" | ", written));
        Assert.AreEqual("blur(8px)", written[^1]);
    }

    [TestMethod]
    public async Task Keyframes_ThroughNone_LandOnEachAuthoredFrame()
    {
        var interop = new FakeBmotionInterop { IsInProcess = true, WebAnimationsSupported = false };
        await using var engine = new BmotionAnimationEngine(interop);
        engine.RegisterElement("el", Values(("filter", "none")));

        await engine.AnimateToAsync("el", Values(("filter", new[] { "none", "blur(6px)", "none" })), Bm.Tween(0.2, BmEase.Linear).ToConfig());

        var written = new List<string>();
        for (int i = 0; i <= 16; i++)
            if (engine.ComputeFrame(i * 16.0)?.GetValueOrDefault("el")?.TryGetValue("filter", out var v) == true) written.Add(v);
        Assert.IsTrue(written.Any(w => w.StartsWith("blur(") && w != "blur(0px)"), string.Join(" | ", written));
        Assert.AreEqual("none", written[^1]);
    }
}
