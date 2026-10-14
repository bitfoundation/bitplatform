using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Surfaces;

/// <summary>
/// Drives BitScrollablePane (/e2e/scrollable-pane) in a real browser: the wheel, the keyboard and the pointer scrolling
/// it; the dimension parameters; Overflow / OverflowX / OverflowY / NoScroll / Horizontal; Overscroll and the scroll it
/// chains to the pane behind; ScrollbarWidth / ScrollbarColor / Gutter / Modern and the auto-hidden bar; Fade on every
/// edge that has more and FadeSize; Focusable / AriaLabel / Role / TabIndex / Disabled; OnScroll with its offset, OnScrollStart,
/// OnScrollEnd and ScrollThrottle; the four OnReached* callbacks with ReachOffset; ScrollToStart / ScrollToEnd / ScrollTo /
/// ScrollBy / ScrollToElement in every alignment / GetScrollOffset / FocusAsync / Refresh, Smooth and reduced motion,
/// InitialScrollTop / Left; AutoScroll with its threshold; PreserveScroll; Snap / SnapAlign / SnapStop; DragScroll,
/// DragMomentum, HorizontalWheel; the public CSS variables; the BitParams cascade; Style / Class; RTL; ExpandOnPrint;
/// Visibility, HtmlAttributes, prerendering, re-rendering and unmounting.
/// </summary>
[TestClass]
public class BitScrollablePaneE2ETests : E2ETestBase
{
    private const string Url = "/e2e/scrollable-pane";

    private ILocator El(string id) => Page.Locator($"#{id}");

