using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Surfaces;

/// <summary>
/// Drives BitCollapse (/e2e/collapse) in a real browser: the grid-track transition actually playing from nothing to the
/// content's height and back (and sideways for Horizontal, from the right in RTL), the closed content taken out of the
/// tab order and the accessibility tree, two-way binding, DefaultExpanded, ExpandAsync / CollapseAsync / ToggleAsync /
/// FocusAsync and OnChange, Disabled, Background and NoPadding, CollapsedSize with its fade, Duration / Delay / Easing /
/// ExpandDuration / CollapseDuration / NoFade / NoAnimation and reduced motion, the four transition events timed off the
/// transition the browser played, NoClip, LazyRender, UnmountOnCollapse, ExpandOnPrint, HiddenUntilFound revealed by an
/// anchor jump (and refused by a one-way Expanded), the region role and its name, the BitParams cascade, Style / Class /
/// Styles / Classes and the public CSS variables, RTL, Visibility, HtmlAttributes, prerendering, re-rendering and
/// unmounting mid-transition.
/// </summary>
[TestClass]
public class BitCollapseE2ETests : E2ETestBase
{
    private const string Url = "/e2e/collapse";
    private const string Transparent = "rgba(0, 0, 0, 0)";

    private ILocator El(string id) => Page.Locator($"#{id}");
    private ILocator Content(string id) => Page.Locator($"#{id}-content");
    private ILocator Wrapper(string id) => Page.Locator($"#{id}-content > .bit-col-wrp");

