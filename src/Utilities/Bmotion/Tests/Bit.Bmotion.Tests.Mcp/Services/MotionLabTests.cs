namespace Bit.Bmotion.Tests.Mcp.Services;

/// <summary>
/// The half of the MCP server that answers by running the real engine rather than by quoting text.
/// <para>
/// These are the tools whose answers an agent cannot sanity-check: nobody can tell by reading that
/// a settle time of 1.4s is wrong. So the tests assert the properties that make the measurements
/// trustworthy - a critically damped spring must measure as not overshooting, a bouncy one must
/// measure as overshooting, a tween must settle at the duration it was given - and the invariants
/// that keep a bad answer from looking like a good one: an unreadable spec must come back as an
/// error rather than as zeroes, and a motion that never rests must say so rather than reporting
/// the frame cap as its settle time.
/// </para>
/// </summary>
[TestClass]
public class MotionLabTests
{
    [TestMethod]
    public async Task Simulate_ATween_SettlesAtTheDurationItWasGiven()
    {
        var result = await BmotionMotionLab.SimulateAsync("tween(0.4, Linear)", 0, 100);

        Assert.IsNull(result.Error);
        Assert.AreEqual("Tween", result.Kind);
        // One frame of tolerance: the engine samples on the 60 fps grid, so it lands on or just past
        // the duration, never before it.
        Assert.AreEqual(0.4, result.SettleSeconds, 0.02, $"Settled at {result.SettleSeconds}s.");
        Assert.AreEqual(0, result.OvershootPercent, 0.001, "A linear tween cannot pass its target.");
        Assert.AreEqual(0, result.TargetCrossings);
    }

    [TestMethod]
    public async Task Simulate_ACriticallyDampedSpring_MeasuresAsNotOvershooting()
    {
        // Damping at 2*sqrt(stiffness*mass) is exactly critical: it eases in and stops dead.
        var result = await BmotionMotionLab.SimulateAsync("spring(stiffness: 100, damping: 20)", 0, 100);

        Assert.AreEqual("Spring", result.Kind);
        Assert.IsTrue(result.OvershootPercent < 0.5, $"Overshot by {result.OvershootPercent}%.");
        Assert.AreEqual(0, result.TargetCrossings);
        StringAssert.Contains(result.Reading, "critically damped");
    }

    [TestMethod]
    public async Task Simulate_AnUnderdampedSpring_MeasuresTheOvershootAndTheWobble()
    {
        var result = await BmotionMotionLab.SimulateAsync("spring(stiffness: 300, damping: 8)", 0, 100);

        Assert.IsTrue(result.OvershootPercent > 5, $"A spring this lightly damped must overshoot; it measured {result.OvershootPercent}%.");
        Assert.IsTrue(result.TargetCrossings >= 2, $"It crossed the target {result.TargetCrossings} times.");
        Assert.IsTrue(result.PeakVelocity > 0);
    }

    /// <summary>
    /// The relationship the whole tool exists to expose: damping is the only lever between these
    /// two, and more of it must measure as less bounce and a shorter settle.
    /// </summary>
    [TestMethod]
    public async Task Simulate_MoreDamping_MeansLessOvershoot()
    {
        var loose = await BmotionMotionLab.SimulateAsync("spring(stiffness: 260, damping: 8)");
        var tight = await BmotionMotionLab.SimulateAsync("spring(stiffness: 260, damping: 26)");

        Assert.IsTrue(tight.OvershootPercent < loose.OvershootPercent,
                      $"Damping 26 overshot {tight.OvershootPercent}%, damping 8 overshot {loose.OvershootPercent}%.");
        Assert.IsTrue(tight.TargetCrossings <= loose.TargetCrossings);
    }

    [TestMethod]
    public async Task Simulate_TheSamples_StartAtTheSourceAndEndAtTheTarget()
    {
        var result = await BmotionMotionLab.SimulateAsync("tween(0.5, InOut)", 20, 180);

        Assert.AreNotEqual(0, result.Samples.Length);
        Assert.IsTrue(result.Samples.Length <= 24, $"{result.Samples.Length} samples is more than a client should be handed.");

        Assert.AreEqual(20, result.Samples[0].Value, 5, "The first sample is not near the starting value.");
        Assert.AreEqual(180, result.Samples[^1].Value, 1, "The last sample is not the resting value.");

        // Time only moves forwards, and the pinned last sample is the end of the motion.
        for (int i = 1; i < result.Samples.Length; i++)
        {
            Assert.IsTrue(result.Samples[i].Seconds >= result.Samples[i - 1].Seconds,
                          $"Sample {i} goes backwards in time.");
        }

        Assert.AreEqual(result.SettleSeconds, result.Samples[^1].Seconds, 1e-6);
    }