    [TestMethod]
    public async Task Prerender_TheServerHtml_ContainsThePaneWithItsInlineSizing()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Url}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        Assert.IsTrue(Regex.IsMatch(html, "id=\"scp-basic\"[^>]*style=\"height:10rem\""), "The pane is not prerendered with its height.");
        Assert.IsTrue(Regex.IsMatch(html, "id=\"scp-focus\"[^>]*role=\"region\""), "The named pane is not prerendered as a region.");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Basic_TheWheelScrollsThePane()
    {
        await OpenAsync(Url);

        var pane = El("scp-basic");
        await Expect(pane).ToHaveCSSAsync("overflow-y", "auto");
        Near(160, (await BoxAsync(pane)).Height, "Height 10rem");
        Assert.IsTrue(await pane.EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight"));

        await WheelOverAsync(pane, 0, 120);
        await PollAsync(() => ScrollTopAsync(pane), top => top > 0, "the wheel did not scroll the pane");
    }

    [TestMethod]
    public async Task Dimensions_EverySizingParameter_ReachesTheBox()
    {
        await OpenAsync(Url);

        var size = await BoxAsync(El("scp-size"));
        Near(288, size.Width, "Width 18rem");
        Near(96, size.Height, "Height 6rem");

        var capped = await BoxAsync(El("scp-minmax"));
        Assert.IsTrue(capped.Width <= 192.5 && capped.Width >= 159.5, $"Min/MaxWidth: {capped.Width}");
        Near(80, capped.Height, "MaxHeight 5rem");

        var full = await BoxAsync(El("scp-full"));
        Near(300, full.Width, "FullSize width");
        Near(120, full.Height, "FullSize height");
        Near(300, (await BoxAsync(El("scp-fullw"))).Width, "FullWidth");
        Near(120, (await BoxAsync(El("scp-fullh"))).Height, "FullHeight");

        var fit = await BoxAsync(El("scp-fit"));
        var fitText = await BoxAsync(El("scp-fit-text"));
        Near(fitText.Width, fit.Width, "FitSize width");
        await Expect(El("scp-fit")).ToHaveAttributeAsync("style", new Regex("width:fit-content"));
        await Expect(El("scp-fitw")).ToHaveAttributeAsync("style", new Regex("width:fit-content"));
        Assert.IsTrue((await BoxAsync(El("scp-fith"))).Height < 100, "FitHeight does not win over the Height written before it.");

        Assert.IsTrue((await BoxAsync(El("scp-auto"))).Width > 100, "AutoSize does not win over the Width.");
        Assert.IsTrue((await BoxAsync(El("scp-autow"))).Width > 100, "AutoWidth does not win over the Width.");
        Assert.IsTrue((await BoxAsync(El("scp-autoh"))).Height > 10, "AutoHeight does not win over the Height.");
    }

    [TestMethod]
    [DataRow("auto", "auto")]
    [DataRow("hidden", "hidden")]
    [DataRow("scroll", "scroll")]
    [DataRow("visible", "visible")]
    public async Task Overflow_EveryValue_ReachesBothAxes(string name, string expected)
    {
        await OpenAsync(Url);

        var pane = El($"scp-ov-{name}");
        await Expect(pane).ToHaveCSSAsync("overflow-x", expected);
        await Expect(pane).ToHaveCSSAsync("overflow-y", expected);

        var scrollable = expected is "auto" or "scroll";
        await pane.EvaluateAsync("el => { el.scrollTop = 30; el.scrollLeft = 30; }");
        if (expected == "visible")
        {
            Assert.AreEqual(0, await ScrollTopAsync(pane));
        }
        else if (scrollable)
        {
            Assert.IsTrue(await ScrollTopAsync(pane) > 0);
            await WheelOverAsync(pane, 0, -200);
            await PollAsync(() => ScrollTopAsync(pane), top => top == 0, "the wheel did not scroll it back");
        }
    }

    [TestMethod]
    public async Task OverflowXAndY_HaveTheLastWordOnTheirAxis()
    {
        await OpenAsync(Url);

        await Expect(El("scp-ovxy")).ToHaveCSSAsync("overflow-x", "scroll");
        await Expect(El("scp-ovxy")).ToHaveCSSAsync("overflow-y", "auto");
    }

    [TestMethod]
    public async Task NoScroll_ClipsAndIgnoresTheWheelTheKeyboardAndTheDrag()
    {
        await OpenAsync(Url);

        var pane = El("scp-noscroll");
        await pane.ScrollIntoViewIfNeededAsync();
        await Expect(pane).ToHaveCSSAsync("overflow-x", "hidden");
        await Expect(pane).ToHaveCSSAsync("overflow-y", "hidden");
        await Expect(pane).Not.ToHaveAttributeAsync("tabindex", new Regex(".*"));
        await Expect(pane).Not.ToHaveClassAsync(HasClass("bit-scp-drg"));

        await WheelOverAsync(pane, 0, 200);
        var box = await BoxAsync(pane);
        await Page.Mouse.MoveAsync((float)(box.X + 20), (float)(box.Y + box.Height - 5));
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync((float)(box.X + 20), (float)(box.Y + 5), new() { Steps = 5 });
        await Page.Mouse.UpAsync();
        await NextFramesAsync();
        Assert.AreEqual(0, await ScrollTopAsync(pane));
    }

    [TestMethod]
    public async Task Horizontal_KeepsTheContentOnOneLine_AndScrollsItSideways()
    {
        await OpenAsync(Url);

        var pane = El("scp-hor");
        await Expect(pane).ToHaveClassAsync(HasClass("bit-scp-hor"));
        await Expect(pane).ToHaveCSSAsync("white-space", "nowrap");
        await Expect(pane).ToHaveCSSAsync("overflow-x", "auto");
        await Expect(pane).ToHaveCSSAsync("overflow-y", "hidden");
        Assert.IsTrue(await pane.EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth"));

        await WheelOverAsync(pane, 150, 0);
        await PollAsync(() => pane.EvaluateAsync<double>("el => el.scrollLeft"), left => left > 0, "a sideways wheel does not scroll it");
    }

    [TestMethod]
    public async Task Overscroll_EveryValue_AndOneAxisEach_ReachTheStyle()
    {
        await OpenAsync(Url);

        foreach (var name in new[] { "auto", "contain", "none" })
        {
            await Expect(El($"scp-os-{name}")).ToHaveCSSAsync("overscroll-behavior-x", name);
            await Expect(El($"scp-os-{name}")).ToHaveCSSAsync("overscroll-behavior-y", name);
        }

        await Expect(El("scp-osxy")).ToHaveCSSAsync("overscroll-behavior-x", "none");
        await Expect(El("scp-osxy")).ToHaveCSSAsync("overscroll-behavior-y", "contain");
    }

    [TestMethod]
    public async Task Overscroll_AutoChainsTheScrollToThePaneBehind_ContainStopsIt()
    {
        if (IsChromium is false) Assert.Inconclusive("Scroll chaining of a synthesized wheel is only deterministic in Chromium.");

        await OpenAsync(Url);

        await El("scp-inner-auto").EvaluateAsync("el => el.scrollTop = el.scrollHeight");
        await WheelOverAsync(El("scp-inner-auto"), 0, 150);
        await PollAsync(() => ScrollTopAsync(El("scp-outer-auto")), top => top > 0, "an Auto pane at its end does not hand the scroll on");

        await El("scp-inner-contain").EvaluateAsync("el => el.scrollTop = el.scrollHeight");
        await WheelOverAsync(El("scp-inner-contain"), 0, 150);
        // The same wheel the Auto pane handed on within a few frames, given the time to do the same.
        await Page.WaitForTimeoutAsync(300);
        Assert.AreEqual(0, await ScrollTopAsync(El("scp-outer-contain")), "a Contain pane handed the scroll on");
    }

    [TestMethod]
    public async Task Scrollbar_WidthColorAndGutter_ReachTheStyle()
    {
        await OpenAsync(Url);

        await Expect(El("scp-sbw-thin")).ToHaveCSSAsync("scrollbar-width", "thin");
        await Expect(El("scp-sbw-none")).ToHaveCSSAsync("scrollbar-width", "none");
        await Expect(El("scp-sbw-auto")).ToHaveCSSAsync("scrollbar-width", "auto");
        await El("scp-sbw-none").EvaluateAsync("el => el.scrollTop = 20");
        Assert.IsTrue(await ScrollTopAsync(El("scp-sbw-none")) > 0, "A hidden scrollbar stops the pane from scrolling.");

        await Expect(El("scp-sbc")).ToHaveCSSAsync("scrollbar-color", "rgb(255, 0, 0) rgb(0, 0, 255)");
        await Expect(El("scp-gut-stable")).ToHaveCSSAsync("scrollbar-gutter", "stable");
        await Expect(El("scp-gut-both")).ToHaveCSSAsync("scrollbar-gutter", "stable both-edges");
        await Expect(El("scp-gut-auto")).ToHaveCSSAsync("scrollbar-gutter", "auto");
    }

    [TestMethod]
    public async Task Modern_AutoHideScrollbar_HidesTheBarWhileIdle_AndRevealsItOnHoverAndScroll()
    {
        await OpenAsync(Url);

        var pane = El("scp-modern");
        await Expect(pane).ToHaveClassAsync(HasClass("bit-scp-mod"));
        await Expect(pane).ToHaveClassAsync(HasClass("bit-scp-ahs"));
        Assert.IsTrue((await StyleAsync(pane, "--bit-scp-sbc")).Trim().Length > 0, "The Modern thumb color is not resolved.");

        await Expect(pane).ToHaveAttributeAsync("data-bit-scp-idle", "");

        await pane.HoverAsync();
        await Expect(pane).Not.ToHaveAttributeAsync("data-bit-scp-idle", new Regex(".*"));

        await El("scp-modern-away").HoverAsync();
        await Expect(pane).ToHaveAttributeAsync("data-bit-scp-idle", "");

        await pane.EvaluateAsync("el => el.scrollTop = 40");
        await Expect(pane).Not.ToHaveAttributeAsync("data-bit-scp-idle", new Regex(".*"));
        await Expect(pane).ToHaveAttributeAsync("data-bit-scp-idle", "");
    }

    [TestMethod]
    public async Task Fade_FadesEveryEdgeThatHasMoreBeyondIt()
    {
        await OpenAsync(Url);

        var pane = El("scp-fade");
        await Expect(pane).ToHaveClassAsync(HasClass("bit-scp-fad"));
        await Expect(pane).ToHaveAttributeAsync("data-bit-scp-fb", "");
        await Expect(pane).Not.ToHaveAttributeAsync("data-bit-scp-ft", new Regex(".*"));
        StringAssert.Contains(await StyleAsync(pane, "mask-image"), "linear-gradient");

        await pane.EvaluateAsync("el => el.scrollTop = 80");
        await Expect(pane).ToHaveAttributeAsync("data-bit-scp-ft", "");
        await Expect(pane).ToHaveAttributeAsync("data-bit-scp-fb", "");

        await pane.EvaluateAsync("el => el.scrollTop = el.scrollHeight");
        await Expect(pane).ToHaveAttributeAsync("data-bit-scp-ft", "");
        await Expect(pane).Not.ToHaveAttributeAsync("data-bit-scp-fb", new Regex(".*"));

        var horizontal = El("scp-fade-hor");
        await Expect(horizontal).ToHaveAttributeAsync("data-bit-scp-fr", "");
        await Expect(horizontal).Not.ToHaveAttributeAsync("data-bit-scp-fl", new Regex(".*"));
        await Expect(horizontal).ToHaveAttributeAsync("style", new Regex("--bit-scp-fsz:3rem"));
        await horizontal.EvaluateAsync("el => el.scrollLeft = el.scrollWidth");
        await Expect(horizontal).ToHaveAttributeAsync("data-bit-scp-fl", "");
        await Expect(horizontal).Not.ToHaveAttributeAsync("data-bit-scp-fr", new Regex(".*"));
        StringAssert.Contains(await StyleAsync(horizontal, "mask-image"), "48px");
    }

    [TestMethod]
    public async Task Focusable_MakesThePaneATabStopTheKeyboardScrolls_NamedAsARegion()
    {
        await OpenAsync(Url);

        var pane = El("scp-focus");
        await Expect(pane).ToHaveAttributeAsync("tabindex", "0");
        await Expect(pane).ToHaveAttributeAsync("role", "region");
        await Expect(pane).ToHaveAttributeAsync("aria-label", "Release notes");
        await Expect(Page.GetByRole(AriaRole.Region, new() { Name = "Release notes" })).ToHaveCountAsync(1);

        await El("btn-focus-pane").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(pane).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("PageDown");
        await PollAsync(() => ScrollTopAsync(pane), top => top > 0, "PageDown does not scroll the focused pane");
        await Page.Keyboard.PressAsync("End");
        await PollAsync(() => pane.EvaluateAsync<bool>("el => Math.abs(el.scrollTop - (el.scrollHeight - el.clientHeight)) < 1"), done => done, "End does not reach the end");
    }

    [TestMethod]
    public async Task FocusAsync_FocusesThePane()
    {
        await OpenAsync(Url);

        await El("btn-focus-pane").ClickAsync();
        await Expect(El("scp-focus")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task RoleAndTabIndex_OverrideTheDefaults_AndDisabledLeavesTheTabOrder()
    {
        await OpenAsync(Url);

        await Expect(El("scp-role")).ToHaveAttributeAsync("role", "log");
        await Expect(El("scp-role-empty")).Not.ToHaveAttributeAsync("role", new Regex(".*"));
        await Expect(El("scp-tabindex")).ToHaveAttributeAsync("tabindex", "2");
        await Expect(El("scp-basic")).Not.ToHaveAttributeAsync("role", new Regex(".*"));

        var disabled = El("scp-disabled");
        await Expect(disabled).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(disabled).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(disabled).ToHaveCSSAsync("pointer-events", "none");
        await WheelOverAsync(disabled, 0, 200);
        await NextFramesAsync();
        Assert.AreEqual(0, await ScrollTopAsync(disabled));
    }

    [TestMethod]
    public async Task OnScroll_ReportsTheOffset_AndOnScrollStartAndEndBracketTheScroll()
    {
        await OpenAsync(Url);

        await El("btn-ev-scroll").ClickAsync();

        await Expect(El("ev-top")).ToHaveTextAsync("200");
        await Expect(El("ev-dir")).ToHaveTextAsync("down");
        await Expect(El("ev-at")).ToHaveTextAsync("");
        await Expect(El("ev-sizes")).ToHaveTextAsync("1200/128/1072/y");
        await Expect(El("ev-percent")).ToHaveTextAsync((200.0 / 1072).ToString("0.00", CultureInfo.InvariantCulture));
        await Expect(El("ev-starts")).ToHaveTextAsync("1");
        await Expect(El("ev-ends")).ToHaveTextAsync("1");
        await Expect(El("ev-end-top")).ToHaveTextAsync("200");

        await El("btn-ev-bottom").ClickAsync();
        await Expect(El("ev-at")).ToHaveTextAsync("bottom");
        await Expect(El("ev-percent")).ToHaveTextAsync("1.00");
        await Expect(El("ev-ends")).ToHaveTextAsync("2");

        await El("scp-events").EvaluateAsync("el => el.scrollTop = 0");
        await Expect(El("ev-at")).ToHaveTextAsync("top");
        await Expect(El("ev-dir")).ToHaveTextAsync("up");
    }

    [TestMethod]
    public async Task ScrollThrottle_SpacesTheReports_AndTheLastOneStillArrives()
    {
        await OpenAsync(Url);

        var pane = El("scp-throttle");
        await pane.EvaluateAsync(@"async el => {
            for (let i = 1; i <= 20; i++) {
                el.scrollTop = i * 20;
                await new Promise(r => requestAnimationFrame(r));
            }
        }");

        await Expect(El("th-top")).ToHaveTextAsync("400");
        var reports = int.Parse((await El("th-scrolls").TextContentAsync())!, CultureInfo.InvariantCulture);
        Assert.IsTrue(reports <= 6, $"{reports} reports for 20 frames of scrolling with a 500ms throttle.");
    }

    [TestMethod]
    public async Task OnReached_FiresOnArrivalAtEachEdge_WithinTheReachOffset()
    {
        await OpenAsync(Url);

        var pane = El("scp-reach");
        // Within the 20px ReachOffset of the bottom counts as there.
        await pane.EvaluateAsync("el => el.scrollTop = el.scrollHeight - el.clientHeight - 15");
        await Expect(El("reach-log")).ToHaveTextAsync(new Regex("bottom$"));
        await Expect(El("reach-rows")).ToHaveTextAsync("20");

        await pane.EvaluateAsync("el => el.scrollTop = 0");
        await Expect(El("reach-log")).ToHaveTextAsync(new Regex("bottom,top$"));

        var horizontal = El("scp-reach-hor");
        await horizontal.EvaluateAsync("el => el.scrollLeft = el.scrollWidth");
        await Expect(El("reach-hor-log")).ToHaveTextAsync(new Regex("right$"));
        await horizontal.EvaluateAsync("el => el.scrollLeft = 0");
        await Expect(El("reach-hor-log")).ToHaveTextAsync(new Regex("right,left$"));
    }

    [TestMethod]
    public async Task Methods_ScrollToStartEndToAndBy_MoveThePane()
    {
        await OpenAsync(Url);

        var pane = El("scp-m");
        var max = await pane.EvaluateAsync<double>("el => el.scrollHeight - el.clientHeight");

        await El("btn-m-end").ClickAsync();
        await PollAsync(() => ScrollTopAsync(pane), top => Math.Abs(top - max) < 1, "ScrollToEnd");
        await El("btn-m-start").ClickAsync();
        await PollAsync(() => ScrollTopAsync(pane), top => top == 0, "ScrollToStart");
        await El("btn-m-to").ClickAsync();
        await PollAsync(() => ScrollTopAsync(pane), top => top == 200, "ScrollTo(null, 200)");
        await El("btn-m-by").ClickAsync();
        await PollAsync(() => ScrollTopAsync(pane), top => top == 250, "ScrollBy(0, 50)");
    }

    [TestMethod]
    [DataRow("btn-m-el", 520)]
    [DataRow("btn-m-el-center", 500)]
    [DataRow("btn-m-el-end", 440)]
    [DataRow("btn-m-el-offset", 530)]
    [DataRow("btn-m-ref", 920)]
    public async Task ScrollToElement_BringsTheElementIntoViewWithTheAlignmentAskedFor(string button, double expected)
    {
        await OpenAsync(Url);

        // Rows are 40px; row 15 starts at 560 in a 160px pane whose ScrollPadding keeps 40px clear at the top.
        await El(button).ScrollIntoViewIfNeededAsync();
        var page = await Page.EvaluateAsync<double>("() => window.scrollY");
        await El(button).ClickAsync();
        await PollAsync(() => ScrollTopAsync(El("scp-m")), top => Math.Abs(top - expected) < 1, $"{button} did not scroll to {expected}");

        // Only the pane moves, never the page around it.
        Assert.AreEqual(page, await Page.EvaluateAsync<double>("() => window.scrollY"), 0.5);
    }

    [TestMethod]
    public async Task ScrollToElement_Nearest_MovesOnlyAsFarAsItHasTo()
    {
        await OpenAsync(Url);

        var pane = El("scp-m");
        await El("btn-m-el-nearest").ClickAsync();
        await PollAsync(() => ScrollTopAsync(pane), top => Math.Abs(top - 440) < 1, "nearest from above brings the row to the bottom edge");

        await pane.EvaluateAsync("el => el.scrollTop = 900");
        await El("btn-m-el-nearest").ClickAsync();
        await PollAsync(() => ScrollTopAsync(pane), top => Math.Abs(top - 520) < 1, "nearest from below brings the row to the top edge");

        await El("btn-m-el-nearest").ClickAsync();
        await NextFramesAsync();
        Assert.AreEqual(520, await ScrollTopAsync(pane), 1);
    }

    [TestMethod]
    public async Task GetScrollOffset_AndRefresh_ReadWhereThePaneStands()
    {
        await OpenAsync(Url);

        await El("btn-m-offset").ClickAsync();
        await Expect(El("m-offset")).ToHaveTextAsync("0|1040|True|False");

        await El("btn-m-end").ClickAsync();
        await PollAsync(() => ScrollTopAsync(El("scp-m")), top => top > 1000, "ScrollToEnd");
        await El("btn-m-offset").ClickAsync();
        await Expect(El("m-offset")).ToHaveTextAsync("1040|1040|False|True");

        await El("btn-m-refresh").ClickAsync();
        await NextFramesAsync();
        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    // A headless Chromium finishes a smooth scroll in the frame it is asked for, so what is checked is what the pane
    // asks the browser for - the behavior handed to scrollTo - along with where it ends up.
    [TestMethod]
    public async Task Smooth_AsksForASmoothMove_AndAnExplicitSmoothFalseForAnInstantOne()
    {
        await OpenAsync(Url);

        var pane = El("scp-smooth");
        await Expect(pane).ToHaveClassAsync(HasClass("bit-scp-smt"));
        await Expect(pane).ToHaveCSSAsync("scroll-behavior", "smooth");
        await RecordScrollBehaviorsAsync();

        await El("btn-smooth-end").ClickAsync();
        var max = await pane.EvaluateAsync<double>("el => el.scrollHeight - el.clientHeight");
        await PollAsync(() => ScrollTopAsync(pane), top => Math.Abs(top - max) < 1, "the smooth move never ended");

        await El("btn-smooth-instant").ClickAsync();
        await PollAsync(() => ScrollTopAsync(pane), top => top == 0, "the instant move never happened");

        CollectionAssert.AreEqual(new[] { "smooth", "instant" }, await ReadScrollBehaviorsAsync());
    }

    [TestMethod]
    public async Task Smooth_UnderReducedMotion_AsksForAnInstantMove()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await OpenAsync(Url);

        await Expect(El("scp-smooth")).ToHaveCSSAsync("scroll-behavior", "auto");
        await RecordScrollBehaviorsAsync();
        await El("btn-smooth-end").ClickAsync();
        var max = await El("scp-smooth").EvaluateAsync<double>("el => el.scrollHeight - el.clientHeight");
        await PollAsync(() => ScrollTopAsync(El("scp-smooth")), top => Math.Abs(top - max) < 1, "the move never happened");

        CollectionAssert.AreEqual(new[] { "instant" }, await ReadScrollBehaviorsAsync());
    }

    [TestMethod]
    public async Task InitialScroll_OpensThePaneAtTheGivenPosition()
    {
        await OpenAsync(Url);

        await PollAsync(() => ScrollTopAsync(El("scp-initial")), top => top == 250, "InitialScrollTop");
        await PollAsync(() => El("scp-initial-left").EvaluateAsync<double>("el => el.scrollLeft"), left => left == 300, "InitialScrollLeft");
    }

    [TestMethod]
    public async Task AutoScroll_KeepsThePanePinnedToTheEnd_UntilTheReaderScrollsAway()
    {
        await OpenAsync(Url);

        var pane = El("scp-auto-scroll");
        for (var i = 0; i < 6; i++)
        {
            await El("btn-as-add").ClickAsync();
        }

        await Expect(El("as-lines")).ToHaveTextAsync("9");
        await PollAsync(() => AtEndAsync(pane), at => at, "the pane is not pinned to the end");

        // Scrolled away beyond the 30px threshold: new lines leave the reader where they are.
        await pane.EvaluateAsync("el => el.scrollTop = 0");
        await El("btn-as-add").ClickAsync();
        await Expect(El("as-lines")).ToHaveTextAsync("10");
        await NextFramesAsync();
        Assert.AreEqual(0, await ScrollTopAsync(pane));

        // Within the threshold counts as at the end.
        await pane.EvaluateAsync("el => el.scrollTop = el.scrollHeight - el.clientHeight - 20");
        await El("btn-as-add").ClickAsync();
        await Expect(El("as-lines")).ToHaveTextAsync("11");
        await PollAsync(() => AtEndAsync(pane), at => at, "the pane is not pinned again within the threshold");
    }

    [TestMethod]
    public async Task PreserveScroll_KeepsTheReadersPlaceWhenContentLandsAbove()
    {
        await OpenAsync(Url);

        await El("scp-preserve").EvaluateAsync("el => el.scrollTop = 120");
        await El("scp-nopreserve").EvaluateAsync("el => el.scrollTop = 120");
        var kept = await RowInPaneAsync("msg-p-103", "scp-preserve");
        var shifted = await RowInPaneAsync("msg-n-103", "scp-nopreserve");

        await El("btn-prepend").ClickAsync();
        await Expect(El("msg-p-95")).ToHaveCountAsync(1);
        await NextFramesAsync();

        Near(320, await ScrollTopAsync(El("scp-preserve")), "the pane moved down by what arrived");
        Near(kept, await RowInPaneAsync("msg-p-103", "scp-preserve"), "the message the reader was on stayed put");
        Near(120, await ScrollTopAsync(El("scp-nopreserve")), "the pane without PreserveScroll stood still");
        Near(shifted + 200, await RowInPaneAsync("msg-n-103", "scp-nopreserve"), "the content of the pane without PreserveScroll moved");
    }

    [TestMethod]
    public async Task Snap_MandatoryWithStartAlignment_ComesToRestOnAnItem()
    {
        await OpenAsync(Url);

        var pane = El("scp-snap");
        await Expect(pane).ToHaveCSSAsync("scroll-snap-type", "both mandatory");
        await Expect(pane).ToHaveClassAsync(HasClass("bit-scp-sna-str"));
        await Expect(pane).ToHaveClassAsync(HasClass("bit-scp-sns"));
        await Expect(pane.Locator("> .e2e-scp-item").First).ToHaveCSSAsync("scroll-snap-align", "start");
        await Expect(pane.Locator("> .e2e-scp-item").First).ToHaveCSSAsync("scroll-snap-stop", "always");

        await pane.EvaluateAsync("el => el.scrollTo({ left: 100, behavior: 'instant' })");
        await PollAsync(() => pane.EvaluateAsync<double>("el => el.scrollLeft"), left => Math.Abs(left - 128) < 1, "the pane did not snap to the second item");
    }

    [TestMethod]
    public async Task SnapAndSnapAlign_EveryValue_ReachTheStyle()
    {
        await OpenAsync(Url);

        await Expect(El("scp-snap-none")).ToHaveCSSAsync("scroll-snap-type", "none");
        // proximity is the default strictness, which the computed value leaves out.
        await Expect(El("scp-snap-proximity")).ToHaveCSSAsync("scroll-snap-type", "both");
        await Expect(El("scp-snap-mandatory")).ToHaveCSSAsync("scroll-snap-type", "both mandatory");

        foreach (var (name, expected) in new[] { ("none", "none"), ("start", "start"), ("center", "center"), ("end", "end") })
        {
            await Expect(El($"scp-align-{name}-child")).ToHaveCSSAsync("scroll-snap-align", expected);
        }
    }

    [TestMethod]
    public async Task DragScroll_ThePointerDragsTheContent_AndTheClickThatEndsADragIsSwallowed()
    {
        await OpenAsync(Url);

        var pane = El("scp-drag");
        await Expect(pane).ToHaveClassAsync(HasClass("bit-scp-drg"));
        await Expect(pane).ToHaveCSSAsync("cursor", "grab");

        await pane.ScrollIntoViewIfNeededAsync();
        var box = await BoxAsync(pane);
        await Page.Mouse.MoveAsync((float)(box.X + box.Width - 30), (float)(box.Y + 20));
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync((float)(box.X + box.Width - 100), (float)(box.Y + 20), new() { Steps = 4 });
        await Expect(pane).ToHaveAttributeAsync("data-bit-scp-drag", "");
        await Expect(pane).ToHaveCSSAsync("cursor", "grabbing");
        await Page.Mouse.MoveAsync((float)(box.X + box.Width - 180), (float)(box.Y + 20), new() { Steps = 4 });
        await Page.Mouse.UpAsync();

        await Expect(pane).Not.ToHaveAttributeAsync("data-bit-scp-drag", new Regex(".*"));
        Near(150, await pane.EvaluateAsync<double>("el => el.scrollLeft"), "dragged 150px", 3);
    }

    [TestMethod]
    public async Task DragScroll_ADragStartedOnAButton_IsTheButtons()
    {
        await OpenAsync(Url);

        var pane = El("scp-drag");
        await pane.ScrollIntoViewIfNeededAsync();
        await pane.EvaluateAsync("el => el.scrollLeft = el.scrollWidth");
        var button = await BoxAsync(El("scp-drag-button"));
        var before = await pane.EvaluateAsync<double>("el => el.scrollLeft");

        await Page.Mouse.MoveAsync((float)(button.X + 60), (float)(button.Y + 30));
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync((float)(button.X + 100), (float)(button.Y + 30), new() { Steps = 4 });
        await Page.Mouse.UpAsync();
        Assert.AreEqual(before, await pane.EvaluateAsync<double>("el => el.scrollLeft"), 0.5);

        await El("scp-drag-button").ClickAsync();
        await Expect(El("drag-button-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task DragMomentum_AReleasedFlickGlidesOn()
    {
        await OpenAsync(Url);

        var pane = El("scp-momentum");
        await pane.ScrollIntoViewIfNeededAsync();
        var box = await BoxAsync(pane);
        await Page.Mouse.MoveAsync((float)(box.X + box.Width - 20), (float)(box.Y + 20));
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync((float)(box.X + box.Width - 220), (float)(box.Y + 20), new() { Steps = 3 });
        await Page.Mouse.UpAsync();

        var released = await pane.EvaluateAsync<double>("el => el.scrollLeft");
        await PollAsync(() => pane.EvaluateAsync<double>("el => el.scrollLeft"), left => left > released + 20, "the flick did not glide on");
    }

    [TestMethod]
    public async Task HorizontalWheel_TurnsTheVerticalWheelIntoASidewaysScroll()
    {
        await OpenAsync(Url);

        var pane = El("scp-wheel");
        await WheelOverAsync(pane, 0, 120);
        await PollAsync(() => pane.EvaluateAsync<double>("el => el.scrollLeft"), left => left > 0, "the vertical wheel did not scroll sideways");

        // A pane without it leaves the vertical wheel to the page.
        await El("scp-hor").ScrollIntoViewIfNeededAsync();
        var scrollY = await Page.EvaluateAsync<double>("() => window.scrollY");
        await WheelOverAsync(El("scp-hor"), 0, 120);
        await PollAsync(() => Page.EvaluateAsync<double>("() => window.scrollY"), y => y > scrollY, "the page did not get the wheel");
        Assert.AreEqual(0, await El("scp-hor").EvaluateAsync<double>("el => el.scrollLeft"));
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_RestyleTheBarTheFadeAndTheFocus_AndFadeSizeWins()
    {
        await OpenAsync(Url);

        var pane = El("scp-var");
        Assert.AreEqual("14px", (await StyleAsync(pane, "--bit-scp-sbs")).Trim());
        Assert.AreEqual("rgb(255, 0, 0)", (await StyleAsync(pane, "--bit-scp-sbc")).Trim());
        Assert.AreEqual("2px", (await StyleAsync(pane, "--bit-scp-sbr")).Trim());
        Assert.AreEqual("rgb(0, 0, 255)", (await StyleAsync(pane, "--bit-scp-sbt")).Trim());
        StringAssert.Contains(await StyleAsync(pane, "mask-image"), "9px");
        StringAssert.Contains(await StyleAsync(El("scp-var-wins"), "mask-image"), "25px");

        await pane.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await PollAsync(async () => (await StyleAsync(pane, "outline-color")) + (await StyleAsync(pane, "box-shadow")), s => s.Contains("rgb(0, 128, 0)"), "the focus ring does not take the focus color");
    }

    [TestMethod]
    public async Task BitParams_FillsTheUnsetParameters_AndThePanesOwnValuesWin()
    {
        await OpenAsync(Url);

        Near(48, (await BoxAsync(El("scp-par-1"))).Height, "a cascaded Height of 3rem");
        await Expect(El("scp-par-1")).ToHaveClassAsync(HasClass("bit-scp-mod"));
        await Expect(El("scp-par-1")).ToHaveCSSAsync("scrollbar-width", "thin");

        Near(96, (await BoxAsync(El("scp-par-own"))).Height, "its own Height");
        await Expect(El("scp-par-own")).Not.ToHaveClassAsync(HasClass("bit-scp-mod"));
        await Expect(El("scp-par-own")).ToHaveCSSAsync("scrollbar-width", "thin");
    }

    [TestMethod]
    public async Task StyleAndClass_LandOnTheBoxThatScrolls()
    {
        await OpenAsync(Url);

        await Expect(El("scp-style")).ToHaveCSSAsync("background-color", "rgb(250, 240, 230)");
        await Expect(El("scp-style")).ToHaveCSSAsync("padding-top", "5px");
        await Expect(El("scp-class")).ToHaveClassAsync(HasClass("e2e-scp-class"));
        await Expect(El("scp-class")).ToHaveCSSAsync("outline-style", "dashed");
    }

    [TestMethod]
    public async Task Rtl_StartIsTheRightEdge_AndTheOffsetsCountFromTheVisualLeft()
    {
        await OpenAsync(Url);

        var pane = El("scp-rtl");
        await Expect(pane).ToHaveAttributeAsync("dir", "rtl");
        Assert.AreEqual(0, await pane.EvaluateAsync<double>("el => el.scrollLeft"));
        var max = await pane.EvaluateAsync<double>("el => el.scrollWidth - el.clientWidth");

        await El("btn-rtl-end").ClickAsync();
        await PollAsync(() => pane.EvaluateAsync<double>("el => el.scrollLeft"), left => Math.Abs(left + max) < 1, "ScrollToEnd did not reach the visual left");
        await Expect(El("rtl-reached")).ToHaveTextAsync(new Regex("left$"));
        await El("btn-rtl-offset").ClickAsync();
        await Expect(El("rtl-offset")).ToHaveTextAsync(string.Create(CultureInfo.InvariantCulture, $"{-max}|0|{max}|True|True|False"));

        await El("btn-rtl-to").ClickAsync();
        await PollAsync(() => pane.EvaluateAsync<double>("el => el.scrollLeft"), left => Math.Abs(left - (100 - max)) < 1, "ScrollTo(100) does not count from the visual left");

        await El("btn-rtl-start").ClickAsync();
        await PollAsync(() => pane.EvaluateAsync<double>("el => el.scrollLeft"), left => Math.Abs(left) < 1, "ScrollToStart did not reach the visual right");
        await Expect(El("rtl-reached")).ToHaveTextAsync(new Regex("right$"));
    }

    [TestMethod]
    public async Task ExpandOnPrint_LiftsTheHeightAndTheScroll_OnPaper()
    {
        await OpenAsync(Url);

        Near(64, (await BoxAsync(El("scp-print"))).Height, "on the screen");
        await Page.EmulateMediaAsync(new() { Media = Media.Print });
        await PollAsync(async () => (await BoxAsync(El("scp-print"))).Height, h => h >= 399, "the pane does not print in full");
        await Expect(El("scp-print")).ToHaveCSSAsync("overflow-y", "visible");
    }

    [TestMethod]
    public async Task Visibility_HiddenKeepsTheBox_CollapsedRemovesIt()
    {
        await OpenAsync(Url);

        await Expect(El("scp-vis-hidden")).ToHaveCSSAsync("visibility", "hidden");
        Assert.IsTrue((await BoxAsync(El("scp-vis-hidden"))).Height > 0);
        await Expect(El("scp-vis-collapsed")).ToHaveCSSAsync("display", "none");
    }

    [TestMethod]
    public async Task HtmlAttributes_AreSplattedOnTheRoot()
    {
        await OpenAsync(Url);

        await Expect(El("scp-attrs")).ToHaveAttributeAsync("data-e2e", "custom");
        await Expect(El("scp-attrs")).ToHaveAttributeAsync("data-extra", "from-dictionary");
    }

    [TestMethod]
    public async Task Rerender_ParametersChangedByThePage_UpdateThePaneAndItsScript()
    {
        await OpenAsync(Url);

        Near(64, (await BoxAsync(El("scp-rr"))).Height, "before");
        await El("btn-rr").ClickAsync();

        await PollAsync(async () => (await BoxAsync(El("scp-rr"))).Height, h => Math.Abs(h - 96) < 1, "the Height did not change");
        await Expect(El("scp-rr")).ToHaveClassAsync(HasClass("bit-scp-mod"));
        await Expect(El("scp-rr")).ToHaveClassAsync(HasClass("bit-scp-fad"));
        // Fade asked for the script at a re-render, which now marks the edge with more beyond it.
        await Expect(El("scp-rr")).ToHaveAttributeAsync("data-bit-scp-fb", "");
    }

    [TestMethod]
    public async Task Unmount_TearsTheScriptDownWithoutAnError()
    {
        await OpenAsync(Url);

        await El("scp-um").EvaluateAsync("el => el.scrollTop = 30");
        await El("btn-unmount").ClickAsync();
        await Expect(El("scp-um")).ToHaveCountAsync(0);
        await El("btn-unmount").ClickAsync();
        await Expect(El("scp-um")).ToHaveCountAsync(1);
        // Set up again: pinned to the end by AutoScroll, faded on the edge with more above it.
        await Expect(El("scp-um")).ToHaveAttributeAsync("data-bit-scp-ft", "");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    // The pointer has to be over the pane for the wheel to reach it, so the pane is brought on screen first.
    private async Task WheelOverAsync(ILocator locator, float dx, float dy)
    {
        await locator.ScrollIntoViewIfNeededAsync();
        var box = await BoxAsync(locator);
        await Page.Mouse.MoveAsync((float)(box.X + box.Width / 2), (float)(box.Y + box.Height / 2));
        await Page.Mouse.WheelAsync(dx, dy);
    }

    // Where a row stands in the visible part of its pane.
    private async Task<double> RowInPaneAsync(string rowId, string paneId)
    {
        return (await BoxAsync(El(rowId))).Y - (await BoxAsync(El(paneId))).Y;
    }

    private static Task<double> ScrollTopAsync(ILocator locator) => locator.EvaluateAsync<double>("el => el.scrollTop");

    private static Task<bool> AtEndAsync(ILocator locator) => locator.EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight && Math.abs(el.scrollTop - (el.scrollHeight - el.clientHeight)) < 1");

    // Records the behavior every scrollTo of the page is asked for, from here on.
    private Task RecordScrollBehaviorsAsync()
    {
        return Page.EvaluateAsync(@"() => {
            window.__e2eBehaviors = [];
            const original = Element.prototype.scrollTo;
            Element.prototype.scrollTo = function (options) {
                if (options && typeof options === 'object' && this.id === 'scp-smooth') window.__e2eBehaviors.push(options.behavior ?? 'auto');
                return original.apply(this, arguments);
            };
        }");
    }

    private Task<string[]> ReadScrollBehaviorsAsync() => Page.EvaluateAsync<string[]>("() => window.__e2eBehaviors.slice()");

    private static async Task<LayoutBox> BoxAsync(ILocator locator)
    {
        var box = await locator.EvaluateAsync<double[]>("el => { const r = el.getBoundingClientRect(); return [r.x, r.y, r.width, r.height]; }");
        return new(box[0], box[1], box[2], box[3]);
    }

    private static Task<string> StyleAsync(ILocator locator, string property)
    {
        return locator.EvaluateAsync<string>("(el, p) => getComputedStyle(el).getPropertyValue(p)", property);
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
}
