using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiAgentStepView at /e2e/aiagentstepview in a real browser: a step in every state (its class, the marker
/// the theme paints for it, the check of a completed one, the words beside it), the line of the timeline, a step the
/// page moves through its states (followed through its own Changed event), the tool calls it made as tags (by name
/// and state, a running one with its dots, the AI font's wrench), the pulse and reduced motion, every
/// --bit-AiAgentStepView-* variable, Styles and Classes, RTL and Disabled.
/// </summary>
[TestClass]
public class BitAiAgentStepViewE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aiagentstepview";

    private ILocator Step(string id) => Page.Locator($"#{id}");

    private ILocator Marker(string id) => Page.Locator($"#{id} > .bit-aiasv-mrk");

    // ---------- states ----------

    [TestMethod]
    public async Task States_EachStatus_HasItsClass()
    {
        await OpenAsync(Route);

        foreach (var (id, cls) in new[]
        {
            ("asv-pending", "bit-aiasv-pnd"), ("asv-running", "bit-aiasv-run"), ("asv-completed", "bit-aiasv-cmp"),
            ("asv-blocked", "bit-aiasv-blk"), ("asv-failed", "bit-aiasv-fld"), ("asv-skipped", "bit-aiasv-skp"),
        })
        {
            await Expect(Step(id)).ToHaveClassAsync(HasClass("bit-aiasv"));
            await Expect(Step(id)).ToHaveClassAsync(HasClass(cls));
        }
    }

    [TestMethod]
    public async Task States_TheMarker_IsPaintedInTheColorOfTheState()
    {
        await OpenAsync(Route);

        var background = await Page.ResolveColorAsync("var(--bit-clr-bg-pri)");
        await Expect(Marker("asv-pending")).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-brd-pri)"));
        await Expect(Marker("asv-pending")).ToHaveCSSAsync("background-color", background);
        await Expect(Marker("asv-skipped")).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-brd-pri)"));
        await Expect(Marker("asv-running")).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-pri)"));
        await Expect(Marker("asv-running")).ToHaveCSSAsync("background-color", background);

        var success = await Page.ResolveColorAsync("var(--bit-clr-suc)");
        await Expect(Marker("asv-completed")).ToHaveCSSAsync("border-top-color", success);
        await Expect(Marker("asv-completed")).ToHaveCSSAsync("background-color", success);
        await Expect(Marker("asv-blocked")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-wrn)"));
        await Expect(Marker("asv-failed")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-err)"));

        // A round marker the size of an icon, and only a completed step carries the check.
        await Expect(Marker("asv-pending")).ToHaveCSSAsync("border-top-left-radius", new Regex(@"^(9999|50%|\d{3,})"));
        await Expect(Marker("asv-pending")).ToHaveCSSAsync("width", await Step("asv-pending").ResolveLengthInAsync("var(--bit-siz-icon-md)"));
        await Expect(Page.Locator("#asv-states .bit-aiasv-mic")).ToHaveCountAsync(1);
        var check = Page.Locator("#asv-completed > .bit-aiasv-mrk > .bit-aiasv-mic");
        await Expect(check).ToBeVisibleAsync();
        await Expect(check).ToHaveClassAsync(HasClass("bit-icon--Accept"));
        await Expect(check).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-suc-text)"));
    }

    [TestMethod]
    public async Task States_TheWords_SayTheTitleTheTimeTheDetailTheSummaryAndTheError()
    {
        await OpenAsync(Route);

        await Expect(Step("asv-pending").Locator(".bit-aiasv-ttl")).ToHaveTextAsync("Pending step");
        await Expect(Step("asv-pending").Locator(".bit-aiasv-dtl")).ToHaveTextAsync("Waits its turn");
        await Expect(Step("asv-pending").Locator(".bit-aiasv-dur")).ToHaveCountAsync(0);

        // A completed step says how long it took, a running one how long it has been running.
        await Expect(Step("asv-completed").Locator(".bit-aiasv-dur")).ToHaveTextAsync(new Regex(@"^\d+ms$"));
        await Expect(Step("asv-completed").Locator(".bit-aiasv-smr")).ToHaveTextAsync("3 sources found");
        await Expect(Step("asv-running").Locator(".bit-aiasv-dur")).ToHaveTextAsync(new Regex(@"^(\d+ms|\d+(\.\d)?s)$"));

        // Drawn on its own, a running step's time still moves on: its clock re-renders it.
        var running = await Step("asv-running").Locator(".bit-aiasv-dur").TextContentAsync();
        await Expect(Step("asv-running").Locator(".bit-aiasv-dur")).Not.ToHaveTextAsync(running!);

        // The status the marker draws is said in words to assistive technologies, out of sight.
        await Expect(Step("asv-failed").Locator(".bit-aiasv-sts")).ToHaveTextAsync("Failed");
        await Expect(Step("asv-failed").Locator(".bit-aiasv-sts")).ToHaveCSSAsync("position", "absolute");

        await Expect(Step("asv-failed").Locator(".bit-aiasv-err")).ToHaveTextAsync("The service timed out");
        await Expect(Step("asv-failed").Locator(".bit-aiasv-err")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-err-fg)"));

        // A step that has not run, or will not, says so in the secondary text color; the others in the primary one.
        var secondary = await Page.ResolveColorAsync("var(--bit-clr-fg-sec)");
        await Expect(Step("asv-pending").Locator(".bit-aiasv-ttl")).ToHaveCSSAsync("color", secondary);
        await Expect(Step("asv-skipped").Locator(".bit-aiasv-ttl")).ToHaveCSSAsync("color", secondary);
        await Expect(Step("asv-completed").Locator(".bit-aiasv-ttl")).Not.ToHaveCSSAsync("color", secondary);
        await Expect(Step("asv-completed").Locator(".bit-aiasv-smr")).ToHaveCSSAsync("color", secondary);
        await Expect(Step("asv-completed").Locator(".bit-aiasv-dur")).ToHaveCSSAsync("font-variant-numeric", "tabular-nums");

        // The marker draws what the words already say, so it is hidden from assistive technologies.
        await Expect(Marker("asv-completed")).ToHaveAttributeAsync("aria-hidden", "true");
    }

    [TestMethod]
    public async Task Timeline_TheLine_JoinsEachStepToTheNextAndTheLastHasNone()
    {
        await OpenAsync(Route);

        var line = await BeforeAsync("#asv-pending", "display", "background-color", "width", "left");
        Assert.AreEqual("block", line[0]);
        Assert.AreEqual(await Page.ResolveColorAsync("var(--bit-clr-brd-sec)"), line[1]);
        Assert.AreEqual(await Step("asv-pending").ResolveLengthInAsync("var(--bit-shp-brd-width-thick)"), line[2]);

        // The line leaves from under the middle of the marker.
        var marker = await Marker("asv-pending").RectAsync();
        var lineLeft = AiE2EHelpers.ParsePx(line[3]) + (await Step("asv-pending").RectAsync()).Left + AiE2EHelpers.ParsePx(line[2]) / 2;
        Assert.AreEqual((marker.Left + marker.Right) / 2, lineLeft, 1.5);

        Assert.AreEqual("none", (await BeforeAsync("#asv-skipped", "display"))[0], "The last step has no line.");
        Assert.AreEqual("none", (await BeforeAsync("#asv-live", "display"))[0], "A step alone has no line.");
    }

    // ---------- a step changing state ----------

    [TestMethod]
    public async Task Live_AStepMovingOn_ChangesItsClassWithoutANewParameter()
    {
        await OpenAsync(Route);

        var step = Step("asv-live");
        await Expect(step).ToHaveClassAsync(HasClass("bit-aiasv-pnd"));

        await Page.Locator("#btn-live-start").ClickAsync();
        await Expect(step).ToHaveClassAsync(HasClass("bit-aiasv-run"));
        await Expect(step).Not.ToHaveClassAsync(HasClass("bit-aiasv-pnd"));
        await Expect(step.Locator(".bit-aiasv-dur")).ToHaveCountAsync(1);

        await Page.Locator("#btn-live-block").ClickAsync();
        await Expect(step).ToHaveClassAsync(HasClass("bit-aiasv-blk"));
        await Expect(step).Not.ToHaveClassAsync(HasClass("bit-aiasv-run"));

        await Page.Locator("#btn-live-complete").ClickAsync();
        await Expect(step).ToHaveClassAsync(HasClass("bit-aiasv-cmp"));
        await Expect(step.Locator(".bit-aiasv-smr")).ToHaveTextAsync("Live step done");
        await Expect(step.Locator(".bit-aiasv-mic")).ToBeVisibleAsync();

        await Page.Locator("#btn-live-reset").ClickAsync();
        await Expect(step).ToHaveClassAsync(HasClass("bit-aiasv-pnd"));
        await Expect(step.Locator(".bit-aiasv-mic")).ToHaveCountAsync(0);
        await Page.Locator("#btn-live-start").ClickAsync();
        await Page.Locator("#btn-live-fail").ClickAsync();
        await Expect(step).ToHaveClassAsync(HasClass("bit-aiasv-fld"));
        await Expect(step.Locator(".bit-aiasv-err")).ToHaveTextAsync("Live step broke");
        await Expect(step).Not.ToHaveClassAsync(HasClass("bit-aiasv-run"));
    }

    // ---------- tool calls ----------

    [TestMethod]
    public async Task Tools_TheCallsOfAStep_AreTagsByNameAndState()
    {
        await OpenAsync(Route);

        var list = Page.Locator("#asv-tools ul.bit-aiasv-tls");
        await Expect(list).ToHaveAttributeAsync("aria-label", "Tool calls");
        var tags = list.Locator("li.bit-aiasv-tli .bit-tag");
        await Expect(tags).ToHaveTextAsync(["search_docs", "flaky_service", "call_unknown"]);
        await Expect(tags.Nth(0)).ToHaveAttributeAsync("title", "search_docs");
        await Expect(tags.Nth(2)).ToHaveAttributeAsync("title", "call_unknown");

        // A failed call says so in the error role; the rest are neutral tags the app's --bit-Tag-* variables reach.
        await Expect(tags.Nth(1)).ToHaveClassAsync(HasClass("bit-tag-err"));
        await Expect(tags.Nth(0)).ToHaveClassAsync(HasClass("bit-tag-dft-ter"));
        await Expect(tags.Nth(0)).Not.ToHaveClassAsync(HasClass("bit-tag-err"));

        // The running call shows the dots of a loader that announces nothing of its own.
        var dots = tags.Nth(0).Locator(".bit-aiasv-tld");
        await Expect(dots).ToBeVisibleAsync();
        await Expect(dots).ToHaveAttributeAsync("role", "none");
        await Expect(tags.Nth(1).Locator(".bit-aiasv-tld")).ToHaveCountAsync(0);

        // Without the conversation the calls are listed by their ids.
        await Expect(Page.Locator("#asv-tools-ids .bit-aiasv-tls .bit-tag")).ToHaveTextAsync(["call_search", "call_flaky", "call_unknown"]);

        await Page.Locator("#btn-tools-finish").ClickAsync();
        await Expect(Step("asv-tools")).ToHaveClassAsync(HasClass("bit-aiasv-cmp"));
        await Expect(Page.Locator("#asv-tools .bit-aiasv-tld")).ToHaveCountAsync(0);
        await Expect(tags.Nth(0)).Not.ToHaveClassAsync(HasClass("bit-tag-err"));
    }

    [TestMethod]
    public async Task Tools_TheWrenchOfATag_IsDrawnFromTheAiFont()
    {
        await OpenAsync(Route);

        var icon = Page.Locator("#asv-tools .bit-aiasv-tli .bit-tag .bit-tag-icn").First;
        await Expect(icon).ToBeVisibleAsync();
        await icon.AssertAiGlyphAsync("Repair");
    }

    // ---------- motion ----------

    [TestMethod]
    public async Task ReducedMotion_TheRunningPulse_StopsUnlessForceAnimationAsksForIt()
    {
        await OpenAsync(Route);

        Assert.IsNotNull(await Marker("asv-motion").AnimationDurationAsync("bit-aiasv-pls"), "A running step pulses.");
        Assert.IsNull(await Marker("asv-completed").AnimationDurationAsync("bit-aiasv-pls"), "A completed one does not.");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await Expect(Marker("asv-motion")).ToHaveCSSAsync("animation-name", "none");
        await Expect(Marker("asv-motion")).Not.ToHaveCSSAsync("box-shadow", "none");
        Assert.IsNotNull(await Marker("asv-motion-forced").AnimationDurationAsync("bit-aiasv-pls"), "ForceAnimation keeps the pulse.");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.NoPreference });
        await Expect(Marker("asv-motion")).ToHaveCSSAsync("animation-name", "bit-aiasv-pls");
    }

    // ---------- look ----------

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheTimeline()
    {
        await OpenAsync(Route);

        await Expect(Marker("asv-vars-pending")).ToHaveCSSAsync("border-top-color", "rgb(1, 2, 3)");
        await Expect(Marker("asv-vars-pending")).ToHaveCSSAsync("width", "30px");
        await Expect(Marker("asv-vars-pending")).ToHaveCSSAsync("height", "30px");
        await Expect(Marker("asv-vars-running")).ToHaveCSSAsync("border-top-color", "rgb(4, 5, 6)");
        await Expect(Marker("asv-vars-completed")).ToHaveCSSAsync("background-color", "rgb(7, 8, 9)");
        await Expect(Page.Locator("#asv-vars-completed .bit-aiasv-mic")).ToHaveCSSAsync("color", "rgb(10, 11, 12)");
        await Expect(Marker("asv-vars-failed")).ToHaveCSSAsync("background-color", "rgb(13, 14, 15)");
        await Expect(Marker("asv-vars-blocked")).ToHaveCSSAsync("background-color", "rgb(16, 17, 18)");
        Assert.AreEqual("rgb(19, 20, 21)", (await BeforeAsync("#asv-vars-pending", "background-color"))[0]);
        await Expect(Step("asv-vars-pending")).ToHaveCSSAsync("column-gap", "21px");
        await Expect(Step("asv-vars-pending")).ToHaveCSSAsync("font-size", "23px");
        await Expect(Step("asv-vars-pending").Locator(".bit-aiasv-dtl")).ToHaveCSSAsync("color", "rgb(22, 23, 24)");
        await Expect(Step("asv-vars-completed").Locator(".bit-aiasv-smr")).ToHaveCSSAsync("color", "rgb(22, 23, 24)");
        await Expect(Step("asv-vars-failed").Locator(".bit-aiasv-err")).ToHaveCSSAsync("color", "rgb(25, 26, 27)");

        // The pulse of a running step is drawn in its running color.
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        // The halo is the running color made translucent, serialized as rgba() or as color(srgb ...) by the engine.
        StringAssert.Matches(await ComputedStyleAsync("#asv-vars-running > .bit-aiasv-mrk", "box-shadow"),
            new Regex(@"rgba\(4, 5, 6|color\(srgb 0\.0156\d* 0\.0196\d* 0\.0235\d*"));
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachTheParts()
    {
        await OpenAsync(Route);

        await Expect(Step("asv-styles").Locator(".bit-aiasv-ttl")).ToHaveCSSAsync("color", "rgb(40, 41, 42)");
        await Expect(Step("asv-styles").Locator(".bit-aiasv-smr")).ToHaveCSSAsync("font-style", "italic");
        await Expect(Marker("asv-styles")).ToHaveClassAsync(HasClass("e2e-asv-marker"));
    }

    [TestMethod]
    public async Task Rtl_AnExplicitOrAnInheritedDirection_PutsTheMarkerAndTheLineOnTheRight()
    {
        await OpenAsync(Route);

        await Expect(Step("asv-rtl")).ToHaveAttributeAsync("dir", "rtl");

        foreach (var id in new[] { "asv-rtl", "asv-rtl-inherited" })
        {
            var marker = await Marker(id).RectAsync();
            var body = await Page.Locator($"#{id} > .bit-aiasv-bdy").RectAsync();
            Assert.IsTrue(marker.Left > body.Left, $"{id}: the marker is on the right of the words.");

            // The line leaves from under the middle of the marker on that side too.
            var line = await Page.Locator($"#{id}").EvaluateAsync<double>(
                "e => { const s = getComputedStyle(e, '::before'); const r = e.getBoundingClientRect(); return r.right - parseFloat(s.right) - parseFloat(s.width) / 2; }");
            Assert.AreEqual((marker.Left + marker.Right) / 2, line, 1.5, id);
        }

        var ltrMarker = await Marker("asv-ltr").RectAsync();
        var ltrBody = await Page.Locator("#asv-ltr > .bit-aiasv-bdy").RectAsync();
        Assert.IsTrue(ltrMarker.Left < ltrBody.Left, "Left to right, the marker is on the left of the words.");
    }

    [TestMethod]
    public async Task Disabled_TheStep_IsDrawnInTheDisabledColorsAndItsMarkerDimmed()
    {
        await OpenAsync(Route);

        var step = Step("asv-disabled");
        await Expect(step).ToHaveClassAsync(HasClass("bit-dis"));
        var disabled = await Page.ResolveColorAsync("var(--bit-clr-fg-dis)");
        await Expect(step).ToHaveCSSAsync("color", disabled);
        await Expect(step.Locator(".bit-aiasv-smr")).ToHaveCSSAsync("color", disabled);
        await Expect(step.Locator(".bit-aiasv-dur")).ToHaveCSSAsync("color", disabled);

        var dimmed = await Page.EvaluateAsync<string>("() => getComputedStyle(document.documentElement).getPropertyValue('--bit-opa-dis').trim()");
        Assert.AreEqual(double.Parse(dimmed, System.Globalization.CultureInfo.InvariantCulture),
                        double.Parse(await ComputedStyleAsync("#asv-disabled > .bit-aiasv-mrk", "opacity"), System.Globalization.CultureInfo.InvariantCulture), 0.001);
        await Expect(Marker("asv-enabled")).ToHaveCSSAsync("opacity", "1");
    }

    // ---------- helpers ----------

    private Task<string[]> BeforeAsync(string selector, params string[] properties)
    {
        return Page.Locator(selector).EvaluateAsync<string[]>(
            "(e, names) => { const s = getComputedStyle(e, '::before'); return names.map(n => s.getPropertyValue(n)); }", properties);
    }
}
