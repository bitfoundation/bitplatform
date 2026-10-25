using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Surfaces;

/// <summary>
/// Drives BitSplitter (/e2e/splitter) in a real browser, each splitter in a 600px box: the gutter dragged with the
/// mouse (OnResizeStart / OnResize / OnResizeEnd, Escape and OnResizeCancel, the double-click reset), moved from the
/// keyboard (arrows, Shift, Page Up / Down, Home / End, KeyboardStep), Vertical, the panel sizes and their Min / Max
/// limits (CSS ones included), Percent bound both ways and one way, DefaultPercent, DragStep, LazyResize and its
/// preview line, GutterSize / GutterHitSize / GutterIconName / GutterTemplate, Collapsible with Enter, Ctrl + arrows,
/// the folded gutter, a snapping drag, SnapSize, CollapsedSize, CollapseSecondPanel and the inert folded panel, the
/// collapse button and its icons, OnCollapsing refusing a fold with every reason, NoResetOnDoubleClick, the public
/// methods, PersistKey in local and session storage across a reload, ReadOnly and Disabled, nesting, the WAI-ARIA
/// separator, the BitParams cascade, external icons, Style / Class / Styles / Classes and the public CSS variables,
/// RTL explicit and inherited, Visibility, HtmlAttributes, prerendering, re-rendering and unmounting.
/// </summary>
[TestClass]
public class BitSplitterE2ETests : E2ETestBase
{
    private const string Url = "/e2e/splitter";

    private ILocator El(string id) => Page.Locator($"#{id}");
    private ILocator Gutter(string id) => Page.Locator($"#{id} > .bit-spl-gtr");
    private ILocator First(string id) => Page.Locator($"#{id} > .bit-spl-fpn");
    private ILocator Second(string id) => Page.Locator($"#{id} > .bit-spl-spn");

