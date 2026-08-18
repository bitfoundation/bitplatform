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

    [TestMethod]
    public async Task AnalyzePlayback_TransformsAndOpacity_GoToTheCompositor()
    {
        var result = await BmotionMotionLab.AnalyzePlaybackAsync(["x", "opacity"], "tween(0.4, InOut)");

        Assert.IsNull(result.Error);
        Assert.IsTrue(result.WorksOnBlazorServer, result.Reason);
        StringAssert.Contains(result.Path, "Compositor");
        Assert.IsNull(result.HowToOffload, "There is nothing to fix about an animation that already offloads.");

        // The timing handed to the browser is read back off the interop, so a wrong duration here
        // would mean the tool is reporting an animation other than the one it ran.
        Assert.AreEqual(400, result.CompositorDurationMs!.Value, 1);
        Assert.IsNotNull(result.CompositorEasing);
    }

    [TestMethod]
    [DataRow("width")]
    [DataRow("height")]
    [DataRow("backgroundColor")]
    [DataRow("top")]
    public async Task AnalyzePlayback_APropertyTheCompositorCannotOwn_StaysOnTheFrameLoopAndSaysWhy(string property)
    {
        var result = await BmotionMotionLab.AnalyzePlaybackAsync([property], "tween(0.4)");

        Assert.IsFalse(result.WorksOnBlazorServer, $"'{property}' was reported as compositor-eligible.");
        StringAssert.Contains(result.Path, "frame loop");
        StringAssert.Contains(result.Reason, property);

        Assert.IsNotNull(result.HowToOffload);
        Assert.AreNotEqual(0, result.HowToOffload.Length, "A frame-loop verdict with no remedy leaves the caller stuck.");
    }

    [TestMethod]
    public async Task AnalyzePlayback_OneIneligiblePropertyInASetTakesTheWholeAnimationOffTheCompositor()
    {
        var result = await BmotionMotionLab.AnalyzePlaybackAsync(["x", "opacity", "height"], "tween(0.4)");

        Assert.IsFalse(result.WorksOnBlazorServer);
        StringAssert.Contains(result.Reason, "height");
        // The properties that were fine are not blamed for it.
        Assert.IsFalse(result.Reason.Contains("opacity -", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task AnalyzePlayback_Inertia_NamesTheTransitionAsTheBlocker()
    {
        var result = await BmotionMotionLab.AnalyzePlaybackAsync(["x"], "inertia(velocity: 500)");

        Assert.IsFalse(result.WorksOnBlazorServer);
        StringAssert.Contains(result.Reason, "inertia");
        Assert.IsTrue(result.HowToOffload!.Any(remedy => remedy.Contains("spring", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// A spring with an initial velocity drops off the compositor silently - it is the case an agent
    /// is least likely to predict, and the one the tool most needs to catch.
    /// </summary>
    [TestMethod]
    public async Task AnalyzePlayback_ASpringWithInitialVelocity_LosesTheCompositor()
    {
        var still = await BmotionMotionLab.AnalyzePlaybackAsync(["x"], "spring(stiffness: 260, damping: 20)");
        var flung = await BmotionMotionLab.AnalyzePlaybackAsync(["x"], "spring(stiffness: 260, damping: 20, velocity: 400)");

        Assert.IsTrue(still.WorksOnBlazorServer, still.Reason);
        Assert.IsFalse(flung.WorksOnBlazorServer, "An initial velocity has to be reported as costing the compositor path.");
        StringAssert.Contains(flung.Reason, "velocity");
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
    }
}
