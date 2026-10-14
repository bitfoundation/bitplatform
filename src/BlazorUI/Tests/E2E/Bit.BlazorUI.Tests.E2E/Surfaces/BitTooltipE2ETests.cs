using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Surfaces;

/// <summary>
/// Drives BitTooltip (/e2e/tooltip and /e2e/tooltip/shown) in a real browser: shown by the pointer, the focus and the
/// click and hidden again (ShowOnHover / ShowOnFocus / ShowOnClick / HideOnClick, Enter and Space, a press outside,
/// Tab away, Escape and NoDismissOnEscape), ShowDelay / HideDelay and BitTooltipGroup (its delays, SkipDelay, one
/// tooltip at a time, AllowMultiple), Interactive, Template / Anchor / LazyRender, the description or label it gives its
/// anchor (Relationship, TooltipId), two-way and one-way IsShown, Show / Hide / Toggle and the events, the BitParams
/// cascade, Escape inside a modal, every Placement and Alignment measured with bounding boxes (RTL explicit and
/// inherited), the arrow, ArrowSize, Offset, MaxWidth and FullWidth, every Color and Size, Style / Class / Styles /
/// Classes, NoAnimation, ZIndex and the public CSS variables, reduced motion, Disabled, Visibility, HtmlAttributes,
/// prerendering, re-rendering and unmounting while shown.
/// </summary>
[TestClass]
public class BitTooltipE2ETests : E2ETestBase
{
    private const string Url = "/e2e/tooltip";
    private const string ShownUrl = "/e2e/tooltip/shown";

    private ILocator El(string id) => Page.Locator($"#{id}");
    private ILocator Tip(string id) => Page.Locator($"#{id} [role=tooltip]");
    private ILocator Wrapper(string id) => Page.Locator($"#{id} > .bit-ttp-wrp");
    private ILocator Arrow(string id) => Page.Locator($"#{id} .bit-ttp-arw");

