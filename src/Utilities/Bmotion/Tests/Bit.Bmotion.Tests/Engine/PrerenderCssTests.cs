namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// Tests for <see cref="BmotionPrerenderCss"/>: compiling an enter animation into the custom
/// properties the library stylesheet animates from, and the stylesheet itself.
/// </summary>
[TestClass]
public class PrerenderCssTests
{
    private static Dictionary<string, object?> Values(BmProps props) => props.ToJsDictionary();

    private static BmotionPrerenderPlan Compile(BmProps? initial, BmProps target, BmTransition? transition = null, bool withReduced = false)
    {
        var full = transition?.ToConfig();
        var plan = BmotionPrerenderCss.Compile(
            initial is null ? null : Values(initial), Values(target), full,
            withReduced ? Bmotion.BuildReducedTransition(full) : null, null);
        Assert.IsNotNull(plan);
        return plan;
    }

    // ── The shipped stylesheet ─────────────────────────────────────────────────

    /// <summary>
    /// wwwroot/bit-bmotion.css is generated from <see cref="BmotionPrerenderCss.BuildStylesheet"/>.
    /// Run the tests with BMOTION_UPDATE_STYLESHEET=1 to regenerate it after changing the generator.
    /// </summary>
    [TestMethod]
    public void ShippedStylesheet_MatchesGenerator()
    {
        var path = FindStylesheet();
        var expected = BmotionPrerenderCss.BuildStylesheet();

        if (Environment.GetEnvironmentVariable("BMOTION_UPDATE_STYLESHEET") == "1")
            File.WriteAllText(path, expected.Replace("\n", "\r\n"), new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        var actual = File.ReadAllText(path).Replace("\r\n", "\n");
        Assert.AreEqual(expected, actual,
            "bit-bmotion.css is out of date: run the tests with BMOTION_UPDATE_STYLESHEET=1 to regenerate it.");
    }

    private static string FindStylesheet()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            var project = Path.Combine(dir.FullName, "Bit.Bmotion", "Bit.Bmotion.csproj");
            if (File.Exists(project)) return Path.Combine(dir.FullName, "Bit.Bmotion", "wwwroot", "bit-bmotion.css");
        }
        throw new InvalidOperationException("Could not locate the Bit.Bmotion project from the test output directory.");
    }

    [TestMethod]
    public void Stylesheet_HasKeyframesForEveryPropertyAndFrameCount()
    {
        var css = BmotionPrerenderCss.BuildStylesheet();
        foreach (var property in BmotionPrerenderCss.AnimatableProperties)
            for (int n = 2; n <= BmotionPrerenderCss.MaxFrames; n++)
                Assert.Contains($"@keyframes bm-{property}-{n}{{", css);

        Assert.Contains("@keyframes bm-opacity-3{0%{opacity:var(--bm-opacity-0)}50%{opacity:var(--bm-opacity-1)}100%{opacity:var(--bm-opacity-2)}}", css);
        // No leading ';' on the animation hooks: they must still match once the browser has
        // reserialized the attribute as "--bm-a: ..." after a CSSOM write.
        Assert.Contains("[style*=\"--bm-a:\"]{animation:var(--bm-a)}", css);
        Assert.Contains("[style*=\"--bm-af:\"]{animation:var(--bm-af)}", css);
        // Only an element that declares both lists itself follows the OS preference.
        Assert.Contains("@media (prefers-reduced-motion:reduce){[style*=\"--bm-af:\"]{animation:var(--bm-ar)}}", css);
        Assert.DoesNotContain("var(--bm-ar,", css, "the reduced list must never fall back onto another element's");
        Assert.Contains("[style*=\";--bm-nj-opacity:\"]{opacity:var(--bm-nj-opacity)!important}", css);
    }

    // ── Resting state ──────────────────────────────────────────────────────────

    [TestMethod]
    public void SettledStyle_IsTheTarget_OverTheInitialState()
    {
        var plan = Compile(Bm.To(opacity: 0, y: 20, backgroundColor: "#000"), Bm.To(opacity: 1, y: 0));

        // y lands on 0 (an identity transform renders nothing); backgroundColor wasn't animated.
        Assert.AreEqual("opacity:1;background-color:#000;", plan.SettledStyle);
        Assert.AreEqual(1.0, plan.Settled["opacity"]);
        Assert.AreEqual(0.0, plan.Settled["y"]);
    }

    [TestMethod]
    public void SettledStyle_MirroredEvenPasses_EndWhereTheyStarted()
    {
        var plan = Compile(Bm.To(opacity: 0), Bm.To(opacity: 1), Bm.Tween(0.5, repeat: BmRepeat.Mirror(1)));
        Assert.AreEqual(0.0, plan.Settled["opacity"]);
    }

    [TestMethod]
    public void SettledStyle_Keyframes_LandOnTheLastFrame()
    {
        var plan = Compile(null, Bm.To(scale: new double[] { 1, 1.2, 1.1 }));
        Assert.AreEqual(1.1, plan.Settled["scale"]);
        Assert.AreEqual("transform:scale(1.1);", plan.SettledStyle);
    }

    [TestMethod]
    public void PathMorph_CannotBePrerenderedAtRest()
    {
        var target = new Dictionary<string, object?> { ["d"] = "M0 0 L10 10" };
        Assert.IsNull(BmotionPrerenderCss.Compile(null, target, null, null, null));
    }

    // ── Tracks and timing ──────────────────────────────────────────────────────

    [TestMethod]
    public void Tween_ProducesOneTrackPerCssProperty()
    {
        var plan = Compile(Bm.To(opacity: 0, y: 20), Bm.To(opacity: 1, y: 0), Bm.Tween(0.5, delay: 0.1));

        var transform = plan.Tracks.Single(t => t.CssProperty == "transform");
        Assert.AreSequenceEqual(new[] { "translate(0px,20px)", "none" }, transform.Frames);
        var opacity = plan.Tracks.Single(t => t.CssProperty == "opacity");
        Assert.AreSequenceEqual(new[] { "0", "1" }, opacity.Frames);

        Assert.AreEqual(500, opacity.Full!.DurationMs, 1e-9);
        Assert.AreEqual(100, opacity.Full.DelayMs, 1e-9);
        Assert.AreEqual(1, opacity.Full.Iterations);
        Assert.IsFalse(opacity.Full.Alternate);
    }

    [TestMethod]
    public void Declarations_HookUpTheStylesheet()
    {
        var plan = Compile(Bm.To(opacity: 0), Bm.To(opacity: 1), Bm.Tween(0.5, ease: BmEase.Linear));
        var css = plan.BuildAnimationDeclarations(BmotionPrerenderMotionPolicy.Full);

        Assert.AreEqual(";--bm-a:bm-opacity-2 500ms linear 0ms 1 normal backwards;--bm-opacity-0:0;--bm-opacity-1:1", css);
    }

    [TestMethod]
    public void Tween_WithoutAFaithfulCssCurve_IsSampledToLinear()
    {
        var plan = Compile(Bm.To(opacity: 0), Bm.To(opacity: 1), Bm.Tween(0.5, ease: BmEase.CircOut));
        Assert.StartsWith("linear(0, ", plan.Tracks.Single().Full!.Easing);
    }

    [TestMethod]
    public void Spring_IsSampledToLinear_WithItsSettlingDuration()
    {
        var plan = Compile(Bm.To(x: 0), Bm.To(x: 100), Bm.Spring(stiffness: 300, damping: 20));
        var timing = plan.Tracks.Single().Full!;

        Assert.StartsWith("linear(", timing.Easing);
        Assert.IsGreaterThan(100, timing.DurationMs, $"a spring takes time to settle, got {timing.DurationMs}ms");
    }

    [TestMethod]
    public void Spring_OnAColour_HoldsTheColourWithinItsEnds_WhileTheMoveOvershoots()
    {
        // Each property is its own CSS animation, so the colour's curve can be held within 0..1 - as the frame loop
        // holds it - while the transform plays the spring's overshoot.
        var plan = Compile(Bm.To(x: 0, backgroundColor: "#1276C6"), Bm.To(x: 100, backgroundColor: "#FD7F36"),
            Bm.Spring(stiffness: 300, damping: 10));

        static double[] Progress(string easing) => System.Text.RegularExpressions.Regex.Matches(easing, @"-?\d+(\.\d+)?")
            .Select(m => double.Parse(m.Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        var colour = Progress(plan.Tracks.Single(t => t.CssProperty == "background-color").Full!.Easing);
        var move = Progress(plan.Tracks.Single(t => t.CssProperty == "transform").Full!.Easing);

        Assert.IsTrue(colour.All(p => p is >= 0 and <= 1), "the colour's curve leaves 0..1");
        Assert.AreEqual(1, colour.Max());
        Assert.IsGreaterThan(1, move.Max(), "the move still overshoots");
    }

    [TestMethod]
    public void SharedSpringEasing_IsDeclaredOnce()
    {
        var plan = Compile(Bm.To(opacity: 0, x: 0), Bm.To(opacity: 1, x: 100), Bm.Spring(stiffness: 300, damping: 20), withReduced: true);
        var css = plan.BuildAnimationDeclarations(BmotionPrerenderMotionPolicy.FollowOs);

        // transform and opacity in the full list, opacity again in the reduced one: one curve.
        Assert.AreEqual(1, css.Split("linear(").Length - 1, css);
        Assert.AreEqual(3, css.Split("var(--bm-e0)").Length - 1, css);
        Assert.Contains(";--bm-e0:linear(", css);
    }

    [TestMethod]
    public void Repeat_MapsToIterationsAndDirection()
    {
        var plan = Compile(Bm.To(rotate: 0), Bm.To(rotate: 360), Bm.Tween(1, ease: BmEase.Linear, repeat: BmRepeat.Mirror()));
        var timing = plan.Tracks.Single().Full!;

        Assert.IsTrue(plan.IsInfinite);
        Assert.IsTrue(timing.IsInfinite);
        Assert.IsTrue(timing.Alternate);
        Assert.Contains(" infinite alternate backwards", plan.BuildAnimationDeclarations(BmotionPrerenderMotionPolicy.Full));
    }

    [TestMethod]
    public void Keyframes_UseTheMatchingFrameCountKeyframes()
    {
        var plan = Compile(null, Bm.To(opacity: new double[] { 0, 1, 0.5 }), Bm.Tween(0.6));
        var css = plan.BuildAnimationDeclarations(BmotionPrerenderMotionPolicy.Full);

        Assert.Contains("--bm-a:bm-opacity-3 600ms", css);
        Assert.Contains(";--bm-opacity-2:0.5", css);
    }

    [TestMethod]
    public void TooManyKeyframes_RenderAtRest()
    {
        var plan = Compile(null, Bm.To(x: new double[] { 0, -10, 10, -10, 10, -5, 0 }), Bm.Tween(0.6));
        Assert.IsEmpty(plan.Tracks);
        Assert.AreEqual(string.Empty, plan.BuildAnimationDeclarations(BmotionPrerenderMotionPolicy.Full));
    }

    [TestMethod]
    public void EvenTimes_Animate()
    {
        var plan = Compile(null, Bm.To(opacity: new double[] { 0, 1, 0.5 }), Bm.Tween(0.6, times: [0, 0.5, 1]));
        Assert.ContainsSingle(plan.Tracks);
    }

    [TestMethod]
    public void UnevenTimes_RenderAtRest()
    {
        var plan = Compile(null, Bm.To(opacity: new double[] { 0, 1, 0.5 }), Bm.Tween(0.6, times: [0, 0.2, 1]));
        Assert.IsEmpty(plan.Tracks);
    }

    [TestMethod]
    public void Inertia_RendersAtRest()
    {
        var plan = Compile(Bm.To(x: 0), Bm.To(x: 100), Bm.Inertia(velocity: 500));
        Assert.IsEmpty(plan.Tracks);
        Assert.AreEqual("transform:translate(100px,0px);", plan.SettledStyle);
    }

    [TestMethod]
    public void TransformComponents_WithDifferentTransitions_RenderAtRest()
    {
        var transition = new BmTween { Duration = 0.5, Properties = new() { ["scale"] = Bm.Tween(1) } };
        var plan = Compile(Bm.To(x: 0, scale: 0.5, opacity: 0), Bm.To(x: 100, scale: 1, opacity: 1), transition);

        // transform is one CSS property, so x and scale can't keep their own timings - but opacity can.
        Assert.DoesNotContain(t => t.CssProperty == "transform", plan.Tracks);
        Assert.Contains(t => t.CssProperty == "opacity", plan.Tracks);
    }

    [TestMethod]
    public void Color_WithAKnownStart_Animates()
    {
        var plan = Compile(Bm.To(backgroundColor: "#ff0000"), Bm.To(backgroundColor: "#0000ff"), Bm.Tween(0.4));
        var track = plan.Tracks.Single();
        Assert.AreEqual("background-color", track.CssProperty);
        Assert.AreSequenceEqual(new[] { "#ff0000", "#0000ff" }, track.Frames);
    }

    [TestMethod]
    public void Color_WithoutAKnownStart_JustLands()
    {
        var plan = Compile(null, Bm.To(backgroundColor: "#0000ff"), Bm.Tween(0.4));
        Assert.IsEmpty(plan.Tracks);
        Assert.AreEqual("background-color:#0000ff;", plan.SettledStyle);
    }

    [TestMethod]
    public void PathDrawing_AnimatesTheDashProperties()
    {
        var plan = Compile(Bm.To(pathLength: 0), Bm.To(pathLength: 1), Bm.Tween(1));
        Assert.AreSequenceEqual(
            new[] { "stroke-dasharray", "stroke-dashoffset" }, plan.Tracks.Select(t => t.CssProperty).ToArray(), Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);
    }

    // ── Reduced motion ─────────────────────────────────────────────────────────

    [TestMethod]
    public void FollowOs_EmitsBothLists_TransformsSnapUnderReduce()
    {
        var plan = Compile(Bm.To(opacity: 0, y: 20), Bm.To(opacity: 1, y: 0), Bm.Tween(0.5, ease: BmEase.Linear), withReduced: true);
        var css = plan.BuildAnimationDeclarations(BmotionPrerenderMotionPolicy.FollowOs);

        Assert.Contains(";--bm-af:bm-transform-2 500ms linear 0ms 1 normal backwards,bm-opacity-2 500ms linear 0ms 1 normal backwards", css);
        Assert.Contains(";--bm-ar:bm-opacity-2 500ms linear 0ms 1 normal backwards", css);
        Assert.DoesNotContain("--bm-a:", css, css);
    }

    [TestMethod]
    public void FixedPolicies_DeclareOnlyTheirOwnList()
    {
        var plan = Compile(Bm.To(opacity: 0, y: 20), Bm.To(opacity: 1, y: 0), Bm.Tween(0.5, ease: BmEase.Linear), withReduced: true);

        // Nested under an element that follows the OS preference, a fixed-policy element must not
        // pick up that element's reduced list: it declares nothing the stylesheet would read as one.
        foreach (var policy in new[] { BmotionPrerenderMotionPolicy.Full, BmotionPrerenderMotionPolicy.Reduced })
        {
            var css = plan.BuildAnimationDeclarations(policy);
            Assert.StartsWith(";--bm-a:", css);
            Assert.IsFalse(css.Contains("--bm-ar") || css.Contains("--bm-af"), css);
        }
    }

    [TestMethod]
    public void Reduced_UsesOnlyTheReducedList()
    {
        var plan = Compile(Bm.To(y: 20), Bm.To(y: 0), Bm.Tween(0.5), withReduced: true);
        // Only a transform animates, and reduced motion snaps it: nothing to play.
        Assert.AreEqual(string.Empty, plan.BuildAnimationDeclarations(BmotionPrerenderMotionPolicy.Reduced));
        Assert.AreNotEqual(string.Empty, plan.BuildAnimationDeclarations(BmotionPrerenderMotionPolicy.Full));
    }

    // ── Continuing from where the prerendered animation got to ─────────────────

    [TestMethod]
    public void Elapsed_ShiftsTheDelay()
    {
        var plan = Compile(Bm.To(opacity: 0), Bm.To(opacity: 1), Bm.Tween(1, ease: BmEase.Linear, delay: 0.2));
        var css = plan.BuildAnimationDeclarations(BmotionPrerenderMotionPolicy.Full, elapsedMs: 700);
        Assert.Contains("bm-opacity-2 1000ms linear -500ms 1 normal backwards", css);
    }

    [TestMethod]
    public void Elapsed_PastTheEnd_LeavesNothingToPlay()
    {
        var plan = Compile(Bm.To(opacity: 0), Bm.To(opacity: 1), Bm.Tween(1, delay: 0.2));
        Assert.AreEqual(string.Empty, plan.BuildAnimationDeclarations(BmotionPrerenderMotionPolicy.Full, elapsedMs: 1200));
    }

    // ── No-script fallback for WhileInView ─────────────────────────────────────

    [TestMethod]
    public void NoScript_ShowsTheInViewState_ForTheTouchedProperties()
    {
        var rest = BmotionPrerenderCss.FirstFrames(Values(Bm.To(opacity: 0, y: 40, backgroundColor: "#000")));
        var css = BmotionPrerenderCss.BuildNoScriptDeclarations(rest, Values(Bm.To(opacity: 1, y: 0)), null);

        Assert.Contains(";--bm-nj-opacity:1", css);
        // y lands on identity, which composes to no transform at all.
        Assert.DoesNotContain("background-color", css, css);
    }
}
