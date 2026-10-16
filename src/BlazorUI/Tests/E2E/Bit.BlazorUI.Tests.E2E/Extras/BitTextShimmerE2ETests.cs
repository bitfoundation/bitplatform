using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Drives BitTextShimmer at /e2e/text-shimmer in a real browser: the band its stylesheet sweeps across the glyphs (the
/// running CSS animation, its timing, its direction and its iterations), the root element and its fallbacks, the
/// spread computed from the text or the content length and the one given as a length, the angle, every Color, the
/// base and gradient colors, Paused, PauseOnHover (pointer and focus), Static, Disabled, the live region pattern, the
/// BitParams cascade and its precedence, every --bit-TextShimmer-* variable, Style/Class/HtmlAttributes, the base
/// parameters, RTL (explicit and inherited), reduced motion with ForceAnimation, forced colors and print, prerendering,
/// re-rendering and unmounting.
/// </summary>
[TestClass]
public class BitTextShimmerE2ETests : E2ETestBase
{
    private const string Route = "/e2e/text-shimmer";

    // ---------- basic ----------

    [TestMethod]
    public async Task Basic_AText_SweepsABandAcrossItsGlyphs()
    {
        await OpenAsync(Route);

        var shimmer = Page.Locator("#tsh-basic");
        await Expect(shimmer).ToHaveTextAsync("Thinking about your question...");
        await Expect(shimmer).ToHaveClassAsync(HasClass("bit-tsh"));
        Assert.AreEqual("P", await shimmer.EvaluateAsync<string>("e => e.tagName"));

        // The glyphs are left transparent and the background is clipped to them, which is what paints the band.
        await Expect(shimmer).ToHaveCSSAsync("-webkit-text-fill-color", "rgba(0, 0, 0, 0)");
        await Expect(shimmer).ToHaveCSSAsync("background-clip", "text");
        await Expect(shimmer).ToHaveCSSAsync("display", "inline-block");
        StringAssert.StartsWith(await ComputedStyleAsync("#tsh-basic", "background-image"), "linear-gradient(90deg");

        // The resting color is the tertiary foreground of the theme, painted under the band too.
        var baseColor = await ResolveColorAsync("var(--bit-clr-fg-ter)");
        await Expect(shimmer).ToHaveCSSAsync("color", baseColor);
        await Expect(shimmer).ToHaveCSSAsync("background-color", baseColor);
        StringAssert.Contains(await ComputedStyleAsync("#tsh-basic", "background-image"), await ResolveColorAsync("var(--bit-clr-fg-pri)"));

        var animation = await AnimationAsync("#tsh-basic");
        Assert.IsNotNull(animation);
        Assert.AreEqual("running", animation.Value.GetProperty("state").GetString());
        Assert.AreEqual(2000, animation.Value.GetProperty("duration").GetDouble(), 1);
        Assert.AreEqual(0, animation.Value.GetProperty("delay").GetDouble(), 0.5);
        Assert.AreEqual("Infinity", animation.Value.GetProperty("iterations").GetString());
        Assert.AreEqual("normal", animation.Value.GetProperty("direction").GetString());
        await Expect(shimmer).ToHaveCSSAsync("animation-timing-function", "linear");
        await Expect(shimmer).ToHaveCSSAsync("animation-fill-mode", "both");

        // The band actually moves: its position is not the same from one moment to the next.
        await WaitForBackgroundToMoveAsync("#tsh-basic");
    }

    [TestMethod]
    public async Task Basic_NoTextNorContent_RendersAnEmptyRootSizedByTheDefaultLength()
    {
        await OpenAsync(Route);

        var shimmer = Page.Locator("#tsh-empty");
        await Expect(shimmer).ToHaveTextAsync("");
        Assert.AreEqual("20px", await CustomPropertyAsync("#tsh-empty", "--bit-tsh-spread"));
    }

    [TestMethod]
    public async Task Basic_TheText_StaysRealTextForCopyingAndScreenReaders()
    {
        await OpenAsync(Route);

        Assert.AreEqual("Thinking about your question...", await Page.Locator("#tsh-basic").InnerTextAsync());

        var selected = await Page.Locator("#tsh-basic").EvaluateAsync<string>(
            """
            e => {
                const range = document.createRange();
                range.selectNodeContents(e);
                const selection = getSelection();
                selection.removeAllRanges();
                selection.addRange(range);
                const text = selection.toString();
                selection.removeAllRanges();
                return text;
            }
            """);
        Assert.AreEqual("Thinking about your question...", selected);
    }

    // ---------- reduced motion, forced colors, print ----------

    [TestMethod]
    public async Task ReducedMotion_Requested_DrawsTheTextAtRestUnlessForceAnimationIsSet()
    {
        await OpenAsync(Route);
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        await Expect(Page.Locator("#tsh-basic")).ToHaveCSSAsync("animation-name", "none");
        Assert.IsNull(await AnimationAsync("#tsh-basic"));
        var color = await ComputedStyleAsync("#tsh-basic", "color");
        await Expect(Page.Locator("#tsh-basic")).ToHaveCSSAsync("-webkit-text-fill-color", color);
        await Expect(Page.Locator("#tsh-basic")).ToHaveCSSAsync("background-image", "none");

        // ForceAnimation keeps it sweeping at its full pace, on the shimmer itself or on an ancestor.
        await Expect(Page.Locator("#tsh-forced")).ToHaveClassAsync(HasClass("bit-fam"));
        foreach (var id in new[] { "tsh-forced", "tsh-forced-ancestor" })
        {
            var animation = await AnimationAsync($"#{id}");
            Assert.IsNotNull(animation, id);
            Assert.AreEqual("running", animation.Value.GetProperty("state").GetString(), id);
            Assert.AreEqual(2000, animation.Value.GetProperty("duration").GetDouble(), 1, id);
            await Expect(Page.Locator($"#{id}")).ToHaveCSSAsync("-webkit-text-fill-color", "rgba(0, 0, 0, 0)");
        }

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.NoPreference });
        await Expect(Page.Locator("#tsh-basic")).ToHaveCSSAsync("animation-name", "bit-tsh-anim");
    }

    [TestMethod]
    public async Task ForcedColors_Active_PaintsTheTextInCanvasTextWithoutTheBand()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit does not emulate forced colors.");

        await OpenAsync(Route);
        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });

        var canvasText = await ResolveColorAsync("CanvasText");
        await Expect(Page.Locator("#tsh-basic")).ToHaveCSSAsync("color", canvasText);
        await Expect(Page.Locator("#tsh-basic")).ToHaveCSSAsync("-webkit-text-fill-color", canvasText);
        await Expect(Page.Locator("#tsh-basic")).ToHaveCSSAsync("animation-name", "none");

        await Expect(Page.Locator("#tsh-disabled")).ToHaveCSSAsync("color", await ResolveColorAsync("GrayText"));
    }

    [TestMethod]
    public async Task Print_TheMedia_PaintsTheTextFlat()
    {
        await OpenAsync(Route);
        await Page.EmulateMediaAsync(new() { Media = Media.Print });

        await Expect(Page.Locator("#tsh-basic")).ToHaveCSSAsync("animation-name", "none");
        var color = await ComputedStyleAsync("#tsh-basic", "color");
        await Expect(Page.Locator("#tsh-basic")).ToHaveCSSAsync("-webkit-text-fill-color", color);
        await Expect(Page.Locator("#tsh-basic")).ToHaveCSSAsync("background-image", "none");
    }

    // ---------- element ----------

    [TestMethod]
    public async Task Element_ATagName_RendersThatTag()
    {
        await OpenAsync(Route);

        Assert.AreEqual("H1", await TagAsync("#tsh-h1"));
        Assert.AreEqual("SPAN", await TagAsync("#tsh-span"));
        Assert.AreEqual("H3", await TagAsync("#tsh-el-padded"), "The value is trimmed.");
        Assert.AreEqual("MY-ELEMENT", await TagAsync("#tsh-el-custom"));
        await Expect(Page.Locator("#tsh-h1")).ToHaveTextAsync("A shimmering heading");

        // A span sits inside the sentence rather than breaking it, and the root has no margin of its own.
        await Expect(Page.Locator("#tsh-sentence")).ToHaveTextAsync("An inline text shimmer in the middle of a sentence.");
        await Expect(Page.Locator("#tsh-h1")).ToHaveCSSAsync("margin-top", "0px");
        var sentence = await Page.Locator("#tsh-sentence").BoundingBoxAsync();
        var span = await Page.Locator("#tsh-span").BoundingBoxAsync();
        Assert.IsTrue(span!.Y >= sentence!.Y - 1 && span.Y + span.Height <= sentence.Y + sentence.Height + 1, "The span is on the sentence's line.");
    }

    [TestMethod]
    public async Task Element_InsideABitText_TakesItsTypography()
    {
        await OpenAsync(Route);

        var typoSize = await ComputedStyleAsync("#tsh-typo", "font-size");
        Assert.AreEqual(typoSize, await ComputedStyleAsync("#tsh-in-text", "font-size"));
        Assert.AreEqual(await ComputedStyleAsync("#tsh-typo", "font-weight"), await ComputedStyleAsync("#tsh-in-text", "font-weight"));
        Assert.AreNotEqual(await ComputedStyleAsync("#tsh-basic", "font-size"), typoSize);
    }

    [TestMethod]
    public async Task Element_NotATagName_FallsBackToAParagraph()
    {
        await OpenAsync(Route);

        foreach (var id in new[] { "tsh-el-blank", "tsh-el-invalid", "tsh-el-digit" })
        {
            Assert.AreEqual("P", await TagAsync($"#{id}"), id);
        }

        await Expect(Page.Locator("#tsh-el-invalid")).Not.ToHaveAttributeAsync("onclick", new Regex(".*"));
        await Expect(Page.Locator("#tsh-el-invalid")).ToHaveTextAsync("Invalid element");
    }

    [TestMethod]
    public async Task Element_AVoidElement_HoldsNoContent()
    {
        await OpenAsync(Route);

        Assert.AreEqual("BR", await TagAsync("#tsh-el-void"));
        Assert.AreEqual(0, await Page.Locator("#tsh-el-void").EvaluateAsync<int>("e => e.childNodes.length"));
        await Expect(Page.GetByText("Never rendered")).ToHaveCountAsync(0);
    }

    // ---------- band ----------

    [DataTestMethod]
    [DataRow("tsh-wide", "95px")]          // 19 characters x 5
    [DataRow("tsh-narrow", "10.5px")]      // 21 characters x 0.5
    [DataRow("tsh-spread-length", "2em")]
    [DataRow("tsh-spread-both", "3ch")]    // SpreadLength wins over Spread, trimmed
    [DataRow("tsh-spread-negative", "0px")]
    [DataRow("tsh-spread-nan", "24px")]    // ignored: 12 characters x the default 2
    [DataRow("tsh-spread-emoji", "4px")]   // two user-perceived characters x 2
    public async Task Band_Spread_SetsTheHalfWidthOfTheBand(string id, string spread)
    {
        await OpenAsync(Route);

        Assert.AreEqual(spread, await CustomPropertyAsync($"#{id}", "--bit-tsh-spread"));

        // The band reaches from its brightest point out by the spread on either side.
        var image = await ComputedStyleAsync($"#{id}", "background-image");
        if (spread.EndsWith("px") && spread != "0px")
        {
            StringAssert.Contains(image, $"50% - {spread}");
        }
    }

    [TestMethod]
    public async Task Band_ASpreadLengthInEm_FollowsTheFontSize()
    {
        await OpenAsync(Route);

        // An h3 of a 2em band: the stop sits two font sizes off the middle, whatever the length of the text.
        var fontSize = ParsePx(await ComputedStyleAsync("#tsh-spread-length", "font-size"));
        var image = await ComputedStyleAsync("#tsh-spread-length", "background-image");
        StringAssert.Contains(image, $"50% - {(fontSize * 2).ToString("0.###", CultureInfo.InvariantCulture)}px");
    }

    [DataTestMethod]
    [DataRow("tsh-angle", "115deg")]
    [DataRow("tsh-angle-negative", "65deg")]
    [DataRow("tsh-angle-nan", "90deg")]
    [DataRow("tsh-rtl-angle", "65deg")]  // mirrored in right-to-left text
    public async Task Band_Angle_TiltsTheBand(string id, string gradientAngle)
    {
        await OpenAsync(Route);

        StringAssert.StartsWith(await ComputedStyleAsync($"#{id}", "background-image"), $"linear-gradient({gradientAngle}");
    }

    // ---------- content ----------

    [TestMethod]
    public async Task Content_ChildContent_WinsOverTextAndIsScaledByContentLength()
    {
        await OpenAsync(Route);

        var shimmer = Page.Locator("#tsh-child");
        await Expect(shimmer).ToHaveTextAsync(new Regex(@"^\s*Thinking really hard about it\.\.\.\s*$"));
        await Expect(shimmer.Locator("#tsh-child-strong")).ToHaveTextAsync("really");
        await Expect(Page.GetByText("Never shown")).ToHaveCountAsync(0);

        // The glyphs of the markup inside are painted by the band too.
        await Expect(Page.Locator("#tsh-child-strong")).ToHaveCSSAsync("-webkit-text-fill-color", "rgba(0, 0, 0, 0)");

        Assert.AreEqual("60px", await CustomPropertyAsync("#tsh-child", "--bit-tsh-spread"));
        Assert.AreEqual("20px", await CustomPropertyAsync("#tsh-child-default", "--bit-tsh-spread"));
        Assert.AreEqual("0px", await CustomPropertyAsync("#tsh-child-negative", "--bit-tsh-spread"));
        Assert.AreEqual("3em", await CustomPropertyAsync("#tsh-child-icon", "--bit-tsh-spread"));
    }

    [TestMethod]
    public async Task Content_APartWithItsOwnFill_KeepsItsColors()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#tsh-child-icon .bit-icon")).ToHaveCountAsync(1);
        var color = await ComputedStyleAsync("#tsh-emoji", "color");
        await Expect(Page.Locator("#tsh-emoji")).ToHaveCSSAsync("-webkit-text-fill-color", color);
        Assert.AreNotEqual("rgba(0, 0, 0, 0)", color);
    }

    // ---------- timing ----------

    [DataTestMethod]
    [DataRow("tsh-slow", 4000d, 0d)]
    [DataRow("tsh-fast", 750d, 0d)]
    [DataRow("tsh-repeat", 4000d, 0d)]             // the default 2s sweep doubled by 2s of rest
    [DataRow("tsh-repeat-duration", 4000d, 0d)]    // a 1s sweep and 3s of rest
    [DataRow("tsh-delay-0", 2000d, 0d)]
    [DataRow("tsh-delay-250", 2000d, 250d)]
    [DataRow("tsh-delay-500", 2000d, 500d)]
    [DataRow("tsh-negative-timing", 0d, 0d)]
    public async Task Timing_DurationRepeatDelayAndDelay_TimeTheSweeps(string id, double duration, double delay)
    {
        await OpenAsync(Route);

        var animation = await AnimationAsync($"#{id}");
        Assert.IsNotNull(animation);
        Assert.AreEqual(duration, animation.Value.GetProperty("duration").GetDouble(), 1);
        Assert.AreEqual(delay, animation.Value.GetProperty("delay").GetDouble(), 0.5);
    }

    [TestMethod]
    public async Task Timing_RepeatDelay_KeepsTheSpeedOfTheBandAndWidensTheBackground()
    {
        await OpenAsync(Route);

        // The cycle is two sweeps long: the background is three widths of the text rather than two, so the band
        // crosses the text at the same speed and rests outside it for the rest.
        Assert.AreEqual("300% 100%", await ComputedStyleAsync("#tsh-repeat", "background-size"));
        Assert.AreEqual("200% 100%", await ComputedStyleAsync("#tsh-basic", "background-size"));
        Assert.AreEqual("500% 100%", await ComputedStyleAsync("#tsh-repeat-duration", "background-size"));
    }

    // ---------- direction & iterations ----------

    [DataTestMethod]
    [DataRow("tsh-basic", "normal")]
    [DataRow("tsh-reversed", "reverse")]
    [DataRow("tsh-alternate", "alternate")]
    [DataRow("tsh-alt-rev", "alternate-reverse")]
    [DataRow("tsh-rtl", "reverse")]
    [DataRow("tsh-rtl-rev", "normal")]
    [DataRow("tsh-rtl-inherited", "reverse")]
    [DataRow("tsh-rtl-inherited-rev", "normal")]
    [DataRow("tsh-rtl-inherited-alt", "alternate-reverse")]
    [DataRow("tsh-rtl-inherited-alt-rev", "alternate")]
    [DataRow("tsh-ltr-in-rtl", "normal")]
    public async Task Direction_ReversedAlternateAndTheReadingDirection_DecideTheWayTheBandRuns(string id, string direction)
    {
        await OpenAsync(Route);

        await Expect(Page.Locator($"#{id}")).ToHaveCSSAsync("animation-direction", direction);
        var animation = await AnimationAsync($"#{id}");
        Assert.AreEqual(direction, animation!.Value.GetProperty("direction").GetString());
    }

    [TestMethod]
    public async Task Direction_ReversedAndAlternate_AddTheirClasses()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#tsh-reversed")).ToHaveClassAsync(HasClass("bit-tsh-rev"));
        await Expect(Page.Locator("#tsh-alternate")).ToHaveClassAsync(HasClass("bit-tsh-alt"));
        await Expect(Page.Locator("#tsh-alt-rev")).ToHaveClassAsync(HasClass("bit-tsh-alt"));
        await Expect(Page.Locator("#tsh-alt-rev")).ToHaveClassAsync(HasClass("bit-tsh-rev"));
        await Expect(Page.Locator("#tsh-basic")).Not.ToHaveClassAsync(HasClass("bit-tsh-rev"));
    }

    [TestMethod]
    public async Task Iterations_ASingleSweep_PlaysOnceAndThenRests()
    {
        await OpenAsync(Route);

        var once = await AnimationAsync("#tsh-once");
        Assert.AreEqual("1", once!.Value.GetProperty("iterations").GetString());
        Assert.AreEqual("3", (await AnimationAsync("#tsh-thrice"))!.Value.GetProperty("iterations").GetString());
        Assert.AreEqual("Infinity", (await AnimationAsync("#tsh-iter-zero"))!.Value.GetProperty("iterations").GetString(), "Iterations below one is ignored.");

        // The single 600ms sweep finishes and leaves the band where it started its rest, outside the text.
        await Page.WaitForFunctionAsync(
            "() => document.getElementById('tsh-once').getAnimations().every(a => a.playState === 'finished')");
        var restingPosition = await ComputedStyleAsync("#tsh-once", "background-position-x");
        await NextFramesAsync();
        Assert.AreEqual(restingPosition, await ComputedStyleAsync("#tsh-once", "background-position-x"));

        // A replay (a new instance through its @key) plays the sweep again.
        await Page.Locator("#btn-replay").ClickAsync();
        await Expect(Page.Locator("#replay-count")).ToHaveTextAsync("1");
        await Page.WaitForFunctionAsync(
            "() => document.getElementById('tsh-once').getAnimations().some(a => a.playState === 'running')");
    }

    // ---------- playback ----------

    [TestMethod]
    public async Task Paused_Toggled_HoldsTheBandAndResumesFromWhereItWas()
    {
        await OpenAsync(Route);

        var shimmer = Page.Locator("#tsh-paused");
        await Expect(shimmer).Not.ToHaveClassAsync(HasClass("bit-tsh-pau"));
        await Page.WaitForFunctionAsync("() => document.getElementById('tsh-paused').getAnimations()[0].currentTime > 300");

        await Page.Locator("#btn-toggle-paused").ClickAsync();
        await Expect(Page.Locator("#paused-state")).ToHaveTextAsync("True");
        await Expect(shimmer).ToHaveClassAsync(HasClass("bit-tsh-pau"));
        await Expect(shimmer).ToHaveCSSAsync("animation-play-state", "paused");
        await Page.WaitForFunctionAsync("() => document.getElementById('tsh-paused').getAnimations()[0].playState === 'paused'");

        var heldAt = (await AnimationAsync("#tsh-paused"))!.Value.GetProperty("current").GetDouble();
        var heldPosition = await ComputedStyleAsync("#tsh-paused", "background-position-x");
        await NextFramesAsync();
        await NextFramesAsync();
        Assert.AreEqual(heldAt, (await AnimationAsync("#tsh-paused"))!.Value.GetProperty("current").GetDouble(), 0.001);
        Assert.AreEqual(heldPosition, await ComputedStyleAsync("#tsh-paused", "background-position-x"));

        await Page.Locator("#btn-toggle-paused").ClickAsync();
        await Expect(shimmer).ToHaveCSSAsync("animation-play-state", "running");
        var resumed = (await AnimationAsync("#tsh-paused"))!.Value.GetProperty("current").GetDouble();
        Assert.IsTrue(resumed >= heldAt - 0.01, $"The sweep carries on from {heldAt}, was {resumed}.");
        await WaitForBackgroundToMoveAsync("#tsh-paused");
    }

    [TestMethod]
    public async Task PauseOnHover_ThePointerOverIt_HoldsTheBandUntilItLeaves()
    {
        await OpenAsync(Route);

        var shimmer = Page.Locator("#tsh-hover");
        await Expect(shimmer).ToHaveClassAsync(HasClass("bit-tsh-poh"));
        await Expect(shimmer).ToHaveCSSAsync("animation-play-state", "running");

        await shimmer.HoverAsync();
        await Expect(shimmer).ToHaveCSSAsync("animation-play-state", "paused");

        await Page.Mouse.MoveAsync(0, 0);
        await Expect(shimmer).ToHaveCSSAsync("animation-play-state", "running");
    }

    [TestMethod]
    public async Task PauseOnHover_TheFocusInsideIt_HoldsTheBandUntilItLeaves()
    {
        await OpenAsync(Route);

        var shimmer = Page.Locator("#tsh-hover-focus");
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(shimmer).ToHaveCSSAsync("animation-play-state", "running");

        await Page.Locator("#tsh-hover-button").FocusAsync();
        await Expect(shimmer).ToHaveCSSAsync("animation-play-state", "paused");

        await Page.Locator("#tsh-hover-button").BlurAsync();
        await Expect(shimmer).ToHaveCSSAsync("animation-play-state", "running");
    }

    [TestMethod]
    public async Task Static_Toggled_TakesTheBandAwayAndRestartsTheSweeps()
    {
        await OpenAsync(Route);

        var shimmer = Page.Locator("#tsh-static");
        await Page.WaitForFunctionAsync("() => document.getElementById('tsh-static').getAnimations()[0]?.currentTime > 1000");

        await Page.Locator("#btn-toggle-static").ClickAsync();
        await Expect(shimmer).ToHaveClassAsync(HasClass("bit-tsh-sta"));
        await Expect(shimmer).ToHaveCSSAsync("animation-name", "none");
        await Expect(shimmer).ToHaveCSSAsync("background-image", "none");
        var color = await ComputedStyleAsync("#tsh-static", "color");
        await Expect(shimmer).ToHaveCSSAsync("-webkit-text-fill-color", color);
        Assert.AreEqual(await ResolveColorAsync("var(--bit-clr-fg-ter)"), color);

        await Page.Locator("#btn-toggle-static").ClickAsync();
        await Expect(shimmer).Not.ToHaveClassAsync(HasClass("bit-tsh-sta"));
        await Expect(shimmer).ToHaveCSSAsync("animation-name", "bit-tsh-anim");
        var restarted = (await AnimationAsync("#tsh-static"))!.Value.GetProperty("current").GetDouble();
        Assert.IsTrue(restarted < 1000, $"The sweeps start from the beginning, the time was {restarted}.");

        // A static shimmer is drawn in its own BaseColor.
        await Expect(Page.Locator("#tsh-static-base")).ToHaveCSSAsync("color", "rgb(10, 20, 30)");
        await Expect(Page.Locator("#tsh-static-base")).ToHaveCSSAsync("-webkit-text-fill-color", "rgb(10, 20, 30)");
    }

    // ---------- disabled ----------

    [TestMethod]
    public async Task Disabled_TheShimmer_IsDimmedAndAtRestUntilEnabled()
    {
        await OpenAsync(Route);

        var shimmer = Page.Locator("#tsh-disabled");
        await Expect(shimmer).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(shimmer).ToHaveCSSAsync("opacity", "0.5");
        await Expect(shimmer).ToHaveCSSAsync("animation-name", "none");
        var color = await ComputedStyleAsync("#tsh-disabled", "color");
        await Expect(shimmer).ToHaveCSSAsync("-webkit-text-fill-color", color);

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(shimmer).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(shimmer).ToHaveCSSAsync("opacity", "1");
        await Expect(shimmer).ToHaveCSSAsync("animation-name", "bit-tsh-anim");

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(shimmer).ToHaveClassAsync(HasClass("bit-dis"));
    }

    [TestMethod]
    public async Task Disabled_TheOpacityVariable_DimsItAsAsked()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#tsh-disabled-var")).ToHaveCSSAsync("opacity", "0.25");
    }

    // ---------- live region ----------

    [TestMethod]
    public async Task Thinking_SplattedRoleStatus_AnnouncesTheTextAndStopsTheBandOnTheAnswer()
    {
        await OpenAsync(Route);

        var shimmer = Page.Locator("#tsh-thinking");
        await Expect(shimmer).ToHaveAttributeAsync("role", "status");
        await Expect(shimmer).ToHaveAttributeAsync("aria-live", "polite");
        await Expect(shimmer).ToHaveTextAsync("Ask a question to see the assistant think.");
        await Expect(shimmer).ToHaveClassAsync(HasClass("bit-tsh-sta"));
        await Expect(Page.GetByRole(AriaRole.Status)).ToHaveTextAsync("Ask a question to see the assistant think.");

        await Page.Locator("#btn-ask").ClickAsync();
        await Expect(shimmer).ToHaveTextAsync("Thinking about your question...");
        await Expect(shimmer).Not.ToHaveClassAsync(HasClass("bit-tsh-sta"));
        await Expect(shimmer).ToHaveCSSAsync("animation-name", "bit-tsh-anim");
        // 30 characters' sweep of 2s with 1s of rest after it.
        Assert.AreEqual(3000, (await AnimationAsync("#tsh-thinking"))!.Value.GetProperty("duration").GetDouble(), 1);

        await Page.Locator("#btn-answer").ClickAsync();
        await Expect(shimmer).ToHaveTextAsync("Here is your answer.");
        await Expect(shimmer).ToHaveClassAsync(HasClass("bit-tsh-sta"));
        await Expect(shimmer).ToHaveCSSAsync("animation-name", "none");
    }

    // ---------- cascading parameters ----------

    [TestMethod]
    public async Task Cascading_BitParams_SetTheDefaultsAndTheShimmersOwnParametersWin()
    {
        await OpenAsync(Route);

        // Duration 3000 with a RepeatDelay of 1000: a cycle of 4 seconds, and the 3em band of the params.
        foreach (var id in new[] { "tsh-cas-1", "tsh-cas-2" })
        {
            Assert.AreEqual(4000, (await AnimationAsync($"#{id}"))!.Value.GetProperty("duration").GetDouble(), 1, id);
            Assert.AreEqual("3em", await CustomPropertyAsync($"#{id}", "--bit-tsh-spread"), id);
        }

        // Its own Duration of 1000 with the params' rest of 1000: a cycle of 2 seconds.
        Assert.AreEqual(2000, (await AnimationAsync("#tsh-cas-own"))!.Value.GetProperty("duration").GetDouble(), 1);

        // A Spread set on the shimmer wins over the params' SpreadLength: 14 characters x 1.
        Assert.AreEqual("14px", await CustomPropertyAsync("#tsh-cas-own-spread", "--bit-tsh-spread"));
    }

    [TestMethod]
    public async Task Cascading_APausedOfTheParams_PausesTheWholeRegion()
    {
        await OpenAsync(Route);

        foreach (var id in new[] { "tsh-cas-1", "tsh-cas-2", "tsh-cas-own", "tsh-cas-own-paused" })
        {
            await Expect(Page.Locator($"#{id}")).ToHaveCSSAsync("animation-play-state", "running");
        }

        await Page.Locator("#btn-pause-all").ClickAsync();

        foreach (var id in new[] { "tsh-cas-1", "tsh-cas-2", "tsh-cas-own" })
        {
            await Expect(Page.Locator($"#{id}")).ToHaveClassAsync(HasClass("bit-tsh-pau"));
            await Expect(Page.Locator($"#{id}")).ToHaveCSSAsync("animation-play-state", "paused");
        }

        // Its own Paused="false" wins over the region's switch.
        await Expect(Page.Locator("#tsh-cas-own-paused")).ToHaveCSSAsync("animation-play-state", "running");

        await Page.Locator("#btn-pause-all").ClickAsync();
        await Expect(Page.Locator("#tsh-cas-1")).ToHaveCSSAsync("animation-play-state", "running");
    }

    [TestMethod]
    public async Task Cascading_AGradientColorOfTheParams_GivesWayToAColorOfTheShimmer()
    {
        await OpenAsync(Route);

        StringAssert.Contains(await ComputedStyleAsync("#tsh-cas-gradient", "background-image"), "rgb(11, 22, 33)");
        Assert.AreEqual("H4", await TagAsync("#tsh-cas-gradient"));
        Assert.AreEqual("H4", await TagAsync("#tsh-cas-color-element"));

        await Expect(Page.Locator("#tsh-cas-own-color")).ToHaveClassAsync(HasClass("bit-tsh-err"));
        var image = await ComputedStyleAsync("#tsh-cas-own-color", "background-image");
        StringAssert.Contains(image, await ResolveColorAsync("var(--bit-clr-err)"));
        Assert.IsFalse(image.Contains("rgb(11, 22, 33)"), image);
    }

    // ---------- color ----------

    [DataTestMethod]
    [DataRow("primary", "pri", "--bit-clr-pri")]
    [DataRow("secondary", "sec", "--bit-clr-sec")]
    [DataRow("tertiary", "ter", "--bit-clr-ter")]
    [DataRow("info", "inf", "--bit-clr-inf")]
    [DataRow("success", "suc", "--bit-clr-suc")]
    [DataRow("warning", "wrn", "--bit-clr-wrn")]
    [DataRow("severewarning", "swr", "--bit-clr-swr")]
    [DataRow("error", "err", "--bit-clr-err")]
    [DataRow("primarybackground", "pbg", "--bit-clr-bg-pri")]
    [DataRow("secondarybackground", "sbg", "--bit-clr-bg-sec")]
    [DataRow("tertiarybackground", "tbg", "--bit-clr-bg-ter")]
    [DataRow("primaryforeground", "pfg", "--bit-clr-fg-pri")]
    [DataRow("secondaryforeground", "sfg", "--bit-clr-fg-sec")]
    [DataRow("tertiaryforeground", "tfg", "--bit-clr-fg-ter")]
    [DataRow("primaryborder", "pbr", "--bit-clr-brd-pri")]
    [DataRow("secondaryborder", "sbr", "--bit-clr-brd-sec")]
    [DataRow("tertiaryborder", "tbr", "--bit-clr-brd-ter")]
    public async Task Color_EachRole_PaintsTheBandInThatRole(string name, string suffix, string token)
    {
        await OpenAsync(Route);

        var id = $"#tsh-color-{name}";
        await Expect(Page.Locator(id)).ToHaveClassAsync(HasClass($"bit-tsh-{suffix}"));

        var expected = await ResolveColorAsync($"var({token})");
        Assert.AreEqual(expected, await ResolveColorInAsync(id, "var(--bit-tsh-gradient-clr)"));
        StringAssert.Contains(await ComputedStyleAsync(id, "background-image"), expected);

        // The text still rests in the dim foreground.
        await Expect(Page.Locator(id)).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-fg-ter)"));
    }

    [TestMethod]
    public async Task Color_BaseAndGradientColors_PaintTheTextAndTheBand()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#tsh-ocean")).ToHaveCSSAsync("color", "rgb(100, 116, 139)");
        await Expect(Page.Locator("#tsh-ocean")).ToHaveCSSAsync("background-color", "rgb(100, 116, 139)");
        StringAssert.Contains(await ComputedStyleAsync("#tsh-ocean", "background-image"), "rgb(34, 211, 238)");

        // GradientColor wins over Color.
        await Expect(Page.Locator("#tsh-gradient-wins")).ToHaveClassAsync(HasClass("bit-tsh-err"));
        StringAssert.Contains(await ComputedStyleAsync("#tsh-gradient-wins", "background-image"), "rgb(1, 2, 3)");
    }

    [TestMethod]
    public async Task Color_ChangedAtRuntime_RepaintsTheBand()
    {
        await OpenAsync(Route);

        var shimmer = Page.Locator("#tsh-color-runtime");
        await Expect(shimmer).ToHaveClassAsync(HasClass("bit-tsh-pri"));

        await Page.Locator("#btn-color").ClickAsync();

        await Expect(shimmer).ToHaveClassAsync(HasClass("bit-tsh-suc"));
        await Expect(shimmer).Not.ToHaveClassAsync(HasClass("bit-tsh-pri"));
        StringAssert.Contains(await ComputedStyleAsync("#tsh-color-runtime", "background-image"), await ResolveColorAsync("var(--bit-clr-suc)"));
    }

    // ---------- style, class, variables ----------

    [TestMethod]
    public async Task StyleAndClass_TheRoot_TakesThem()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#tsh-styled")).ToHaveCSSAsync("font-size", "32px");
        await Expect(Page.Locator("#tsh-styled")).ToHaveCSSAsync("font-weight", "700");
        await Expect(Page.Locator("#tsh-classy")).ToHaveClassAsync(HasClass("e2e-tsh-class"));
        await Expect(Page.Locator("#tsh-classy")).ToHaveClassAsync(HasClass("bit-tsh"));
    }

    [TestMethod]
    public async Task HtmlAttributes_Splatted_AreMergedWithTheComponentsOwn()
    {
        await OpenAsync(Route);

        var shimmer = Page.Locator("#tsh-html-attrs");
        await Expect(shimmer).ToHaveClassAsync(HasClass("from-class"));
        await Expect(shimmer).ToHaveClassAsync(HasClass("from-attrs"));
        await Expect(shimmer).ToHaveClassAsync(HasClass("bit-tsh"));
        await Expect(shimmer).ToHaveCSSAsync("color", "rgb(255, 0, 0)");
        await Expect(shimmer).ToHaveCSSAsync("letter-spacing", "3px");
        await Expect(shimmer).ToHaveAttributeAsync("data-test", "splatted");
        await Expect(shimmer).ToHaveAttributeAsync("title", "A title");

        await Expect(Page.Locator("#tsh-splatted-id")).ToHaveClassAsync(HasClass("bit-tsh"));
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheShimmer()
    {
        await OpenAsync(Route);

        var shimmer = Page.Locator("#tsh-var");
        await Expect(shimmer).ToHaveCSSAsync("color", "rgb(5, 6, 7)");
        var image = await ComputedStyleAsync("#tsh-var", "background-image");
        StringAssert.Contains(image, "rgb(255, 105, 180)");
        StringAssert.StartsWith(image, "linear-gradient(105deg");
        Assert.AreEqual("2em", await CustomPropertyAsync("#tsh-var", "--bit-tsh-spread"));

        var animation = (await AnimationAsync("#tsh-var"))!.Value;
        Assert.AreEqual(6000, animation.GetProperty("duration").GetDouble(), 1, "A 3s sweep and 3s of rest.");
        Assert.AreEqual(400, animation.GetProperty("delay").GetDouble(), 0.5);
        Assert.AreEqual("4", animation.GetProperty("iterations").GetString());
    }

    [TestMethod]
    public async Task CssVariables_TheShimmersOwnParameters_WinOverThem()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#tsh-var-own")).ToHaveCSSAsync("color", "rgb(9, 9, 9)");
        var image = await ComputedStyleAsync("#tsh-var-own", "background-image");
        StringAssert.Contains(image, "rgb(0, 191, 255)");
        Assert.IsFalse(image.Contains("rgb(255, 105, 180)"), image);
        StringAssert.StartsWith(image, "linear-gradient(95deg");
        Assert.AreEqual("22px", await CustomPropertyAsync("#tsh-var-own", "--bit-tsh-spread"));

        var animation = (await AnimationAsync("#tsh-var-own"))!.Value;
        Assert.AreEqual(3000, animation.GetProperty("duration").GetDouble(), 1, "A 1.5s sweep and 1.5s of rest.");
        Assert.AreEqual(100, animation.GetProperty("delay").GetDouble(), 0.5);
        Assert.AreEqual("2", animation.GetProperty("iterations").GetString());
    }

    [TestMethod]
    public async Task CssVariables_ANestedShimmer_StartsFromThePublicVariablesNotTheOuterParameters()
    {
        await OpenAsync(Route);

        StringAssert.Contains(await ComputedStyleAsync("#tsh-var-nested-outer", "background-image"), "rgb(0, 128, 0)");
        var inner = await ComputedStyleAsync("#tsh-var-nested-inner", "background-image");
        StringAssert.Contains(inner, "rgb(255, 105, 180)");
        Assert.IsFalse(inner.Contains("rgb(0, 128, 0)"), inner);
    }

    // ---------- accessibility & base ----------

    [TestMethod]
    public async Task AriaLabel_TheShimmer_IsNamedByIt()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#tsh-aria")).ToHaveAttributeAsync("aria-label", "Assistant is thinking");
        await Expect(Page.Locator("#tsh-basic")).Not.ToHaveAttributeAsync("aria-label", new Regex(".*"));
    }

    [TestMethod]
    public async Task TabIndex_AFocusableShimmer_DrawsTheFocusRingFromTheKeyboardOnly()
    {
        await OpenAsync(Route);

        var shimmer = Page.Locator("#tsh-tab");
        await Expect(shimmer).ToHaveAttributeAsync("tabindex", "0");
        await Page.Locator("#tsh-aria").EvaluateAsync("e => { e.tabIndex = -1; e.focus(); }");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(shimmer).ToBeFocusedAsync();
        Assert.AreNotEqual("none", await ComputedStyleAsync("#tsh-tab", "box-shadow"));

        await Page.Locator("#tsh-tab-script").FocusAsync();
        await Expect(Page.Locator("#tsh-tab-script")).ToBeFocusedAsync();
        Assert.AreEqual("none", await ComputedStyleAsync("#tsh-tab-script", "box-shadow"));
    }

    [TestMethod]
    public async Task Visibility_Cycled_HidesAndCollapsesTheShimmer()
    {
        await OpenAsync(Route);

        var shimmer = Page.Locator("#tsh-visibility");
        await Expect(shimmer).ToBeVisibleAsync();

        await Page.Locator("#btn-visibility").ClickAsync();
        await Expect(Page.Locator("#visibility-state")).ToHaveTextAsync("Hidden");
        await Expect(shimmer).ToHaveCSSAsync("visibility", "hidden");
        Assert.IsTrue((await shimmer.BoundingBoxAsync())!.Height > 0, "A hidden shimmer keeps its room.");

        await Page.Locator("#btn-visibility").ClickAsync();
        await Expect(Page.Locator("#visibility-state")).ToHaveTextAsync("Collapsed");
        await Expect(shimmer).ToHaveCSSAsync("display", "none");

        await Page.Locator("#btn-visibility").ClickAsync();
        await Expect(shimmer).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Rtl_AnExplicitDir_RendersTheDirectionAndMirrorsTheBand()
    {
        await OpenAsync(Route);

        var shimmer = Page.Locator("#tsh-rtl");
        await Expect(shimmer).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(shimmer).ToHaveAttributeAsync("dir", "rtl");
        await Expect(shimmer).ToHaveCSSAsync("direction", "rtl");
        Assert.AreEqual("-1", (await CustomPropertyAsync("#tsh-rtl", "--bit-tsh-mirror")).Trim());
        Assert.AreEqual("-1", (await CustomPropertyAsync("#tsh-rtl-inherited", "--bit-tsh-mirror")).Trim());
        Assert.AreEqual("1", (await CustomPropertyAsync("#tsh-ltr-in-rtl", "--bit-tsh-mirror")).Trim());
        await Expect(Page.Locator("#tsh-ltr-in-rtl")).ToHaveAttributeAsync("dir", "ltr");
    }

    // ---------- lifecycle ----------

    [TestMethod]
    public async Task Prerender_ThePagesHtml_ContainsTheShimmerBeforeTheCircuitConnects()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Matches(html, new Regex(@"<p id=""tsh-basic""[^>]*class=""[^""]*\bbit-tsh\b[^>]*>Thinking about your question\.\.\.</p>"));
        StringAssert.Matches(html, new Regex(@"<h1 id=""tsh-h1"""));
        StringAssert.Contains(html, "--bit-tsh-spread:95px");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Prerender_WithoutTheCircuit_TheShimmerAlreadySweeps()
    {
        // Pure CSS: with Blazor's script blocked the prerendered shimmer is already animated.
        await Page.RouteAsync("**/_framework/blazor.web.js", route => route.FulfillAsync(new() { Status = 200, ContentType = "text/javascript", Body = "" }));
        await Page.GotoAsync($"{BaseUrl}{Route}");

        await Expect(Page.Locator("#status")).ToHaveTextAsync("Loading");
        var animation = await AnimationAsync("#tsh-basic");
        Assert.IsNotNull(animation);
        Assert.AreEqual("running", animation.Value.GetProperty("state").GetString());
    }

    [TestMethod]
    public async Task Parameters_ChangedAtRuntime_ReRenderTheShimmer()
    {
        await OpenAsync(Route);

        var shimmer = Page.Locator("#tsh-rerender");
        await Expect(shimmer).ToHaveTextAsync("Before");
        Assert.AreEqual("P", await TagAsync("#tsh-rerender"));

        await Page.Locator("#btn-rerender").ClickAsync();

        await Expect(shimmer).ToHaveTextAsync("After");
        Assert.AreEqual("H3", await TagAsync("#tsh-rerender"));
        await Expect(shimmer).ToHaveClassAsync(HasClass("bit-tsh-rev"));
        await Expect(shimmer).ToHaveCSSAsync("animation-direction", "reverse");
        Assert.AreEqual(5000, (await AnimationAsync("#tsh-rerender"))!.Value.GetProperty("duration").GetDouble(), 1);
        Assert.AreEqual("10px", await CustomPropertyAsync("#tsh-rerender", "--bit-tsh-spread"));
    }

    [TestMethod]
    public async Task Unmount_TheShimmer_LeavesNoErrorBehind()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#tsh-mounted")).ToHaveCountAsync(1);
        await Page.Locator("#btn-unmount").ClickAsync();
        await Expect(Page.Locator("#tsh-mounted")).ToHaveCountAsync(0);
        await Page.Locator("#btn-unmount").ClickAsync();
        await Expect(Page.Locator("#tsh-mounted")).ToHaveTextAsync("Mounted shimmer");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    // ---------- helpers ----------

    private Task<string> TagAsync(string selector) => Page.Locator(selector).EvaluateAsync<string>("e => e.tagName");

    private Task<string> CustomPropertyAsync(string selector, string name)
        => Page.Locator(selector).EvaluateAsync<string>($"e => getComputedStyle(e).getPropertyValue('{name}').trim()");

    private async Task<JsonElement?> AnimationAsync(string selector)
    {
        var result = await Page.Locator(selector).EvaluateAsync<JsonElement>(
            """
            e => {
                const a = e.getAnimations().find(x => x.animationName === 'bit-tsh-anim');
                if (!a) return null;
                const t = a.effect.getComputedTiming();
                return { state: a.playState, duration: Number(t.duration), delay: t.delay, iterations: String(t.iterations), direction: t.direction, current: Number(a.currentTime ?? 0) };
            }
            """);

        return result.ValueKind == JsonValueKind.Null ? null : result;
    }

    private async Task WaitForBackgroundToMoveAsync(string selector)
    {
        var first = await ComputedStyleAsync(selector, "background-position-x");

        await Page.WaitForFunctionAsync(
            "([sel, first]) => getComputedStyle(document.querySelector(sel)).backgroundPositionX !== first",
            new[] { selector, first });
    }

    private Task<string> ResolveColorAsync(string cssColor)
    {
        return Page.EvaluateAsync<string>(
            """
            value => {
                const probe = document.createElement('div');
                probe.style.color = value;
                document.body.appendChild(probe);
                const resolved = getComputedStyle(probe).color;
                probe.remove();
                return resolved;
            }
            """, cssColor);
    }

    // Resolves a color inside an element, so the custom properties it declares are read the way its own rules read them.
    private Task<string> ResolveColorInAsync(string selector, string cssColor)
    {
        return Page.Locator(selector).EvaluateAsync<string>(
            """
            (e, value) => {
                const probe = document.createElement('span');
                probe.style.color = value;
                e.appendChild(probe);
                const resolved = getComputedStyle(probe).color;
                probe.remove();
                return resolved;
            }
            """, cssColor);
    }

    private static double ParsePx(string value) => double.Parse(value.Replace("px", "").Trim(), CultureInfo.InvariantCulture);
}
