using System.Globalization;
using System.Text.RegularExpressions;
using Bit.Bmotion.Tests.TestInfra;

namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// The browser plays whatever it can reproduce exactly (IMPROVEMENT_PLAN.md, AD-1): which shape an animation takes
/// (two-keyframe timeline or frame-by-frame recording), that a recording is the frame loop's own output, that values
/// the engine never set are read off the page first and handed back to the stylesheet afterwards, that a retargeted
/// spring carries its velocity, and that none of this depends on the runtime having a frame loop.
/// </summary>
[TestClass]
public partial class BrowserPlaybackTests
{
    private static (BmotionAnimationEngine Engine, FakeBmotionInterop Interop) NewEngine(bool inProcess = true)
    {
        var interop = new FakeBmotionInterop { IsInProcess = inProcess, SupportsLinearEasing = true };
        return (new BmotionAnimationEngine(interop), interop);
    }

    private static Dictionary<string, object?> Values(params (string Key, object Value)[] values)
        => values.ToDictionary(v => v.Key, v => (object?)v.Value);

    private static Dictionary<string, object>[] Keyframes(FakeBmotionInterop interop, int index = -1)
    {
        var plays = interop.ArgsOf("playWaapiAnimation");
        return (Dictionary<string, object>[])plays[index < 0 ? plays.Count + index : index][2]!;
    }

    private static Dictionary<string, object?> Timing(FakeBmotionInterop interop, int index = -1)
    {
        var plays = interop.ArgsOf("playWaapiAnimation");
        return (Dictionary<string, object?>)plays[index < 0 ? plays.Count + index : index][3]!;
    }

    private static BmotionElementDiagnostics State(BmotionAnimationEngine engine, string id = "el")
        => engine.GetDiagnostics().Single(d => d.Id == id);

    // ── Shapes ────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task OneTweenForEveryKey_PlaysAsATwoKeyframeTimeline()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el", Values(("backgroundColor", "#000000"), ("width", "100px"), ("filter", "blur(2px)")));

        await engine.AnimateToAsync("el",
            Values(("x", 50.0), ("opacity", 0.5), ("backgroundColor", "#ff0000"), ("width", "300px"), ("filter", "blur(0px)")),
            Bm.Tween(0.4).ToConfig());

