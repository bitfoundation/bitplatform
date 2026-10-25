using Bit.Bmotion.Tests.TestInfra;

namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// <c>width</c>/<c>height</c> to and from <c>"auto"</c>: the natural size is measured and played to, the element lands
/// on <c>"auto"</c> itself (so it follows its content again), an animation from <c>"auto"</c> starts at the size on
/// screen, and an animation that is interrupted or has nothing to measure doesn't leave <c>"auto"</c> behind.
/// </summary>
[TestClass]
public class AutoSizeTests
{
    private static (BmotionAnimationEngine Engine, FakeBmotionInterop Interop) NewEngine(bool inProcess = true)
    {
        var interop = new FakeBmotionInterop
        {
            IsInProcess = inProcess,
            SupportsLinearEasing = true,
            AutoStyle = (_, key) => key == "height" ? "120px" : null,
            ComputedStyle = (_, key) => key == "height" ? "120px" : null,
        };
        return (new BmotionAnimationEngine(interop), interop);
    }

    private static Dictionary<string, object?> Values(params (string Key, object Value)[] values)
        => values.ToDictionary(v => v.Key, v => (object?)v.Value);

    private static Dictionary<string, object>[] Keyframes(FakeBmotionInterop interop)
        => (Dictionary<string, object>[])interop.ArgsOf("playWaapiAnimation").Last()[2]!;

    private static BmotionElementDiagnostics State(BmotionAnimationEngine engine)
        => engine.GetDiagnostics().Single(d => d.Id == "el");

    [TestMethod]
    public async Task ToAuto_PlaysToTheNaturalSize_AndLandsOnAuto()
    {
        foreach (var inProcess in new[] { true, false })
        {
            var (engine, interop) = NewEngine(inProcess);
            await using var _ = engine;
            engine.RegisterElement("el", Values(("height", "0px")));

            await engine.AnimateToAsync("el", Values(("height", "auto")), Bm.Tween(0.3).ToConfig());

            CollectionAssert.AreEqual(new[] { "height" }, (string[])interop.ArgsOf("readStyles").Last()[3]!, "the natural size is measured");
            var kf = Keyframes(interop);
            Assert.AreEqual("120px", kf[^1]["height"], "and played to");
            interop.FinishWaapi();
            Assert.AreEqual("auto", await WrittenHeightAsync(engine, interop), $"then it is auto itself ({(inProcess ? "WebAssembly" : "Server")})");
            Assert.AreEqual("auto", State(engine).StringValues["height"]);
        }
    }

    [TestMethod]
    public async Task ToAuto_OnTheFrameLoop_LandsOnAuto()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        interop.WebAnimationsSupported = false;
        engine.RegisterElement("el", Values(("height", "0px")));

        await engine.AnimateToAsync("el", Values(("height", "auto")), Bm.Tween(0.1).ToConfig());