    [TestMethod]
    public async Task Prerender_TheServerHtml_ContainsTheClosedSectionTakenOutOfReach()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Url}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        Assert.IsTrue(Regex.IsMatch(html, "id=\"col-basic\"[^>]*class=\"[^\"]*bit-col-col"), "The closed collapse is not prerendered closed.");
        Assert.IsTrue(Regex.IsMatch(html, "id=\"col-basic-content\"[^>]*inert"), "The closed content is not prerendered inert.");
        Assert.IsTrue(Regex.IsMatch(html, "id=\"col-find-content\"[^>]*hidden=\"until-found\""), "The searchable content is not prerendered hidden-until-found.");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Expanded_OpensToTheContentsHeight_AndClosesToNothing()
    {
        await OpenAsync(Url);

        var root = El("col-basic");
        await Expect(root).ToHaveClassAsync(HasClass("bit-col-col"));
        Near(0, (await BoxAsync(root)).Height, "closed");
        await Expect(Content("col-basic")).ToHaveAttributeAsync("inert", "");
        await Expect(Content("col-basic")).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Content("col-basic")).ToHaveAttributeAsync("role", "region");
        await Expect(Page.Locator("#col-basic .e2e-col-line").First).ToBeHiddenAsync();

        await El("btn-basic").ClickAsync();

        await Expect(root).ToHaveClassAsync(HasClass("bit-col-exp"));
        await Expect(Content("col-basic")).Not.ToHaveAttributeAsync("inert", new Regex(".*"));
        await Expect(Content("col-basic")).ToHaveAttributeAsync("tabindex", "0");
        await WaitForFullHeightAsync("col-basic");
        await Expect(Page.Locator("#col-basic .e2e-col-line").First).ToBeVisibleAsync();
        await Expect(Content("col-basic")).ToHaveCSSAsync("opacity", "1");

        await El("btn-basic").ClickAsync();

        await Expect(root).ToHaveClassAsync(HasClass("bit-col-col"));
        await PollAsync(async () => (await BoxAsync(root)).Height, h => h < 0.5, "the section never closed");
        await Expect(Content("col-basic")).ToHaveCSSAsync("visibility", "hidden");
    }

    [TestMethod]
    public async Task Expanded_TheHeightTransitionPlaysThroughIntermediateHeights()
    {
        await OpenAsync(Url);

        await StartSamplerAsync("col-basic", "height");
        await El("btn-basic").ClickAsync();
        await WaitForFullHeightAsync("col-basic");

        var samples = await ReadSamplesAsync();
        var full = (await BoxAsync(Wrapper("col-basic"))).Height;
        Assert.IsTrue(samples.Count(s => s.Value > 1 && s.Value < full - 1) >= 3, $"[{string.Join(", ", samples.Select(s => s.Value))}] full {full}");
    }

    [TestMethod]
    public async Task Expanded_AClosedSection_IsOutOfTheTabOrder()
    {
        await OpenAsync(Url);

        await El("btn-basic").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(El("col-basic-after")).ToBeFocusedAsync();

        await El("btn-basic").ClickAsync();
        await Expect(El("col-basic")).ToHaveClassAsync(HasClass("bit-col-exp"));
        await El("btn-basic").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Content("col-basic")).ToBeFocusedAsync();
        await Expect(Content("col-basic")).ToHaveCSSAsync("outline-style", "solid");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(El("col-basic-inner")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Binding_TwoWay_ThePageDrivesTheState_WithoutAnOnChange()
    {
        await OpenAsync(Url);

        await El("btn-bound").ClickAsync();
        await Expect(El("col-bound")).ToHaveClassAsync(HasClass("bit-col-exp"));
        await Expect(El("bound-value")).ToHaveTextAsync("True");

        await El("btn-bound").ClickAsync();
        await Expect(El("col-bound")).ToHaveClassAsync(HasClass("bit-col-col"));
        await Expect(El("bound-changes")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Methods_ExpandCollapseAndToggle_DriveTheSectionAndReportThroughOnChange()
    {
        await OpenAsync(Url);

        var root = El("col-ref");
        await Expect(root).ToHaveClassAsync(HasClass("bit-col-exp"));

        await El("btn-collapse").ClickAsync();
        await Expect(root).ToHaveClassAsync(HasClass("bit-col-col"));
        await Expect(El("ref-changes")).ToHaveTextAsync("1");
        await Expect(El("ref-last")).ToHaveTextAsync("False");

        await El("btn-collapse").ClickAsync();
        await El("btn-toggle").ClickAsync();
        await Expect(root).ToHaveClassAsync(HasClass("bit-col-exp"));
        await Expect(El("ref-changes")).ToHaveTextAsync("2");
        await Expect(El("ref-last")).ToHaveTextAsync("True");

        await El("btn-expand").ClickAsync();
        await NextFramesAsync();
        await Expect(El("ref-changes")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Disabled_KeepsItsState_IgnoresTheMethods_AndTakesTheDisabledColors()
    {
        await OpenAsync(Url);

        var root = El("col-ref");
        var enabledColor = await StyleAsync(root, "color");
        var enabledBg = await StyleAsync(root, "background-color");

        await El("btn-disable").ClickAsync();
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Content("col-ref")).ToHaveAttributeAsync("tabindex", "-1");

        await El("btn-toggle").ClickAsync();
        await El("btn-collapse").ClickAsync();
        await NextFramesAsync();
        await Expect(root).ToHaveClassAsync(HasClass("bit-col-exp"));
        await Expect(El("ref-changes")).ToHaveTextAsync("0");

        await PollAsync(() => StyleAsync(root, "color"), c => c != enabledColor, "the disabled color is not applied");
        Assert.AreNotEqual(enabledBg, await StyleAsync(root, "background-color"));
        Assert.AreEqual(await StyleAsync(root, "color"), await StyleAsync(El("col-dis"), "color"));
    }

    [TestMethod]
    public async Task Background_EveryColorKind_AndNoPadding_PaintTheSurface()
    {
        await OpenAsync(Url);

        var expected = new (string Name, string Class)[] { ("primary", "bit-col-pbg"), ("secondary", "bit-col-sbg"), ("tertiary", "bit-col-tbg"), ("transparent", "bit-col-rbg") };
        var fills = new List<string>();
        foreach (var (name, cssClass) in expected)
        {
            await Expect(El($"col-bg-{name}")).ToHaveClassAsync(HasClass(cssClass));
            fills.Add(await StyleAsync(El($"col-bg-{name}"), "background-color"));
        }

        Assert.AreEqual(4, fills.Distinct().Count(), string.Join(" | ", fills));
        Assert.AreEqual(Transparent, fills[3]);
        Assert.AreEqual(fills[0], await StyleAsync(El("col-bg-unset"), "background-color"));

        Assert.IsTrue(Px(await StyleAsync(Wrapper("col-bg-unset"), "padding-top")) > 0);
        await Expect(El("col-bg-transparent")).ToHaveClassAsync(HasClass("bit-col-npd"));
        await Expect(Wrapper("col-bg-transparent")).ToHaveCSSAsync("padding-top", "0px");
        Near((await BoxAsync(El("col-bg-transparent"))).X, (await BoxAsync(El("col-nopad-text"))).X, "the content touches the edge");
    }

    [TestMethod]
    public async Task Horizontal_OpensSideways_ToTheWidthOfItsContent()
    {
        await OpenAsync(Url);

        var root = El("col-hor");
        await Expect(root).ToHaveClassAsync(HasClass("bit-col-hor"));
        Near(0, (await BoxAsync(root)).Width, "closed");

        await StartSamplerAsync("col-hor", "width");
        await El("btn-hor").ClickAsync();

        await PollAsync(async () =>
        {
            var width = (await BoxAsync(root)).Width;
            var content = (await BoxAsync(Wrapper("col-hor"))).Width;
            return (width, content);
        }, s => s.width > 50 && Math.Abs(s.width - s.content) < 1, "the section never reached its content's width");

        var full = (await BoxAsync(root)).Width;
        var text = await BoxAsync(El("col-hor-text"));
        var padding = Px(await StyleAsync(Wrapper("col-hor"), "padding-left"));
        Near(text.Width + 2 * padding, full, "the width of the content");
        var samples = await ReadSamplesAsync();
        Assert.IsTrue(samples.Count(s => s.Value > 1 && s.Value < full - 1) >= 3, $"[{string.Join(", ", samples.Select(s => s.Value))}]");
        Assert.IsTrue((await BoxAsync(root)).Height > 10);
    }

    [TestMethod]
    public async Task Horizontal_InRtl_OpensFromTheRightEdge()
    {
        await OpenAsync(Url);

        var box = await BoxAsync(El("hor-rtl-box"));
        await El("btn-hor").ClickAsync();
        await PollAsync(async () => (await BoxAsync(El("col-hor-rtl"))).Width, w => w > 50, "the RTL section never opened");

        var root = await BoxAsync(El("col-hor-rtl"));
        Near(box.X + box.Width, root.X + root.Width, "anchored on the right");
        await Expect(El("col-hor-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(El("col-hor-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
    }

    [TestMethod]
    public async Task CollapsedSize_LeavesAReadablePeek_AndOpensToTheFullHeight()
    {
        await OpenAsync(Url);

        var root = El("col-peek");
        await Expect(root).ToHaveClassAsync(HasClass("bit-col-pek"));
        await Expect(root).ToHaveClassAsync(HasClass("bit-col-nfd"));
        Near(72, (await BoxAsync(root)).Height, "a 4.5rem peek");
        await Expect(Content("col-peek")).Not.ToHaveAttributeAsync("inert", new Regex(".*"));
        await Expect(Content("col-peek")).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Content("col-peek")).ToHaveCSSAsync("opacity", "1");
        await Expect(Content("col-peek")).ToHaveCSSAsync("visibility", "visible");
        await Expect(Content("col-peek")).ToHaveCSSAsync("mask-image", "none");

        await El("btn-peek").ClickAsync();
        await WaitForFullHeightAsync("col-peek");
        Assert.IsTrue((await BoxAsync(root)).Height > 150);

        // A fade on the trailing edge of a closed peek, and a sideways peek is a width.
        StringAssert.Contains(await StyleAsync(Content("col-peek-fade"), "mask-image"), "linear-gradient");
        Near(48, (await BoxAsync(El("col-peek-fade"))).Height, "a 3rem peek");
        Near(80, (await BoxAsync(El("col-peek-hor"))).Width, "an 80px sideways peek");
    }

    [TestMethod]
    public async Task Transition_DurationDelayAndEasing_ReachTheTrack()
    {
        await OpenAsync(Url);

        var root = El("col-slow");
        Assert.AreEqual(1000, ToMs(await StyleAsync(root, "transition-duration")), 1);
        Assert.AreEqual(200, ToMs(await StyleAsync(root, "transition-delay")), 1);
        Assert.AreEqual("cubic-bezier(0.68, -0.55, 0.27, 1.55)", await StyleAsync(root, "transition-timing-function"));

        await StartSamplerAsync("col-slow", "height");
        await El("btn-trans").ClickAsync();
        await WaitForFullHeightAsync("col-slow");

        // The delay holds the track shut for a while after the state changes, and the track then grows over the
        // second the Duration gives it.
        var samples = await ReadSamplesAsync();
        var full = (await BoxAsync(Wrapper("col-slow"))).Height;
        var changed = samples.First(s => s.Expanded);
        var opened = samples.First(s => s.Value > 0.5);
        var reached = samples.First(s => s.Value >= full - 0.5);
        Assert.IsTrue(opened.Time - changed.Time >= 150, $"The delay was {opened.Time - changed.Time}ms.");
        Assert.IsTrue(reached.Time - changed.Time >= 700, $"The track opened in {reached.Time - changed.Time}ms.");
    }

    [TestMethod]
    public async Task Transition_ExpandAndCollapseDurations_PaceEachDirection()
    {
        await OpenAsync(Url);

        var root = El("col-dirs");
        Assert.AreEqual(200, ToMs(await StyleAsync(root, "transition-duration")), 1);
        await El("btn-trans").ClickAsync();
        await Expect(root).ToHaveClassAsync(HasClass("bit-col-exp"));
        Assert.AreEqual(900, ToMs(await StyleAsync(root, "transition-duration")), 1);
        Assert.AreEqual(300, ToMs(await StyleAsync(El("col-theme"), "transition-duration")), 1);
    }

    [TestMethod]
    public async Task Transition_NoFadeAndNoAnimation()
    {
        await OpenAsync(Url);

        await Expect(El("col-nofade")).ToHaveClassAsync(HasClass("bit-col-nfd"));
        await Expect(Content("col-nofade")).ToHaveCSSAsync("opacity", "1");
        await Expect(Content("col-theme")).ToHaveCSSAsync("opacity", "0");

        await Expect(El("col-noanim")).ToHaveClassAsync(HasClass("bit-col-nan"));
        Assert.AreEqual(0, ToMs(await StyleAsync(El("col-noanim"), "transition-duration")), 0.001);

        await StartSamplerAsync("col-noanim", "height");
        await El("btn-trans").ClickAsync();
        await WaitForFullHeightAsync("col-noanim");
        var samples = await ReadSamplesAsync();
        var full = (await BoxAsync(Wrapper("col-noanim"))).Height;
        Assert.AreEqual(0, samples.Count(s => s.Value > 1 && s.Value < full - 1), $"[{string.Join(", ", samples.Select(s => s.Value))}]");
    }

    [TestMethod]
    public async Task ReducedMotion_CollapsesEveryPace_UnlessForceAnimation()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await OpenAsync(Url);

        Assert.IsTrue(ToMs(await StyleAsync(El("col-theme"), "transition-duration")) < 0.1);
        Assert.IsTrue(ToMs(await StyleAsync(El("col-slow"), "transition-duration")) < 0.1, "A Duration does not follow reduced motion.");
        Assert.IsTrue(ToMs(await StyleAsync(El("col-slow"), "transition-delay")) < 0.1, "A Delay does not follow reduced motion.");
        await Expect(El("col-force")).ToHaveClassAsync(HasClass("bit-fam"));
        Assert.AreEqual(300, ToMs(await StyleAsync(El("col-force"), "transition-duration")), 1);
    }

    [TestMethod]
    public async Task Events_TheIngOnesFireAtOnce_TheEdOnesWhenTheTransitionHasPlayed()
    {
        await OpenAsync(Url);

        await Expect(El("ev-log")).ToHaveTextAsync("");

        var watch = Stopwatch.StartNew();
        await El("btn-events").ClickAsync();
        // OnExpanding is raised as the new state is taken, ahead of the OnChange that reports it.
        await Expect(El("ev-log")).ToHaveTextAsync(new Regex("^Expanding\\|Change:True"));
        var started = watch.ElapsedMilliseconds;
        await Expect(El("ev-log")).ToHaveTextAsync("Expanding|Change:True|Expanded");
        Assert.IsTrue(watch.ElapsedMilliseconds - started >= 400, $"OnExpanded came {watch.ElapsedMilliseconds - started}ms after OnExpanding.");

        await El("btn-events").ClickAsync();
        await Expect(El("ev-log")).ToHaveTextAsync("Expanding|Change:True|Expanded|Collapsing|Change:False|Collapsed");
    }

    [TestMethod]
    public async Task Events_OnExpanded_WaitsForAPaceTheVariableSet()
    {
        await OpenAsync(Url);

        Assert.AreEqual(700, ToMs(await StyleAsync(El("col-ev-var"), "transition-duration")), 1);
        await El("btn-ev-var").ClickAsync();
        await Expect(El("ev-var-gap")).ToHaveTextAsync(new Regex(@"^\d+$"));
        var gap = int.Parse((await El("ev-var-gap").TextContentAsync())!, CultureInfo.InvariantCulture);
        Assert.IsTrue(gap >= 600, $"OnExpanded came {gap}ms after OnExpanding for a 700ms transition.");
    }

    [TestMethod]
    public async Task NoClip_StopsClippingOnceTheSectionHasOpened()
    {
        await OpenAsync(Url);

        await El("btn-clip").ClickAsync();
        await Expect(El("col-noclip")).Not.ToHaveClassAsync(HasClass("bit-col-ncl"));

        await Expect(El("noclip-expanded")).ToHaveTextAsync("1");
        await Expect(El("col-noclip")).ToHaveClassAsync(HasClass("bit-col-ncl"));
        await Expect(Content("col-noclip")).ToHaveCSSAsync("overflow-x", "visible");
        await Expect(Content("col-clip")).ToHaveCSSAsync("overflow-x", "hidden");

        await El("btn-clip").ClickAsync();
        await Expect(El("col-noclip")).Not.ToHaveClassAsync(HasClass("bit-col-ncl"));
    }

    [TestMethod]
    public async Task LazyRender_BuildsTheContentOnTheFirstOpen_AndKeepsIt()
    {
        await OpenAsync(Url);

        await Expect(El("col-lazy-input")).ToHaveCountAsync(0);
        await El("btn-render").ClickAsync();
        await Expect(El("col-lazy-input")).ToBeVisibleAsync();
        await El("col-lazy-input").FillAsync("kept");

        await El("btn-render").ClickAsync();
        await Expect(El("col-lazy")).ToHaveClassAsync(HasClass("bit-col-col"));
        await Expect(El("col-lazy-input")).ToBeHiddenAsync();
        await Expect(El("col-lazy-input")).ToHaveCountAsync(1);
        await Expect(El("col-lazy-input")).ToHaveValueAsync("kept");
    }

    [TestMethod]
    public async Task UnmountOnCollapse_DropsTheContentOnceTheClosePlayed()
    {
        await OpenAsync(Url);

        await El("btn-render").ClickAsync();
        await Expect(El("col-unmount-input")).ToBeVisibleAsync();
        await El("col-unmount-input").FillAsync("lost");

        await El("btn-render").ClickAsync();
        await Expect(El("col-unmount")).ToHaveClassAsync(HasClass("bit-col-col"));
        await Expect(El("col-unmount-input")).ToHaveCountAsync(0);

        await El("btn-render").ClickAsync();
        await Expect(El("col-unmount-input")).ToHaveValueAsync("");
    }

    [TestMethod]
    public async Task ExpandOnPrint_PrintsAClosedSectionOpen()
    {
        await OpenAsync(Url);

        await Expect(El("col-print")).ToHaveClassAsync(HasClass("bit-col-eop"));
        Near(0, (await BoxAsync(El("col-print"))).Height, "closed on the screen");
        await Expect(El("col-print-lazy-text")).ToHaveCountAsync(1);

        await Page.EmulateMediaAsync(new() { Media = Media.Print });

        await PollAsync(async () => (await BoxAsync(El("col-print"))).Height, h => h > 10, "the section does not print open");
        await Expect(Content("col-print")).ToHaveCSSAsync("opacity", "1");
        await Expect(Content("col-print")).ToHaveCSSAsync("visibility", "visible");
        await Expect(El("col-print-text")).ToBeVisibleAsync();
        Near(0, (await BoxAsync(El("col-basic"))).Height, "a section without ExpandOnPrint prints closed");

        await Page.EmulateMediaAsync(new() { Media = Media.Screen });
        await PollAsync(async () => (await BoxAsync(El("col-print"))).Height, h => h < 0.5, "the section stays open on the screen");
    }

    [TestMethod]
    public async Task HiddenUntilFound_AnAnchorJump_OpensTheSectionAroundTheMatch()
    {
        await OpenAsync(Url);
        await SkipWithoutHiddenUntilFoundAsync();

        await Expect(Content("col-find")).ToHaveAttributeAsync("hidden", "until-found");
        await Expect(El("col-find")).ToHaveClassAsync(HasClass("bit-col-huf"));
        await Expect(El("col-find-target")).ToBeHiddenAsync();

        await El("link-col-find").ClickAsync();

        await Expect(El("find-value")).ToHaveTextAsync("True");
        await Expect(El("find-changes")).ToHaveTextAsync("1");
        await Expect(El("col-find")).ToHaveClassAsync(HasClass("bit-col-exp"));
        await Expect(Content("col-find")).Not.ToHaveAttributeAsync("hidden", new Regex(".*"));
        await Expect(El("col-find-target")).ToBeVisibleAsync();

        await El("btn-find").ClickAsync();
        await Expect(El("col-find")).ToHaveClassAsync(HasClass("bit-col-col"));
        await Expect(Content("col-find")).ToHaveAttributeAsync("hidden", "until-found");
    }

    [TestMethod]
    public async Task HiddenUntilFound_AOneWayExpanded_RefusesTheReveal()
    {
        await OpenAsync(Url);
        await SkipWithoutHiddenUntilFoundAsync();

        await Expect(Content("col-find-oneway")).ToHaveAttributeAsync("hidden", "until-found");

        await Page.EvaluateAsync("() => { location.hash = 'col-find-oneway-target'; }");

        await Expect(El("col-find-oneway")).Not.ToHaveClassAsync(HasClass("bit-col-huf"));
        await Expect(El("col-find-oneway")).ToHaveClassAsync(HasClass("bit-col-col"));
        await Expect(Content("col-find-oneway")).Not.ToHaveAttributeAsync("hidden", new Regex(".*"));
        await Expect(Content("col-find-oneway")).ToHaveAttributeAsync("inert", "");
        await Expect(El("col-find-oneway-target")).ToBeHiddenAsync();

        // A disabled one is never offered to the search.
        await Expect(Content("col-find-disabled")).Not.ToHaveAttributeAsync("hidden", new Regex(".*"));
        await Expect(Content("col-find-disabled")).ToHaveAttributeAsync("inert", "");
    }

    [TestMethod]
    public async Task Accessibility_TheContentIsARegionNamedByItsTrigger()
    {
        await OpenAsync(Url);

        await Expect(El("a11y-trigger")).ToHaveAttributeAsync("aria-controls", "col-a11y-content");
        await Expect(Content("col-a11y")).ToHaveAttributeAsync("role", "region");
        await Expect(Content("col-a11y")).ToHaveAttributeAsync("aria-labelledby", "a11y-trigger");

        await El("a11y-trigger").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(El("a11y-after")).ToBeFocusedAsync();

        await El("a11y-trigger").ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Region, new() { Name = "Shipping details" })).ToBeVisibleAsync();
        await El("a11y-trigger").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Content("col-a11y")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(El("col-a11y-link")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task FocusAsync_FromOnExpanded_MovesTheFocusIntoTheOpenSection()
    {
        await OpenAsync(Url);

        await El("btn-focus").ClickAsync();

        await Expect(Content("col-focus")).ToBeFocusedAsync();
        await Expect(Content("col-focus")).ToHaveAttributeAsync("aria-label", "Focused section");
        await Expect(Page.GetByRole(AriaRole.Region, new() { Name = "Focused section" })).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Role_AnEmptyOrNamelessRoleDropsTheName_AndTabIndexMinusOneLeavesTheTabOrder()
    {
        await OpenAsync(Url);

        await Expect(Content("col-role-empty")).Not.ToHaveAttributeAsync("role", new Regex(".*"));
        await Expect(Content("col-role-empty")).Not.ToHaveAttributeAsync("aria-label", new Regex(".*"));
        await Expect(Content("col-role-none")).ToHaveAttributeAsync("role", "none");
        await Expect(Content("col-role-none")).Not.ToHaveAttributeAsync("aria-label", new Regex(".*"));
        await Expect(Content("col-role-group")).ToHaveAttributeAsync("role", "group");
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "A named group" })).ToHaveCountAsync(1);
        await Expect(Content("col-tabindex")).ToHaveAttributeAsync("tabindex", "-1");
    }

    [TestMethod]
    public async Task BitParams_FillsTheUnsetParameters_AndTheCollapsesOwnValuesWin()
    {
        await OpenAsync(Url);

        await Expect(El("col-par-1")).ToHaveClassAsync(HasClass("bit-col-sbg"));
        await Expect(El("col-par-1")).ToHaveClassAsync(HasClass("bit-col-npd"));
        await Expect(Wrapper("col-par-1")).ToHaveCSSAsync("padding-top", "0px");
        Assert.AreEqual(800, ToMs(await StyleAsync(El("col-par-1"), "transition-duration")), 1);

        await Expect(El("col-par-own")).Not.ToHaveClassAsync(HasClass("bit-col-npd"));
        Assert.IsTrue(Px(await StyleAsync(Wrapper("col-par-own"), "padding-top")) > 0);
        await Expect(El("col-par-own")).ToHaveClassAsync(HasClass("bit-col-sbg"));
    }

    [TestMethod]
    public async Task StyleAndClass_ApplyToTheRoot()
    {
        await OpenAsync(Url);

        await Expect(El("col-style")).ToHaveCSSAsync("border-left-width", "4px");
        await Expect(El("col-style")).ToHaveCSSAsync("border-left-color", "rgb(0, 101, 239)");
        await Expect(El("col-class")).ToHaveClassAsync(HasClass("e2e-col-class"));
        await Expect(El("col-class")).ToHaveCSSAsync("outline-style", "dashed");
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachEveryPart_AndTheStateOnesFollowTheState()
    {
        await OpenAsync(Url);

        await Expect(El("col-styles")).ToHaveCSSAsync("margin-top", "6px");
        await Expect(El("col-styles")).ToHaveCSSAsync("border-top-color", "rgb(0, 128, 0)");
        await Expect(Content("col-styles")).ToHaveCSSAsync("word-spacing", "2px");
        await Expect(Wrapper("col-styles")).ToHaveCSSAsync("font-weight", "700");

        await Expect(El("col-classes")).ToHaveClassAsync(HasClass("e2e-col-root"));
        await Expect(El("col-classes")).ToHaveClassAsync(HasClass("e2e-col-expanded"));
        await Expect(El("col-classes")).ToHaveCSSAsync("border-top-color", "rgb(10, 20, 30)");
        await Expect(Content("col-classes")).ToHaveClassAsync(HasClass("e2e-col-content"));
        await Expect(Content("col-classes")).ToHaveCSSAsync("word-spacing", "4px");
        await Expect(Wrapper("col-classes")).ToHaveClassAsync(HasClass("e2e-col-wrapper"));
        await Expect(Wrapper("col-classes")).ToHaveCSSAsync("font-style", "italic");

        await El("btn-style").ClickAsync();

        await Expect(El("col-styles")).ToHaveCSSAsync("border-top-color", "rgb(128, 0, 0)");
        await Expect(El("col-classes")).ToHaveClassAsync(HasClass("e2e-col-collapsed"));
        await Expect(El("col-classes")).Not.ToHaveClassAsync(HasClass("e2e-col-expanded"));
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_Restyle_AndTheParametersWinOverThem()
    {
        await OpenAsync(Url);

        var root = El("col-var");
        await Expect(root).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(root).ToHaveCSSAsync("background-color", "rgb(250, 240, 230)");
        await Expect(root).ToHaveCSSAsync("font-size", "19px");
        await Expect(Wrapper("col-var")).ToHaveCSSAsync("padding-top", "21px");
        Assert.AreEqual(750, ToMs(await StyleAsync(root, "transition-duration")), 1);
        await Expect(root).ToHaveCSSAsync("transition-timing-function", "linear");

        var wins = El("col-var-wins");
        Assert.AreEqual(await StyleAsync(El("col-bg-tertiary"), "background-color"), await StyleAsync(wins, "background-color"));
        await Expect(Wrapper("col-var-wins")).ToHaveCSSAsync("padding-top", "0px");
        Assert.AreEqual(100, ToMs(await StyleAsync(wins, "transition-duration")), 1);
        await Expect(wins).ToHaveCSSAsync("transition-timing-function", "ease-in");
    }

    [TestMethod]
    public async Task Rtl_DirRtl_RendersTheDirection()
    {
        await OpenAsync(Url);

        await Expect(El("col-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(El("col-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Wrapper("col-rtl")).ToHaveCSSAsync("direction", "rtl");
    }

    [TestMethod]
    public async Task Visibility_HiddenKeepsTheBox_CollapsedRemovesIt()
    {
        await OpenAsync(Url);

        await Expect(El("col-vis-hidden")).ToHaveCSSAsync("visibility", "hidden");
        Assert.IsTrue((await BoxAsync(El("col-vis-hidden"))).Height > 0);
        await Expect(El("col-vis-collapsed")).ToHaveCSSAsync("display", "none");
    }

    [TestMethod]
    public async Task HtmlAttributes_AreSplattedOnTheRoot()
    {
        await OpenAsync(Url);

        await Expect(El("col-attrs")).ToHaveAttributeAsync("data-e2e", "custom");
        await Expect(El("col-attrs")).ToHaveAttributeAsync("data-extra", "from-dictionary");
    }

    [TestMethod]
    public async Task Rerender_ParametersChangedByThePage_UpdateTheCollapse()
    {
        await OpenAsync(Url);

        await Expect(El("col-rr")).ToHaveClassAsync(HasClass("bit-col-col"));
        await El("btn-rr").ClickAsync();

        await Expect(El("col-rr")).ToHaveClassAsync(HasClass("bit-col-exp"));
        await Expect(El("col-rr")).ToHaveClassAsync(HasClass("bit-col-tbg"));
        await Expect(El("col-rr")).ToHaveClassAsync(HasClass("bit-col-npd"));
        await Expect(El("col-rr-text")).ToHaveTextAsync("After");
        await Expect(El("col-rr-text")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Unmount_InTheMiddleOfATransition_RemovesTheCollapseWithoutAnError()
    {
        await OpenAsync(Url);

        await El("btn-um-toggle").ClickAsync();
        await Expect(El("col-um")).ToHaveClassAsync(HasClass("bit-col-col"));
        await El("btn-unmount").ClickAsync();
        await Expect(El("col-um")).ToHaveCountAsync(0);

        await El("btn-unmount").ClickAsync();
        await Expect(El("col-um")).ToHaveCountAsync(1);
        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    private async Task SkipWithoutHiddenUntilFoundAsync()
    {
        if (await Page.EvaluateAsync<bool>("() => 'onbeforematch' in document.body") is false)
        {
            Assert.Inconclusive($"{BrowserName} does not support hidden=\"until-found\" (no beforematch event).");
        }
    }

    private async Task WaitForFullHeightAsync(string id)
    {
        await PollAsync(async () =>
        {
            var root = (await BoxAsync(El(id))).Height;
            var wrapper = (await BoxAsync(Wrapper(id))).Height;
            return (root, wrapper);
        }, s => s.wrapper > 0 && Math.Abs(s.root - s.wrapper) < 0.5, $"{id} never reached the height of its content");
    }

    // Records the root's size and whether it carries the expanded class, frame by frame, with the time of each frame.
    private Task StartSamplerAsync(string id, string dimension)
    {
        return Page.EvaluateAsync(@"([id, dimension]) => {
            const root = document.getElementById(id);
            window.__e2eSamples = [];
            const tick = () => {
                const r = root.getBoundingClientRect();
                window.__e2eSamples.push([performance.now(), dimension === 'width' ? r.width : r.height, root.classList.contains('bit-col-exp') ? 1 : 0]);
                if (window.__e2eSamples.length < 600) requestAnimationFrame(tick);
            };
            requestAnimationFrame(tick);
        }", new[] { id, dimension });
    }

    private async Task<Sample[]> ReadSamplesAsync()
    {
        var raw = await Page.EvaluateAsync<double[][]>("() => window.__e2eSamples.slice()");
        return raw.Select(s => new Sample(s[0], s[1], s[2] > 0)).ToArray();
    }

    private static async Task<LayoutBox> BoxAsync(ILocator locator)
    {
        var box = await locator.EvaluateAsync<double[]>("el => { const r = el.getBoundingClientRect(); return [r.x, r.y, r.width, r.height]; }");
        return new(box[0], box[1], box[2], box[3]);
    }

    private static Task<string> StyleAsync(ILocator locator, string property)
    {
        return locator.EvaluateAsync<string>("(el, p) => getComputedStyle(el).getPropertyValue(p)", property);
    }

    private static double Px(string value) => double.Parse(value.Replace("px", ""), CultureInfo.InvariantCulture);

    private static double ToMs(string value)
    {
        value = value.Split(',')[0].Trim();

        return value.EndsWith("ms", StringComparison.Ordinal)
            ? double.Parse(value[..^2], CultureInfo.InvariantCulture)
            : double.Parse(value[..^1], CultureInfo.InvariantCulture) * 1000;
    }

    private static void Near(double expected, double actual, string what, double tolerance = 1.5)
    {
        Assert.IsTrue(Math.Abs(expected - actual) <= tolerance, $"{what}: expected {expected}, got {actual}.");
    }

    private async Task<T> PollAsync<T>(Func<Task<T>> read, Func<T, bool> done, string message)
    {
        var started = DateTime.UtcNow;
        T value;
        do
        {
            value = await read();
            if (done(value)) return value;
            await NextFramesAsync();
        } while (DateTime.UtcNow - started < TimeSpan.FromMilliseconds(DefaultTimeout));

        Assert.Fail($"{message} (last value: {value})");
        return value;
    }

    private sealed record LayoutBox(double X, double Y, double Width, double Height);

    private sealed record Sample(double Time, double Value, bool Expanded);
}