        var kf = Keyframes(interop);
        Assert.AreEqual(2, kf.Length);
        Assert.AreEqual("rgba(0,0,0,1)", kf[0]["backgroundColor"], "colours are re-encoded for an sRGB mix");
        Assert.AreEqual("rgba(255,0,0,1)", kf[1]["backgroundColor"]);
        Assert.AreEqual("100px", kf[0]["width"]);
        Assert.AreEqual("300px", kf[1]["width"]);
        Assert.AreEqual(400.0, (double)Timing(interop)["duration"]!);
        Assert.AreNotEqual("linear", Timing(interop)["easing"], "the curve is the timing easing");
    }

    [TestMethod]
    public async Task ASpringOnAColour_PlaysItsCurveHeldWithinTheTwoColours()
    {
        // The frame loop holds a colour between its ends while a spring overshoots them; a browser handed the
        // spring's own curve would carry it past the target (blue → orange through "more orange than orange").
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el", Values(("backgroundColor", "#1276C6")));

        await engine.AnimateToAsync("el", Values(("backgroundColor", "#FD7F36")), Bm.Spring(stiffness: 300, damping: 10).ToConfig());

        Assert.AreEqual(2, Keyframes(interop).Length, "still the timeline shape");
        var easing = (string)Timing(interop)["easing"]!;
        StringAssert.StartsWith(easing, "linear(");
        var progress = Regex.Matches(easing, @"-?\d+(\.\d+)?").Select(m => double.Parse(m.Value, CultureInfo.InvariantCulture)).ToArray();
        Assert.IsTrue(progress.All(p => p is >= 0 and <= 1), $"the curve leaves 0..1: {easing}");
        Assert.AreEqual(1, progress.Max(), "and still reaches the target");
    }

    [TestMethod]
    public async Task ASpringOnAColourAndAMove_IsRecorded_TheMoveOvershootingAndTheColourHeld()
    {
        // One timing easing can't hold the colour and let the move overshoot, so the animation is recorded - which
        // plays exactly what the frame loop draws for each.
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el", Values(("backgroundColor", "rgb(100, 0, 0)")));

        await engine.AnimateToAsync("el", Values(("x", 100.0), ("backgroundColor", "rgb(200, 0, 0)")), Bm.Spring(stiffness: 300, damping: 10).ToConfig());

        var kf = Keyframes(interop);
        Assert.AreEqual("linear", Timing(interop)["easing"], "recorded");
        Assert.IsTrue(kf.Max(WebAnimationKeyframes.TranslateX) > 100, "the move overshoots, as the spring does");
        var reds = kf.Where(k => k.ContainsKey("backgroundColor"))
            .Select(k => int.Parse(Regex.Match((string)k["backgroundColor"], @"rgba?\((\d+)").Groups[1].Value, CultureInfo.InvariantCulture))
            .ToArray();
        Assert.IsTrue(reds.Length > 2 && reds.All(r => r is >= 100 and <= 200), $"the colour leaves its two ends: {string.Join(", ", reds)}");
    }

    [TestMethod]
    [DataRow(0.0, 0.0, DisplayName = "held on its first keyframe")]
    [DataRow(150.0, 15.0, DisplayName = "started late")]
    public async Task ARetarget_StartsWhereTheBrowserHasReallyGot_NotWhereTheEnginesClockSays(double browserPlayedMs, double expectedX)
    {
        // A task that runs long - an element's first animation after the page loads, say - holds the new Web Animation
        // on its first keyframe until the frame that shows it, while the engine's clock for it started when it built
        // it. Retargeted by its clock, the element would jump ahead to where it never got.
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el");
        interop.WaapiPlayhead = (_, _) => (browserPlayedMs, false);

        await engine.AnimateToAsync("el", Values(("x", 100.0)), Bm.Tween(1, BmEase.Linear).ToConfig());
        await Task.Delay(400);   // by the engine's clock: 40px along
        await engine.AnimateToAsync("el", Values(("x", -100.0)), Bm.Tween(1, BmEase.Linear).ToConfig());

        Assert.AreEqual(expectedX, WebAnimationKeyframes.TranslateX(Keyframes(interop)[0]), 0.5,
            "the retarget starts from what the element shows");
    }

    [TestMethod]
    public async Task TheBrowsersPlayhead_IsAskedFor_OnlyUntilItsStartIsFinal()
    {
        // Once the browser has fixed when an animation started, the engine's clock is corrected that once and runs on
        // its own from there: nothing that reads it later goes back to the browser.
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el");
        bool final = false;
        interop.WaapiPlayhead = (_, _) => (100, final);
        await engine.AnimateToAsync("el", Values(("x", 100.0)), Bm.Tween(1, BmEase.Linear).ToConfig());
        var state = engine.GetState("el")!;
        int reads = interop.WaapiPlayheadReads;

        state.TransformsAfter(0);
        state.TransformsAfter(0);
        Assert.AreEqual(reads + 2, interop.WaapiPlayheadReads, "asked while the start may still move");

        final = true;
        state.TransformsAfter(0);
        double x = state.TransformsAfter(0)["x"];
        Assert.AreEqual(reads + 3, interop.WaapiPlayheadReads, "and once more, when the browser says the start is final");
        Assert.AreEqual(10, x, 2, "the clock carries on from where the browser was");
    }

    [TestMethod]
    public async Task WhereTheTransformWillBe_AsksOnlyAboutTheAnimationsThatMoveIt()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el");
        interop.WaapiPlayhead = (_, _) => (0, false);
        await engine.AnimateToAsync("el", Values(("opacity", 0.5)), Bm.Tween(1).ToConfig());
        await engine.AnimateToAsync("el", Values(("x", 100.0)), Bm.Tween(1, BmEase.Linear).ToConfig());
        Assert.AreEqual(2, interop.ArgsOf("playWaapiAnimation").Count, "two animations in the browser");
        int reads = interop.WaapiPlayheadReads;

        engine.GetState("el")!.TransformsAfter(100);

        Assert.AreEqual(reads + 1, interop.WaapiPlayheadReads, "the opacity animation has nothing to say about it");
    }

    [TestMethod]
    public async Task ASeek_DoesNotAskWhereAnAnimationOfTheKeysItPutsBackHadGot()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el", Values(("opacity", 1.0)));
        interop.WaapiPlayhead = (_, _) => (500, false);
        await engine.AnimateToAsync("el", Values(("x", 100.0)), Bm.Tween(1, BmEase.Linear).ToConfig());
        await engine.AnimateToAsync("el", Values(("opacity", 0.0)), Bm.Tween(1, BmEase.Linear).ToConfig());
        int reads = interop.WaapiPlayheadReads;

        engine.ResetForSeek("el", Values(("x", 0.0)));

        Assert.AreEqual(reads + 1, interop.WaapiPlayheadReads, "only the opacity animation, which the seek leaves where it got to");
        Assert.AreEqual(0, engine.GetState("el")!.Transforms["x"], 1e-9);
        Assert.AreEqual(0.5, (double)engine.GetState("el")!.CurrentValue("opacity")!, 0.01);
    }

    [TestMethod]
    public async Task ARetarget_WithoutTheBrowsersPlayhead_FallsBackToTheEnginesClock()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el");

        await engine.AnimateToAsync("el", Values(("x", 100.0)), Bm.Tween(1, BmEase.Linear).ToConfig());
        await Task.Delay(400);
        await engine.AnimateToAsync("el", Values(("x", -100.0)), Bm.Tween(1, BmEase.Linear).ToConfig());

        Assert.IsTrue(WebAnimationKeyframes.TranslateX(Keyframes(interop)[0]) > 30, "the plan's own clock, as before");
    }

    [TestMethod]
    public async Task PerPropertyTransitions_AreRecorded_FrameByFrame()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el");

        var transition = Bm.Tween(0.5).ToConfig();
        transition.Properties = new() { ["opacity"] = Bm.Tween(0.1).ToConfig() };
        await engine.AnimateToAsync("el", Values(("x", 100.0), ("opacity", 0.0)), transition);

        var kf = Keyframes(interop);
        Assert.AreEqual("linear", Timing(interop)["easing"]);
        Assert.AreEqual(500.0, (double)Timing(interop)["duration"]!, 1e-6);
        // 0.1 s into 0.5 s: the opacity's own transition is over while x is still on its way.
        Assert.AreEqual(0, WebAnimationKeyframes.At(kf, 0.2, k => WebAnimationKeyframes.Number(k, "opacity")), 2e-3);
        Assert.IsTrue(WebAnimationKeyframes.At(kf, 0.2, WebAnimationKeyframes.TranslateX) < 100);
    }

    [TestMethod]
    public async Task ARecording_IsExactlyWhatTheFrameLoopDraws()
    {
        // The same spring, recorded for the browser and ticked on the frame loop, frame for frame.
        var spring = Bm.Spring(stiffness: 170, damping: 12).ToConfig();
        spring.Velocity = 400; // a spring with initial velocity is always recorded

        var (recorded, interop) = NewEngine();
        await using var _ = recorded;
        recorded.RegisterElement("el");
        await recorded.AnimateToAsync("el", Values(("x", 200.0)), spring);
        var keyframes = Keyframes(interop);
        // Fewer keyframes than frames are sent - the ones linear interpolation can't reproduce - so the browser's
        // value at each frame is read the way it plays them.
        int frames = (int)Math.Round((double)Timing(interop)["duration"]! / (1000.0 / 60)) + 1;
        Assert.IsTrue(keyframes.Length < frames, "the recording is simplified");
        var browser = Enumerable.Range(0, frames)
            .Select(i => WebAnimationKeyframes.At(keyframes, (double)i / (frames - 1), WebAnimationKeyframes.TranslateX)).ToArray();

        var (looped, loopInterop) = NewEngine();
        await using var __ = looped;
        loopInterop.WebAnimationsSupported = false;
        looped.RegisterElement("el");
        await looped.AnimateToAsync("el", Values(("x", 200.0)), spring);
        var loop = new List<double>();
        for (int i = 0; i < browser.Length; i++)
        {
            looped.ComputeFrame(i * 1000.0 / 60);
            loop.Add(State(looped).Transforms["x"]);
        }

        Assert.AreEqual(loop.Count, browser.Length);
        // Within the tenth of a pixel the simplification allows.
        for (int i = 0; i < loop.Count; i++) Assert.AreEqual(loop[i], browser[i], 0.11, $"frame {i}");
    }

    [TestMethod]
    public async Task AMirroredRepeat_RecordsTheWayBack_WithTheSameCurve()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el");

        // ease-out out and ease-out back: the browser's alternate direction would play the way back eased in.
        await engine.AnimateToAsync("el", Values(("x", 100.0)), Bm.Tween(0.5, BmEase.Out, repeat: BmRepeat.Mirror()).ToConfig());

        var kf = Keyframes(interop);
        Assert.AreEqual(-1, (int)Timing(interop)["iterations"]!, "the out-and-back cycle loops forever");
        Assert.AreEqual("normal", Timing(interop)["direction"]);
        Assert.AreEqual(1000.0, (double)Timing(interop)["duration"]!, 1e-6, "one cycle is the way out and the way back");
        double X(double offset) => WebAnimationKeyframes.At(kf, offset, WebAnimationKeyframes.TranslateX);
        Assert.AreEqual(100, X(0.5), 0.11, "out at half-way");
        // The way back eases out too: it leaves the far end fast and settles slowly.
        Assert.IsTrue(X(0.5) - X(0.55) > X(0.95) - X(1.0), "the way back starts fast and settles slowly");
    }

    [TestMethod]
    public async Task AnEndlesslyReversingRepeat_StaysOnTheFrameLoop()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el");

        await engine.AnimateToAsync("el", Values(("x", 100.0)), Bm.Tween(0.5, repeat: BmRepeat.Reverse()).ToConfig());

        Assert.AreEqual(0, interop.CountOf("playWaapiAnimation"));
        Assert.IsTrue(State(engine).ActiveProperties.Contains("x"));
    }

    [TestMethod]
    public async Task SvgGeometry_StaysOnTheFrameLoop_AndTheRestStillPlaysInTheBrowser()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el", Values(("d", "M0 0 L10 10")));

        await engine.AnimateToAsync("el", Values(("d", "M0 0 L20 20"), ("opacity", 0.5)), Bm.Tween(0.4).ToConfig());

        var state = State(engine);
        CollectionAssert.AreEqual(new[] { "d" }, state.ActiveProperties.ToArray(), "an attribute isn't a Web Animation's");
        CollectionAssert.AreEqual(new[] { "opacity" }, state.BrowserProperties.ToArray());
    }

    [TestMethod]
    public async Task APerFrameCallback_KeepsTheAnimationOnTheFrameLoop()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el");
        engine.SetOnFrame("el", _ => { });

        await engine.AnimateToAsync("el", Values(("x", 100.0)), Bm.Tween(0.4).ToConfig());

        Assert.AreEqual(0, interop.CountOf("playWaapiAnimation"));
    }

    [TestMethod]
    public async Task OklabColours_PlayInOklab_OrAreRecordedWithout()
    {
        var oklab = Bm.Tween(0.4, colorSpace: BmColorSpace.Oklab).ToConfig();

        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el", Values(("color", "#0000ff")));
        await engine.AnimateToAsync("el", Values(("color", "#ffff00")), oklab);
        StringAssert.StartsWith((string)Keyframes(interop)[0]["color"], "oklab(", "the browser mixes oklab() colours in OKLab");
        Assert.AreEqual(2, Keyframes(interop).Length);

        var (old, oldInterop) = NewEngine();
        await using var __ = old;
        oldInterop.SupportsOklab = false;
        old.RegisterElement("el", Values(("color", "#0000ff")));
        await old.AnimateToAsync("el", Values(("color", "#ffff00")), oklab);
        Assert.IsTrue(Keyframes(oldInterop).Length > 2, "without oklab() the C# OKLab mix is recorded");
    }

    [TestMethod]
    public async Task AnAnimationTheBrowserRejects_ReplaysFromWhereItStarted()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        // The browser takes a round trip to say no; nothing was on screen meanwhile.
        var rejected = new TaskCompletionSource<bool>();
        interop.PlayWaapiHandler = (_, _, _, _) => rejected.Task;
        engine.RegisterElement("el", Values(("x", 0.0)));

        var clock = System.Diagnostics.Stopwatch.StartNew();
        await engine.AnimateToAsync("el", Values(("x", 100.0)), Bm.Tween(1, BmEase.Linear).ToConfig());
        await Task.Delay(20);
        // A rejection is only told apart from a cancellation within the engine's 50 ms never-started window; a
        // machine too loaded to answer inside it can't judge this either way.
        if (clock.ElapsedMilliseconds > 45) Assert.Inconclusive($"the rejection took {clock.ElapsedMilliseconds} ms to deliver");
        rejected.SetResult(false);
        engine.ComputeFrame(0);

        // Realized at its wall-clock age instead, the replay would start a few pixels in (20 ms of 1 s = 2px).
        Assert.AreEqual(0, State(engine).Transforms["x"], 0.5, "the replay starts where the rejected animation did");
    }

    [TestMethod]
    public async Task AColourTarget_EndsOnItsAuthoredValue()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el", Values(("color", "#000000")));

        await engine.AnimateToAsync("el", Values(("color", "oklch(0.7 0.25 140)")), Bm.Tween(0.2).ToConfig());
        interop.FinishWaapi();

        // The browser committed its sRGB rendition; the authored wide-gamut colour is written back over it.
        var frame = engine.ComputeFrame(0);
        Assert.AreEqual("oklch(0.7 0.25 140)", frame!["el"]["color"]);
    }

    // ── Blazor Server: the browser runs a network hop behind ──────────────────

    [TestMethod]
    public async Task Server_ATimelineStartsFromWhatTheBrowserShows()
    {
        var (engine, interop) = NewEngine(inProcess: false);
        await using var _ = engine;
        engine.RegisterElement("el", Values(("x", 10.0)));

        await engine.AnimateToAsync("el", Values(("x", 100.0)), Bm.Tween(0.4).ToConfig());
        Assert.AreEqual(1, Keyframes(interop).Length, "no first keyframe: the browser starts from the value on screen");

        // An authored first frame is the author's, not the engine's guess, and is kept.
        await engine.AnimateToAsync("el", Values(("x", new double[] { 0, 50 })), Bm.Tween(0.4).ToConfig());
        Assert.AreEqual(2, Keyframes(interop).Length);
    }

    [TestMethod]
    public async Task Server_ARecordingDropsItsFirstFrame_AndKeepsTheRestEvenlySpaced()
    {
        var (engine, interop) = NewEngine(inProcess: false);
        await using var _ = engine;
        engine.RegisterElement("el");

        var transition = Bm.Tween(0.5).ToConfig();
        transition.Properties = new() { ["opacity"] = Bm.Tween(0.1).ToConfig() };
        await engine.AnimateToAsync("el", Values(("x", 100.0), ("opacity", 0.0)), transition);

        var kf = Keyframes(interop);
        Assert.IsTrue(kf.All(k => k.ContainsKey("offset")), "every keyframe says where it falls");
        Assert.IsTrue((double)kf[0]["offset"] > 0, "the first frame is left to the browser");
        Assert.AreEqual(1.0, (double)kf[^1]["offset"], 1e-9);
    }

    [TestMethod]
    public async Task Server_GestureOverlays_PlayInTheBrowser()
    {
        var (engine, interop) = NewEngine(inProcess: false);
        await using var _ = engine;
        engine.RegisterElement("el");

        await engine.ActivateGestureLayerAsync("el", "hover", Values(("scale", 1.2)), Bm.Tween(0.3).ToConfig());
        Assert.AreEqual(1, interop.CountOf("playWaapiAnimation"), "the hover animates rather than snapping");

        await engine.DeactivateGestureLayerAsync("el", "hover");
        Assert.AreEqual(2, interop.CountOf("playWaapiAnimation"), "and so does letting go of it");
        Assert.IsTrue(interop.WasCalled("cancelWaapiAnimation"), "the hover was interrupted where it was");
    }

    // ── Velocity ──────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task ARetargetedSpring_InheritsTheFrameLoopsVelocity()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        interop.WebAnimationsSupported = false;
        engine.RegisterElement("el");
        var spring = Bm.Spring(stiffness: 60, damping: 12).ToConfig();

        await engine.AnimateToAsync("el", Values(("x", 300.0)), spring);
        for (int i = 0; i <= 9; i++) engine.ComputeFrame(i * 16.0);
        double atRetarget = State(engine).Transforms["x"];

        await engine.AnimateToAsync("el", Values(("x", 0.0)), spring);
        engine.ComputeFrame(160);
        engine.ComputeFrame(176);

        Assert.IsTrue(State(engine).Transforms["x"] > atRetarget, "still carried forwards by its momentum");
    }

    [TestMethod]
    public async Task ARetargetedSpring_InheritsTheBrowsersVelocity_AndIsRecorded()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el");
        var spring = Bm.Spring(stiffness: 60, damping: 12).ToConfig();

        // A browser animation of x mid-flight, moving right at 1 px/ms: a long linear plan half way.
        engine.GetState("el")!.AddWaapiPlan(new BmotionElementAnimationState.WaapiPlan
        {
            Token = 77,
            StartMs = Environment.TickCount64 - 500,
            DelayMs = 0,
            DurationMs = 1000,
            Progress = [0, 1],
            Values = new() { ["x"] = (0, 1000) },
            Iterations = 0,
            Mirror = false,
        });

        await engine.AnimateToAsync("el", Values(("x", 0.0)), spring);

        var kf = Keyframes(interop);
        Assert.IsTrue(kf.Length > 2, "a spring carrying velocity has a curve of its own: recorded");
        double frame = (1000.0 / 60) / (double)Timing(interop)["duration"]!;
        Assert.IsTrue(WebAnimationKeyframes.At(kf, frame, WebAnimationKeyframes.TranslateX) > WebAnimationKeyframes.TranslateX(kf[0]),
            "it keeps going the way it was going before it turns round");
    }

    [TestMethod]
    public async Task AnAuthoredVelocity_WinsOverTheInheritedOne()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        interop.WebAnimationsSupported = false;
        engine.RegisterElement("el");

        await engine.AnimateToAsync("el", Values(("x", 300.0)), Bm.Spring(stiffness: 60, damping: 12).ToConfig());
        for (int i = 0; i <= 9; i++) engine.ComputeFrame(i * 16.0);
        double atRetarget = State(engine).Transforms["x"];

        var backwards = Bm.Spring(stiffness: 60, damping: 12, velocity: -2000).ToConfig();
        await engine.AnimateToAsync("el", Values(("x", 0.0)), backwards);
        engine.ComputeFrame(160);
        engine.ComputeFrame(176);

        Assert.IsTrue(State(engine).Transforms["x"] < atRetarget);
    }

    // ── Values the engine never set ───────────────────────────────────────────

    [TestMethod]
    public async Task AnUnseenStartingValue_IsReadOffThePage()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        interop.ComputedStyle = (_, key) => key == "width" ? "100px" : null;
        engine.RegisterElement("el");

        await engine.AnimateToAsync("el", Values(("width", "300px")), Bm.Tween(0.4).ToConfig());

        var read = interop.ArgsOf("readStyles").Single();
        CollectionAssert.AreEqual(new[] { "width" }, (string[])read[1]!);
        Assert.AreEqual("100px", Keyframes(interop)[0]["width"], "animated from the stylesheet's width");

        // Known from then on: nothing is read again.
        await engine.AnimateToAsync("el", Values(("width", "200px")), Bm.Tween(0.4).ToConfig());
        Assert.AreEqual(1, interop.CountOf("readStyles"));
    }

    [TestMethod]
    public async Task AHoverOnAStylesheetColour_StartsFromIt_AndHandsItBack()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        string stylesheet = "rgb(0, 0, 255)";
        interop.ComputedStyle = (_, key) => key == "backgroundColor" ? stylesheet : null;
        engine.RegisterElement("el");

        await engine.ActivateGestureLayerAsync("el", "hover", Values(("backgroundColor", "rgb(255, 0, 0)")), Bm.Tween(0.3).ToConfig());
        Assert.AreEqual("rgba(0,0,255,1)", Keyframes(interop)[0]["backgroundColor"], "from the stylesheet blue, not transparent black");
        interop.FinishWaapi();

        // Meanwhile the stylesheet changed (a theme switch): the hover lets go to what it says now.
        stylesheet = "rgb(0, 128, 0)";
        await engine.DeactivateGestureLayerAsync("el", "hover");
        Assert.AreEqual("rgba(0,128,0,1)", Keyframes(interop)[1]["backgroundColor"]);
        CollectionAssert.AreEqual(new[] { "backgroundColor" }, (string[])interop.ArgsOf("readStyles").Last()[2]!);

        interop.FinishWaapi();
        // The hand-back follows the finish on its continuation: tick until it is flushed.
        string? written = null;
        for (int i = 0; i < 100 && written != ""; i++)
        {
            await Task.Delay(5);
            if (engine.ComputeFrame(i * 16.0)?.GetValueOrDefault("el")?.TryGetValue("backgroundColor", out var v) == true) written = v;
        }
        Assert.AreEqual("", written, "the inline declaration is removed once it's back");
        Assert.IsFalse(State(engine).StringValues.ContainsKey("backgroundColor"), "and read afresh next time");
    }

    [TestMethod]
    public async Task AValueTakenAgainBeforeItsHandBack_IsNotHandedBack()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        interop.ComputedStyle = (_, key) => key == "backgroundColor" ? "rgb(0, 0, 255)" : null;
        engine.RegisterElement("el");

        await engine.ActivateGestureLayerAsync("el", "hover", Values(("backgroundColor", "rgb(255, 0, 0)")), Bm.Tween(0.3).ToConfig());
        await engine.DeactivateGestureLayerAsync("el", "hover");
        // Hovered again while the way back plays: the way back is interrupted and never finishes.
        await engine.ActivateGestureLayerAsync("el", "hover", Values(("backgroundColor", "rgb(255, 0, 0)")), Bm.Tween(0.3).ToConfig());
        interop.FinishWaapi();
        var frame = engine.ComputeFrame(0);

        Assert.IsFalse(frame?["el"].TryGetValue("backgroundColor", out var v) == true && v == "", "nothing is handed back");
        Assert.AreEqual("rgb(255, 0, 0)", State(engine).StringValues["backgroundColor"]);
    }

    [TestMethod]
    public async Task ALeaveThatOvertakesItsEnter_LeavesNoOverlayBehind()
    {
        var (engine, interop) = NewEngine(inProcess: false);
        await using var _ = engine;
        var gate = new TaskCompletionSource();
        interop.ComputedStyle = (_, key) => key == "backgroundColor" ? "rgb(0, 0, 255)" : null;
        interop.ReadStylesGate = gate.Task;
        engine.RegisterElement("el");
        var context = new ManualSynchronizationContext();

        // The enter waits on its read (a round trip on Server); the pointer leaves meanwhile.
        var enter = ManualSynchronizationContext.RunUnder(context, () => engine.ActivateGestureLayerAsync("el", "hover", Values(("backgroundColor", "rgb(255, 0, 0)")), Bm.Tween(0.3).ToConfig()).AsTask());
        var leave = ManualSynchronizationContext.RunUnder(context, () => engine.DeactivateGestureLayerAsync("el", "hover").AsTask());
        gate.SetResult();
        context.Pump();
        await Task.WhenAll(enter, leave);

        // Only the way back plays - an enter that resumed after its leave would leave the overlay on for good.
        Assert.IsFalse(interop.ArgsOf("playWaapiAnimation").Any(p => ((Dictionary<string, object>[])p[2]!)[^1]["backgroundColor"] as string == "rgba(255,0,0,1)"),
            "the overtaken enter must not play");
    }

    // ── Drag release ──────────────────────────────────────────────────────────

    [TestMethod]
    public async Task ADragRelease_IsOneBrowserAnimation_WithATransitionPerAxis()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el");
        engine.SetDragPosition("el", 30, 250);

        // x coasts on momentum; y was dragged past its bound and springs back to it.
        await engine.EndDragAsync("el", velX: 2, velY: 0, momentum: true,
            new BmDragConstraints { Top = 0, Bottom = 200 }, axis: null, snapTransition: null);

        Assert.AreEqual(1, interop.CountOf("playWaapiAnimation"), "two animations of one transform would cancel each other");
        var last = (string)Keyframes(interop)[^1]["transform"];
        StringAssert.Contains(last, "translate(110px,200px)");
    }
}