    [TestMethod]
    public async Task Simulate_TheSparkline_IsDrawnAndBounded()
    {
        var result = await BmotionMotionLab.SimulateAsync("spring(stiffness: 260, damping: 12)");

        Assert.AreNotEqual(string.Empty, result.Sparkline);
        Assert.IsTrue(result.Sparkline.Length <= 48, $"A {result.Sparkline.Length}-character sparkline does not fit a terminal.");
    }

    [TestMethod]
    public async Task Simulate_ADescendingMotion_MeasuresOvershootInTheDirectionOfTravel()
    {
        // Travelling downwards, "past the target" means below it. A sign error here would report a
        // bouncy spring as perfectly damped.
        var result = await BmotionMotionLab.SimulateAsync("spring(stiffness: 300, damping: 8)", 100, 0);

        Assert.IsTrue(result.OvershootPercent > 5, $"Measured {result.OvershootPercent}% travelling downwards.");
        Assert.AreEqual(100, result.From);
        Assert.AreEqual(0, result.To);
    }

    [TestMethod]
    public async Task Simulate_AZeroLengthMotion_ReportsNothingRatherThanDividingByIt()
    {
        var result = await BmotionMotionLab.SimulateAsync("tween(0.3)", 50, 50);

        Assert.IsNull(result.Error);
        Assert.AreEqual(0, result.OvershootPercent);
        Assert.IsFalse(double.IsNaN(result.OvershootPercent) || double.IsInfinity(result.OvershootPercent));
    }

