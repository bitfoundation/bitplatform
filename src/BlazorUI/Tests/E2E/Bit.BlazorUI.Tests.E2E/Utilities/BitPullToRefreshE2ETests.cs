using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Utilities;

/// <summary>
/// BitPullToRefresh in a real browser (/e2e/pull-to-refresh) with the mouse: a pull past the trigger and one short of
/// it, the refresh held until OnRefresh returns, OnPullStart / OnPullMove / OnPullEnd / OnPullCancel / OnStateChange
/// with their arguments, IsRefreshing / State / PullProgress, the strip and the indicator the script draws, a scrolled
/// anchor and a sideways drag left alone, Trigger / Factor / Margin / Threshold / MaxPull, Direction Up,
/// ScrollerSelector / ScrollerElement / FullWidth, CompleteDelay and the Loading / Release / Complete templates,
/// IndicatorTemplate, RefreshAsync and the live-region labels, Disabled, NoMouse, the BitParams cascade, every Color
/// and CustomColor, Style / Class / Styles / Classes and the --bit-PullToRefresh-* variables in every state, Dir,
/// Visibility, attributes, reduced motion, the prerendered markup and an unmount mid-refresh. Touch is in
/// BitPullToRefreshTouchE2ETests.
/// </summary>
[TestClass]
public class BitPullToRefreshE2ETests : E2ETestBase
{
    private const string Route = "/e2e/pull-to-refresh";

