using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Utilities;

/// <summary>
/// BitSticky in a real browser (/e2e/sticky), every sticky scrolled in a container of its own: pinned to the top by
/// default, every Placement (the vertical ones, the horizontal ones in LTR, in an inherited rtl and with Dir=Rtl, and
/// the ones that fall back to the top), the Top / Bottom / Left / Right offsets (pixels, lengths, decimals, a calc,
/// winning over a Placement), the derived stuck state (OnStuckChanged, IsStuck, StuckEdges, StuckClass, StuckStyle,
/// the per-edge classes, RefreshAsync), OnStuckEdgesChanged through a TopAndBottom bar and into a corner, Element
/// (semantic tags, a table header, an invalid name, a void tag, a tag changed at runtime) and the group role of a
/// named div, ZIndex and the --bit-Sticky-* variables, ElevateOnStuck, ScrollPadding keeping a focused link out from
/// under the bar, Disabled, the BitParams cascade, Id / Style / Class / Visibility / HtmlAttributes / Dir, a sticky
/// on the page itself, the prerendered markup and an unmount with the detection attached.
/// </summary>
[TestClass]
public class BitStickyE2ETests : E2ETestBase
{
    private const string Route = "/e2e/sticky";

    [TestMethod]
    public async Task Prerender_TheServerHtml_ContainsTheStickyWithItsClasses()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "id=\"stk-basic\"");
        StringAssert.Contains(html, "class=\"bit-stk-top bit-stk e2e-stk-bar\"");
        StringAssert.Contains(html, "<header id=\"stk-header\" aria-label=\"Section header\" class=\"bit-stk-top bit-stk e2e-stk-bar\"");
        StringAssert.Contains(html, "id=\"status\">Loading");

        await OpenAsync(Route);
        await Expect(Page.Locator("#stk-basic")).ToHaveCSSAsync("position", "sticky");
    }

    [TestMethod]
    public async Task Basic_ScrollingTheContainer_KeepsTheBarAtItsTop()
    {
        await OpenAsync(Route);

        var sticky = Page.Locator("#stk-basic");
        await Expect(sticky).ToHaveClassAsync(HasClass("bit-stk-top"));
        await Expect(sticky).ToHaveCSSAsync("top", "0px");
        await Expect(sticky).ToHaveCSSAsync("z-index", "1");

        Assert.AreEqual(0, (await OffsetAsync("#stk-basic", "#v-basic")).Top, 1);

        await ScrollAsync("#v-basic", 150);
        Assert.AreEqual(0, (await OffsetAsync("#stk-basic", "#v-basic")).Top, 1, "The bar scrolled away with the content.");

        // A paragraph that followed it has passed underneath it.
        var paragraph = await Page.Locator("#v-basic p").First.BoundingBoxAsync();
        var bar = await sticky.BoundingBoxAsync();
        Assert.IsTrue(paragraph!.Y < bar!.Y, "The content did not pass under the bar.");
    }

    [TestMethod]
    [DataRow("Top", "bit-stk-top", false, true)]
    [DataRow("Bottom", "bit-stk-btm", true, false)]
    [DataRow("TopAndBottom", "bit-stk-tab", true, true)]
    [DataRow("Left", "bit-stk-top", false, true)]
    [DataRow("Right", "bit-stk-top", false, true)]
    [DataRow("Center", "bit-stk-top", false, true)]
    [DataRow("none", "bit-stk-top", false, true)]
    public async Task Placement_Vertical_PinsToTheEdgesItNames(string placement, string cssClass, bool pinsAtBottom, bool pinsAtTop)
    {
        await OpenAsync(Route);

        await Page.ClickAsync($"#btn-place-{placement}");
        await Expect(Page.Locator("#place-current")).ToHaveTextAsync(placement);

        var sticky = Page.Locator("#stk-place");
        await Expect(sticky).ToHaveClassAsync(HasClass(cssClass));
        await Expect(sticky).ToHaveClassAsync(new Regex($"^{Regex.Escape(cssClass)} "));

        // Unscrolled, its place (300px down) is below the fold of the 200px box.
        await ScrollAsync("#v-place", 0);
        var atStart = await OffsetAsync("#stk-place", "#v-place");
        if (pinsAtBottom)
        {
            Assert.AreEqual(0, atStart.FromBottom, 1, "Not pinned to the bottom edge.");
            await Expect(Page.Locator("#place-edges")).ToHaveTextAsync("Bottom");
        }
        else
        {
            Assert.AreEqual(300, atStart.Top, 1, "Pinned although Bottom was not asked for.");
            await Expect(Page.Locator("#place-edges")).ToHaveTextAsync("None");
        }

        // Scrolled past it, its place is above the top.
        await ScrollAsync("#v-place", 400);
        var pastIt = await OffsetAsync("#stk-place", "#v-place");
        if (pinsAtTop)
        {
            Assert.AreEqual(0, pastIt.Top, 1, "Not pinned to the top edge.");
            await Expect(Page.Locator("#place-edges")).ToHaveTextAsync("Top");
        }
        else
        {
            Assert.AreEqual(-100, pastIt.Top, 1, "Pinned although Top was not asked for.");
            await Expect(Page.Locator("#place-edges")).ToHaveTextAsync("None");
        }
    }

    [TestMethod]
    [DataRow("Start", "bit-stk-srt")]
    [DataRow("End", "bit-stk-end")]
    [DataRow("StartAndEnd", "bit-stk-sae")]
    public async Task Placement_Horizontal_FollowsTheReadingDirection(string placement, string cssClass)
    {
        await OpenAsync(Route);

        await Page.ClickAsync($"#btn-hplace-{placement}");
        await Expect(Page.Locator("#hplace-current")).ToHaveTextAsync(placement);

        foreach (var id in new[] { "#stk-h-ltr", "#stk-h-rtl", "#stk-h-dir" })
        {
            await Expect(Page.Locator(id)).ToHaveClassAsync(HasClass(cssClass));
        }

        var start = placement is "Start" or "StartAndEnd";
        var end = placement is "End" or "StartAndEnd";

        // LTR: its place is 400px from the left, past the right fold of the 300px box; Start is the left.
        await ScrollLeftAsync("#h-ltr", 0);
        var ltr = await OffsetAsync("#stk-h-ltr", "#h-ltr");
        if (end) Assert.AreEqual(0, ltr.FromRight, 1, "LTR End: not pinned to the right.");
        else Assert.AreEqual(400, ltr.Left, 1, "LTR: pinned although End was not asked for.");
        await Expect(Page.Locator("#h-ltr-edges")).ToHaveTextAsync(end ? "Right" : "None");

        await ScrollLeftAsync("#h-ltr", 500);
        ltr = await OffsetAsync("#stk-h-ltr", "#h-ltr");
        if (start) Assert.AreEqual(0, ltr.Left, 1, "LTR Start: not pinned to the left.");
        await Expect(Page.Locator("#h-ltr-edges")).ToHaveTextAsync(start ? "Left" : "None");

        // RTL, inherited from an ancestor and set by Dir: the row runs from the right, Start is the right.
        foreach (var (sticky, box, edges) in new[] { ("#stk-h-rtl", "#h-rtl", "#h-rtl-edges"), ("#stk-h-dir", "#h-dir", "#h-dir-edges") })
        {
            await ScrollLeftAsync(box, 0);
            var rtl = await OffsetAsync(sticky, box);
            if (end) Assert.AreEqual(0, rtl.Left, 1, $"{sticky} End: not pinned to the left.");
            else Assert.AreEqual(400, rtl.FromRight, 1, $"{sticky}: pinned although End was not asked for.");
            await Expect(Page.Locator(edges)).ToHaveTextAsync(end ? "Left" : "None");

            await ScrollLeftAsync(box, -500);
            rtl = await OffsetAsync(sticky, box);
            if (start) Assert.AreEqual(0, rtl.FromRight, 1, $"{sticky} Start: not pinned to the right.");
            await Expect(Page.Locator(edges)).ToHaveTextAsync(start ? "Right" : "None");
        }
    }

    [TestMethod]
    public async Task Dir_Rtl_RendersTheClassAndTheAttribute()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#stk-h-dir")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Page.Locator("#stk-h-dir")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Page.Locator("#stk-dir")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Page.Locator("#stk-dir")).ToHaveCSSAsync("direction", "rtl");

        await Expect(Page.Locator("#stk-h-rtl")).Not.ToHaveAttributeAsync("dir", new Regex(".*"));
        await Expect(Page.Locator("#stk-h-rtl")).Not.ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Page.Locator("#stk-h-rtl")).ToHaveCSSAsync("direction", "rtl");
    }

    [TestMethod]
    public async Task Offsets_TopAndBottom_PinAtTheirDistances_AndReplaceTheDefaultTop()
    {
        await OpenAsync(Route);

        var sticky = Page.Locator("#stk-offsets");
        await Expect(sticky).ToHaveAttributeAsync("class", "bit-stk e2e-stk-bar");
        await Expect(sticky).ToHaveAttributeAsync("style", new Regex("top: 16px;\\s*bottom: 2rem"));

        await ScrollAsync("#v-offsets", 0);
        Assert.AreEqual(32, (await OffsetAsync("#stk-offsets", "#v-offsets")).FromBottom, 1);

        await ScrollAsync("#v-offsets", 400);
        Assert.AreEqual(16, (await OffsetAsync("#stk-offsets", "#v-offsets")).Top, 1);
    }

    [TestMethod]
    public async Task Offsets_LeftAndRight_PinAtTheirDistancesHorizontally()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#stk-offsets-h")).ToHaveAttributeAsync("style", new Regex("left: 16px;\\s*right: 2rem"));

        await ScrollLeftAsync("#h-offsets", 0);
        Assert.AreEqual(32, (await OffsetAsync("#stk-offsets-h", "#h-offsets")).FromRight, 1);

        await ScrollLeftAsync("#h-offsets", 500);
        Assert.AreEqual(16, (await OffsetAsync("#stk-offsets-h", "#h-offsets")).Left, 1);
    }

    [TestMethod]
    public async Task Offsets_WinOverAPlacement_AndAcceptDecimalsAndCalc()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#stk-offsets-mix")).ToHaveClassAsync(HasClass("bit-stk-top"));
        await Expect(Page.Locator("#stk-offsets-mix")).ToHaveCSSAsync("top", "10px");
        await Expect(Page.Locator("#stk-offsets-decimal")).ToHaveAttributeAsync("style", new Regex("top: 9.5px"));
        await Expect(Page.Locator("#stk-offsets-decimal")).ToHaveCSSAsync("top", "9.5px");
        await Expect(Page.Locator("#stk-offsets-calc")).ToHaveAttributeAsync("style", new Regex(Regex.Escape("top: calc(1rem + 2px)")));
        await Expect(Page.Locator("#stk-offsets-calc")).ToHaveCSSAsync("top", "18px");

        await ScrollAsync("#v-offsets-mix", 300);
        Assert.AreEqual(10, (await OffsetAsync("#stk-offsets-mix", "#v-offsets-mix")).Top, 1);
        Assert.AreEqual(9.5, (await OffsetAsync("#stk-offsets-decimal", "#v-offsets-mix")).Top, 1);
        Assert.AreEqual(18, (await OffsetAsync("#stk-offsets-calc", "#v-offsets-mix")).Top, 1);
    }

    [TestMethod]
    public async Task StuckState_ScrollingPinsAndUnpins_TheCallbackTheClassesTheStyleAndTheProperties()
    {
        await OpenAsync(Route);

        var sticky = Page.Locator("#stk-stuck");

        // Unscrolled, it has not moved, so it is not stuck.
        await Expect(Page.Locator("#stuck-value")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#stuck-count")).ToHaveTextAsync("0");
        await Expect(sticky).Not.ToHaveClassAsync(HasClass("bit-stk-stc"));
        await Expect(sticky).ToHaveCSSAsync("font-weight", "300");
        await Page.ClickAsync("#btn-read-stuck");
        await Expect(Page.Locator("#stuck-read")).ToHaveTextAsync("False|None");

        await ScrollAsync("#v-stuck", 100);

        await Expect(Page.Locator("#stuck-value")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#stuck-count")).ToHaveTextAsync("1");
        await Expect(sticky).ToHaveClassAsync(HasClass("bit-stk-stc"));
        await Expect(sticky).ToHaveClassAsync(HasClass("bit-stk-stc-top"));
        await Expect(sticky).ToHaveClassAsync(HasClass("e2e-stuck"));
        await Expect(sticky).ToHaveCSSAsync("outline-color", "rgb(255, 0, 0)");
        // StuckStyle is appended after Style, so it wins while stuck.
        await Expect(sticky).ToHaveCSSAsync("font-weight", "700");
        await Expect(sticky).ToHaveTextAsync("Stuck");
        await Page.ClickAsync("#btn-read-stuck");
        await Expect(Page.Locator("#stuck-read")).ToHaveTextAsync("True|Top");

        // Further scrolling while stuck raises nothing more.
        await ScrollAsync("#v-stuck", 200);
        await Page.ClickAsync("#btn-read-stuck");
        await Expect(Page.Locator("#stuck-count")).ToHaveTextAsync("1");

        await ScrollAsync("#v-stuck", 0);

        await Expect(Page.Locator("#stuck-value")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#stuck-count")).ToHaveTextAsync("2");
        await Expect(sticky).Not.ToHaveClassAsync(HasClass("bit-stk-stc"));
        await Expect(sticky).Not.ToHaveClassAsync(HasClass("e2e-stuck"));
        await Expect(sticky).ToHaveCSSAsync("font-weight", "300");
    }

    [TestMethod]
    public async Task RefreshAsync_ReadsALayoutChangeNothingReported()
    {
        await OpenAsync(Route);

        await ScrollAsync("#v-stuck", 30);
        await Expect(Page.Locator("#stuck-value")).ToHaveTextAsync("False");

        // The paragraph above it shrinks: the bar now rests 10px down in a box scrolled by 30, so it is pinned, but
        // neither the bar, nor the box, nor a scroll says so.
        await Page.Locator("#v-stuck p").First.EvaluateAsync("p => p.style.height = '10px'");
        await NextFramesAsync();
        Assert.AreEqual(0, (await OffsetAsync("#stk-stuck", "#v-stuck")).Top, 1);

        await Page.ClickAsync("#btn-refresh");
        await Expect(Page.Locator("#refreshed")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#stuck-value")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#stk-stuck")).ToHaveClassAsync(HasClass("bit-stk-stc"));
    }

    [TestMethod]
    public async Task StuckEdges_TopAndBottom_ReportsEachEdge_AndTheMoveBetweenThemFlipsNothing()
    {
        await OpenAsync(Route);

        var sticky = Page.Locator("#stk-edges");

        await Expect(Page.Locator("#edges-value")).ToHaveTextAsync("Bottom");
        await Expect(Page.Locator("#edges-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#edges-stuck-count")).ToHaveTextAsync("1");
        await Expect(sticky).ToHaveClassAsync(HasClass("bit-stk-stc-btm"));

        await ScrollAsync("#v-edges", 200);
        await Expect(Page.Locator("#edges-value")).ToHaveTextAsync("None");
        await Expect(Page.Locator("#edges-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#edges-stuck-count")).ToHaveTextAsync("2");
        await Expect(sticky).Not.ToHaveClassAsync(HasClass("bit-stk-stc"));

        await ScrollAsync("#v-edges", 400);
        await Expect(Page.Locator("#edges-value")).ToHaveTextAsync("Top");
        await Expect(Page.Locator("#edges-count")).ToHaveTextAsync("3");
        await Expect(Page.Locator("#edges-stuck-count")).ToHaveTextAsync("3");
        await Expect(sticky).ToHaveClassAsync(HasClass("bit-stk-stc-top"));
        await Expect(sticky).Not.ToHaveClassAsync(HasClass("bit-stk-stc-btm"));

        // Straight from the top edge to the bottom one: the edges change, the stuck state does not.
        await ScrollAsync("#v-edges", 0);
        await Expect(Page.Locator("#edges-value")).ToHaveTextAsync("Bottom");
        await Expect(Page.Locator("#edges-count")).ToHaveTextAsync("4");
        await Expect(Page.Locator("#edges-stuck-count")).ToHaveTextAsync("3");
    }

    [TestMethod]
    public async Task StuckEdges_PinnedIntoACorner_ReportsBothEdges()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#corner-edges")).ToHaveTextAsync("None");

        await Page.Locator("#v-corner").EvaluateAsync("el => el.scrollTo(100, 100)");

        await Expect(Page.Locator("#corner-edges")).ToHaveTextAsync("Top, Left");
        await Expect(Page.Locator("#stk-corner")).ToHaveClassAsync(HasClass("bit-stk-stc-top"));
        await Expect(Page.Locator("#stk-corner")).ToHaveClassAsync(HasClass("bit-stk-stc-lft"));
        var offset = await OffsetAsync("#stk-corner", "#v-corner");
        Assert.AreEqual(0, offset.Top, 1);
        Assert.AreEqual(0, offset.Left, 1);

        await Page.Locator("#v-corner").EvaluateAsync("el => el.scrollTo(0, 100)");
        await Expect(Page.Locator("#corner-edges")).ToHaveTextAsync("Top");
    }

    [TestMethod]
    public async Task Element_RendersTheTagAsked_AndANamedDivAsAGroup()
    {
        await OpenAsync(Route);

        Assert.AreEqual("HEADER", await TagAsync("#stk-header"));
        await Expect(Page.Locator("#stk-header")).Not.ToHaveAttributeAsync("role", new Regex(".*"));
        await Expect(Page.Locator("#stk-header")).ToHaveAttributeAsync("aria-label", "Section header");
        await Expect(Page.Locator("#stk-header")).ToHaveCSSAsync("position", "sticky");

        Assert.AreEqual("DIV", await TagAsync("#stk-group"));
        await Expect(Page.Locator("#stk-group")).ToHaveAttributeAsync("role", "group");
        await Expect(Page.Locator("#stk-group")).ToHaveAttributeAsync("aria-label", "Named div");
        await Expect(Page.Locator("#stk-labelledby")).ToHaveAttributeAsync("role", "group");
        await Expect(Page.Locator("#stk-role")).ToHaveAttributeAsync("role", "toolbar");
        await Expect(Page.Locator("#stk-basic")).Not.ToHaveAttributeAsync("role", new Regex(".*"));

        // A name a tag cannot have falls back to the div; a void tag holds no content.
        Assert.AreEqual("DIV", await TagAsync("#stk-invalid"));
        await Expect(Page.Locator("#stk-invalid")).ToHaveTextAsync("Invalid tag");
        Assert.AreEqual("HR", await TagAsync("#stk-void"));
        Assert.AreEqual(0, await Page.Locator("#stk-void").EvaluateAsync<int>("el => el.childNodes.length"));

        // A footer, pinned to the bottom.
        Assert.AreEqual("FOOTER", await TagAsync("#stk-footer"));
        Assert.AreEqual(0, (await OffsetAsync("#stk-footer", "#v-element")).FromBottom, 1);
    }

    [TestMethod]
    public async Task Element_ATableHeaderCell_PinsWhileTheRowsScroll()
    {
        await OpenAsync(Route);

        Assert.AreEqual("TH", await TagAsync("#stk-th"));
        await ScrollAsync("#v-table", 150);
        Assert.AreEqual(0, (await OffsetAsync("#stk-th", "#v-table")).Top, 1);
    }

    [TestMethod]
    public async Task Element_ChangedAtRuntime_ReplacesTheTagAndKeepsTheDetection()
    {
        await OpenAsync(Route);

        Assert.AreEqual("NAV", await TagAsync("#stk-runtime"));
        await ScrollAsync("#v-element", 300);
        await Expect(Page.Locator("#runtime-stuck")).ToHaveTextAsync("True");

        await ScrollAsync("#v-element", 0);
        await Expect(Page.Locator("#runtime-stuck")).ToHaveTextAsync("False");

        await Page.ClickAsync("#btn-element-nav");
        await Expect(Page.Locator("#runtime-element")).ToHaveTextAsync("aside");
        await Expect(Page.Locator("aside#stk-runtime")).ToHaveCountAsync(1);

        await ScrollAsync("#v-element", 300);
        await Expect(Page.Locator("#runtime-stuck")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#stk-runtime")).ToHaveClassAsync(HasClass("bit-stk-stc"));
    }

    [TestMethod]
    public async Task ZIndex_DefaultsToOne_TheVariableSetsIt_TheParameterWins()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#stk-z-default")).ToHaveCSSAsync("z-index", "1");
        await Expect(Page.Locator("#stk-z-param")).ToHaveCSSAsync("z-index", "3");
        await Expect(Page.Locator("#stk-z-var")).ToHaveCSSAsync("z-index", "5");
        await Expect(Page.Locator("#stk-z-var-param")).ToHaveCSSAsync("z-index", "7");
    }

    [TestMethod]
    public async Task ElevateOnStuck_ASurfaceAndAShadowOnlyWhilePinned()
    {
        await OpenAsync(Route);

        var sticky = Page.Locator("#stk-elevate");
        await Expect(sticky).ToHaveClassAsync(HasClass("bit-stk-elv"));
        await Expect(sticky).ToHaveCSSAsync("box-shadow", "none");
        await Expect(sticky).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");

        await ScrollAsync("#v-elevate", 100);

        await Expect(sticky).ToHaveClassAsync(HasClass("bit-stk-stc"));
        await Expect(sticky).Not.ToHaveCSSAsync("box-shadow", "none");
        await Expect(sticky).Not.ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");

        await ScrollAsync("#v-elevate", 0);
        await Expect(sticky).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
        await Expect(sticky).ToHaveCSSAsync("box-shadow", "none");
    }

    [TestMethod]
    public async Task ElevateOnStuck_AStyleBackgroundWins_AndTheVariablesRestyleIt()
    {
        await OpenAsync(Route);

        await ScrollAsync("#v-elevate-style", 100);
        await Expect(Page.Locator("#stk-elevate-style")).ToHaveClassAsync(HasClass("bit-stk-stc"));
        await Expect(Page.Locator("#stk-elevate-style")).ToHaveCSSAsync("background-color", "rgb(250, 200, 0)");

        await ScrollAsync("#v-elevate-vars", 100);
        var sticky = Page.Locator("#stk-elevate-vars");
        await Expect(sticky).ToHaveClassAsync(HasClass("bit-stk-stc-top"));
        await Expect(sticky).ToHaveCSSAsync("background-color", "rgb(0, 128, 0)");
        await Expect(sticky).ToHaveCSSAsync("box-shadow", new Regex(Regex.Escape("rgb(255, 0, 0) 0px 4px 0px 0px")));
    }

    [TestMethod]
    public async Task ElevateOnStuck_TheShadowArrivesGradually_AndInstantlyUnderReducedMotion()
    {
        await OpenAsync(Route);

        var full = await TransitionMsAsync("#stk-elevate");
        Assert.IsTrue(full >= 50, $"The shadow does not transition without reduced motion ({full}ms).");
        await Expect(Page.Locator("#stk-elevate")).ToHaveCSSAsync("transition-property", "box-shadow");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        Assert.IsTrue(await TransitionMsAsync("#stk-elevate") < 1, "The shadow still transitions under reduced motion.");
    }

    [TestMethod]
    public async Task ForcedColors_AStuckElevatedStickyDrawsAnOutlineForItsLostShadow()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit does not emulate forced colors.");

        await OpenAsync(Route);

        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });
        await ScrollAsync("#v-elevate", 100);

        var sticky = Page.Locator("#stk-elevate");
        await Expect(sticky).ToHaveClassAsync(HasClass("bit-stk-stc"));
        await Expect(sticky).ToHaveCSSAsync("outline-style", "solid");

        await ScrollAsync("#v-elevate", 0);
        await Expect(sticky).Not.ToHaveClassAsync(HasClass("bit-stk-stc"));
        await Expect(sticky).ToHaveCSSAsync("outline-style", "none");
    }

    [TestMethod]
    public async Task Print_TheStickinessIsDroppedOnPaper()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#stk-basic")).ToHaveCSSAsync("position", "sticky");

        await Page.EmulateMediaAsync(new() { Media = Media.Print });

        await Expect(Page.Locator("#stk-basic")).ToHaveCSSAsync("position", "static");
        await Expect(Page.Locator("#stk-elevate")).ToHaveCSSAsync("transition-duration", "0s");
    }

    [TestMethod]
    public async Task ScrollPadding_ReservesTheBarsRoom_AndKeepsAFocusedLinkOutFromUnderIt()
    {
        await OpenAsync(Route);

        var box = Page.Locator("#v-padding");
        // Its 10px offset plus its 30px height.
        await Expect(box).ToHaveCSSAsync("scroll-padding-top", "40px");

        // A link the scroll left under the bar: moving the focus to it brings it out from under the bar.
        await ScrollAsync("#v-padding", 200);
        await Page.FocusAsync("#pad-link-6");
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Page.Locator("#pad-link-5")).ToBeFocusedAsync();
        await NextFramesAsync();
        var link = (await Page.Locator("#pad-link-5").BoundingBoxAsync())!;
        var bar = (await Page.Locator("#stk-padding").BoundingBoxAsync())!;
        Assert.IsTrue(link.Y >= bar.Y + bar.Height - 1, $"The focused link ({link.Y}) is under the bar ending at {bar.Y + bar.Height}.");

        // Turned off, the room is handed back.
        await Page.ClickAsync("#btn-toggle-padding");
        await Expect(Page.Locator("#padding-on")).ToHaveTextAsync("False");
        await Expect(box).ToHaveCSSAsync("scroll-padding-top", "auto");

        await Page.ClickAsync("#btn-toggle-padding");
        await Expect(box).ToHaveCSSAsync("scroll-padding-top", "40px");
    }

    [TestMethod]
    public async Task Disabled_StepsBackIntoTheFlow_ReleasesTheDetectionAndThePadding()
    {
        await OpenAsync(Route);

        var sticky = Page.Locator("#stk-disabled");
        var box = Page.Locator("#v-disabled");
        await Expect(box).ToHaveCSSAsync("scroll-padding-top", "30px");

        await ScrollAsync("#v-disabled", 100);
        await Expect(Page.Locator("#disabled-stuck")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#disabled-stuck-count")).ToHaveTextAsync("1");

        // Disabled while stuck: it reports the flip back, and scrolls away with the content.
        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(sticky).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(sticky).ToHaveCSSAsync("position", "static");
        await Expect(Page.Locator("#disabled-stuck")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#disabled-stuck-count")).ToHaveTextAsync("2");
        await Expect(sticky).Not.ToHaveClassAsync(HasClass("bit-stk-stc"));
        await Expect(box).ToHaveCSSAsync("scroll-padding-top", "auto");
        Assert.AreEqual(-100, (await OffsetAsync("#stk-disabled", "#v-disabled")).Top, 1);

        await ScrollAsync("#v-disabled", 150);
        await Page.ClickAsync("#btn-read-stuck");
        await Expect(Page.Locator("#disabled-stuck-count")).ToHaveTextAsync("2");

        // Enabled again at runtime.
        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(sticky).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(sticky).ToHaveCSSAsync("position", "sticky");
        await Expect(box).ToHaveCSSAsync("scroll-padding-top", "30px");
        await ScrollAsync("#v-disabled", 120);
        await Expect(Page.Locator("#disabled-stuck")).ToHaveTextAsync("True");
        Assert.AreEqual(0, (await OffsetAsync("#stk-disabled", "#v-disabled")).Top, 1);
    }

    [TestMethod]
    public async Task Cascade_BitParamsSetsTheDefaults_AnExplicitParameterWins_AndAChangeReachesThem()
    {
        await OpenAsync(Route);

        var cascaded = Page.Locator("#stk-cascade");
        var own = Page.Locator("#stk-cascade-own");

        await Expect(cascaded).ToHaveClassAsync(HasClass("bit-stk-elv"));
        await Expect(cascaded).ToHaveCSSAsync("top", "8px");
        await Expect(own).Not.ToHaveClassAsync(HasClass("bit-stk-elv"));
        await Expect(own).ToHaveCSSAsync("top", "40px");

        await ScrollAsync("#v-cascade", 150);
        Assert.AreEqual(8, (await OffsetAsync("#stk-cascade", "#v-cascade")).Top, 1);
        await Expect(cascaded).ToHaveClassAsync(HasClass("bit-stk-stc"));
        await Expect(cascaded).Not.ToHaveCSSAsync("box-shadow", "none");

        await Page.ClickAsync("#btn-cascade-change");

        await Expect(cascaded).Not.ToHaveClassAsync(HasClass("bit-stk-elv"));
        await Expect(cascaded).ToHaveCSSAsync("top", "20px");
        await Expect(cascaded).ToHaveCSSAsync("z-index", "9");
        await Expect(own).ToHaveCSSAsync("top", "40px");
        await Expect(own).ToHaveCSSAsync("z-index", "9");
        await NextFramesAsync();
        Assert.AreEqual(20, (await OffsetAsync("#stk-cascade", "#v-cascade")).Top, 1);
    }

    [TestMethod]
    public async Task StyleClassAndAttributes_LandOnTheRoot()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#stk-style")).ToHaveCSSAsync("background-color", "rgb(200, 0, 0)");
        await Expect(Page.Locator("#stk-class")).ToHaveClassAsync(HasClass("e2e-stk-class"));
        await Expect(Page.Locator("#stk-class")).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(Page.Locator("#stk-class")).ToHaveAttributeAsync("data-e2e", "sticky-attr");
        await Expect(Page.Locator("#stk-class")).ToHaveAttributeAsync("title", "sticky title");
    }

    [TestMethod]
    public async Task Visibility_HiddenAndCollapsed()
    {
        await OpenAsync(Route);

        var sticky = Page.Locator("#stk-vis");
        await Expect(sticky).ToBeVisibleAsync();

        await Page.ClickAsync("#btn-vis-hidden");
        await Expect(sticky).ToHaveCSSAsync("visibility", "hidden");

        await Page.ClickAsync("#btn-vis-collapsed");
        await Expect(sticky).ToHaveCSSAsync("display", "none");

        await Page.ClickAsync("#btn-vis-visible");
        await Expect(sticky).ToBeVisibleAsync();
        await Expect(sticky).ToHaveCSSAsync("visibility", "visible");
    }

    [TestMethod]
    public async Task CssVariables_OffsetsSetOnAnAncestor_MoveWhereTheBarPins()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#stk-vars")).ToHaveCSSAsync("z-index", "5");

        await ScrollAsync("#v-vars", 0);
        Assert.AreEqual(14, (await OffsetAsync("#stk-vars", "#v-vars")).FromBottom, 1);
        await ScrollAsync("#v-vars", 400);
        Assert.AreEqual(12, (await OffsetAsync("#stk-vars", "#v-vars")).Top, 1);

        await ScrollLeftAsync("#h-vars", 0);
        Assert.AreEqual(18, (await OffsetAsync("#stk-vars-h", "#h-vars")).FromRight, 1);
        await ScrollLeftAsync("#h-vars", 500);
        Assert.AreEqual(16, (await OffsetAsync("#stk-vars-h", "#h-vars")).Left, 1);
    }

    [TestMethod]
    public async Task Page_AStickyOnThePageItself_PinsToTheViewport()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#page-stuck")).ToHaveTextAsync("False");

        var top = await Page.Locator("#stk-page").EvaluateAsync<double>("el => el.getBoundingClientRect().top + window.scrollY");
        await Page.EvaluateAsync("y => window.scrollTo(0, y)", top + 300);
        await NextFramesAsync();

        Assert.AreEqual(0, (await Page.Locator("#stk-page").BoundingBoxAsync())!.Y, 1);
        await Expect(Page.Locator("#page-stuck")).ToHaveTextAsync("True");

        await Page.EvaluateAsync("() => window.scrollTo(0, 0)");
        await Expect(Page.Locator("#page-stuck")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Unmount_WithTheDetectionAttached_HandsThePaddingBackWithoutAnError()
    {
        await OpenAsync(Route);

        var box = Page.Locator("#v-unmount");
        await Expect(box).ToHaveCSSAsync("scroll-padding-top", "30px");
        await ScrollAsync("#v-unmount", 100);
        await Expect(Page.Locator("#stk-unmount")).ToHaveClassAsync(HasClass("bit-stk-stc"));

        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#unmount-shown")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#stk-unmount")).ToHaveCountAsync(0);
        await Expect(box).ToHaveCSSAsync("scroll-padding-top", "auto");

        // Scrolling after the removal reaches no disposed listener.
        await ScrollAsync("#v-unmount", 0);
        await ScrollAsync("#v-unmount", 150);

        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#stk-unmount")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#stk-unmount")).ToHaveClassAsync(HasClass("bit-stk-stc"));

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    private async Task ScrollAsync(string selector, double top)
    {
        await Page.Locator(selector).EvaluateAsync("(el, top) => el.scrollTop = top", top);
        await NextFramesAsync();
    }

    private async Task ScrollLeftAsync(string selector, double left)
    {
        await Page.Locator(selector).EvaluateAsync("(el, left) => el.scrollLeft = left", left);
        await NextFramesAsync();
    }

    private Task<double> TransitionMsAsync(string selector)
    {
        return Page.Locator(selector).EvaluateAsync<double>(@"el => {
            const v = getComputedStyle(el).transitionDuration.split(',')[0].trim();
            return v.endsWith('ms') ? parseFloat(v) : parseFloat(v) * 1000;
        }");
    }

    private Task<string> TagAsync(string selector) => Page.Locator(selector).EvaluateAsync<string>("el => el.tagName");

    // Where the element sits inside the client box (inside the border) of its scroller.
    private async Task<Offset> OffsetAsync(string element, string scroller)
    {
        var values = await Page.EvaluateAsync<double[]>(@"([e, s]) => {
            const el = document.querySelector(e).getBoundingClientRect();
            const box = document.querySelector(s);
            const r = box.getBoundingClientRect();
            const top = r.top + box.clientTop, left = r.left + box.clientLeft;
            return [el.top - top, top + box.clientHeight - el.bottom, el.left - left, left + box.clientWidth - el.right];
        }", new[] { element, scroller });

        return new(values[0], values[1], values[2], values[3]);
    }

    private sealed record Offset(double Top, double FromBottom, double Left, double FromRight);
}