        var written = new List<string>();
        for (int i = 0; i <= 12; i++)
            if (engine.ComputeFrame(i * 16.0)?.GetValueOrDefault("el")?.TryGetValue("height", out var v) == true) written.Add(v);
        Assert.IsTrue(written.Contains("120px") || written.Any(w => w.EndsWith("px")), "it grows in pixels");
        Assert.AreEqual("auto", written[^1], "and lands on auto");
    }

    [TestMethod]
    public async Task FromAuto_StartsAtTheSizeOnScreen()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el", Values(("height", "auto")));

        await engine.AnimateToAsync("el", Values(("height", "0px")), Bm.Tween(0.3).ToConfig());

        CollectionAssert.Contains((string[])interop.ArgsOf("readStyles").Last()[1]!, "height", "the size on screen is read");
        var kf = Keyframes(interop);
        Assert.AreEqual("120px", kf[0]["height"]);
        Assert.AreEqual("0px", kf[^1]["height"]);
        interop.FinishWaapi();
        Assert.AreEqual("0px", State(engine).StringValues["height"], "a size that isn't auto stays what it was played to");
    }

    [TestMethod]
    public async Task FromAuto_OnServer_TheBrowserIsGivenTheMeasuredStart()
    {
        // Without a frame loop the browser starts from what the page shows (the implicit first keyframe) - but the
        // page shows "auto", which doesn't interpolate: the height alone is given its start, the opacity isn't.
        var (engine, interop) = NewEngine(inProcess: false);
        await using var _ = engine;
        engine.RegisterElement("el", Values(("height", "auto"), ("opacity", 1.0)));

        await engine.AnimateToAsync("el", Values(("height", "0px"), ("opacity", 0.5)), Bm.Tween(0.3).ToConfig());

        var kf = Keyframes(interop);
        Assert.AreEqual(2, kf.Length);
        Assert.AreEqual("120px", kf[0]["height"]);
        Assert.IsFalse(kf[0].ContainsKey("opacity"), "the rest still starts from what is on screen");
        Assert.AreEqual("0px", kf[1]["height"]);

        // Once the page has been given pixels, the next animation starts from what it shows again.
        interop.FinishWaapi();
        await engine.AnimateToAsync("el", Values(("height", "50px")), Bm.Tween(0.3).ToConfig());
        Assert.AreEqual(1, Keyframes(interop).Length);
    }

    [TestMethod]
    public async Task ThereAndBack_MeasuresEachTime()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el", Values(("height", "0px")));

        await engine.AnimateToAsync("el", Values(("height", "auto")), Bm.Tween(0.2).ToConfig());
        interop.FinishWaapi();
        engine.ComputeFrame(0);
        // The content grew while it was auto.
        interop.ComputedStyle = (_, key) => key == "height" ? "180px" : null;

        await engine.AnimateToAsync("el", Values(("height", "0px")), Bm.Tween(0.2).ToConfig());
        Assert.AreEqual("180px", Keyframes(interop)[0]["height"], "it collapses from the size it grew to");
    }

    [TestMethod]
    public async Task AnInterruptedAnimationToAuto_DoesNotLandOnAuto()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el", Values(("height", "0px")));

        await engine.AnimateToAsync("el", Values(("height", "auto")), Bm.Tween(0.3).ToConfig());
        await engine.AnimateToAsync("el", Values(("height", "40px")), Bm.Tween(0.3).ToConfig());
        interop.FinishWaapi();
        var frame = engine.ComputeFrame(0);

        Assert.IsFalse(frame?.GetValueOrDefault("el")?.GetValueOrDefault("height") == "auto");
        Assert.AreEqual("40px", State(engine).StringValues["height"]);
    }

    [TestMethod]
    public async Task AnUnrelatedAnimation_DoesNotStopTheLanding()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el", Values(("height", "0px")));

        await engine.AnimateToAsync("el", Values(("height", "auto")), Bm.Tween(0.3).ToConfig());
        await engine.AnimateToAsync("el", Values(("opacity", 0.5)), Bm.Tween(0.3).ToConfig());
        interop.FinishWaapi();
        engine.ComputeFrame(0);

        Assert.AreEqual("auto", State(engine).StringValues["height"]);
    }

    [TestMethod]
    public async Task AnInstantChangeToAuto_IsAuto()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el", Values(("height", "0px")));

        await engine.AnimateToAsync("el", Values(("height", "auto")), Bm.Tween(0).ToConfig());
        var frame = engine.ComputeFrame(0);

        Assert.AreEqual("auto", frame!["el"]["height"]);
        Assert.AreEqual(0, interop.CountOf("playWaapiAnimation"));
    }

    [TestMethod]
    public async Task KeyframesThroughAuto_AreMeasured()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el", Values(("height", "0px")));

        await engine.AnimateToAsync("el", Values(("height", new[] { "0px", "auto", "60px" })), Bm.Tween(0.3).ToConfig());

        var kf = Keyframes(interop);
        Assert.IsTrue(kf.Any(k => (string)k["height"] == "120px"), "the auto frame is the natural size");
        interop.FinishWaapi();
        engine.ComputeFrame(0);
        Assert.AreEqual("60px", State(engine).StringValues["height"], "it ends where its last frame says");
    }

    [TestMethod]
    public async Task NothingToMeasure_SnapsToAuto_AsBefore()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        interop.AutoStyle = (_, _) => "auto"; // display: none - no layout to measure
        engine.RegisterElement("el", Values(("height", "0px")));

        await engine.AnimateToAsync("el", Values(("height", "auto")), Bm.Tween(0.3).ToConfig());

        Assert.AreEqual("auto", (string)Keyframes(interop)[^1]["height"], "the browser shows the snap");
        interop.FinishWaapi();
        Assert.AreEqual("auto", State(engine).StringValues["height"]);
    }

    // What the engine last wrote for the height: on a frame (WebAssembly) or flushed to the page (Server).
    private static async Task<string?> WrittenHeightAsync(BmotionAnimationEngine engine, FakeBmotionInterop interop)
    {
        for (int i = 0; i < 50; i++)
        {
            if (engine.ComputeFrame(i * 16.0)?.GetValueOrDefault("el")?.TryGetValue("height", out var v) == true) return v;
            var flushed = interop.ArgsOf("applyStyles").Select(a => (Dictionary<string, string>)a[1]!).LastOrDefault(s => s.ContainsKey("height"));
            if (flushed is not null) return flushed["height"];
            await Task.Delay(5);
        }
        return null;
    }
}