    [TestMethod]
    public async Task Prerender_TheServerHtml_ContainsTheTooltipAndTheRelationship()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Url}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "role=\"tooltip\"");
        Assert.IsTrue(Regex.IsMatch(html, "id=\"ttp-basic\"[^>]*aria-describedby=\""), "The description is not prerendered.");
        StringAssert.Contains(html, "This is the tooltip text");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Hover_ShowsTheTooltip_AndLeavingTheAnchorHidesIt()
    {
        await OpenAsync(Url);

        await Expect(Tip("ttp-basic")).ToBeHiddenAsync();
        await El("ttp-basic-anchor").HoverAsync();

        await Expect(Tip("ttp-basic")).ToBeVisibleAsync();
        await Expect(Tip("ttp-basic")).ToHaveTextAsync("This is the tooltip text");
        await Expect(Wrapper("ttp-basic")).ToHaveClassAsync(HasClass("bit-ttp-vis"));

        await El("away-basic").HoverAsync();
        await Expect(Tip("ttp-basic")).ToBeHiddenAsync();
        await Expect(Wrapper("ttp-basic")).Not.ToHaveClassAsync(HasClass("bit-ttp-vis"));
    }

    [TestMethod]
    public async Task Focus_ShowsTheTooltip_AndTabbingAwayHidesIt_AndTheAnchorIsDescribedByIt()
    {
        await OpenAsync(Url);

        var id = (await Tip("ttp-basic").GetAttributeAsync("id"))!;
        await Expect(El("ttp-basic")).ToHaveAttributeAsync("aria-describedby", id);
        await Expect(El("ttp-basic-anchor")).ToHaveAttributeAsync("aria-describedby", id);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Hover over me" })).ToHaveAccessibleDescriptionAsync("This is the tooltip text");

        await Page.Locator("body").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(El("ttp-basic-anchor")).ToBeFocusedAsync();
        await Expect(Tip("ttp-basic")).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Tip("ttp-basic")).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task Escape_DismissesAShownTooltip_UnlessNoDismissOnEscape()
    {
        await OpenAsync(Url);

        await El("ttp-basic-anchor").HoverAsync();
        await Expect(Tip("ttp-basic")).ToBeVisibleAsync();
        await Expect(Wrapper("ttp-basic")).ToHaveAttributeAsync("data-bit-ttp-esc", "");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Tip("ttp-basic")).ToBeHiddenAsync();

        await El("ttp-noesc-anchor").HoverAsync();
        await Expect(Tip("ttp-noesc")).ToBeVisibleAsync();
        await Expect(Wrapper("ttp-noesc")).Not.ToHaveAttributeAsync("data-bit-ttp-esc", new Regex(".*"));
        await Page.Keyboard.PressAsync("Escape");
        await NextFramesAsync();
        await Expect(Tip("ttp-noesc")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Disabled_TurnsTheTooltipOff_ButADisabledAnchorStillGetsItsTooltip()
    {
        await OpenAsync(Url);

        await Expect(El("ttp-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await El("ttp-disabled-anchor").HoverAsync();
        await El("ttp-disabled-anchor").FocusAsync();
        await NextFramesAsync();
        await Expect(Tip("ttp-disabled")).ToBeHiddenAsync();

        await El("ttp-disabled-button-anchor").HoverAsync(new() { Force = true });
        await Expect(Tip("ttp-disabled-button")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ShowOnFocusFalse_OnlyThePointerShowsIt()
    {
        await OpenAsync(Url);

        await El("ttp-nofocus-anchor").FocusAsync();
        await Page.Keyboard.PressAsync("Shift");
        await NextFramesAsync();
        await Expect(Tip("ttp-nofocus")).ToBeHiddenAsync();

        await El("ttp-nofocus-anchor").HoverAsync();
        await Expect(Tip("ttp-nofocus")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ShowOnHoverFalse_OnlyTheFocusShowsIt()
    {
        await OpenAsync(Url);

        await El("ttp-nohover-anchor").HoverAsync();
        await NextFramesAsync();
        await Expect(Tip("ttp-nohover")).ToBeHiddenAsync();

        await El("ttp-nofocus-anchor").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(El("ttp-nohover-anchor")).ToBeFocusedAsync();
        await Expect(Tip("ttp-nohover")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ShowOnClick_APressTogglesIt_AndAPressOutsideOrATabAwayDismissesIt()
    {
        await OpenAsync(Url);

        var anchor = El("ttp-click-anchor");
        await anchor.HoverAsync();
        await NextFramesAsync();
        await Expect(Tip("ttp-click")).ToBeHiddenAsync();

        await anchor.ClickAsync();
        await Expect(Tip("ttp-click")).ToBeVisibleAsync();
        await Expect(Wrapper("ttp-click")).ToHaveAttributeAsync("data-bit-ttp-clk", "");
        await anchor.ClickAsync();
        await Expect(Tip("ttp-click")).ToBeHiddenAsync();

        await anchor.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Tip("ttp-click")).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync(" ");
        await Expect(Tip("ttp-click")).ToBeHiddenAsync();

        // A press on the tooltip itself - to select its text - leaves it open; one outside dismisses it.
        await anchor.ClickAsync();
        await Expect(Tip("ttp-click")).ToBeVisibleAsync();
        var tip = await BoxAsync(Tip("ttp-click"));
        await Page.Mouse.ClickAsync((float)(tip.X + 5), (float)(tip.Y + tip.Height / 2));
        await NextFramesAsync();
        await Expect(Tip("ttp-click")).ToBeVisibleAsync();
        await El("away-triggers").ClickAsync();
        await Expect(Tip("ttp-click")).ToBeHiddenAsync();

        await anchor.ClickAsync();
        await Expect(Tip("ttp-click")).ToBeVisibleAsync();
        await anchor.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Tip("ttp-click")).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task HideOnClick_APressOrAKeyHidesAHoverTooltip_AndStillPressesTheAnchor()
    {
        await OpenAsync(Url);

        var anchor = El("ttp-hideclick-anchor");
        await anchor.HoverAsync();
        await Expect(Tip("ttp-hideclick")).ToBeVisibleAsync();
        await anchor.ClickAsync();
        await Expect(Tip("ttp-hideclick")).ToBeHiddenAsync();
        await Expect(El("hideclick-anchor-clicks")).ToHaveTextAsync("1");

        await El("ttp-after").FocusAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(anchor).ToBeFocusedAsync();
        await Expect(Tip("ttp-hideclick")).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Tip("ttp-hideclick")).ToBeHiddenAsync();
        await Expect(El("hideclick-anchor-clicks")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task ShowDelay_HoldsThePointer_ButNotTheKeyboard()
    {
        await OpenAsync(Url);

        var watch = Stopwatch.StartNew();
        await El("ttp-showdelay-anchor").HoverAsync();
        await Expect(Tip("ttp-showdelay")).ToBeVisibleAsync();
        Assert.IsTrue(watch.ElapsedMilliseconds >= 600, $"Shown after {watch.ElapsedMilliseconds}ms.");

        await El("away-delays").HoverAsync();
        await Expect(Tip("ttp-showdelay")).ToBeHiddenAsync();

        await El("ttp-hidedelay-anchor").FocusAsync();
        watch.Restart();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(El("ttp-showdelay-anchor")).ToBeFocusedAsync();
        await Expect(Tip("ttp-showdelay")).ToBeVisibleAsync();
        Assert.IsTrue(watch.ElapsedMilliseconds < 600, $"The keyboard waited {watch.ElapsedMilliseconds}ms.");
    }

    [TestMethod]
    public async Task HideDelay_KeepsTheTooltipAWhileAfterThePointerLeaves()
    {
        await OpenAsync(Url);

        await El("ttp-hidedelay-anchor").HoverAsync();
        await Expect(Tip("ttp-hidedelay")).ToBeVisibleAsync();

        await El("away-delays").HoverAsync();
        var watch = Stopwatch.StartNew();
        Assert.IsTrue(await Tip("ttp-hidedelay").IsVisibleAsync(), "Hidden without waiting for HideDelay.");
        await Expect(Tip("ttp-hidedelay")).ToBeHiddenAsync();
        Assert.IsTrue(watch.ElapsedMilliseconds >= 850, $"Hidden after {watch.ElapsedMilliseconds}ms.");
    }

    [TestMethod]
    public async Task Group_HandsItsDelaysOn_AndSkipsTheShowDelayWhileTheLastOneIsFresh()
    {
        await OpenAsync(Url);

        var watch = Stopwatch.StartNew();
        await El("ttp-g1-anchor").HoverAsync();
        await Expect(Tip("ttp-g1")).ToBeVisibleAsync();
        Assert.IsTrue(watch.ElapsedMilliseconds >= 600, $"The first of the row was shown after {watch.ElapsedMilliseconds}ms.");

        await El("away-group").HoverAsync();
        await Expect(Tip("ttp-g1")).ToBeHiddenAsync();

        watch.Restart();
        await El("ttp-g2-anchor").HoverAsync();
        await Expect(Tip("ttp-g2")).ToBeVisibleAsync();
        Assert.IsTrue(watch.ElapsedMilliseconds < 500, $"The next one waited {watch.ElapsedMilliseconds}ms inside the skip window.");

        await El("away-group").HoverAsync();
        await Expect(Tip("ttp-g2")).ToBeHiddenAsync();

        // Past the 1500ms skip window the group's delay is back - except for a tooltip that set one of its own.
        await Page.WaitForTimeoutAsync(1700);
        watch.Restart();
        await El("ttp-g3-anchor").HoverAsync();
        await Expect(Tip("ttp-g3")).ToBeVisibleAsync();
        Assert.IsTrue(watch.ElapsedMilliseconds < 500, $"A tooltip with a ShowDelay of its own waited {watch.ElapsedMilliseconds}ms.");

        await El("away-group").HoverAsync();
        await Expect(Tip("ttp-g3")).ToBeHiddenAsync();
        await Page.WaitForTimeoutAsync(1700);
        watch.Restart();
        await El("ttp-g1-anchor").HoverAsync();
        await Expect(Tip("ttp-g1")).ToBeVisibleAsync();
        Assert.IsTrue(watch.ElapsedMilliseconds >= 600, $"Past the skip window the first one was shown after {watch.ElapsedMilliseconds}ms.");
    }

    [TestMethod]
    public async Task Group_MovingStraightOnFromAShownTooltip_ShowsTheNextOneAtOnce()
    {
        await OpenAsync(Url);

        await El("ttp-g1-anchor").HoverAsync();
        await Expect(Tip("ttp-g1")).ToBeVisibleAsync();

        var watch = Stopwatch.StartNew();
        await El("ttp-g2-anchor").HoverAsync();
        await Expect(Tip("ttp-g2")).ToBeVisibleAsync();
        Assert.IsTrue(watch.ElapsedMilliseconds < 500, $"The next tooltip along waited {watch.ElapsedMilliseconds}ms while the reader was reading the row.");
        await Expect(Tip("ttp-g1")).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task Group_ShowsOneTooltipAtATime_UnlessAllowMultiple()
    {
        await OpenAsync(Url);

        await El("btn-show-g12").ClickAsync();
        await Expect(Tip("ttp-g2")).ToBeVisibleAsync();
        await Expect(Tip("ttp-g1")).ToBeHiddenAsync();

        await El("btn-show-many").ClickAsync();
        await Expect(Tip("ttp-m1")).ToBeVisibleAsync();
        await Expect(Tip("ttp-m2")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Interactive_ThePointerCanMoveOntoTheTooltip_AndNonInteractiveHidesOnLeavingTheAnchor()
    {
        await OpenAsync(Url);

        await El("ttp-itr-anchor").HoverAsync();
        await Expect(Tip("ttp-itr")).ToBeVisibleAsync();
        await Expect(El("ttp-itr")).ToHaveClassAsync(HasClass("bit-ttp-itr"));
        await Expect(Tip("ttp-itr")).ToHaveCSSAsync("pointer-events", "auto");
        var tip = await BoxAsync(Tip("ttp-itr"));
        await Page.Mouse.MoveAsync((float)(tip.X + tip.Width / 2), (float)(tip.Y + tip.Height / 2), new() { Steps = 5 });
        await Page.WaitForTimeoutAsync(300);
        await Expect(Tip("ttp-itr")).ToBeVisibleAsync();

        await El("ttp-noitr-anchor").HoverAsync();
        await Expect(Tip("ttp-noitr")).ToBeVisibleAsync();
        await Expect(Tip("ttp-noitr")).ToHaveCSSAsync("pointer-events", "none");
        var other = await BoxAsync(Tip("ttp-noitr"));
        await Page.Mouse.MoveAsync((float)(other.X + other.Width / 2), (float)(other.Y + other.Height / 2), new() { Steps = 5 });
        await Expect(Tip("ttp-noitr")).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task TemplateAnchorAndLazyRender_FillTheTooltip()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#ttp-tpl [role=tooltip] #ttp-tpl-content")).ToHaveCountAsync(1);
        await El("ttp-tpl-anchor").HoverAsync();
        await Expect(El("ttp-tpl-content")).ToBeVisibleAsync();

        await Expect(Tip("ttp-lazy")).ToHaveTextAsync("");
        await El("ttp-lazy-anchor").HoverAsync();
        await Expect(Tip("ttp-lazy")).ToHaveTextAsync("Rendered on first show");
        await El("away-tpl").HoverAsync();
        await Expect(Tip("ttp-lazy")).ToBeHiddenAsync();
        await Expect(Tip("ttp-lazy")).ToHaveTextAsync("Rendered on first show");

        // A tooltip with nothing to say renders no surface and no relationship.
        await Expect(Tip("ttp-empty")).ToHaveCountAsync(0);
        await Expect(El("ttp-empty")).Not.ToHaveAttributeAsync("aria-describedby", new Regex(".*"));
    }

    [TestMethod]
    public async Task Relationship_LabelDescriptionAndNone_NameDescribeOrLeaveTheAnchor()
    {
        await OpenAsync(Url);

        var labelId = (await Tip("ttp-label").GetAttributeAsync("id"))!;
        await Expect(El("ttp-label")).ToHaveAttributeAsync("aria-labelledby", labelId);
        await Expect(El("ttp-label")).Not.ToHaveAttributeAsync("aria-describedby", new Regex(".*"));
        await Expect(El("ttp-label-anchor")).ToHaveAttributeAsync("aria-labelledby", labelId);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Save the current document" })).ToHaveCountAsync(1);

        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Revert" })).ToHaveAccessibleDescriptionAsync("Everything you have written since the last save");

        await Expect(El("ttp-none")).Not.ToHaveAttributeAsync("aria-describedby", new Regex(".*"));
        await Expect(El("ttp-none")).Not.ToHaveAttributeAsync("aria-labelledby", new Regex(".*"));
        await Expect(El("ttp-none-anchor")).ToHaveAttributeAsync("aria-describedby", (await Tip("ttp-none").GetAttributeAsync("id"))!);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Trash" })).ToHaveAccessibleDescriptionAsync("Discard");

        // An anchor that names a description of its own keeps it.
        await Expect(El("ttp-own-anchor")).ToHaveAttributeAsync("aria-describedby", "own-description");
    }

    [TestMethod]
    public async Task IsShown_TwoWay_ThePageAndTheTriggersDriveTheSameState()
    {
        await OpenAsync(Url);

        await El("btn-bind").ClickAsync();
        await Expect(Tip("ttp-bind")).ToBeVisibleAsync();
        await Expect(El("bind-value")).ToHaveTextAsync("True");

        await El("ttp-bind-anchor").HoverAsync();
        await El("away-params").HoverAsync();
        await Expect(Tip("ttp-bind")).ToBeHiddenAsync();
        await Expect(El("bind-value")).ToHaveTextAsync("False");

        await El("ttp-bind-anchor").HoverAsync();
        await Expect(El("bind-value")).ToHaveTextAsync("True");
    }

    [TestMethod]
    public async Task IsShown_OneWay_LeavesTheStateToThePageAlone()
    {
        await OpenAsync(Url);

        await El("ttp-ctl-anchor").HoverAsync();
        await NextFramesAsync();
        await Expect(Tip("ttp-ctl")).ToBeHiddenAsync();

        await El("btn-ctl").ClickAsync();
        await Expect(Tip("ttp-ctl")).ToBeVisibleAsync();
        await Expect(Wrapper("ttp-ctl")).Not.ToHaveAttributeAsync("data-bit-ttp-esc", new Regex(".*"));
        await El("ttp-ctl-anchor").HoverAsync();
        await Page.Keyboard.PressAsync("Escape");
        await El("away-params").HoverAsync();
        await NextFramesAsync();
        await Expect(Tip("ttp-ctl")).ToBeVisibleAsync();

        await El("btn-ctl").ClickAsync();
        await Expect(Tip("ttp-ctl")).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task Methods_ShowHideAndToggle_ActAtOnce_AndTheEventsReportEachChange()
    {
        await OpenAsync(Url);

        await El("btn-m-show").ClickAsync();
        await Expect(Tip("ttp-m")).ToBeVisibleAsync();
        await Expect(El("ev-shows")).ToHaveTextAsync("1");
        await Expect(El("ev-last")).ToHaveTextAsync("True");

        await El("btn-m-show").ClickAsync();
        await NextFramesAsync();
        await Expect(El("ev-shows")).ToHaveTextAsync("1");

        await El("btn-m-hide").ClickAsync();
        await Expect(Tip("ttp-m")).ToBeHiddenAsync();
        await Expect(El("ev-hides")).ToHaveTextAsync("1");
        await Expect(El("ev-last")).ToHaveTextAsync("False");

        await El("btn-m-toggle").ClickAsync();
        await Expect(Tip("ttp-m")).ToBeVisibleAsync();
        await El("btn-m-toggle").ClickAsync();
        await Expect(Tip("ttp-m")).ToBeHiddenAsync();

        await Expect(El("ev-toggles")).ToHaveTextAsync("4");
        await Expect(El("ev-shows")).ToHaveTextAsync("2");
        await Expect(El("ev-hides")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task BitParams_FillsTheUnsetParameters_AndTheTooltipsOwnValuesWin()
    {
        await OpenAsync(Url);

        await El("ttp-par-1-anchor").HoverAsync();
        await Expect(Tip("ttp-par-1")).ToBeVisibleAsync();
        await Expect(Wrapper("ttp-par-1")).ToHaveClassAsync(HasClass("bit-ttp-btm"));
        await Expect(El("ttp-par-1")).ToHaveClassAsync(HasClass("bit-ttp-pri"));
        await Expect(Arrow("ttp-par-1")).ToHaveCountAsync(0);
        Assert.IsTrue((await BoxAsync(Tip("ttp-par-1"))).Y > (await BoxAsync(El("ttp-par-1"))).Y);

        await El("ttp-par-own-anchor").HoverAsync();
        await Expect(Tip("ttp-par-own")).ToBeVisibleAsync();
        await Expect(Wrapper("ttp-par-own")).ToHaveClassAsync(HasClass("bit-ttp-top"));
        await Expect(El("ttp-par-own")).ToHaveClassAsync(HasClass("bit-ttp-sec"));
        await Expect(Arrow("ttp-par-own")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task InsideAModal_EscapeDismissesTheTooltipFirst_AndTheModalOnTheNextPress()
    {
        await OpenAsync(Url);

        await El("btn-open-modal").ClickAsync();
        await Expect(El("ttp-in-modal-anchor")).ToBeVisibleAsync();
        await El("ttp-in-modal-anchor").HoverAsync();
        await Expect(Tip("ttp-in-modal")).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Tip("ttp-in-modal")).ToBeHiddenAsync();
        await NextFramesAsync();
        await Expect(El("modal-open")).ToHaveTextAsync("True");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(El("modal-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task ReducedMotion_CollapsesTheFade()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await OpenAsync(Url);

        Assert.IsTrue(ToMs(await StyleAsync(Wrapper("ttp-basic"), "transition-duration")) < 0.1);
        await El("ttp-basic-anchor").HoverAsync();
        await Expect(Tip("ttp-basic")).ToBeVisibleAsync();
        await Expect(Wrapper("ttp-basic")).ToHaveCSSAsync("opacity", "1");
    }

    [TestMethod]
    public async Task Visibility_HiddenKeepsTheBox_CollapsedRemovesIt()
    {
        await OpenAsync(Url);

        await Expect(El("ttp-vis-hidden")).ToHaveCSSAsync("visibility", "hidden");
        Assert.IsTrue((await BoxAsync(El("ttp-vis-hidden"))).Width > 0);
        await Expect(El("ttp-vis-collapsed")).ToHaveCSSAsync("display", "none");
    }

    [TestMethod]
    public async Task HtmlAttributes_AreSplattedOnTheRoot()
    {
        await OpenAsync(Url);

        await Expect(El("ttp-attrs")).ToHaveAttributeAsync("data-e2e", "custom");
        await Expect(El("ttp-attrs")).ToHaveAttributeAsync("data-extra", "from-dictionary");
    }

    [TestMethod]
    public async Task Rerender_ParametersChangedByThePage_UpdateTheTooltip()
    {
        await OpenAsync(Url);

        await Expect(Tip("ttp-rr")).ToBeHiddenAsync();
        await El("btn-rr").ClickAsync();

        await Expect(Tip("ttp-rr")).ToBeVisibleAsync();
        await Expect(Tip("ttp-rr")).ToHaveTextAsync("After");
        await Expect(El("ttp-rr")).ToHaveClassAsync(HasClass("bit-ttp-err"));
        await Expect(Wrapper("ttp-rr")).ToHaveClassAsync(HasClass("bit-ttp-btm"));
    }

    [TestMethod]
    public async Task Unmount_WhileShownAndWaitingToHide_RemovesTheTooltipWithoutAnError()
    {
        await OpenAsync(Url);

        await El("ttp-um-anchor").HoverAsync();
        await Expect(Tip("ttp-um")).ToBeVisibleAsync();
        await El("btn-unmount").HoverAsync();
        await El("btn-unmount").ClickAsync();
        await Expect(El("ttp-um")).ToHaveCountAsync(0);
        await Page.WaitForTimeoutAsync(600);

        await El("btn-unmount").ClickAsync();
        await Expect(El("ttp-um")).ToHaveCountAsync(1);
        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    [DataRow("top")]
    [DataRow("bottom")]
    [DataRow("left")]
    [DataRow("right")]
    [DataRow("start")]
    [DataRow("end")]
    [DataRow("center")]
    public async Task Placement_PutsTheTooltipOnItsSide_TheGapAway_WithTheArrowPointingAtTheAnchor(string name)
    {
        await OpenAsync(ShownUrl);

        var id = $"tp-{name}";
        await SettleAsync(id);
        var anchor = await BoxAsync(El(id));
        var tip = await BoxAsync(Tip(id));
        var arrow = await BoxAsync(Arrow(id));
        var arrowX = arrow.X + arrow.Width / 2;
        var arrowY = arrow.Y + arrow.Height / 2;

        switch (name)
        {
            case "top" or "center":
                Near(anchor.Y - 10, tip.Y + tip.Height, "above, 10px off");
                Near(anchor.X + anchor.Width / 2, tip.X + tip.Width / 2, "centered");
                Near(tip.Y + tip.Height, arrowY, "the arrow on the bottom edge");
                break;
            case "bottom":
                Near(anchor.Y + anchor.Height + 10, tip.Y, "below, 10px off");
                Near(anchor.X + anchor.Width / 2, tip.X + tip.Width / 2, "centered");
                Near(tip.Y, arrowY, "the arrow on the top edge");
                break;
            case "left" or "start":
                Near(anchor.X - 10, tip.X + tip.Width, "on the left, 10px off");
                Near(anchor.Y + anchor.Height / 2, tip.Y + tip.Height / 2, "centered");
                Near(tip.X + tip.Width, arrowX, "the arrow on the right edge");
                break;
            default:
                Near(anchor.X + anchor.Width + 10, tip.X, "on the right, 10px off");
                Near(anchor.Y + anchor.Height / 2, tip.Y + tip.Height / 2, "centered");
                Near(tip.X, arrowX, "the arrow on the left edge");
                break;
        }
    }

    [TestMethod]
    [DataRow("start", "left")]
    [DataRow("end", "right")]
    [DataRow("left", "left")]
    [DataRow("right", "right")]
    [DataRow("center", "center")]
    public async Task Alignment_LinesATooltipAboveTheAnchorUpAcrossIt(string name, string edge)
    {
        await OpenAsync(ShownUrl);

        var id = $"ta-{name}";
        await SettleAsync(id);
        var anchor = await BoxAsync(El(id));
        var tip = await BoxAsync(Tip(id));

        switch (edge)
        {
            case "left": Near(anchor.X, tip.X, "left edges"); break;
            case "right": Near(anchor.X + anchor.Width, tip.X + tip.Width, "right edges"); break;
            default: Near(anchor.X + anchor.Width / 2, tip.X + tip.Width / 2, "centers"); break;
        }
    }

    [TestMethod]
    [DataRow("top", "top")]
    [DataRow("bottom", "bottom")]
    [DataRow("start", "top")]
    [DataRow("middle", "middle")]
    public async Task Alignment_LinesATooltipBesideTheAnchorUpAlongIt(string name, string edge)
    {
        await OpenAsync(ShownUrl);

        var id = $"ts-{name}";
        await SettleAsync(id);
        var anchor = await BoxAsync(El(id));
        var tip = await BoxAsync(Tip(id));

        switch (edge)
        {
            case "top": Near(anchor.Y, tip.Y, "top edges"); break;
            case "bottom": Near(anchor.Y + anchor.Height, tip.Y + tip.Height, "bottom edges"); break;
            default: Near(anchor.Y + anchor.Height / 2, tip.Y + tip.Height / 2, "middles"); break;
        }
    }

    [TestMethod]
    public async Task Rtl_StartAndEndFollowTheReadingDirection_ExplicitOrInherited()
    {
        await OpenAsync(ShownUrl);

        await SettleAsync("tp-rtl-start");
        await Expect(El("tp-rtl-start")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(El("tp-rtl-start")).ToHaveClassAsync(HasClass("bit-rtl"));
        var anchor = await BoxAsync(El("tp-rtl-start"));
        Near(anchor.X + anchor.Width + 10, (await BoxAsync(Tip("tp-rtl-start"))).X, "Start is on the right");

        var inherited = await BoxAsync(El("tp-rtl-inh"));
        var inheritedTip = await BoxAsync(Tip("tp-rtl-inh"));
        Near(inherited.X - 10, inheritedTip.X + inheritedTip.Width, "End is on the left");

        var aligned = await BoxAsync(El("tp-rtl-align"));
        var alignedTip = await BoxAsync(Tip("tp-rtl-align"));
        Near(aligned.X + aligned.Width, alignedTip.X + alignedTip.Width, "aligned to the start, which is the right");
    }

    [TestMethod]
    public async Task ArrowSizeOffsetAndHideArrow_ShapeTheGapAndTheArrow()
    {
        await OpenAsync(ShownUrl);

        await SettleAsync("tw-default");
        Near(12 * Math.Sqrt(2), (await BoxAsync(Arrow("tw-default"))).Width, "the default 12px arrow, turned");
        await Expect(Arrow("tw-noarrow")).ToHaveCountAsync(0);

        var big = await BoxAsync(Arrow("tw-arrow"));
        Near(18 * Math.Sqrt(2), big.Width, "an 18px arrow, turned");
        var bigAnchor = await BoxAsync(El("tw-arrow"));
        var bigTip = await BoxAsync(Tip("tw-arrow"));
        Near(bigAnchor.Y - 18 * 0.7071, bigTip.Y + bigTip.Height, "an arrow larger than the offset is given its room");

        var offAnchor = await BoxAsync(El("tw-offset"));
        var offTip = await BoxAsync(Tip("tw-offset"));
        Near(offAnchor.Y - 24, offTip.Y + offTip.Height, "Offset 24");
    }

    [TestMethod]
    public async Task MaxWidthAndFullWidth_SizeTheTooltipAndItsRoot()
    {
        await OpenAsync(ShownUrl);

        var capped = await BoxAsync(Tip("tw-max"));
        Assert.IsTrue(capped.Width <= 181, $"MaxWidth 10rem: {capped.Width}");
        Assert.IsTrue(capped.Height > 50, "The long text does not wrap.");

        var uncapped = await BoxAsync(Tip("tw-nomax"));
        Assert.IsTrue(uncapped.Width > 250 && uncapped.Height < 40, $"MaxWidth none: {uncapped.Width}x{uncapped.Height}");

        Near(400, (await BoxAsync(El("tw-full"))).Width, "FullWidth");
        await Expect(El("tw-full")).ToHaveClassAsync(HasClass("bit-ttp-flw"));
        Assert.IsTrue((await BoxAsync(El("tw-shrunk"))).Width < 300, "Without FullWidth the root is shrunk to its anchor.");
    }

    [TestMethod]
    public async Task Color_EveryRole_PaintsTheSurfaceAndTheArrow()
    {
        await OpenAsync(ShownUrl);

        var suffixes = new[] { "pri", "sec", "ter", "inf", "suc", "wrn", "swr", "err", "pbg", "sbg", "tbg", "pfg", "sfg", "tfg", "pbr", "sbr", "tbr" };
        var semantic = new List<string>();
        foreach (var suffix in suffixes)
        {
            var id = $"tc-{suffix}";
            await Expect(El(id)).ToHaveClassAsync(HasClass($"bit-ttp-{suffix}"));
            var background = await StyleAsync(Tip(id), "background-color");
            Assert.AreEqual(background, await StyleAsync(Arrow(id), "background-color"), $"{suffix}: the arrow is not painted like the surface.");
            Assert.AreNotEqual(background, await StyleAsync(Tip(id), "color"), $"{suffix}: the text is the color of the surface.");
            if (semantic.Count < 8) semantic.Add(background);
        }

        Assert.AreEqual(8, semantic.Distinct().Count(), string.Join(" | ", semantic));
        await Expect(El("tc-unset")).Not.ToHaveClassAsync(new Regex(@"bit-ttp-(pri|sec|ter|inf|suc|wrn|swr|err)\b"));
    }

    [TestMethod]
    public async Task Size_EverySize_SetsTheTextAndThePadding()
    {
        await OpenAsync(ShownUrl);

        var fonts = new List<double>();
        var paddings = new List<double>();
        foreach (var size in new[] { "sm", "md", "lg" })
        {
            await Expect(El($"tz-{size}")).ToHaveClassAsync(HasClass($"bit-ttp-{size}"));
            fonts.Add(Px(await StyleAsync(Tip($"tz-{size}"), "font-size")));
            paddings.Add(Px(await StyleAsync(Tip($"tz-{size}"), "padding-top")));
        }

        Assert.IsTrue(fonts[0] < fonts[1] && fonts[1] < fonts[2], string.Join(", ", fonts));
        Assert.IsTrue(paddings[0] < paddings[1] && paddings[1] < paddings[2], string.Join(", ", paddings));
        // Unset, the text is as small as Small's.
        Assert.AreEqual(fonts[0], Px(await StyleAsync(Tip("tz-unset"), "font-size")));
    }

    [TestMethod]
    public async Task StyleStylesClassesZIndexAndNoAnimation_ReachTheirParts()
    {
        await OpenAsync(ShownUrl);

        await Expect(El("tst-style")).ToHaveCSSAsync("margin-top", "3px");

        await Expect(El("tst-styles")).ToHaveCSSAsync("padding-top", "1px");
        await Expect(Wrapper("tst-styles")).ToHaveCSSAsync("word-spacing", "5px");
        await Expect(Tip("tst-styles")).ToHaveCSSAsync("box-shadow", "rgb(0, 255, 255) 0px 0px 8px 0px");
        await Expect(Arrow("tst-styles")).ToHaveCSSAsync("outline-color", "rgb(7, 8, 9)");

        await Expect(El("tst-classes")).ToHaveClassAsync(HasClass("e2e-ttp-class-root"));
        await Expect(Wrapper("tst-classes")).ToHaveCSSAsync("word-spacing", "3px");
        await Expect(Tip("tst-classes")).ToHaveCSSAsync("letter-spacing", "2px");
        await Expect(Arrow("tst-classes")).ToHaveCSSAsync("outline-color", "rgb(4, 5, 6)");

        await Expect(Tip("tst-zindex")).ToHaveCSSAsync("z-index", "5000");
        await Expect(Arrow("tst-zindex")).ToHaveCSSAsync("z-index", "5001");
        await Expect(El("tst-zindex")).ToHaveClassAsync(HasClass("bit-ttp-nan"));
        await Expect(Wrapper("tst-zindex")).ToHaveCSSAsync("transition-property", "none");
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_RestyleTheTooltip_AndColorAndSizeWinOverThem()
    {
        await OpenAsync(ShownUrl);

        await SettleAsync("tst-var");
        var tip = Tip("tst-var");
        await Expect(tip).ToHaveCSSAsync("background-color", "rgb(30, 41, 59)");
        await Expect(Arrow("tst-var")).ToHaveCSSAsync("background-color", "rgb(30, 41, 59)");
        await Expect(tip).ToHaveCSSAsync("color", "rgb(248, 250, 252)");
        await Expect(tip).ToHaveCSSAsync("padding-top", "14px");
        await Expect(tip).ToHaveCSSAsync("font-size", "17px");
        await Expect(tip).ToHaveCSSAsync("font-weight", "700");
        await Expect(tip).ToHaveCSSAsync("text-align", "center");
        await Expect(tip).ToHaveCSSAsync("border-top-left-radius", "0px");
        Assert.IsTrue((await BoxAsync(tip)).Width <= 120 + 28 + 0.5, "The max-width variable does not wrap the text.");
        var anchor = await BoxAsync(El("tst-var"));
        Near(anchor.Y - 20, (await BoxAsync(tip)).Y + (await BoxAsync(tip)).Height, "the offset variable");

        var wins = Tip("tst-var-wins");
        Assert.AreEqual(await StyleAsync(Tip("tc-err"), "background-color"), await StyleAsync(wins, "background-color"));
        Assert.AreEqual(await StyleAsync(Tip("tz-lg"), "font-size"), await StyleAsync(wins, "font-size"));
    }

    private Task SettleAsync(string id)
    {
        return PollAsync(() => Page.Locator($"#{id} > .bit-ttp-wrp").EvaluateAsync<bool>("el => [el, ...el.querySelectorAll('*')].every(e => e.getAnimations().length === 0)"), done => done, $"{id} never settled");
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
}