    [TestMethod]
    public async Task Prerender_TheServerHtml_ContainsThePanelsAndTheSeparator()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Url}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "id=\"spl-basic\"");
        StringAssert.Contains(html, "role=\"separator\"");
        StringAssert.Contains(html, "id=\"spl-basic-first\"");
        StringAssert.Contains(html, "id=\"spl-basic-second\"");
        Assert.IsTrue(Regex.IsMatch(html, "id=\"spl-limits\"[^>]*--bit-spl-fpn-size:200px"), "The first panel size is not prerendered.");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Basic_SplitsTheBoxEvenly_AroundASeparatorWiredToTheFirstPanel()
    {
        await OpenAsync(Url);

        Near(295, await WidthAsync(First("spl-basic")), "first panel");
        Near(295, await WidthAsync(Second("spl-basic")), "second panel");
        Near(10, await WidthAsync(Gutter("spl-basic")), "gutter");

        var gutter = Gutter("spl-basic");
        await Expect(gutter).ToHaveAttributeAsync("role", "separator");
        await Expect(gutter).ToHaveAttributeAsync("aria-orientation", "vertical");
        await Expect(gutter).ToHaveAttributeAsync("aria-label", "Resize the panels");
        await Expect(gutter).ToHaveAttributeAsync("aria-controls", (await First("spl-basic").GetAttributeAsync("id"))!);
        await Expect(gutter).ToHaveAttributeAsync("tabindex", "0");
        await Expect(gutter).ToHaveAttributeAsync("aria-valuemin", "0");
        // The range is narrowed by measurement to what the limits of the panels leave: the gutter's own 10px.
        await Expect(gutter).ToHaveAttributeAsync("aria-valuemax", "98.33");
        await Expect(gutter).ToHaveAttributeAsync("aria-valuenow", "49.17");
        await Expect(gutter).ToHaveAttributeAsync("aria-valuetext", "49.17%");
        await Expect(gutter).ToHaveCSSAsync("cursor", "col-resize");
        await Expect(Page.GetByRole(AriaRole.Separator, new() { Name = "Resize the panels" })).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Drag_TheMouseMovesTheGutter_AndTheResizeIsReportedFromStartToEnd()
    {
        await OpenAsync(Url);

        await DragGutterAsync("spl-basic", 100, 0, async () =>
        {
            await Expect(El("spl-basic")).ToHaveClassAsync(HasClass("bit-spl-drg"));
            await Expect(Page.Locator("body")).ToHaveClassAsync(HasClass("bit-spl-drg-bdy"));
            Near(395, await WidthAsync(First("spl-basic")), "the first panel follows the pointer");
        });

        Near(395, await WidthAsync(First("spl-basic")), "first panel");
        Near(195, await WidthAsync(Second("spl-basic")), "second panel");
        await Expect(El("ev-starts")).ToHaveTextAsync("1");
        await Expect(El("ev-start")).ToHaveTextAsync("49.17");
        await Expect(El("ev-ends")).ToHaveTextAsync("1");
        await Expect(El("ev-end")).ToHaveTextAsync("65.83");
        await Expect(El("ev-cancels")).ToHaveTextAsync("0");
        Assert.IsTrue(int.Parse((await El("ev-resizes").TextContentAsync())!, CultureInfo.InvariantCulture) >= 1);
        await Expect(Gutter("spl-basic")).ToHaveAttributeAsync("aria-valuenow", "65.83");
        await Expect(El("spl-basic")).Not.ToHaveClassAsync(HasClass("bit-spl-drg"));
        await Expect(Page.Locator("body")).Not.ToHaveClassAsync(HasClass("bit-spl-drg-bdy"));
    }

    [TestMethod]
    public async Task Drag_Escape_CancelsTheDragAndPutsTheSplitBack()
    {
        await OpenAsync(Url);

        var gutter = await BoxAsync(Gutter("spl-basic"));
        var x = (float)(gutter.X + gutter.Width / 2);
        var y = (float)(gutter.Y + gutter.Height / 2);
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + 80, y, new() { Steps = 4 });
        await PollAsync(() => WidthAsync(First("spl-basic")), w => Math.Abs(w - 375) < 1.5, "the drag did not move the gutter");

        await Expect(Gutter("spl-basic")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Page.Mouse.UpAsync();

        await PollAsync(() => WidthAsync(First("spl-basic")), w => Math.Abs(w - 295) < 1.5, "Escape did not put the split back");
        await Expect(El("ev-cancels")).ToHaveTextAsync("1");
        await Expect(El("ev-cancel")).ToHaveTextAsync("49.17");
        await Expect(El("ev-ends")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Keyboard_ArrowsShiftPageKeysHomeAndEnd_MoveTheGutter()
    {
        await OpenAsync(Url);

        var first = First("spl-basic");
        await Gutter("spl-basic").FocusAsync();

        await Page.Keyboard.PressAsync("ArrowRight");
        await PollAsync(() => WidthAsync(first), w => Math.Abs(w - 305) < 1, "ArrowRight");
        // The keyboard is in use, so the focus ring is drawn.
        await Expect(Gutter("spl-basic")).ToHaveCSSAsync("box-shadow", new Regex("^(?!none).+"));
        await Page.Keyboard.PressAsync("Shift+ArrowRight");
        await PollAsync(() => WidthAsync(first), w => Math.Abs(w - 405) < 1, "Shift+ArrowRight");
        await Page.Keyboard.PressAsync("PageUp");
        await PollAsync(() => WidthAsync(first), w => Math.Abs(w - 305) < 1, "PageUp");
        await Page.Keyboard.PressAsync("PageDown");
        await PollAsync(() => WidthAsync(first), w => Math.Abs(w - 405) < 1, "PageDown");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await PollAsync(() => WidthAsync(first), w => Math.Abs(w - 395) < 1, "ArrowLeft");
        await Page.Keyboard.PressAsync("End");
        await PollAsync(() => WidthAsync(first), w => Math.Abs(w - 590) < 1, "End");
        await Page.Keyboard.PressAsync("Home");
        await PollAsync(() => WidthAsync(first), w => w < 1, "Home");

        // A key move is a resize like a drag: it is opened, and closed once the keys come back up.
        await Expect(El("ev-ends")).Not.ToHaveTextAsync("0");
        Assert.IsTrue(int.Parse((await El("ev-starts").TextContentAsync())!, CultureInfo.InvariantCulture) >= 1);
        await Expect(Gutter("spl-basic")).ToHaveAttributeAsync("aria-valuenow", "0");
    }

    [TestMethod]
    public async Task DoubleClick_ResetsTheSplitToTheDeclaredSizes()
    {
        await OpenAsync(Url);

        await DragGutterAsync("spl-basic", 100, 0);
        Near(395, await WidthAsync(First("spl-basic")), "dragged");

        await Gutter("spl-basic").DblClickAsync();
        await PollAsync(() => WidthAsync(First("spl-basic")), w => Math.Abs(w - 295) < 1.5, "the double-click did not reset the split");
        await Expect(El("ev-dbl")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Vertical_StacksThePanels_AndTheGutterMovesUpAndDown()
    {
        await OpenAsync(Url);

        var root = "spl-vert";
        await Expect(El(root)).ToHaveClassAsync(HasClass("bit-spl-vrt"));
        await Expect(Gutter(root)).ToHaveAttributeAsync("aria-orientation", "horizontal");
        await Expect(Gutter(root)).ToHaveCSSAsync("cursor", "row-resize");
        Near(145, await HeightAsync(First(root)), "the top panel");
        Assert.IsTrue((await BoxAsync(Second(root))).Y > (await BoxAsync(First(root))).Y);

        await DragGutterAsync(root, 0, 50);
        Near(195, await HeightAsync(First(root)), "dragged down");

        await Gutter(root).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await PollAsync(() => HeightAsync(First(root)), h => Math.Abs(h - 205) < 1, "ArrowDown");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.Keyboard.PressAsync("ArrowUp");
        await PollAsync(() => HeightAsync(First(root)), h => Math.Abs(h - 185) < 1, "ArrowUp");
        await Page.Keyboard.PressAsync("ArrowRight");
        await NextFramesAsync();
        Near(185, await HeightAsync(First(root)), "a sideways arrow on a vertical splitter");
    }

    [TestMethod]
    public async Task Limits_HoldTheGutterForThePointerAndTheKeyboard()
    {
        await OpenAsync(Url);

        var root = "spl-limits";
        Near(200, await WidthAsync(First(root)), "FirstPanelSize");
        await Expect(Gutter(root)).ToHaveAttributeAsync("aria-valuemin", "20");

        await DragGutterAsync(root, -200, 0);
        Near(120, await WidthAsync(First(root)), "FirstPanelMinSize");
        await DragGutterAsync(root, 300, 0);
        Near(320, await WidthAsync(First(root)), "FirstPanelMaxSize");

        await Gutter(root).FocusAsync();
        await Page.Keyboard.PressAsync("Home");
        await PollAsync(() => WidthAsync(First(root)), w => Math.Abs(w - 120) < 1, "Home stops at the minimum");
        await Page.Keyboard.PressAsync("End");
        await PollAsync(() => WidthAsync(First(root)), w => Math.Abs(w - 320) < 1, "End stops at the maximum");
    }

    [TestMethod]
    public async Task Limits_SecondPanelSizeAndMaxSize_AndACssMinimumOfThePage()
    {
        await OpenAsync(Url);

        var root = "spl-second";
        Near(150, await WidthAsync(Second(root)), "SecondPanelSize");

        // The second panel may not grow past 300px, which holds the first at 290px - above its 30% CSS minimum.
        await DragGutterAsync(root, -400, 0);
        Near(300, await WidthAsync(Second(root)), "SecondPanelMaxSize");
        Near(290, await WidthAsync(First(root)), "the first panel");
    }

    [TestMethod]
    public async Task Percent_TwoWay_ThePageAndTheGutterDriveTheSameShare()
    {
        await OpenAsync(Url);

        var root = "spl-percent";
        Near(300, await WidthAsync(First(root)), "50%");

        await El("btn-pct-25").ClickAsync();
        await PollAsync(() => WidthAsync(First(root)), w => Math.Abs(w - 150) < 1, "the page did not move the split");

        await DragGutterAsync(root, 60, 0);
        await Expect(El("pct-value")).ToHaveTextAsync("35");
        Near(210, await WidthAsync(First(root)), "dragged");

        await Gutter(root).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(El("pct-value")).ToHaveTextAsync("36.67");

        await El("btn-pct-75").ClickAsync();
        await PollAsync(() => WidthAsync(First(root)), w => Math.Abs(w - 450) < 1, "75%");
    }

    [TestMethod]
    public async Task DefaultPercent_StartsTheSplit_AndTheResetComesBackToIt()
    {
        await OpenAsync(Url);

        Near(180, await WidthAsync(First("spl-default")), "30%");
        await DragGutterAsync("spl-default", 60, 0);
        Near(240, await WidthAsync(First("spl-default")), "dragged");
        await Gutter("spl-default").DblClickAsync();
        await PollAsync(() => WidthAsync(First("spl-default")), w => Math.Abs(w - 180) < 1.5, "the reset did not return to DefaultPercent");
    }

    [TestMethod]
    public async Task Percent_OneWay_ThePageKeepsTheSplitItHolds()
    {
        await OpenAsync(Url);

        Near(240, await WidthAsync(First("spl-oneway")), "40%");
        await DragGutterAsync("spl-oneway", 100, 0);
        await PollAsync(() => WidthAsync(First("spl-oneway")), w => Math.Abs(w - 240) < 1.5, "a one-way Percent did not keep the split");
    }

    [TestMethod]
    public async Task DragStep_LandsTheSplitOnTheGrid_ForThePointerAndTheKeyboard()
    {
        await OpenAsync(Url);

        var root = "spl-step";
        await DragGutterAsync(root, 73, 0);
        Near(250, await WidthAsync(First(root)), "273px rounded to the 50px grid");

        await DragGutterAsync(root, 30, 0);
        Near(300, await WidthAsync(First(root)), "280px rounded to the 50px grid");

        await Gutter(root).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await PollAsync(() => WidthAsync(First(root)), w => Math.Abs(w - 250) < 1, "a key step lands on the grid too");
    }

    [TestMethod]
    public async Task LazyResize_DragsALineAndMovesThePanelsOnlyWhenTheDragEnds()
    {
        await OpenAsync(Url);

        var root = "spl-lazy";
        var preview = Page.Locator("#spl-lazy > .bit-spl-prv");
        await Expect(preview).ToHaveAttributeAsync("aria-hidden", "true");

        await DragGutterAsync(root, 100, 0, async () =>
        {
            await Expect(preview).ToHaveClassAsync(HasClass("bit-spl-prv-on"));
            Near(295, await WidthAsync(First(root)), "the panels stay put during a lazy drag");
            var line = await BoxAsync(preview);
            var gutter = await BoxAsync(Gutter(root));
            Assert.IsTrue(line.X > gutter.X + 50, $"The preview line ({line.X}) did not follow the pointer past the gutter ({gutter.X}).");
        });

        await Expect(preview).Not.ToHaveClassAsync(HasClass("bit-spl-prv-on"));
        Near(395, await WidthAsync(First(root)), "the panels moved at the end");
    }

    [TestMethod]
    public async Task GutterSizeAndGutterHitSize_DrawAThinGutterThatIsStillEasyToTakeHoldOf()
    {
        await OpenAsync(Url);

        Near(20, await WidthAsync(Gutter("spl-gsize")), "GutterSize 20");
        Near(1, await WidthAsync(Gutter("spl-hit")), "a hairline gutter");
        Assert.AreEqual("16px", await PseudoStyleAsync(Gutter("spl-hit"), "::before", "width"));

        // A press 6px off the hairline still lands in the 16px strip it answers in.
        await Gutter("spl-hit").ScrollIntoViewIfNeededAsync();
        var gutter = await BoxAsync(Gutter("spl-hit"));
        var x = (float)(gutter.X + 6);
        var y = (float)(gutter.Y + gutter.Height / 2);
        var before = await WidthAsync(First("spl-hit"));
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + 50, y, new() { Steps = 4 });
        await Page.Mouse.UpAsync();
        Near(before + 50, await WidthAsync(First("spl-hit")), "the hit area takes hold of the gutter");
    }

    [TestMethod]
    public async Task GutterIconNameAndGutterTemplate_DecorateTheGutter()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#spl-basic > .bit-spl-gtr > .bit-spl-gti")).ToHaveCountAsync(1);

        var icon = Page.Locator("#spl-icon > .bit-spl-gtr > i.bit-spl-gic");
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--GripperDotsVertical"));
        await Expect(icon).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Page.Locator("#spl-icon > .bit-spl-gtr > .bit-spl-gti")).ToHaveCountAsync(0);

        await Expect(Page.Locator("#spl-tpl > .bit-spl-gtr > #spl-tpl-grip")).ToHaveTextAsync("::");
        await Expect(Page.Locator("#spl-tpl > .bit-spl-gtr > .bit-spl-gti")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Collapsible_EnterAndCtrlArrows_FoldTheFirstPanelAndBringItBack()
    {
        await OpenAsync(Url);

        var root = "spl-col";
        await Gutter(root).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(El("col-value")).ToHaveTextAsync("True");
        await Expect(El(root)).ToHaveClassAsync(HasClass("bit-spl-col"));
        await PollAsync(() => WidthAsync(First(root)), w => Math.Abs(w - 8) < 1, "folded to CollapsedSize");
        await Expect(First(root)).Not.ToHaveAttributeAsync("inert", new Regex(".*"));

        await Page.Keyboard.PressAsync("Enter");
        await Expect(El("col-value")).ToHaveTextAsync("False");
        await PollAsync(() => WidthAsync(First(root)), w => Math.Abs(w - 180) < 1, "back where it was");

        await Page.Keyboard.PressAsync("Control+ArrowLeft");
        await Expect(El("col-value")).ToHaveTextAsync("True");
        await Page.Keyboard.PressAsync("Control+ArrowLeft");
        await NextFramesAsync();
        await Expect(El("col-value")).ToHaveTextAsync("True");
        await Page.Keyboard.PressAsync("Control+ArrowRight");
        await Expect(El("col-value")).ToHaveTextAsync("False");

        await Expect(El("col-changes")).ToHaveTextAsync("4");
    }

    [TestMethod]
    public async Task Collapsible_ThePageFoldsIt_AndAPressOnTheFoldedGutterOpensIt()
    {
        await OpenAsync(Url);

        var root = "spl-col";
        await El("btn-col-page").ClickAsync();
        await PollAsync(() => WidthAsync(First(root)), w => Math.Abs(w - 8) < 1, "the page did not fold the panel");
        await Expect(Gutter(root)).ToHaveCSSAsync("cursor", "pointer");

        // The render that folded the panel tells the gutter's script so with a call of its own; one more round trip
        // to the circuit has it landed before the gutter is pressed.
        await El("btn-sync").ClickAsync();
        await Expect(El("syncs")).ToHaveTextAsync("1");

        await Gutter(root).ScrollIntoViewIfNeededAsync();
        var gutter = await BoxAsync(Gutter(root));
        await Page.Mouse.ClickAsync((float)(gutter.X + gutter.Width / 2), (float)(gutter.Y + gutter.Height / 2));
        await Expect(El("col-value")).ToHaveTextAsync("False");
        await PollAsync(() => WidthAsync(First(root)), w => Math.Abs(w - 180) < 1, "the press did not open the panel");
    }

    [TestMethod]
    public async Task Collapsible_ADragIntoTheEdge_SnapsThePanelShut()
    {
        await OpenAsync(Url);

        var root = "spl-col";
        await DragGutterAsync(root, -175, 0);
        await Expect(El("col-value")).ToHaveTextAsync("True");
        await PollAsync(() => WidthAsync(First(root)), w => Math.Abs(w - 8) < 1, "the drag did not snap the panel shut");
    }

    [TestMethod]
    public async Task SnapSize_SetsHowCloseToTheEdgeTheDragHasToEnd()
    {
        await OpenAsync(Url);

        var root = "spl-snap";
        await DragGutterAsync(root, -120, 0);
        Near(60, await WidthAsync(First(root)), "60px is above the 40px SnapSize");
        await Expect(El(root)).Not.ToHaveClassAsync(HasClass("bit-spl-col"));

        await DragGutterAsync(root, -30, 0);
        await Expect(El(root)).ToHaveClassAsync(HasClass("bit-spl-col"));
        await PollAsync(() => WidthAsync(First(root)), w => Math.Abs(w - 8) < 1, "30px is under the SnapSize");
    }

    [TestMethod]
    public async Task CollapsedSizeZero_TakesTheFoldedPanelOutOfTheTabOrder()
    {
        await OpenAsync(Url);

        var root = "spl-col0";
        await Gutter(root).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await PollAsync(() => WidthAsync(First(root)), w => w < 0.5, "folded to nothing");
        await Expect(First(root)).ToHaveAttributeAsync("inert", "");

        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(El("spl-col0-inner")).Not.ToBeFocusedAsync();

        await Gutter(root).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(First(root)).Not.ToHaveAttributeAsync("inert", new Regex(".*"));
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(El("spl-col0-inner")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task CollapseSecondPanel_FoldsTheSecondPanelInstead()
    {
        await OpenAsync(Url);

        var root = "spl-col2";
        Near(180, await WidthAsync(Second(root)), "SecondPanelSize");
        await Gutter(root).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(El(root)).ToHaveClassAsync(HasClass("bit-spl-cse"));
        await PollAsync(() => WidthAsync(Second(root)), w => Math.Abs(w - 8) < 1, "the second panel did not fold");
        Near(582, await WidthAsync(First(root)), "the first panel takes the rest");

        // The fold reaches the gutter's script with a call of its own; one round trip has it landed before the key.
        await El("btn-sync").ClickAsync();
        await Expect(El("syncs")).ToHaveTextAsync("1");
        await Gutter(root).FocusAsync();
        await Page.Keyboard.PressAsync("Control+ArrowLeft");
        await PollAsync(() => WidthAsync(Second(root)), w => Math.Abs(w - 180) < 1, "Ctrl+ArrowLeft, away from the second panel, opens it");
    }

    [TestMethod]
    public async Task ShowCollapseButton_FoldsThePanelFromTheGutter_WithAChevronThatFollowsTheState()
    {
        await OpenAsync(Url);

        var root = "spl-btn";
        var button = Page.Locator("#spl-btn > .bit-spl-gtr > .bit-spl-cbt");
        var icon = button.Locator(".bit-spl-cbi");
        await Expect(button).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(icon).ToHaveClassAsync(HasClass("bit-spl-cbd"));
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--ChevronRight"));
        await Expect(icon).ToHaveClassAsync(HasClass("bit-ico-r180"));

        await button.ClickAsync();
        await PollAsync(() => WidthAsync(First(root)), w => Math.Abs(w - 8) < 1, "the button did not fold the panel");
        await Expect(icon).Not.ToHaveClassAsync(HasClass("bit-ico-r180"));

        await button.ClickAsync();
        await PollAsync(() => WidthAsync(First(root)), w => Math.Abs(w - 180) < 1, "the button did not open the panel");

        var own = Page.Locator("#spl-btn-icons .bit-spl-cbi");
        await Expect(own).ToHaveClassAsync(HasClass("bit-icon--ChevronLeftSmall"));
        await Expect(own).Not.ToHaveClassAsync(HasClass("bit-spl-cbd"));
        await Page.Locator("#spl-btn-icons .bit-spl-cbt").ClickAsync();
        await Expect(own).ToHaveClassAsync(HasClass("bit-icon--ChevronRightSmall"));

        var external = Page.Locator("#spl-btn-ext .bit-spl-cbi");
        await Expect(external).ToHaveClassAsync(HasClass("e2e-collapse"));
        await Page.Locator("#spl-btn-ext .bit-spl-cbt").ClickAsync();
        await Expect(external).ToHaveClassAsync(HasClass("e2e-expand"));
    }

    [TestMethod]
    public async Task OnCollapsing_ReportsWhatAskedForTheFold_AndCanRefuseIt()
    {
        await OpenAsync(Url);

        var root = "spl-cancel";
        await El("btn-lock").ClickAsync();
        await Page.Locator("#spl-cancel .bit-spl-cbt").ClickAsync();
        await Expect(El("collapsing-log")).ToHaveTextAsync("collapse:Gutter:refused");
        await NextFramesAsync();
        Near(180, await WidthAsync(First(root)), "a refused fold");

        await El("btn-lock").ClickAsync();
        await Gutter(root).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(El("collapsing-log")).ToHaveTextAsync("collapse:Gutter:refused,collapse:Gutter");
        await PollAsync(() => WidthAsync(First(root)), w => Math.Abs(w - 8) < 1, "the fold went through");

        await El("btn-cancel-method").ClickAsync();
        await Expect(El("collapsing-log")).ToHaveTextAsync("collapse:Gutter:refused,collapse:Gutter,expand:Method");

        await DragGutterAsync(root, -175, 0);
        await Expect(El("collapsing-log")).ToHaveTextAsync(new Regex(",collapse:Drag$"));
    }

    [TestMethod]
    public async Task NoResetOnDoubleClick_LeavesTheDoubleClickToThePage()
    {
        await OpenAsync(Url);

        var root = "spl-noreset";
        Near(150, await WidthAsync(First(root)), "FirstPanelSize");
        await Gutter(root).DblClickAsync();
        await Expect(El("even-value")).ToHaveTextAsync("50");
        await PollAsync(() => WidthAsync(First(root)), w => Math.Abs(w - 300) < 1, "the page's own double-click did not even the split");
    }

    [TestMethod]
    public async Task Methods_SetGetResetCollapseExpandToggleAndFocus()
    {
        await OpenAsync(Url);

        var root = "spl-m";
        await El("btn-m-get").ClickAsync();
        await Expect(El("m-percent")).ToHaveTextAsync("30");

        await El("btn-m-set").ClickAsync();
        await PollAsync(() => WidthAsync(First(root)), w => Math.Abs(w - 150) < 1, "SetPercent");
        await El("btn-m-get").ClickAsync();
        await Expect(El("m-percent")).ToHaveTextAsync("25");

        await El("btn-m-reset").ClickAsync();
        await PollAsync(() => WidthAsync(First(root)), w => Math.Abs(w - 180) < 1, "ResetSize");

        // Not turned away by a splitter that is not Collapsible.
        await El("btn-m-collapse").ClickAsync();
        await PollAsync(() => WidthAsync(First(root)), w => Math.Abs(w - 8) < 1, "Collapse");
        await El("btn-m-expand").ClickAsync();
        await PollAsync(() => WidthAsync(First(root)), w => Math.Abs(w - 180) < 1, "Expand");
        await El("btn-m-toggle").ClickAsync();
        await PollAsync(() => WidthAsync(First(root)), w => Math.Abs(w - 8) < 1, "ToggleCollapse");
        await El("btn-m-toggle").ClickAsync();
        await PollAsync(() => WidthAsync(First(root)), w => Math.Abs(w - 180) < 1, "ToggleCollapse back");

        await El("btn-m-focus").ClickAsync();
        await Expect(Gutter(root)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task PersistKey_RemembersThePositionAndTheFoldAcrossAReload()
    {
        await OpenAsync(Url);

        await DragGutterAsync("spl-persist", 100, 0);
        Near(250, await WidthAsync(First("spl-persist")), "dragged");
        await PollAsync(() => Page.EvaluateAsync<bool>("() => localStorage.length > 0 && Object.keys(localStorage).some(k => k.includes('e2e-spl-local'))"), stored => stored, "nothing was stored");

        await DragGutterAsync("spl-session", 50, 0);
        await PollAsync(() => Page.EvaluateAsync<bool>("() => Object.keys(sessionStorage).some(k => k.includes('e2e-spl-session'))"), stored => stored, "nothing was stored for the session");
        Assert.IsFalse(await Page.EvaluateAsync<bool>("() => Object.keys(localStorage).some(k => k.includes('e2e-spl-session'))"), "The session splitter wrote to local storage.");

        await Page.ReloadAsync();
        await WaitForStatusAsync("Ready");
        await PollAsync(() => WidthAsync(First("spl-persist")), w => Math.Abs(w - 250) < 1.5, "the position was not restored");
        await PollAsync(() => WidthAsync(First("spl-session")), w => Math.Abs(w - 200) < 1.5, "the session position was not restored");

        await Gutter("spl-persist").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await PollAsync(() => WidthAsync(First("spl-persist")), w => Math.Abs(w - 8) < 1, "folded");

        await Page.ReloadAsync();
        await WaitForStatusAsync("Ready");
        await Expect(El("spl-persist")).ToHaveClassAsync(HasClass("bit-spl-col"));
        await PollAsync(() => WidthAsync(First("spl-persist")), w => Math.Abs(w - 8) < 1, "the fold was not restored");
        await Expect(El("persist-reason")).ToHaveTextAsync("Restore");
    }

    [TestMethod]
    public async Task ReadOnly_LocksTheSplitButKeepsTheLook_AndTheMethodsStillWork()
    {
        await OpenAsync(Url);

        var root = "spl-ro";
        var gutter = Gutter(root);
        await Expect(El(root)).ToHaveClassAsync(HasClass("bit-spl-rdo"));
        await Expect(gutter).Not.ToHaveAttributeAsync("tabindex", new Regex(".*"));
        await Expect(gutter).Not.ToHaveAttributeAsync("aria-valuenow", new Regex(".*"));
        await Expect(gutter).Not.ToHaveAttributeAsync("aria-label", new Regex(".*"));
        await Expect(gutter).ToHaveCSSAsync("cursor", "default");
        Assert.AreEqual(await StyleAsync(Gutter("spl-basic"), "background-color"), await StyleAsync(gutter, "background-color"));

        await DragGutterAsync(root, 100, 0);
        Near(150, await WidthAsync(First(root)), "a read-only gutter does not move");

        await El("btn-ro-set").ClickAsync();
        await PollAsync(() => WidthAsync(First(root)), w => Math.Abs(w - 420) < 1, "SetPercent on a read-only splitter");
    }

    [TestMethod]
    public async Task Disabled_LocksTheSplitAndDimsIt()
    {
        await OpenAsync(Url);

        var root = "spl-dis";
        await Expect(El(root)).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Gutter(root)).Not.ToHaveAttributeAsync("tabindex", new Regex(".*"));
        await Expect(Gutter(root)).ToHaveCSSAsync("cursor", "default");
        // Dimmed as a whole, the panels with it; a read-only one is not.
        Assert.IsTrue(double.Parse(await StyleAsync(El(root), "opacity"), CultureInfo.InvariantCulture) < 1);
        await Expect(El("spl-ro")).ToHaveCSSAsync("opacity", "1");

        await DragGutterAsync(root, 100, 0);
        Near(150, await WidthAsync(First(root)), "a disabled gutter does not move");
    }

    [TestMethod]
    public async Task KeyboardStep_SetsHowFarOneArrowPressMoves()
    {
        await OpenAsync(Url);

        await Gutter("spl-a11y").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await PollAsync(() => WidthAsync(First("spl-a11y")), w => Math.Abs(w - 350) < 1, "a 50px step");
        await Expect(El("a11y-value")).ToHaveTextAsync("58.33");
        await Expect(Gutter("spl-a11y")).ToHaveAttributeAsync("aria-valuenow", "58.33");
    }

    [TestMethod]
    public async Task Nested_AnInnerSplitterMovesOnItsOwn()
    {
        await OpenAsync(Url);

        var outer = await WidthAsync(First("spl-outer"));
        Near(80, await HeightAsync(Second("spl-inner")), "SecondPanelSize of the inner splitter");

        await DragGutterAsync("spl-inner", 0, -40);
        Near(120, await HeightAsync(Second("spl-inner")), "the inner gutter moved");
        Near(outer, await WidthAsync(First("spl-outer")), "the outer split stayed put");

        await DragGutterAsync("spl-outer", 50, 0);
        Near(outer + 50, await WidthAsync(First("spl-outer")), "the outer gutter moved");
        Near(120, await HeightAsync(Second("spl-inner")), "the inner split stayed put");
    }

    [TestMethod]
    public async Task BitParams_FillsTheUnsetParameters_AndTheSplittersOwnValuesWin()
    {
        await OpenAsync(Url);

        Near(16, await WidthAsync(Gutter("spl-par-1")), "a cascaded GutterSize");
        await Expect(Page.Locator("#spl-par-1 .bit-spl-cbt")).ToHaveCountAsync(1);
        await Expect(El("spl-par-1")).ToHaveClassAsync(HasClass("bit-spl-cpb"));

        Near(6, await WidthAsync(Gutter("spl-par-own")), "its own GutterSize");
        await Expect(Page.Locator("#spl-par-own .bit-spl-cbt")).ToHaveCountAsync(0);
        await Expect(El("spl-par-own")).ToHaveClassAsync(HasClass("bit-spl-cpb"));
    }

    [TestMethod]
    public async Task ExternalIcons_DrawTheGivenClassesOnTheGutter()
    {
        await OpenAsync(Url);

        var fa = Page.Locator("#spl-ext i.bit-spl-gic");
        await Expect(fa).ToHaveClassAsync(HasClass("fa-arrows-left-right"));
        await Expect(fa).ToHaveClassAsync(HasClass("fa-solid"));
        var bi = Page.Locator("#spl-ext-bi i.bit-spl-gic");
        await Expect(bi).ToHaveClassAsync(HasClass("bi-grip-vertical"));
        await Expect(bi).Not.ToHaveClassAsync(HasClass("bit-icon--GripperDotsVertical"));
    }

    [TestMethod]
    public async Task StyleClassStylesAndClasses_ReachTheirParts()
    {
        await OpenAsync(Url);

        await Expect(El("spl-style")).ToHaveCSSAsync("border-top-color", "rgb(0, 101, 239)");
        await Expect(El("spl-class")).ToHaveClassAsync(HasClass("e2e-spl-class"));
        await Expect(El("spl-class")).ToHaveCSSAsync("outline-style", "dashed");

        await Expect(El("spl-styles")).ToHaveCSSAsync("outline-color", "rgb(9, 9, 9)");
        await Expect(First("spl-styles")).ToHaveCSSAsync("background-color", "rgb(230, 230, 250)");
        await Expect(Second("spl-styles")).ToHaveCSSAsync("background-color", "rgb(255, 228, 225)");
        await Expect(Gutter("spl-styles")).ToHaveCSSAsync("background-color", "rgb(0, 128, 0)");
        await Expect(Page.Locator("#spl-styles .bit-spl-gti")).ToHaveCSSAsync("background-color", "rgb(255, 0, 0)");
        await Expect(Page.Locator("#spl-styles .bit-spl-cbt")).ToHaveCSSAsync("background-color", "rgb(255, 255, 0)");
        await Expect(Page.Locator("#spl-styles .bit-spl-cbi")).ToHaveCSSAsync("color", "rgb(0, 0, 255)");
        await Expect(Page.Locator("#spl-styles > .bit-spl-prv")).ToHaveCSSAsync("background-color", "rgb(255, 0, 255)");

        await Expect(El("spl-classes")).ToHaveClassAsync(HasClass("e2e-spl-root"));
        await Expect(First("spl-classes")).ToHaveCSSAsync("background-color", "rgb(240, 248, 255)");
        await Expect(Second("spl-classes")).ToHaveCSSAsync("background-color", "rgb(255, 250, 240)");
        await Expect(Gutter("spl-classes")).ToHaveCSSAsync("box-shadow", new Regex("rgb\\(4, 5, 6\\)"));
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_RestyleTheGutter_AndGutterSizeWins()
    {
        await OpenAsync(Url);

        var gutter = Gutter("spl-var");
        Near(18, await WidthAsync(gutter), "the gutter-size variable");
        await Expect(gutter).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(Page.Locator("#spl-var .bit-spl-gti")).ToHaveCSSAsync("background-color", "rgb(200, 0, 0)");
        await Expect(Page.Locator("#spl-var .bit-spl-cbt")).ToHaveCSSAsync("background-color", "rgb(0, 200, 0)");
        await Expect(Page.Locator("#spl-var .bit-spl-cbt")).ToHaveCSSAsync("border-top-left-radius", "2px");

        await gutter.ScrollIntoViewIfNeededAsync();
        var box = await BoxAsync(gutter);
        await Page.Mouse.MoveAsync((float)(box.X + box.Width / 2), (float)(box.Y + 10));
        await Expect(gutter).ToHaveCSSAsync("background-color", "rgb(40, 50, 60)");

        Near(4, await WidthAsync(Gutter("spl-var-wins")), "GutterSize wins over the variable");
    }

    [TestMethod]
    public async Task Rtl_TheFirstPanelIsOnTheRight_AndTheGutterMovesTheOtherWay()
    {
        await OpenAsync(Url);

        var root = "spl-rtl";
        await Expect(El(root)).ToHaveAttributeAsync("dir", "rtl");
        var first = await BoxAsync(First(root));
        var second = await BoxAsync(Second(root));
        Assert.IsTrue(first.X > second.X, "The first panel is not on the right.");
        Near(200, first.Width, "FirstPanelSize");

        await DragGutterAsync(root, -100, 0);
        Near(300, await WidthAsync(First(root)), "dragging left grows the first panel");

        await Gutter(root).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await PollAsync(() => WidthAsync(First(root)), w => Math.Abs(w - 310) < 1, "ArrowLeft grows the first panel");

        // The chevron points at the first panel, which is on the right.
        await Expect(Page.Locator("#spl-rtl .bit-spl-cbi")).Not.ToHaveClassAsync(HasClass("bit-ico-r180"));
        await Page.Keyboard.PressAsync("Control+ArrowRight");
        await PollAsync(() => WidthAsync(First(root)), w => Math.Abs(w - 8) < 1, "Ctrl+ArrowRight folds the panel on the right");
    }

    [TestMethod]
    public async Task Rtl_InheritedFromAnAncestor_PutsTheFirstPanelOnTheRight()
    {
        await OpenAsync(Url);

        var root = "spl-rtl-inh";
        Assert.IsTrue((await BoxAsync(First(root))).X > (await BoxAsync(Second(root))).X, "The first panel is not on the right.");
        await DragGutterAsync(root, -50, 0);
        Near(250, await WidthAsync(First(root)), "dragging left grows the first panel");
    }

    [TestMethod]
    public async Task Visibility_HiddenKeepsTheBox_CollapsedRemovesIt()
    {
        await OpenAsync(Url);

        await Expect(El("spl-vis-hidden")).ToHaveCSSAsync("visibility", "hidden");
        Assert.IsTrue((await BoxAsync(El("spl-vis-hidden"))).Height > 0);
        await Expect(El("spl-vis-collapsed")).ToHaveCSSAsync("display", "none");
    }

    [TestMethod]
    public async Task HtmlAttributes_AreSplattedOnTheRoot()
    {
        await OpenAsync(Url);

        await Expect(El("spl-attrs")).ToHaveAttributeAsync("data-e2e", "custom");
        await Expect(El("spl-attrs")).ToHaveAttributeAsync("data-extra", "from-dictionary");
    }

    [TestMethod]
    public async Task Rerender_ParametersChangedByThePage_ReLayTheSplitter()
    {
        await OpenAsync(Url);

        var root = "spl-rr";
        Near(150, await WidthAsync(First(root)), "before");
        await El("btn-rr").ClickAsync();

        await Expect(El(root)).ToHaveClassAsync(HasClass("bit-spl-vrt"));
        await PollAsync(() => HeightAsync(First(root)), h => Math.Abs(h - 60) < 1, "the new FirstPanelSize");
        Near(24, await HeightAsync(Gutter(root)), "the new GutterSize");
        await Expect(Gutter(root)).ToHaveAttributeAsync("aria-orientation", "horizontal");

        await Gutter(root).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await PollAsync(() => HeightAsync(First(root)), h => Math.Abs(h - 70) < 1, "the keyboard follows the new orientation");
    }

    [TestMethod]
    public async Task Unmount_InTheMiddleOfADrag_RemovesTheSplitterWithoutAnError()
    {
        await OpenAsync(Url);

        await Gutter("spl-um").ScrollIntoViewIfNeededAsync();
        var gutter = await BoxAsync(Gutter("spl-um"));
        await Page.Mouse.MoveAsync((float)(gutter.X + gutter.Width / 2), (float)(gutter.Y + 10));
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync((float)(gutter.X + 60), (float)(gutter.Y + 10), new() { Steps = 3 });
        await El("btn-unmount").DispatchEventAsync("click");
        await Expect(El("spl-um")).ToHaveCountAsync(0);
        await Page.Mouse.UpAsync();
        await Expect(Page.Locator("body")).Not.ToHaveClassAsync(HasClass("bit-spl-drg-bdy"));

        await El("btn-unmount").ClickAsync();
        await Expect(El("spl-um")).ToHaveCountAsync(1);
        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    // Presses the middle of the gutter, moves the pointer by (dx, dy) in a few steps - running what the caller wants to
    // check while the pointer is still down - and lets go.
    private async Task DragGutterAsync(string id, float dx, float dy, Func<Task>? whileDown = null)
    {
        var gutter = Gutter(id);
        await gutter.ScrollIntoViewIfNeededAsync();
        var box = await BoxAsync(gutter);

        // Near one end of the gutter rather than at its middle, where a collapse button sits.
        var vertical = box.Width > box.Height;
        var x = (float)(vertical ? box.X + 12 : box.X + box.Width / 2);
        var y = (float)(vertical ? box.Y + box.Height / 2 : box.Y + 12);

        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + dx, y + dy, new() { Steps = 6 });
        await NextFramesAsync();

        if (whileDown is not null) await whileDown();

        await Page.Mouse.UpAsync();
        await NextFramesAsync();
    }

    private static async Task<double> WidthAsync(ILocator locator) => (await BoxAsync(locator)).Width;

    private static async Task<double> HeightAsync(ILocator locator) => (await BoxAsync(locator)).Height;

    private static async Task<LayoutBox> BoxAsync(ILocator locator)
    {
        var box = await locator.EvaluateAsync<double[]>("el => { const r = el.getBoundingClientRect(); return [r.x, r.y, r.width, r.height]; }");
        return new(box[0], box[1], box[2], box[3]);
    }

    private static Task<string> StyleAsync(ILocator locator, string property)
    {
        return locator.EvaluateAsync<string>("(el, p) => getComputedStyle(el).getPropertyValue(p)", property);
    }

    private static Task<string> PseudoStyleAsync(ILocator locator, string pseudo, string property)
    {
        return locator.EvaluateAsync<string>("(el, [s, p]) => getComputedStyle(el, s).getPropertyValue(p)", new[] { pseudo, property });
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