    [TestMethod]
    public async Task Prerender_TheServerHtml_ContainsTheGroupItsLiveRegionAndTheGlyph()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "<div id=\"ptr-basic\" class=\"bit-ptr\" role=\"group\">");
        StringAssert.Contains(html, "<span role=\"status\" class=\"bit-ptr-vhd\"></span>");
        StringAssert.Contains(html, "class=\"bit-ptr-spw\"");
        StringAssert.Contains(html, "id=\"status\">Loading");

        await OpenAsync(Route);
        await Expect(Page.Locator("#ptr-basic")).ToHaveCSSAsync("position", "relative");
    }

    [TestMethod]
    public async Task MousePull_PastTheTrigger_RefreshesAndHoldsTheIndicatorUntilTheHandlerReturns()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#ptr-basic");
        var strip = root.Locator(".bit-ptr-lod");
        var wrapper = root.Locator(".bit-ptr-spw");
        var live = root.Locator("[role=status]");

        var (x, y) = await PullStartPointAsync("#ptr-basic");
        var box = (await root.BoundingBoxAsync())!;

        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x, y + 150, new() { Steps = 15 });

        await Expect(Page.Locator("#starts")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#start-args")).ToHaveTextAsync($"{box.Y:0}|{box.X:0}|{box.Width:0}");
        // 150px of travel over a Factor of 1.5 is 100, held at the Trigger of 80.
        await Expect(Page.Locator("#last-move")).ToHaveTextAsync("80");
        await Expect(wrapper).ToHaveClassAsync(HasClass("bit-ptr-crl"));
        await Expect(live).ToHaveTextAsync("Release to refresh");
        await Expect(root).ToHaveClassAsync(HasClass("bit-ptr-drg"));
        // The strip opens to the pull: 80 x 1.5 + the Margin of 30.
        await Expect(strip).ToHaveCSSAsync("min-height", "150px");
        await Expect(root.Locator(".bit-ptr-spn")).ToHaveCSSAsync("opacity", "1");

        await Page.Locator("#btn-read").DispatchEventAsync("click");
        await Expect(Page.Locator("#read")).ToHaveTextAsync("False|CanRelease|1");

        await Page.Mouse.UpAsync();

        await Expect(Page.Locator("#refreshes")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#ends")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#last-end")).ToHaveTextAsync("80");
        await Expect(wrapper).ToHaveClassAsync(HasClass("bit-ptr-swr"));
        await Expect(root.Locator(".bit-ptr-spn")).ToHaveClassAsync(HasClass("bit-ptr-spin"));
        await Expect(live).ToHaveTextAsync("Refreshing");
        await Expect(strip).ToHaveCSSAsync("min-height", "150px");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-ptr-drg"));

        await Page.ClickAsync("#btn-read");
        await Expect(Page.Locator("#read")).ToHaveTextAsync("True|Refreshing|1");

        await Page.ClickAsync("#btn-finish");

        await Expect(live).ToHaveTextAsync("");
        await Expect(wrapper).Not.ToHaveClassAsync(HasClass("bit-ptr-swr"));
        await Expect(strip).ToHaveCSSAsync("min-height", "0px");
        await Expect(Page.Locator("#states")).ToHaveTextAsync("Pulling,CanRelease,Refreshing,Idle");

        await Page.ClickAsync("#btn-read");
        await Expect(Page.Locator("#read")).ToHaveTextAsync("False|Idle|0");
    }

    [TestMethod]
    public async Task MousePull_ShortOfTheTrigger_SnapsBackWithoutRefreshing()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#ptr-basic");
        var (x, y) = await PullStartPointAsync("#ptr-basic");

        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x, y + 60, new() { Steps = 6 });

        await Expect(Page.Locator("#last-move")).ToHaveTextAsync("40");
        await Expect(root.Locator(".bit-ptr-lod")).ToHaveCSSAsync("min-height", "90px");
        await Expect(root.Locator(".bit-ptr-spw")).Not.ToHaveClassAsync(HasClass("bit-ptr-crl"));
        await Expect(root.Locator(".bit-ptr-spn")).ToHaveCSSAsync("opacity", "0.6");
        await Page.Locator("#btn-read").DispatchEventAsync("click");
        await Expect(Page.Locator("#read")).ToHaveTextAsync("False|Pulling|0.5");

        await Page.Mouse.UpAsync();

        await Expect(Page.Locator("#ends")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#last-end")).ToHaveTextAsync("40");
        await Expect(Page.Locator("#states")).ToHaveTextAsync("Pulling,Idle");
        await Expect(root.Locator(".bit-ptr-lod")).ToHaveCSSAsync("min-height", "0px");
        await Expect(Page.Locator("#refreshes")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task MousePull_TakenBackAboveItsStart_IsCancelled()
    {
        await OpenAsync(Route);

        var (x, y) = await PullStartPointAsync("#ptr-basic");
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x, y + 60, new() { Steps = 6 });
        await Expect(Page.Locator("#last-move")).ToHaveTextAsync("40");

        await Page.Mouse.MoveAsync(x, y - 10, new() { Steps = 4 });

        await Expect(Page.Locator("#cancels")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#states")).ToHaveTextAsync("Pulling,Idle");
        await Page.Mouse.UpAsync();
        await Expect(Page.Locator("#refreshes")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#ptr-basic .bit-ptr-lod")).ToHaveCSSAsync("min-height", "0px");
    }

    [TestMethod]
    public async Task MousePull_OnAScrolledAnchor_IsLeftToTheScroller()
    {
        await OpenAsync(Route);

        await Page.Locator("#basic-anchor").EvaluateAsync("el => el.scrollTop = 100");

        var (x, y) = await PullStartPointAsync("#ptr-basic");
        await MousePullAsync(x, y, 150);

        await Page.ClickAsync("#btn-read");
        await Expect(Page.Locator("#read")).ToHaveTextAsync("False|Idle|0");
        await Expect(Page.Locator("#starts")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#moves")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#refreshes")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task MouseDrag_Sideways_IsLeftToTheContent()
    {
        await OpenAsync(Route);

        var (x, y) = await PullStartPointAsync("#ptr-basic");
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + 60, y + 5, new() { Steps = 6 });
        await Page.Mouse.MoveAsync(x + 60, y + 160, new() { Steps = 6 });
        await Page.Mouse.UpAsync();

        await Page.ClickAsync("#btn-read");
        await Expect(Page.Locator("#read")).ToHaveTextAsync("False|Idle|0");
        await Expect(Page.Locator("#starts")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#moves")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#cancels")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#refreshes")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Behavior_ThresholdFactorMarginAndMaxPull_ShapeThePull()
    {
        await OpenAsync(Route);

        var strip = Page.Locator("#ptr-behavior .bit-ptr-lod");
        var (x, y) = await PullStartPointAsync("#ptr-behavior");

        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();

        // Inside the 20px dead zone nothing is drawn.
        await Page.Mouse.MoveAsync(x, y + 15, new() { Steps = 3 });
        await NextFramesAsync();
        await Expect(Page.Locator("#behavior-moves")).ToHaveTextAsync("0");

        // (100 - 20) / 2 = 40, drawn at 40 x 2 + 10.
        await Page.Mouse.MoveAsync(x, y + 100, new() { Steps = 6 });
        await Expect(Page.Locator("#behavior-move")).ToHaveTextAsync("40");
        await Expect(strip).ToHaveCSSAsync("min-height", "90px");

        // (300 - 20) / 2 = 140: past the Trigger of 100, up to the MaxPull of 150.
        await Page.Mouse.MoveAsync(x, y + 300, new() { Steps = 10 });
        await Expect(Page.Locator("#behavior-move")).ToHaveTextAsync("140");
        await Expect(strip).ToHaveCSSAsync("min-height", "290px");

        // Further still, held at the MaxPull.
        await Page.Mouse.MoveAsync(x, y + 400, new() { Steps = 5 });
        await Expect(Page.Locator("#behavior-move")).ToHaveTextAsync("150");

        await Page.Mouse.UpAsync();
        await Expect(Page.Locator("#behavior-end")).ToHaveTextAsync("150");
        await Expect(Page.Locator("#behavior-refreshes")).ToHaveTextAsync("1");
        await Expect(strip).ToHaveCSSAsync("min-height", "0px");
    }

    [TestMethod]
    public async Task Direction_Up_PullsFromTheBottomOfTheAnchor()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#ptr-up");
        await Expect(root).ToHaveClassAsync(HasClass("bit-ptr-up"));
        await Expect(root.Locator(".bit-ptr-lod")).ToHaveCSSAsync("bottom", "0px");

        // At the top of its list, an upward pull is the list's scroll.
        var (x, _) = await PullStartPointAsync("#ptr-up");
        var box = (await root.BoundingBoxAsync())!;
        var bottom = MathF.Round(box.Y + box.Height - 20);
        await MousePullAsync(x, bottom, -150);
        await Page.ClickAsync("#btn-read");
        await Expect(Page.Locator("#up-refreshes")).ToHaveTextAsync("0");

        // The drag selected text; a press on a selection would start the browser's own drag and drop of it.
        await Page.EvaluateAsync("() => window.getSelection().removeAllRanges()");
        await Page.Locator("#up-anchor").EvaluateAsync("el => el.scrollTop = el.scrollHeight");
        (x, _) = await PullStartPointAsync("#ptr-up");
        box = (await root.BoundingBoxAsync())!;
        bottom = MathF.Round(box.Y + box.Height - 20);

        await Page.Mouse.MoveAsync(x, bottom);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x, bottom - 150, new() { Steps = 15 });
        await Expect(Page.Locator("#up-move")).ToHaveTextAsync("80");
        await Expect(root.Locator(".bit-ptr-lod")).ToHaveCSSAsync("min-height", "150px");
        // The strip opens from the bottom edge of the anchor.
        var strip = (await root.Locator(".bit-ptr-lod").BoundingBoxAsync())!;
        box = (await root.BoundingBoxAsync())!;
        Assert.AreEqual(box.Y + box.Height, strip.Y + strip.Height, 1);
        Assert.AreEqual(150, strip.Height, 1);
        await Page.Mouse.UpAsync();

        await Expect(Page.Locator("#up-refreshes")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task ScrollerSelector_TheNamedScrollerDecidesWhereThePullStarts_AndFullWidthFillsTheParent()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#ptr-selector");
        await Expect(root).ToHaveClassAsync(HasClass("bit-ptr-flw"));
        await Expect(root).ToHaveCSSAsync("width", "500px");
        await Expect(Page.Locator("#selector-scroller")).ToHaveCSSAsync("overscroll-behavior-y", "contain");

        var (x, y) = await PullStartPointAsync("#ptr-selector");
        await MousePullAsync(x, y, 150);
        await Expect(Page.Locator("#selector-refreshes")).ToHaveTextAsync("1");

        await Page.Locator("#selector-scroller").EvaluateAsync("el => el.scrollTop = 100");
        await NextFramesAsync();
        await MousePullAsync(x, y + 40, 150);
        await Page.ClickAsync("#btn-read");
        await Expect(Page.Locator("#selector-refreshes")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task ScrollerElement_TheReferencedScrollerDecidesWhereThePullStarts()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#ptr-element")).ToHaveCSSAsync("width", "300px");
        await Expect(Page.Locator("#element-scroller")).ToHaveCSSAsync("overscroll-behavior-y", "contain");

        var (x, y) = await PullStartPointAsync("#ptr-element");
        await MousePullAsync(x, y + 40, 150);
        await Expect(Page.Locator("#element-refreshes")).ToHaveTextAsync("1");

        await Page.Locator("#element-scroller").EvaluateAsync("el => el.scrollTop = 100");
        await NextFramesAsync();
        await MousePullAsync(x, y + 40, 150);
        await Page.ClickAsync("#btn-read");
        await Expect(Page.Locator("#element-refreshes")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task CompleteDelay_HoldsTheCheckmarkAfterTheRefresh_ThenGoesIdle()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#ptr-complete");
        var (x, y) = await PullStartPointAsync("#ptr-complete");
        await MousePullAsync(x, y, 150);

        await Expect(Page.Locator("#complete-refreshes")).ToHaveTextAsync("1");
        await Expect(root.Locator(".bit-ptr-spw")).ToHaveClassAsync(HasClass("bit-ptr-cmp"));
        await Expect(root.Locator("[role=status]")).ToHaveTextAsync("Refresh complete");
        await Expect(root.Locator(".bit-ptr-spn path")).ToHaveAttributeAsync("d", "M9 16.17 4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z");

        await Expect(root.Locator("[role=status]")).ToHaveTextAsync("");
        await Expect(root.Locator(".bit-ptr-spw")).Not.ToHaveClassAsync(HasClass("bit-ptr-cmp"));
        await Expect(root.Locator(".bit-ptr-lod")).ToHaveCSSAsync("min-height", "0px");
    }

    [TestMethod]
    public async Task Templates_LoadingReleaseAndComplete_ReplaceTheGlyphOfTheirStage()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#tpl-loading")).ToHaveCountAsync(1);

        var (x, y) = await PullStartPointAsync("#ptr-templates");
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x, y + 60, new() { Steps = 6 });
        await Expect(Page.Locator("#tpl-loading")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#tpl-release")).ToHaveCountAsync(0);

        await Page.Mouse.MoveAsync(x, y + 150, new() { Steps = 6 });
        await Expect(Page.Locator("#tpl-release")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#tpl-loading")).ToHaveCountAsync(0);

        await Page.Mouse.UpAsync();
        await Expect(Page.Locator("#tpl-complete")).ToHaveCountAsync(1);

        await Expect(Page.Locator("#tpl-loading")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#tpl-complete")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task IndicatorTemplate_IsHandedTheStateAndTheProgress()
    {
        await OpenAsync(Route);

        var indicator = Page.Locator("#ind");
        await Expect(indicator).ToHaveTextAsync("Idle|0");
        await Expect(Page.Locator("#ptr-indicator .bit-ptr-ind")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Page.Locator("#ptr-indicator .bit-ptr-spw")).ToHaveCountAsync(0);

        var (x, y) = await PullStartPointAsync("#ptr-indicator");
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x, y + 60, new() { Steps = 6 });
        await Expect(indicator).ToHaveTextAsync("Pulling|0.5");

        await Page.Mouse.MoveAsync(x, y + 150, new() { Steps = 6 });
        await Expect(indicator).ToHaveTextAsync("CanRelease|1");

        await Page.Mouse.UpAsync();
        await Expect(indicator).ToHaveTextAsync("Complete|1");
        await Expect(indicator).ToHaveTextAsync("Idle|0");
    }

    [TestMethod]
    public async Task RefreshAsync_RunsARefreshFromCode_WithTheLabelsAnnounced_AndCompletesWithIt()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#ptr-programmatic");
        var live = root.Locator("[role=status]");
        await Expect(root).ToHaveAttributeAsync("aria-label", "Messages");
        await Expect(root).ToHaveAttributeAsync("role", "group");

        await Page.ClickAsync("#btn-refresh");

        await Expect(Page.Locator("#programmatic-refreshes")).ToHaveTextAsync("1");
        await Expect(live).ToHaveTextAsync("Loading new messages");
        await Expect(root.Locator(".bit-ptr-spw")).ToHaveClassAsync(HasClass("bit-ptr-swr"));
        // The strip opens to the trigger's height without a pull.
        await Expect(root.Locator(".bit-ptr-lod")).ToHaveCSSAsync("min-height", "150px");
        await Page.ClickAsync("#btn-read-programmatic");
        await Expect(Page.Locator("#programmatic-read")).ToHaveTextAsync("True|Refreshing");
        await Expect(Page.Locator("#programmatic-done")).ToHaveTextAsync("0");

        // A second call while one runs is ignored.
        await Page.ClickAsync("#btn-refresh");

        await Page.ClickAsync("#btn-finish-programmatic");
        await Expect(live).ToHaveTextAsync("Messages are up to date");
        await Expect(live).ToHaveTextAsync("");
        await Expect(Page.Locator("#programmatic-done")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#programmatic-refreshes")).ToHaveTextAsync("1");

        // The custom ReleaseLabel speaks during a pull.
        var (x, y) = await PullStartPointAsync("#ptr-programmatic");
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x, y + 150, new() { Steps = 10 });
        await Expect(live).ToHaveTextAsync("Release to load new messages");
        await Page.Mouse.MoveAsync(x, y - 10, new() { Steps = 4 });
        await Page.Mouse.UpAsync();
        await Expect(live).ToHaveTextAsync("");
    }

    [TestMethod]
    public async Task Disabled_TakesNoPull_LeavesTheContentLive_AndEnablingItAtRuntimeBringsItBack()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#ptr-disabled");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(root).ToHaveCSSAsync("touch-action", "auto");

        var (x, y) = await PullStartPointAsync("#ptr-disabled", fromTop: 40);
        await MousePullAsync(x, y, 150);

        await Page.ClickAsync("#disabled-content-btn");
        await Expect(Page.Locator("#disabled-content-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#disabled-starts")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#disabled-refreshes")).ToHaveTextAsync("0");

        // The drag selected text and so scrolled the anchor a little: a disabled one leaves its scroll to the browser.
        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Page.Locator("#disabled-anchor").EvaluateAsync("el => el.scrollTop = 0");
        // Firefox has no pan-up / pan-down touch-action values, so the one the script writes is dropped there.
        if (IsFirefox is false) await Expect(root).ToHaveCSSAsync("touch-action", "pan-x pan-down pinch-zoom");
        await Expect(Page.Locator("#disabled-anchor")).ToHaveCSSAsync("overscroll-behavior-y", "contain");

        (x, y) = await PullStartPointAsync("#ptr-disabled", fromTop: 40);
        await MousePullAsync(x, y, 150);
        await Expect(Page.Locator("#disabled-starts")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#disabled-refreshes")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task NoMouse_AMouseDragIsNoPull()
    {
        await OpenAsync(Route);

        var (x, y) = await PullStartPointAsync("#ptr-nomouse");
        await MousePullAsync(x, y, 150);

        await Page.ClickAsync("#btn-read");
        await Expect(Page.Locator("#nomouse-starts")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#nomouse-refreshes")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Cascade_BitParamsSetsTheDefaults_AnExplicitParameterWins_AndAChangeReachesThem()
    {
        await OpenAsync(Route);

        var cascaded = Page.Locator("#ptr-cascade");
        var own = Page.Locator("#ptr-cascade-own");
        await Expect(cascaded).ToHaveAttributeAsync("style", new Regex(@"--bit-ptr-color:\s*var\(--bit-clr-err\)"));
        await Expect(own).ToHaveAttributeAsync("style", new Regex(@"--bit-ptr-color:\s*var\(--bit-clr-suc\)"));

        // 70px of travel is 46.7: past the cascaded Trigger of 40, short of the own 200.
        var (x, y) = await PullStartPointAsync("#ptr-cascade");
        await MousePullAsync(x, y, 70);
        await Expect(Page.Locator("#cascade-refreshes")).ToHaveTextAsync("1");

        (x, y) = await PullStartPointAsync("#ptr-cascade-own");
        await MousePullAsync(x, y, 150);
        await Page.ClickAsync("#btn-read");
        await Expect(Page.Locator("#cascade-own-refreshes")).ToHaveTextAsync("0");

        await Page.ClickAsync("#btn-cascade-change");

        await Expect(cascaded).ToHaveAttributeAsync("style", new Regex(@"--bit-ptr-color:\s*var\(--bit-clr-wrn\)"));
        await Expect(own).ToHaveAttributeAsync("style", new Regex(@"--bit-ptr-color:\s*var\(--bit-clr-suc\)"));

        (x, y) = await PullStartPointAsync("#ptr-cascade");
        await MousePullAsync(x, y, 70);
        await Page.ClickAsync("#btn-read");
        await Expect(Page.Locator("#cascade-refreshes")).ToHaveTextAsync("1");
    }

    [TestMethod]
    [DataRow("Primary", "pri")]
    [DataRow("Secondary", "sec")]
    [DataRow("Tertiary", "ter")]
    [DataRow("Info", "inf")]
    [DataRow("Success", "suc")]
    [DataRow("Warning", "wrn")]
    [DataRow("SevereWarning", "swr")]
    [DataRow("Error", "err")]
    [DataRow("PrimaryBackground", "bg-pri")]
    [DataRow("SecondaryBackground", "bg-sec")]
    [DataRow("TertiaryBackground", "bg-ter")]
    [DataRow("PrimaryForeground", "fg-pri")]
    [DataRow("SecondaryForeground", "fg-sec")]
    [DataRow("TertiaryForeground", "fg-ter")]
    [DataRow("PrimaryBorder", "brd-pri")]
    [DataRow("SecondaryBorder", "brd-sec")]
    [DataRow("TertiaryBorder", "brd-ter")]
    public async Task Color_EachValue_PaintsTheGlyphWithItsThemeColor(string color, string token)
    {
        await OpenAsync(Route);

        await Page.ClickAsync($"#btn-color-{color}");
        await Expect(Page.Locator("#color-current")).ToHaveTextAsync(color);

        var root = Page.Locator("#ptr-color");
        await Expect(root).ToHaveAttributeAsync("style", new Regex($@"^--bit-ptr-color:\s*var\(--bit-clr-{token}\)"));

        var expected = await ResolveColorAsync($"var(--bit-clr-{token})");
        await Expect(root.Locator(".bit-ptr-spn")).ToHaveCSSAsync("color", expected);
    }

    [TestMethod]
    public async Task CustomColor_AppliesWhileColorIsUnset()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#ptr-color");
        var primary = await ResolveColorAsync("var(--bit-clr-pri)");
        await Expect(root.Locator(".bit-ptr-spn")).ToHaveCSSAsync("color", primary);

        await Page.ClickAsync("#btn-color-none");
        await Expect(root).ToHaveAttributeAsync("style", new Regex(@"^--bit-ptr-color:\s*#b400ff"));
        await Expect(root.Locator(".bit-ptr-spn")).ToHaveCSSAsync("color", "rgb(180, 0, 255)");
    }

    [TestMethod]
    [Ignore("KNOWN BUG: the script writes touch-action inline on the root and caches what it wrote (touchActionInEffect), so a render that rewrites the root's style attribute (a runtime change of Color, CustomColor, Style, Styles.Root or Visibility) drops it for good until the scroller scrolls or a gesture parameter changes.")]
    public async Task TouchAction_SurvivesARenderThatRewritesTheRootStyle()
    {
        await OpenAsync(Route);

        // The script hands the pull's direction to the browser as an inline touch-action on the root.
        var root = Page.Locator("#ptr-color");
        await Expect(root).ToHaveCSSAsync("touch-action", "pan-x pan-down pinch-zoom");

        // A Color changed at runtime renders the root's style attribute anew.
        await Page.ClickAsync("#btn-color-Secondary");
        await Expect(root).ToHaveAttributeAsync("style", new Regex("bit-clr-sec"));

        await Expect(root).ToHaveCSSAsync("touch-action", "pan-x pan-down pinch-zoom");
    }

    [TestMethod]
    public async Task Classes_ReachEveryPart_InEveryState()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#ptr-classes");
        var wrapper = root.Locator(".bit-ptr-spw");
        var spinner = root.Locator(".bit-ptr-spn");

        await Expect(root).ToHaveClassAsync(HasClass("e2e-ptr-class"));
        await Expect(root).ToHaveClassAsync(HasClass("e2e-ptr-cls-root"));
        await Expect(root).ToHaveCSSAsync("border-top-color", "rgb(10, 0, 0)");
        await Expect(root.Locator(".bit-ptr-lod")).ToHaveClassAsync(HasClass("e2e-ptr-cls-loading"));
        await Expect(wrapper).ToHaveClassAsync(HasClass("e2e-ptr-cls-spw"));
        await Expect(wrapper).ToHaveCSSAsync("outline-color", "rgb(30, 0, 0)");
        await Expect(spinner).ToHaveClassAsync(HasClass("e2e-ptr-cls-spn"));

        var (x, y) = await PullStartPointAsync("#ptr-classes");
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x, y + 150, new() { Steps = 10 });
        await Expect(wrapper).ToHaveClassAsync(HasClass("e2e-ptr-cls-spw-crl"));
        await Expect(wrapper).ToHaveCSSAsync("outline-color", "rgb(40, 0, 0)");
        await Expect(spinner).ToHaveClassAsync(HasClass("e2e-ptr-cls-spn-crl"));

        await Page.Mouse.UpAsync();
        await Expect(wrapper).ToHaveClassAsync(HasClass("e2e-ptr-cls-spw-swr"));
        await Expect(wrapper).ToHaveCSSAsync("outline-color", "rgb(50, 0, 0)");
        await Expect(spinner).ToHaveClassAsync(HasClass("e2e-ptr-cls-spn-swr"));
        await Expect(spinner).ToHaveCSSAsync("border-left-color", "rgb(90, 0, 0)");

        await Page.ClickAsync("#btn-finish-vars");
        await Expect(wrapper).ToHaveClassAsync(HasClass("e2e-ptr-cls-spw-cmp"));
        await Expect(wrapper).ToHaveCSSAsync("outline-color", "rgb(60, 0, 0)");
        await Expect(spinner).ToHaveClassAsync(HasClass("e2e-ptr-cls-spn-cmp"));

        await Expect(wrapper).Not.ToHaveClassAsync(HasClass("e2e-ptr-cls-spw-cmp"));
        await Expect(wrapper).ToHaveClassAsync(HasClass("e2e-ptr-cls-spw"));
    }

    [TestMethod]
    public async Task Styles_ReachEveryPart_InEveryState()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#ptr-styles");
        var wrapper = root.Locator(".bit-ptr-spw");
        var spinner = root.Locator(".bit-ptr-spn");

        await Expect(root).ToHaveCSSAsync("padding-top", "3px");
        await Expect(root).ToHaveCSSAsync("margin-left", "5px");
        await Expect(root.Locator(".bit-ptr-lod")).ToHaveCSSAsync("padding-bottom", "4px");
        await Expect(wrapper).ToHaveCSSAsync("padding-left", "1px");
        await Expect(spinner).ToHaveCSSAsync("margin-left", "1px");

        var (x, y) = await PullStartPointAsync("#ptr-styles");
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x, y + 150, new() { Steps = 10 });
        await Expect(wrapper).ToHaveCSSAsync("padding-right", "2px");
        await Expect(spinner).ToHaveCSSAsync("margin-right", "2px");

        await Page.Mouse.UpAsync();
        await Expect(wrapper).ToHaveCSSAsync("padding-right", "3px");
        await Expect(spinner).ToHaveCSSAsync("margin-right", "3px");

        await Page.ClickAsync("#btn-finish-vars");
        await Expect(wrapper).ToHaveCSSAsync("padding-right", "4px");
        await Expect(spinner).ToHaveCSSAsync("margin-right", "4px");

        await Expect(wrapper).ToHaveCSSAsync("padding-right", "0px");
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheStripTheDiscAndTheGlyph_InEveryState()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#ptr-vars");
        var strip = root.Locator(".bit-ptr-lod");
        var wrapper = root.Locator(".bit-ptr-spw");
        var spinner = root.Locator(".bit-ptr-spn");

        await Expect(strip).ToHaveCSSAsync("z-index", "7");
        await Expect(strip).ToHaveCSSAsync("background-color", "rgb(255, 200, 200)");

        var (x, y) = await PullStartPointAsync("#ptr-vars");
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x, y + 60, new() { Steps = 6 });
        // Half way: the faded glyph at half its sizes.
        await Expect(spinner).ToHaveCSSAsync("opacity", "0.3");
        await Expect(wrapper).ToHaveCSSAsync("width", "25px");
        await Expect(spinner).ToHaveCSSAsync("width", "15px");

        await Page.Mouse.MoveAsync(x, y + 150, new() { Steps = 6 });
        await Expect(spinner).ToHaveCSSAsync("opacity", "1");
        await Expect(wrapper).ToHaveCSSAsync("width", "50px");
        await Expect(spinner).ToHaveCSSAsync("width", "30px");
        await Expect(wrapper).ToHaveCSSAsync("background-color", "rgb(255, 255, 0)");
        await Expect(wrapper).ToHaveCSSAsync("border-top-left-radius", "4px");
        await Expect(wrapper).ToHaveCSSAsync("box-shadow", "none");
        await Expect(spinner).ToHaveCSSAsync("color", "rgb(0, 100, 0)");

        await Page.Mouse.UpAsync();
        await Expect(wrapper).ToHaveClassAsync(HasClass("bit-ptr-swr"));
        await Expect(wrapper).ToHaveCSSAsync("background-color", "rgb(0, 0, 255)");

        await Page.ClickAsync("#btn-finish-vars");
        await Expect(wrapper).ToHaveClassAsync(HasClass("bit-ptr-cmp"));
        await Expect(wrapper).ToHaveCSSAsync("background-color", "rgb(0, 255, 0)");
    }

    [TestMethod]
    public async Task Rtl_DirRendersTheClassAndTheAttribute_AndThePullStillWorks()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#ptr-rtl");
        await Expect(root).ToHaveAttributeAsync("dir", "rtl");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(root).ToHaveCSSAsync("direction", "rtl");
        await Expect(root).ToHaveAttributeAsync("data-e2e", "ptr-attr");
        await Expect(root).ToHaveAttributeAsync("title", "ptr title");

        var (x, y) = await PullStartPointAsync("#ptr-rtl");
        await MousePullAsync(x, y, 150);
        await Expect(Page.Locator("#rtl-refreshes")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Visibility_HiddenAndCollapsed()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#ptr-vis");
        await Expect(root).ToBeVisibleAsync();

        await Page.ClickAsync("#btn-vis-hidden");
        await Expect(root).ToHaveCSSAsync("visibility", "hidden");

        await Page.ClickAsync("#btn-vis-collapsed");
        await Expect(root).ToHaveCSSAsync("display", "none");

        await Page.ClickAsync("#btn-vis-visible");
        await Expect(root).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ReducedMotion_TheStripClosesWithoutATransition()
    {
        await OpenAsync(Route);

        var full = await TransitionMsAsync("#ptr-basic .bit-ptr-lod", "bit-ptr-rtn");
        Assert.IsTrue(full >= 50, $"The strip does not animate without reduced motion ({full}ms).");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        Assert.IsTrue(await TransitionMsAsync("#ptr-basic .bit-ptr-lod", "bit-ptr-rtn") < 1, "The strip still animates under reduced motion.");
    }

    [TestMethod]
    public async Task Unmount_InTheMiddleOfARefresh_LeavesNoErrorBehind()
    {
        await OpenAsync(Route);

        var (x, y) = await PullStartPointAsync("#ptr-unmount");
        await MousePullAsync(x, y, 150);
        await Expect(Page.Locator("#unmount-refreshes")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#ptr-unmount .bit-ptr-spw")).ToHaveClassAsync(HasClass("bit-ptr-swr"));

        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#ptr-unmount")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-finish-unmount");
        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#ptr-unmount")).ToHaveCountAsync(1);

        // The one mounted again pulls as usual.
        (x, y) = await PullStartPointAsync("#ptr-unmount");
        await MousePullAsync(x, y, 150);
        await Expect(Page.Locator("#unmount-refreshes")).ToHaveTextAsync("2");
        await Page.ClickAsync("#btn-finish-unmount");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    // Brings the pull to refresh to the top of the viewport and returns a point near the top of its anchor.
    private async Task<(float X, float Y)> PullStartPointAsync(string selector, float fromTop = 20)
    {
        var root = Page.Locator(selector);
        await root.EvaluateAsync("el => el.scrollIntoView({ block: 'start' })");
        await NextFramesAsync();
        var box = (await root.BoundingBoxAsync())!;

        return (MathF.Round(box.X + 150), MathF.Round(box.Y + fromTop));
    }

    private async Task MousePullAsync(float x, float y, float dy, int steps = 10)
    {
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x, y + dy, new() { Steps = steps });
        await Page.Mouse.UpAsync();
    }

    private Task<string> ResolveColorAsync(string value)
    {
        return Page.EvaluateAsync<string>(@"value => {
            const probe = document.createElement('span');
            probe.style.color = value;
            document.body.appendChild(probe);
            const color = getComputedStyle(probe).color;
            probe.remove();
            return color;
        }", value);
    }

    // The transition of an element while it carries the given class, put on for the reading and taken off again.
    private Task<double> TransitionMsAsync(string selector, string cssClass)
    {
        return Page.Locator(selector).EvaluateAsync<double>(@"(el, cls) => {
            const had = el.classList.contains(cls);
            el.classList.add(cls);
            const v = getComputedStyle(el).transitionDuration.split(',')[0].trim();
            if (!had) el.classList.remove(cls);
            return v.endsWith('ms') ? parseFloat(v) : parseFloat(v) * 1000;
        }", cssClass);
    }
}