    [TestMethod]
    public async Task Simulate_Inertia_RetargetsTheMeasurementAtWhereItActuallyStops()
    {
        var result = await BmotionMotionLab.SimulateAsync("inertia(velocity: 500)", 0, 100);

        Assert.AreEqual("Inertia", result.Kind);
        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("ignores the target", StringComparison.OrdinalIgnoreCase)),
                      $"Warnings: {string.Join(" | ", result.Warnings)}");

        // Reported against the resting point, not against the caller's 100 - otherwise the overshoot
        // would be an artefact of a target inertia never had.
        Assert.AreNotEqual(100, result.To);
        Assert.AreEqual(result.To, result.Samples[^1].Value, 0.5);
        StringAssert.Contains(result.Reading, "glide");
    }

    [TestMethod]
    public async Task Simulate_ASpringThatNeverSettles_SaysSoInsteadOfReportingTheFrameCap()
    {
        var result = await BmotionMotionLab.SimulateAsync("spring(stiffness: 400, damping: 0.05)", 0, 100);

        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("come to rest", StringComparison.OrdinalIgnoreCase)),
                      $"A ringing spring reported no warning. Warnings: {string.Join(" | ", result.Warnings)}");
        // And the advice names the lever, rather than only the symptom.
        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("damping", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task Simulate_AnUnreadableSpec_IsDataTheCallerCanAct_Not_AThrownCall()
    {
        var result = await BmotionMotionLab.SimulateAsync("swoosh(0.4)", 0, 100);

        Assert.IsNotNull(result.Error);
        // Reading carries the same text, so a client that renders only the human-facing field still
        // learns what went wrong.
        Assert.AreEqual(result.Error, result.Reading);
        Assert.AreEqual(0, result.Samples.Length);
        Assert.AreEqual("Unknown", result.Kind);
        // The spec is echoed back, so the caller can see what the server thought it was given.
        StringAssert.Contains(result.Transition, "swoosh");
    }

    /// <summary>
    /// Nothing in a simulation may depend on wall-clock time, on scheduling or on a previous run:
    /// the same question asked twice has to give the same answer, or an agent tuning a transition is
    /// chasing noise.
    /// </summary>
    [TestMethod]
    public async Task Simulate_IsDeterministic_AcrossRunsAndUnderConcurrency()
    {
        var first = await BmotionMotionLab.SimulateAsync("spring(stiffness: 260, damping: 12)", 0, 100);
        var second = await BmotionMotionLab.SimulateAsync("spring(stiffness: 260, damping: 12)", 0, 100);

        Assert.AreEqual(first.SettleSeconds, second.SettleSeconds);
        Assert.AreEqual(first.OvershootPercent, second.OvershootPercent);
        Assert.AreEqual(first.TargetCrossings, second.TargetCrossings);
        Assert.AreEqual(first.Sparkline, second.Sparkline);

        // Every run builds its own engine and interop, so eight at once must not see each other.
        var concurrent = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => BmotionMotionLab.SimulateAsync("spring(stiffness: 260, damping: 12)", 0, 100)));

        foreach (var result in concurrent)
        {
            Assert.AreEqual(first.SettleSeconds, result.SettleSeconds);
            Assert.AreEqual(first.Sparkline, result.Sparkline);
        }
    }

    [TestMethod]
    public async Task SampleEase_ReturnsACurveThatStartsAtZeroAndEndsAtOne()
    {
        foreach (var ease in Enum.GetValues<BmEase>())
        {
            var curve = await BmotionMotionLab.SampleEaseAsync(ease);

            Assert.AreEqual(11, curve.Length, $"BmEase.{ease} sampled {curve.Length} points.");
            Assert.AreEqual(0, curve[0], 0.02, $"BmEase.{ease} does not start at 0.");
            Assert.AreEqual(1, curve[^1], 0.02, $"BmEase.{ease} does not end at 1.");
        }
    }

    [TestMethod]
    public async Task SampleEase_Linear_IsTheIdentity()
    {
        var curve = await BmotionMotionLab.SampleEaseAsync(BmEase.Linear);

        for (int i = 0; i < curve.Length; i++)
        {
            Assert.AreEqual(i / 10.0, curve[i], 0.02, $"Linear at t={i / 10.0} sampled {curve[i]}.");
        }
    }

    [TestMethod]
    public async Task SampleEase_HonoursTheRequestedNumberOfPoints()
    {
        Assert.AreEqual(5, (await BmotionMotionLab.SampleEaseAsync(BmEase.Out, 5)).Length);
        Assert.AreEqual(21, (await BmotionMotionLab.SampleEaseAsync(BmEase.Out, 21)).Length);
    }

    /// <summary>
    /// A curve needs two points to be read at all, and the sampling fraction divides by points - 1.
    /// A caller asking for fewer gets the smallest readable curve rather than an empty array or a
    /// division by zero.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(-3)]
    public async Task SampleEase_FewerThanTwoPoints_StillSamplesTheEnds(int points)
    {
        var curve = await BmotionMotionLab.SampleEaseAsync(BmEase.Linear, points);

        Assert.AreEqual(2, curve.Length);
        Assert.AreEqual(0, curve[0], 0.02);
        Assert.AreEqual(1, curve[1], 0.02);
    }

    [TestMethod]
    public async Task AnalyzePlayback_TransformsAndOpacity_PlayInTheBrowserAsATimeline()
    {
        var result = await BmotionMotionLab.AnalyzePlaybackAsync(["x", "opacity"], "tween(0.4, InOut)");

        Assert.IsNull(result.Error);
        Assert.IsTrue(result.WorksOnBlazorServer, result.Reason);
        Assert.AreEqual("Browser (timeline)", result.Path);
        Assert.AreEqual("Browser (timeline)", result.OnWebAssembly);
        Assert.AreEqual("Browser (timeline)", result.OnBlazorServer);
        Assert.IsNull(result.HowToOffload, "There is nothing to fix about an animation the browser already plays.");
        Assert.IsNull(result.HowToPlayAsTimeline, "A timeline is already the cheapest shape.");

        // The timing handed to the browser is read back off the interop, so a wrong duration here
        // would mean the tool is reporting an animation other than the one it ran.
        Assert.AreEqual(400, result.BrowserDurationMs!.Value, 1);
        Assert.AreEqual(2, result.BrowserKeyframes, "A timeline is two keyframes whatever its length.");

        // A timeline's easing is the curve itself - never the plain 'linear' a recording plays with.
        Assert.IsNotNull(result.BrowserEasing);
        Assert.AreNotEqual("linear", result.BrowserEasing);
    }

    /// <summary>
    /// The case the old rule got wrong: lengths and colours used to need the frame loop and jump on
    /// Blazor Server. The engine now hands them to the browser as a timeline like any transform, on
    /// Server too - which the tool has to measure, not remember.
    /// </summary>
    [TestMethod]
    [DataRow("width")]
    [DataRow("height")]
    [DataRow("backgroundColor")]
    [DataRow("top")]
    [DataRow("filter")]
    [DataRow("boxShadow")]
    public async Task AnalyzePlayback_APropertyThatUsedToNeedTheFrameLoop_IsPlayedByTheBrowserOnServer(string property)
    {
        var result = await BmotionMotionLab.AnalyzePlaybackAsync([property], "tween(0.4)");

        Assert.IsNull(result.Error);
        Assert.IsTrue(result.WorksOnBlazorServer, $"'{property}' was reported as snapping on Blazor Server: {result.Reason}");
        Assert.AreEqual("Browser (timeline)", result.Path, result.Reason);
        Assert.AreEqual("Browser (timeline)", result.OnBlazorServer, "Measured as Blazor Server, the browser was not handed it.");
        Assert.IsNull(result.FrameLoopProperties);
        Assert.IsNull(result.HowToOffload);
    }

    /// <summary>
    /// The browser plays it, but not for free: a length or a colour off the compositor still costs
    /// layout or paint on every frame, and the reason says what the compositor equivalent is.
    /// </summary>
    [TestMethod]
    [DataRow("width", "scale")]
    [DataRow("top", "x/y")]
    [DataRow("backgroundColor", "opacity")]
    public async Task AnalyzePlayback_APropertyOffTheCompositor_NamesWhatItCostsAndTheCheaperEquivalent(string property, string equivalent)
    {
        var result = await BmotionMotionLab.AnalyzePlaybackAsync([property], "tween(0.4)");

        StringAssert.Contains(result.Reason, property);
        StringAssert.Contains(result.Reason, equivalent);
    }

    [TestMethod]
    public async Task AnalyzePlayback_TransformsAndALength_ShareOneTimeline()
    {
        var result = await BmotionMotionLab.AnalyzePlaybackAsync(["x", "opacity", "height"], "tween(0.4)");

        Assert.IsTrue(result.WorksOnBlazorServer, result.Reason);
        Assert.AreEqual("Browser (timeline)", result.Path);
        Assert.AreEqual(2, result.BrowserKeyframes);
    }

    /// <summary>
    /// An SVG path morph is the one property no Web Animation can play: Web Animations animate CSS,
    /// and d is an attribute. It is the frame-loop case that is left, and the tool has to say so.
    /// </summary>
    [TestMethod]
    public async Task AnalyzePlayback_AnSvgPathMorph_StaysOnTheFrameLoopAndSaysWhy()
    {
        var result = await BmotionMotionLab.AnalyzePlaybackAsync(["d"], "tween(0.4)");

        Assert.IsFalse(result.WorksOnBlazorServer);
        Assert.AreEqual("Frame loop only (instant on Blazor Server)", result.Path);
        Assert.AreEqual("Frame loop", result.OnWebAssembly);
        Assert.AreEqual("Instant", result.OnBlazorServer);
        CollectionAssert.AreEqual(new[] { "d" }, result.FrameLoopProperties);
        StringAssert.Contains(result.Reason, "attribute");
        Assert.IsNull(result.BrowserKeyframes, "Nothing was handed to the browser.");

        Assert.IsNotNull(result.HowToOffload);
        Assert.AreNotEqual(0, result.HowToOffload.Length, "A frame-loop verdict with no remedy leaves the caller stuck.");
    }

    [TestMethod]
    public async Task AnalyzePlayback_OneFrameLoopOnlyPropertyInASet_StaysOnTheLoopAlone()
    {
        var result = await BmotionMotionLab.AnalyzePlaybackAsync(["x", "opacity", "d"], "tween(0.4)");

        Assert.IsFalse(result.WorksOnBlazorServer);
        Assert.AreEqual("Browser (timeline) + frame loop only (instant on Blazor Server)", result.Path);
        CollectionAssert.AreEqual(new[] { "d" }, result.FrameLoopProperties);

        // The rest still reaches the browser, and is not blamed for the part that does not.
        Assert.AreEqual(2, result.BrowserKeyframes);
        StringAssert.Contains(result.Reason, "attribute");
        Assert.IsFalse(result.Reason.Contains("opacity is", StringComparison.Ordinal));
    }

    /// <summary>
    /// Inertia has no target to describe a curve to, so it used to be a frame-loop animation. The
    /// engine now records its glide, and the browser plays the recording on every runtime.
    /// </summary>
    [TestMethod]
    public async Task AnalyzePlayback_Inertia_IsRecorded_AndSaysWhyItIsNotATimeline()
    {
        var result = await BmotionMotionLab.AnalyzePlaybackAsync(["x"], "inertia(velocity: 500)");

        Assert.IsTrue(result.WorksOnBlazorServer, result.Reason);
        Assert.AreEqual("Browser (recorded)", result.Path);
        StringAssert.Contains(result.Reason, "inertia");
        Assert.IsNull(result.HowToOffload, "The browser plays it; there is nothing to get it there.");
        Assert.IsTrue(result.HowToPlayAsTimeline!.Any(remedy => remedy.Contains("spring", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// What a recording costs: more keyframes than a timeline's two, each carrying its own offset,
    /// played with a linear easing because the keyframes already carry the curve.
    /// </summary>
    [TestMethod]
    public async Task AnalyzePlayback_ARecording_SendsMoreKeyframesThanATimeline_PlayedLinearly()
    {
        var recorded = await BmotionMotionLab.AnalyzePlaybackAsync(["x"], "inertia(velocity: 500)");

        Assert.AreEqual("linear", recorded.BrowserEasing);
        Assert.IsTrue(recorded.BrowserKeyframes > 2, $"A recording of a glide sent {recorded.BrowserKeyframes} keyframes.");
        StringAssert.Contains(recorded.Reason, "keyframes");
    }

    /// <summary>
    /// A spring with an initial velocity is still played by the browser - but as a recording rather
    /// than a timeline. It is the case an agent is least likely to predict, and the cost it most
    /// needs to see.
    /// </summary>
    [TestMethod]
    public async Task AnalyzePlayback_ASpringWithInitialVelocity_IsRecordedRatherThanATimeline()
    {
        var still = await BmotionMotionLab.AnalyzePlaybackAsync(["x"], "spring(stiffness: 260, damping: 20)");
        var flung = await BmotionMotionLab.AnalyzePlaybackAsync(["x"], "spring(stiffness: 260, damping: 20, velocity: 400)");

        Assert.AreEqual("Browser (timeline)", still.Path, still.Reason);
        Assert.AreEqual("Browser (recorded)", flung.Path, "An initial velocity has to be reported as costing the timeline.");
        Assert.IsTrue(still.WorksOnBlazorServer);
        Assert.IsTrue(flung.WorksOnBlazorServer);
        StringAssert.Contains(flung.Reason, "velocity");
    }

    /// <summary>
    /// Transitions a spec string cannot write go through the C# overload. These are the engine's
    /// reasons for recording rather than describing a curve: each must come back as a recording that
    /// still plays on Blazor Server.
    /// </summary>
    [TestMethod]
    public async Task AnalyzePlayback_PerPropertyTransitionsMirrorsAndRepeatDelays_AreRecorded()
    {
        var perProperty = new BmTween { Duration = 0.4, Properties = new() { ["opacity"] = new BmTween { Duration = 0.2 } } };
        var mirrored = new BmTween { Duration = 0.4, Repeat = BmRepeat.Mirror(2) };
        var delayed = new BmTween { Duration = 0.4, Repeat = BmRepeat.Loop(2, delay: 0.3) };

        foreach (var (transition, properties, cause) in new (BmTransition, string[], string)[]
        {
            (perProperty, ["x", "opacity"], "per-property"),
            (mirrored, ["x"], "mirrored"),
            (delayed, ["x"], "waits between iterations"),
        })
        {
            var result = await BmotionMotionLab.AnalyzePlaybackAsync(properties, transition);

            Assert.AreEqual("Browser (recorded)", result.Path, $"{cause}: {result.Reason}");
            Assert.IsTrue(result.WorksOnBlazorServer, cause);
            StringAssert.Contains(result.Reason, cause);
            Assert.AreNotEqual(0, result.HowToPlayAsTimeline?.Length ?? 0, $"{cause}: no way to a timeline was offered.");
        }
    }

    /// <summary>
    /// What is left of the frame loop besides attributes: a per-frame callback, an endlessly reversing
    /// repeat, and a recording too long to make. Each animates on WebAssembly and lands at once on
    /// Blazor Server, and the reason names the one that applies.
    /// </summary>
    [TestMethod]
    public async Task AnalyzePlayback_OnUpdateEndlessReverseAndLongRecordings_StayOnTheFrameLoop()
    {
        var onUpdate = new BmTween { Duration = 0.4, OnUpdate = _ => { } };
        var endlessReverse = new BmTween { Duration = 0.4, Repeat = BmRepeat.Reverse() };
        var tooLong = new BmTween { Duration = 4, Repeat = BmRepeat.Mirror(2) };

        foreach (var (transition, cause) in new (BmTransition, string)[]
        {
            (onUpdate, "OnUpdate"),
            (endlessReverse, "BmRepeat.Reverse"),
            (tooLong, "10 seconds"),
        })
        {
            var result = await BmotionMotionLab.AnalyzePlaybackAsync(["x"], transition);

            Assert.AreEqual("Frame loop only (instant on Blazor Server)", result.Path, $"{cause}: {result.Reason}");
            Assert.AreEqual("Instant", result.OnBlazorServer, cause);
            Assert.IsFalse(result.WorksOnBlazorServer, cause);
            StringAssert.Contains(result.Reason, cause);
            Assert.AreNotEqual(0, result.HowToOffload?.Length ?? 0, $"{cause}: no remedy was offered.");
        }
    }

    /// <summary>
    /// The ten-second limit is a recording's, not a timeline's: a long plain tween is still two
    /// keyframes, and still plays in the browser.
    /// </summary>
    [TestMethod]
    public async Task AnalyzePlayback_ALongPlainTween_IsStillATimeline()
    {
        var result = await BmotionMotionLab.AnalyzePlaybackAsync(["x"], "tween(12)");

        Assert.AreEqual("Browser (timeline)", result.Path, result.Reason);
        Assert.AreEqual(12_000, result.BrowserDurationMs!.Value, 1);
    }

    [TestMethod]
    public async Task AnalyzePlayback_AZeroDurationTween_IsInstantEverywhereAlike()
    {
        var result = await BmotionMotionLab.AnalyzePlaybackAsync(["x", "width"], "tween(0)");

        Assert.AreEqual("Instant", result.Path);
        Assert.AreEqual("Instant", result.OnWebAssembly);
        Assert.AreEqual("Instant", result.OnBlazorServer);
        // Nothing animates anywhere, so Server does exactly what WebAssembly does.
        Assert.IsTrue(result.WorksOnBlazorServer);
        StringAssert.Contains(result.Reason, "no duration");
    }

    [TestMethod]
    public async Task AnalyzePlayback_AnUnknownPropertyName_IsReportedRatherThanIgnored()
    {
        var result = await BmotionMotionLab.AnalyzePlaybackAsync(["x", "wobbliness"], "tween(0.3)");

        StringAssert.Contains(result.Reason, "wobbliness");
        StringAssert.Contains(result.Reason, "no such argument");
    }

    [TestMethod]
    public async Task AnalyzePlayback_AnUnreadableTransition_IsNotAVerdictAboutBlazorServer()
    {
        var result = await BmotionMotionLab.AnalyzePlaybackAsync(["x"], "swoosh(0.4)");

        Assert.IsNotNull(result.Error);
        Assert.AreEqual("Not analysed", result.Path);
        Assert.IsFalse(result.WorksOnBlazorServer);
        Assert.AreEqual(result.Error, result.Reason);
    }

    [TestMethod]
    public async Task AnalyzePlayback_NoPropertiesAtAll_DoesNotThrow()
    {
        var result = await BmotionMotionLab.AnalyzePlaybackAsync([], "tween(0.3)");

        Assert.AreEqual(0, result.Properties.Length);
        Assert.IsNotNull(result.Reason);

        // An animation of nothing is not a verdict about the transition - instant or otherwise.
        Assert.AreEqual("Not analysed", result.Path);
        Assert.IsNotNull(result.Error);
    }
}
