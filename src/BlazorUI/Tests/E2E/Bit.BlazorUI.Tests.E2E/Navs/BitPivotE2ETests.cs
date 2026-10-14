using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Navs;

/// <summary>
/// End-to-end tests of BitPivot (/e2e/pivot) in a real browser: the prerendered tablist, selecting tabs by click and
/// keyboard (arrows mirrored in RTL and turned in a vertical header, Home/End, Enter/Space, Loop, SelectOnFocus,
/// Navigable), the WAI-ARIA wiring, IconName/ItemCount/Stacked, every HeaderType, Placement, Alignment, FullWidth and Gap,
/// the Menu, Slide, Scroll, Wrap and None overflow behaviors (the menu and the slide buttons following the viewport as it
/// is narrowed and widened), the menu's keyboard and Escape inside a dialog, HeaderStart/HeaderEnd and the Header/Body
/// templates, the two-way and one-way SelectedKey, DefaultSelectedKey, SelectItemByKey/SelectedItem/Items, OnChange,
/// OnItemClick, OnChanging, KeepMounted/MountAll/HeaderOnly, dismissible, addable and reorderable tabs (drag and
/// Ctrl+arrows), the disabled contract, every Color and Size, the external icons, Style/Class/Styles/Classes, item
/// Visibility, the BitParams cascade, the public css variables, RTL (explicit and inherited), reduced motion and an
/// unmount.
/// </summary>
[TestClass]
public class BitPivotE2ETests : E2ETestBase
{
    private const string Url = "/e2e/pivot";
    private const string Transparent = "rgba(0, 0, 0, 0)";
    private const string Identity = "matrix(1, 0, 0, 1, 0, 0)";

    private static readonly (string Color, string Suffix, string Main)[] Colors =
    [
        ("Primary", "pri", "--bit-clr-pri"), ("Secondary", "sec", "--bit-clr-sec"), ("Tertiary", "ter", "--bit-clr-ter"),
        ("Info", "inf", "--bit-clr-inf"), ("Success", "suc", "--bit-clr-suc"), ("Warning", "wrn", "--bit-clr-wrn"),
        ("SevereWarning", "swr", "--bit-clr-swr"), ("Error", "err", "--bit-clr-err"),
        ("PrimaryBackground", "pbg", "--bit-clr-bg-pri"), ("SecondaryBackground", "sbg", "--bit-clr-bg-sec"),
        ("TertiaryBackground", "tbg", "--bit-clr-bg-ter"), ("PrimaryForeground", "pfg", "--bit-clr-fg-pri"),
        ("SecondaryForeground", "sfg", "--bit-clr-fg-sec"), ("TertiaryForeground", "tfg", "--bit-clr-fg-ter"),
        ("PrimaryBorder", "pbr", "--bit-clr-brd-pri"), ("SecondaryBorder", "sbr", "--bit-clr-brd-sec"),
        ("TertiaryBorder", "tbr", "--bit-clr-brd-ter")
    ];

    [TestMethod]
    public async Task Prerender_TheServerHtml_ContainsTheTablistAndTheSelectedPanel()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Url}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "id=\"pv-basic\"");
        StringAssert.Contains(html, "role=\"tablist\"");
        StringAssert.Contains(html, "aria-selected=\"true\"");
        StringAssert.Contains(html, "Pivot #1: File");
        StringAssert.Contains(html, ">Loading<");

        await OpenAsync(Url);
        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    [TestMethod]
    public async Task Basic_TheFirstTabStartsSelected_AndAClickSelectsAnother()
    {
        await OpenAsync(Url);

        var tablist = Root("pv-basic").Locator("> .bit-pvt-hwr > [role='tablist']");
        await Expect(tablist).ToHaveAttributeAsync("aria-orientation", "horizontal");
        await Expect(tablist).ToHaveAttributeAsync("tabindex", "-1");

        var file = Tab("pv-basic", "File");
        var shared = Tab("pv-basic", "Shared with me");
        await Expect(file).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(file).ToHaveClassAsync(HasClass("bit-pvti-sel"));
        await Expect(file).ToHaveAttributeAsync("tabindex", "0");
        await Expect(shared).ToHaveAttributeAsync("aria-selected", "false");
        await Expect(shared).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(file).ToHaveAttributeAsync("aria-posinset", "1");
        await Expect(file).ToHaveAttributeAsync("aria-setsize", "3");

        var panel = Panel("pv-basic");
        await Expect(panel).ToHaveAttributeAsync("role", "tabpanel");
        await Expect(panel).ToHaveAttributeAsync("tabindex", "0");
        await Expect(panel.Locator("#basic-panel-1")).ToBeVisibleAsync();
        await Expect(panel).ToHaveAttributeAsync("aria-labelledby", (await file.GetAttributeAsync("id"))!);
        await Expect(file).ToHaveAttributeAsync("aria-controls", (await panel.GetAttributeAsync("id"))!);

        var width = (await BoxAsync(shared)).Width;
        await shared.ClickAsync();

        await Expect(shared).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(file).ToHaveAttributeAsync("aria-selected", "false");
        await Expect(Panel("pv-basic").Locator("#basic-panel-2")).ToBeVisibleAsync();
        await Expect(Page.Locator("#basic-panel-1")).ToHaveCountAsync(0);
        await Expect(shared).ToHaveCSSAsync("font-weight", "600");
        await Expect(file).ToHaveCSSAsync("font-weight", "400");

        // The indicator scales in under the new tab and away from the old one.
        await ExpectPseudoAsync(shared, "transform", Identity);
        await ExpectPseudoAsync(file, "transform", "matrix(0, 0, 0, 1, 0, 0)");
        Assert.AreEqual(width, (await BoxAsync(shared)).Width, 0.5, "A tab keeps its width when it turns bold.");
    }

    [TestMethod]
    public async Task IconCountAndStacked_RenderAroundTheHeaderText()
    {
        await OpenAsync(Url);

        await Expect(Tab("pv-icons", "Files").Locator(".bit-pvti-icn i")).ToHaveClassAsync(HasClass("bit-icon--FabricFolder"));
        await Expect(Tab("pv-icons", "Files").Locator(".bit-pvti-icn i")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Tab("pv-icons", "Shared").Locator(".bit-pvti-cnr")).ToHaveTextAsync("(32)");
        await Expect(Tab("pv-icons", "Recent").Locator(".bit-pvti-cnr")).ToHaveTextAsync("(12)");
        var icon = await BoxAsync(Tab("pv-icons", "Recent").Locator(".bit-pvti-icn"));
        var text = await BoxAsync(Tab("pv-icons", "Recent").Locator(".bit-pvti-txt"));
        Assert.IsTrue(icon.X < text.X, "The icon comes before the text.");

        await Expect(Root("pv-stacked")).ToHaveClassAsync(HasClass("bit-pvt-stk"));
        var stackedIcon = await BoxAsync(Tab("pv-stacked", "Home").Locator(".bit-pvti-icn"));
        var stackedText = await BoxAsync(Tab("pv-stacked", "Home").Locator(".bit-pvti-txt"));
        Assert.IsTrue(stackedIcon.Y + stackedIcon.Height <= stackedText.Y + 1, "Stacked puts the icon above the text.");
    }

    [TestMethod]
    [DataRow("Link", "bit-pvt-lnk")]
    [DataRow("Tab", "bit-pvt-tab")]
    [DataRow("Outline", "bit-pvt-oln")]
    public async Task HeaderType_EveryType_MarksTheSelectedTabItsOwnWay(string type, string cls)
    {
        await OpenAsync(Url);
        await Page.Mouse.MoveAsync(0, 0);

        var id = $"pv-type-{type}";
        await Expect(Root(id)).ToHaveClassAsync(HasClass(cls));
        var selected = Tab(id, "File");
        var primary = await ResolveColorAsync("var(--bit-clr-pri)");

        switch (type)
        {
            case "Link":
                await ExpectPseudoAsync(selected, "background-color", primary);
                await Expect(selected).ToHaveCSSAsync("background-color", Transparent);
                await Expect(Root("pv-type-default")).ToHaveClassAsync(HasClass("bit-pvt-lnk"));
                break;
            case "Tab":
                await Expect(selected).ToHaveCSSAsync("background-color", primary);
                await Expect(selected).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-pri-text)"));
                await Expect(Tab(id, "Shared")).ToHaveCSSAsync("background-color", Transparent);
                break;
            default:
                await Expect(selected).ToHaveCSSAsync("border-top-color", primary);
                Assert.AreEqual("none", await PseudoAsync(selected, "display"), "The line indicator gives way to the edge.");
                await Expect(Tab(id, "Shared")).ToHaveCSSAsync("border-top-color", Transparent);
                break;
        }
    }

    [TestMethod]
    [DataRow("Start", "flex-start")]
    [DataRow("Center", "center")]
    [DataRow("End", "flex-end")]
    [DataRow("SpaceBetween", "space-between")]
    public async Task Alignment_PlacesTheTabsAlongTheHeader(string alignment, string justify)
    {
        await OpenAsync(Url);

        var id = $"pv-align-{alignment}";
        var tablist = Root(id).Locator("> .bit-pvt-hwr > [role='tablist']");
        await Expect(tablist).ToHaveCSSAsync("justify-content", justify);

        var header = await BoxAsync(tablist);
        var first = await BoxAsync(Tab(id, "File"));
        var last = await BoxAsync(Tab(id, "Recent"));
        var left = first.X - header.X;
        var right = header.X + header.Width - (last.X + last.Width);

        switch (alignment)
        {
            case "Start": Assert.IsTrue(left < 2 && right > 100); break;
            case "End": Assert.IsTrue(right < 2 && left > 100); break;
            case "Center": Assert.AreEqual(left, right, 3); break;
            default: Assert.IsTrue(left < 2 && right < 2); break;
        }
    }

    [TestMethod]
    public async Task FullWidthAndGap_ShareTheHeaderAndSpaceTheTabs()
    {
        await OpenAsync(Url);

        await Expect(Root("pv-full-width")).ToHaveClassAsync(HasClass("bit-pvt-fwd"));
        var header = await BoxAsync(Root("pv-full-width").Locator("> .bit-pvt-hwr > [role='tablist']"));
        var a = await BoxAsync(Tab("pv-full-width", "Overview"));
        var b = await BoxAsync(Tab("pv-full-width", "Activity"));
        var c = await BoxAsync(Tab("pv-full-width", "Settings"));
        Assert.AreEqual(a.Width, b.Width, 1);
        Assert.AreEqual(b.Width, c.Width, 1);
        Assert.AreEqual(header.Width, a.Width + b.Width + c.Width + 8, 2, "The three tabs and two 4px gaps fill the header.");
        Assert.AreEqual(4, b.X - (a.X + a.Width), 1);

        var file = await BoxAsync(Tab("pv-gap", "File"));
        var shared = await BoxAsync(Tab("pv-gap", "Shared"));
        Assert.AreEqual(32, shared.X - (file.X + file.Width), 1, "Gap=2rem.");
    }

    [TestMethod]
    [DataRow("Top", "bit-pvt-top")]
    [DataRow("Bottom", "bit-pvt-btm")]
    [DataRow("Start", "bit-pvt-sta")]
    [DataRow("End", "bit-pvt-end")]
    public async Task Placement_PutsTheHeaderOnThatSide(string placement, string cls)
    {
        await OpenAsync(Url);

        var id = $"pv-place-{placement}";
        await Expect(Root(id)).ToHaveClassAsync(HasClass(cls));

        var tablist = Root(id).Locator("> .bit-pvt-hwr > [role='tablist']");
        var header = await BoxAsync(tablist);
        var panel = await BoxAsync(Panel(id));
        var vertical = placement is "Start" or "End";
        await Expect(tablist).ToHaveAttributeAsync("aria-orientation", vertical ? "vertical" : "horizontal");

        switch (placement)
        {
            case "Top": Assert.IsTrue(header.Y + header.Height <= panel.Y + 1); break;
            case "Bottom": Assert.IsTrue(panel.Y + panel.Height <= header.Y + 1); break;
            case "Start": Assert.IsTrue(header.X + header.Width <= panel.X + 1); break;
            default: Assert.IsTrue(panel.X + panel.Width <= header.X + 1); break;
        }

        if (vertical)
        {
            var first = await BoxAsync(Tab(id, "File"));
            var second = await BoxAsync(Tab(id, "Shared"));
            Assert.IsTrue(second.Y > first.Y, "A vertical header stacks its tabs.");
        }
    }

    [TestMethod]
    public async Task Placement_ANestedPivotKeepsItsOwnPositionAndSelection()
    {
        await OpenAsync(Url);

        await Expect(Root("pv-nested-outer").Locator("> .bit-pvt-hwr > [role='tablist']")).ToHaveAttributeAsync("aria-orientation", "vertical");
        var inner = Root("pv-nested-inner");
        await Expect(inner).ToHaveClassAsync(HasClass("bit-pvt-top"));
        await Expect(inner).ToHaveClassAsync(HasClass("bit-pvt-lnk"));
        await Expect(inner.Locator("> .bit-pvt-hwr > [role='tablist']")).ToHaveAttributeAsync("aria-orientation", "horizontal");

        await Tab("pv-nested-inner", "Security").ClickAsync();
        await Expect(Tab("pv-nested-inner", "Security")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Tab("pv-nested-outer", "Account")).ToHaveAttributeAsync("aria-selected", "true");
    }

    [TestMethod]
    public async Task OverflowMenu_FoldsWhatDoesNotFit_AndSelectsFromTheMenu()
    {
        await OpenAsync(Url);

        var more = More("pv-menu");
        await Expect(more).ToBeVisibleAsync();
        await Expect(more).ToHaveAttributeAsync("aria-haspopup", "menu");
        await Expect(more).ToHaveAttributeAsync("aria-label", "More tabs");
        await Expect(more).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(more).ToHaveAttributeAsync("aria-controls", (await Menu("pv-menu").GetAttributeAsync("id"))!);
        await Expect(Tab("pv-menu", "Hotel")).ToBeHiddenAsync();
        await Expect(Tab("pv-menu", "Hotel")).ToHaveClassAsync(HasClass("bit-pvt-ovh"));
        await Expect(Tab("pv-menu", "Alpha")).ToBeVisibleAsync();
        // A folded tab still counts in the set the visible ones announce.
        await Expect(Tab("pv-menu", "Alpha")).ToHaveAttributeAsync("aria-setsize", "8");
        await Expect(Tab("pv-menu", "Hotel")).ToHaveAttributeAsync("aria-posinset", "8");

        await more.ClickAsync();
        var menu = Menu("pv-menu");
        await Expect(menu).ToBeVisibleAsync();
        await Expect(menu).ToBeFocusedAsync();
        await Expect(more).ToHaveAttributeAsync("aria-expanded", "true");

        var hotel = menu.Locator("[role='menuitemradio']").Filter(new() { HasText = "Hotel" });
        await Expect(hotel).ToHaveAttributeAsync("aria-checked", "false");
        await Expect(menu.Locator("[role='menuitemradio']").Filter(new() { HasText = "Alpha" })).ToHaveCountAsync(0);
        await hotel.ClickAsync();

        await Expect(menu).ToBeHiddenAsync();
        await Expect(Page.Locator("#menu-key")).ToHaveTextAsync("Hotel");
        await Expect(Panel("pv-menu")).ToContainTextAsync("Content of Hotel");
        // The selected tab is folded away, so the button holding it says it holds the selection.
        await Expect(more).ToHaveClassAsync(HasClass("bit-pvt-mor-sel"));
        await Expect(more).ToBeFocusedAsync();

        await more.ClickAsync();
        await Expect(menu).ToBeVisibleAsync();
        await Expect(menu).ToBeFocusedAsync();
        await Expect(hotel).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(hotel).ToHaveClassAsync(HasClass("bit-pvt-mni-sel"));

        // A click outside closes it, handing the focus back to the button.
        await Page.Mouse.ClickAsync(5, 5);
        await Expect(menu).ToBeHiddenAsync();
        await Expect(more).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task OverflowMenu_Keyboard_ArrowsHomeEndTypeaheadEnterEscapeAndTab()
    {
        await OpenAsync(Url);

        var more = More("pv-menu");
        var menu = Menu("pv-menu");
        await Expect(more).ToBeVisibleAsync();
        var items = menu.Locator("[role='menuitemradio']");
        // The script shows the button as it folds the header, and the menu's items follow a round trip later.
        await Expect(items.Nth(1)).ToBeAttachedAsync();
        await Expect(items.Filter(new() { HasText = "Golf" })).ToBeAttachedAsync();
        var count = await items.CountAsync();

        await more.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(menu).ToBeFocusedAsync();
        await ExpectActiveAsync(menu, items.Nth(0));

        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectActiveAsync(menu, items.Nth(1));
        await Page.Keyboard.PressAsync("End");
        await ExpectActiveAsync(menu, items.Nth(count - 1));
        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectActiveAsync(menu, items.Nth(0));
        await Page.Keyboard.PressAsync("ArrowUp");
        await ExpectActiveAsync(menu, items.Nth(count - 1));
        await Page.Keyboard.PressAsync("Home");
        await ExpectActiveAsync(menu, items.Nth(0));
        await Page.Keyboard.PressAsync("g");
        await ExpectActiveAsync(menu, items.Filter(new() { HasText = "Golf" }));

        await Page.Keyboard.PressAsync("Escape");
        await Expect(menu).ToBeHiddenAsync();
        await Expect(more).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(menu).ToBeFocusedAsync();
        await ExpectActiveAsync(menu, items.Nth(count - 1));
        await Page.Keyboard.PressAsync("Tab");
        await Expect(menu).ToBeHiddenAsync();
        await Expect(more).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(menu).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("g");
        await ExpectActiveAsync(menu, items.Filter(new() { HasText = "Golf" }));
        await Page.Keyboard.PressAsync("Enter");
        await Expect(menu).ToBeHiddenAsync();
        await Expect(Page.Locator("#menu-key")).ToHaveTextAsync("Golf");
    }

    [TestMethod]
    public async Task OverflowMenu_NarrowingTheViewportFoldsTabs_AndWideningItBringsThemBack()
    {
        await OpenAsync(Url);

        var more = More("pv-menu-fluid");
        await Expect(Tab("pv-menu-fluid", "Hotel")).ToBeVisibleAsync();
        await Expect(more).ToBeHiddenAsync();

        await Page.SetViewportSizeAsync(380, 720);
        await Expect(more).ToBeVisibleAsync();
        await Expect(Tab("pv-menu-fluid", "Hotel")).ToBeHiddenAsync();
        var header = await BoxAsync(Root("pv-menu-fluid").Locator("> .bit-pvt-hwr"));
        Assert.IsTrue(header.Width <= 380, "The header fits the narrow viewport.");

        await Page.SetViewportSizeAsync(1280, 720);
        await Expect(more).ToBeHiddenAsync();
        await Expect(Tab("pv-menu-fluid", "Hotel")).ToBeVisibleAsync();
        await Expect(Root("pv-menu-fluid").Locator(".bit-pvt-ovh")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task OverflowMenu_AVerticalHeaderFoldsToo_AndIsWalkedWithUpAndDown()
    {
        await OpenAsync(Url);

        await Expect(More("pv-menu-vertical")).ToBeVisibleAsync();
        await Expect(Tab("pv-menu-vertical", "Hotel")).ToBeHiddenAsync();

        await Tab("pv-menu-vertical", "Alpha").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Tab("pv-menu-vertical", "Bravo")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Tab("pv-menu-vertical", "Alpha")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task OverflowSlide_TheButtonsScrollTheHeader_AndDisableAtTheEnds()
    {
        await OpenAsync(Url);

        var prev = Root("pv-slide").Locator(".bit-pvt-spv");
        var next = Root("pv-slide").Locator(".bit-pvt-snx");
        await Expect(prev).ToHaveAttributeAsync("aria-label", "Earlier tabs");
        await Expect(next).ToHaveAttributeAsync("aria-label", "Later tabs");
        await Expect(prev.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--ChevronLeft"));
        await Expect(next.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--ChevronRight"));
        await Expect(prev).ToBeDisabledAsync();
        await Expect(prev).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(next).ToBeEnabledAsync();

        await next.ClickAsync();
        await Page.WaitForFunctionAsync("() => document.querySelector('#pv-slide [role=tablist]').scrollLeft > 0");
        await Expect(prev).ToBeEnabledAsync();

        // Pressed until it disables itself at the far end, each press waited out until the header has moved on (it
        // scrolls smoothly) or the button has disabled itself.
        for (var i = 0; i < 20 && await next.IsEnabledAsync(); i++)
        {
            var before = await Page.EvaluateAsync<double>("() => document.querySelector('#pv-slide [role=tablist]').scrollLeft");

            try
            {
                await next.ClickAsync(new() { Timeout = 5000 });
            }
            catch (TimeoutException)
            {
                continue;
            }

            await Page.WaitForFunctionAsync(@"before => {
                const header = document.querySelector('#pv-slide [role=tablist]');
                return document.querySelector('#pv-slide .bit-pvt-snx').disabled || header.scrollLeft > before + 1;
            }", before);
        }

        await Expect(next).ToBeDisabledAsync();
        await Page.WaitForFunctionAsync(@"() => {
            const header = document.querySelector('#pv-slide [role=tablist]');
            return Math.abs(header.scrollWidth - header.clientWidth - header.scrollLeft) <= 2;
        }");

        // The tab the keyboard reaches is brought into view.
        await Tab("pv-slide", "Hotel").FocusAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(Tab("pv-slide", "Alpha")).ToBeFocusedAsync();
        await Expect(Tab("pv-slide", "Alpha")).ToBeInViewportAsync();
        await Expect(prev).ToBeDisabledAsync();
    }

    [TestMethod]
    public async Task OverflowSlide_AutoHideSlideButtons_FollowTheViewportWidth()
    {
        await OpenAsync(Url);

        var buttons = Root("pv-slide-auto").Locator(".bit-pvt-sbt");
        await Expect(buttons).ToHaveCountAsync(0);

        await Page.SetViewportSizeAsync(380, 720);
        await Expect(buttons).ToHaveCountAsync(2);

        await Page.SetViewportSizeAsync(1280, 720);
        await Expect(buttons).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task OverflowSlide_AVerticalHeaderSlidesUpAndDown()
    {
        await OpenAsync(Url);

        var next = Root("pv-slide-vertical").Locator(".bit-pvt-snx");
        await Expect(next.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--ChevronDown"));
        await Expect(Root("pv-slide-vertical").Locator(".bit-pvt-spv i")).ToHaveClassAsync(HasClass("bit-icon--ChevronUp"));
        await Expect(next).ToBeEnabledAsync();
        await next.ClickAsync();
        await Page.WaitForFunctionAsync("() => document.querySelector('#pv-slide-vertical [role=tablist]').scrollTop > 0");
    }

    [TestMethod]
    public async Task OverflowScrollWrapAndNone_HandleTabsThatDoNotFit()
    {
        await OpenAsync(Url);

        var scroll = Root("pv-scroll").Locator("> .bit-pvt-hwr > [role='tablist']");
        await Expect(Root("pv-scroll")).ToHaveClassAsync(HasClass("bit-pvt-scr"));
        await Expect(scroll).ToHaveCSSAsync("overflow-x", "auto");
        Assert.IsTrue(await scroll.EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth"));
        await Tab("pv-scroll", "Alpha").FocusAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(Tab("pv-scroll", "Hotel")).ToBeFocusedAsync();
        await Page.WaitForFunctionAsync("() => document.querySelector('#pv-scroll [role=tablist]').scrollLeft > 0");

        var wrap = Root("pv-wrap").Locator("> .bit-pvt-hwr > [role='tablist']");
        await Expect(Root("pv-wrap")).ToHaveClassAsync(HasClass("bit-pvt-wrp"));
        await Expect(wrap).ToHaveCSSAsync("flex-wrap", "wrap");
        var tab = await BoxAsync(Tab("pv-wrap", "Alpha"));
        Assert.IsTrue((await BoxAsync(wrap)).Height > tab.Height * 1.8, "The tabs run onto more lines.");
        var box = await BoxAsync(Page.Locator("#box-wrap"));
        var hotel = await BoxAsync(Tab("pv-wrap", "Hotel"));
        Assert.IsTrue(hotel.X + hotel.Width <= box.X + box.Width + 1);

        await Expect(Root("pv-none")).ToHaveClassAsync(HasClass("bit-pvt-non"));
        var noneBox = await BoxAsync(Page.Locator("#box-none"));
        var last = await BoxAsync(Tab("pv-none", "Hotel"));
        Assert.IsTrue(last.X + last.Width > noneBox.X + noneBox.Width, "None lets the tabs overflow.");
        await Expect(More("pv-none")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task HeaderStartAndHeaderEnd_SitOutsideTheTablist_AndKeepTheirOwnTabStop()
    {
        await OpenAsync(Url);

        var root = Root("pv-header-content");
        await Expect(root.Locator("> .bit-pvt-hwr > .bit-pvt-hst #header-start-content")).ToHaveTextAsync("Start");
        await Expect(root.Locator("> .bit-pvt-hwr > .bit-pvt-hnd #header-end-button")).ToBeVisibleAsync();
        await Expect(root.Locator("[role='tablist'] #header-end-button")).ToHaveCountAsync(0);

        var start = await BoxAsync(root.Locator(".bit-pvt-hst"));
        var end = await BoxAsync(root.Locator(".bit-pvt-hnd"));
        var file = await BoxAsync(Tab("pv-header-content", "File"));
        Assert.IsTrue(start.X < file.X && file.X < end.X);

        await Tab("pv-header-content", "File").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tab("pv-header-content", "Shared")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#header-end-button")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Templates_HeaderAndBody_AndAHeaderInputKeepsItsKeys()
    {
        await OpenAsync(Url);

        var first = Root("pv-templates").Locator("> .bit-pvt-hwr > [role='tablist'] > [role='tab']").Nth(0);
        var second = Root("pv-templates").Locator("> .bit-pvt-hwr > [role='tablist'] > [role='tab']").Nth(1);
        await Expect(first.Locator(".bit-pvti-hdr .custom-header")).ToHaveTextAsync("Header #1");
        await Expect(first.Locator(".custom-header")).ToHaveCSSAsync("color", "rgb(255, 0, 0)");
        await Expect(Panel("pv-templates").Locator("#template-body-1")).ToBeVisibleAsync();

        var icon = await BoxAsync(second.Locator(".bit-pvti-icn"));
        var header = await BoxAsync(second.Locator(".custom-header"));
        var count = second.Locator(".bit-pvti-cnr");
        await Expect(count).ToHaveTextAsync("(99)");
        Assert.IsTrue(icon.X < header.X && header.X < (await BoxAsync(count)).X, "The icon before the header, the count after it.");

        await second.ClickAsync(new() { Position = new() { X = 5, Y = 5 } });
        await Expect(Panel("pv-templates").Locator("#template-body-2")).ToBeVisibleAsync();

        // The keys typed into the header reach the input rather than the tablist.
        var input = Page.Locator("#header-input");
        await input.ClickAsync();
        await input.PressSequentiallyAsync("a b");
        await Expect(input).ToHaveValueAsync("a b");
        await input.PressAsync("Home");
        await Expect(input).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task SelectedKey_TwoWay_FollowsThePageAndTheUser_AndRefusesADisabledTab()
    {
        await OpenAsync(Url);

        await Expect(Tab("pv-binding", "Samples")).ToHaveAttributeAsync("aria-selected", "true");

        await Tab("pv-binding", "Files").ClickAsync();
        await Expect(Page.Locator("#selected-key")).ToHaveTextAsync("2");

        await Page.Locator("#btn-key-3").ClickAsync();
        await Expect(Tab("pv-binding", "Recent")).ToHaveAttributeAsync("aria-selected", "true");

        await Page.Locator("#btn-key-disabled").ClickAsync();
        await Expect(Page.Locator("#selected-key")).ToHaveTextAsync("3");
        await Expect(Tab("pv-binding", "Off")).ToHaveAttributeAsync("aria-selected", "false");

        await Page.Locator("#btn-select-by-key").ClickAsync();
        await Expect(Tab("pv-binding", "Last")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Page.Locator("#selected-key")).ToHaveTextAsync("4");

        await Page.Locator("#btn-read-ref").ClickAsync();
        await Expect(Page.Locator("#ref-state")).ToHaveTextAsync("Last|5|1,2,3,4,5");
    }

    [TestMethod]
    public async Task DefaultSelectedKeyOneWayKeyAndIsSelected_PickTheStartingTab()
    {
        await OpenAsync(Url);

        await Expect(Tab("pv-default-key", "B")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Panel("pv-default-key").Locator("#default-key-panel")).ToBeVisibleAsync();
        await Tab("pv-default-key", "C").ClickAsync();
        await Expect(Tab("pv-default-key", "C")).ToHaveAttributeAsync("aria-selected", "true");

        // A key bound one way is the page's alone.
        await Tab("pv-one-way", "Y").ClickAsync();
        await Expect(Tab("pv-one-way", "X")).ToHaveAttributeAsync("aria-selected", "true");
        await Page.Locator("#btn-one-way-y").ClickAsync();
        await Expect(Tab("pv-one-way", "Y")).ToHaveAttributeAsync("aria-selected", "true");

        await Expect(Tab("pv-is-selected", "Q")).ToHaveAttributeAsync("aria-selected", "true");
    }

    [TestMethod]
    public async Task Events_OnChangeOnItemClickAndTheItemsOwnOnClick()
    {
        await OpenAsync(Url);

        await Expect(Tab("pv-events", "Foo")).ToHaveAttributeAsync("title", "The first tab");

        await Tab("pv-events", "Bar").ClickAsync();
        await Expect(Page.Locator("#changed")).ToHaveTextAsync("Bar");
        await Expect(Page.Locator("#change-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#clicked")).ToHaveTextAsync("Bar");
        await Expect(Page.Locator("#click-count")).ToHaveTextAsync("1");

        // A click on the selected tab is a click, not a change.
        await Tab("pv-events", "Bar").ClickAsync();
        await Expect(Page.Locator("#click-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#change-count")).ToHaveTextAsync("1");

        await Tab("pv-events", "Biz").ClickAsync();
        await Expect(Page.Locator("#biz-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#change-count")).ToHaveTextAsync("2");

        // The keyboard raises them too.
        await Tab("pv-events", "Biz").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Tab("pv-events", "Bar")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#changed")).ToHaveTextAsync("Bar");
        await Expect(Page.Locator("#click-count")).ToHaveTextAsync("4");
    }

    [TestMethod]
    public async Task OnChanging_CancelKeepsTheCurrentTab()
    {
        await OpenAsync(Url);

        await Page.Locator("#lock-history").CheckAsync();
        await Tab("pv-changing", "History").ClickAsync();
        await Expect(Page.Locator("#refused")).ToHaveTextAsync("History");
        await Expect(Tab("pv-changing", "Draft")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Tab("pv-changing", "History")).ToHaveAttributeAsync("aria-selected", "false");

        await Tab("pv-changing", "Preview").ClickAsync();
        await Expect(Tab("pv-changing", "Preview")).ToHaveAttributeAsync("aria-selected", "true");

        await Page.Locator("#lock-history").UncheckAsync();
        await Tab("pv-changing", "History").ClickAsync();
        await Expect(Tab("pv-changing", "History")).ToHaveAttributeAsync("aria-selected", "true");
    }

    [TestMethod]
    public async Task Panels_OnlyTheSelectedOneIsRenderedByDefault()
    {
        await OpenAsync(Url);

        await Expect(Root("pv-panels-default").Locator("> [role='tabpanel']")).ToHaveCountAsync(1);
        await Panel("pv-panels-default").Locator("input").FillAsync("hello");
        await Tab("pv-panels-default", "Second").ClickAsync();
        await Expect(Panel("pv-panels-default").Locator("input")).ToHaveAttributeAsync("aria-label", "Second");
        await Tab("pv-panels-default", "First").ClickAsync();
        await Expect(Panel("pv-panels-default").Locator("input")).ToHaveValueAsync("");
    }

    [TestMethod]
    public async Task KeepMounted_KeepsTheShownPanels_AndRendersNoUnseenOne()
    {
        await OpenAsync(Url);

        var root = Root("pv-keep-mounted");
        await Expect(root.Locator("> [role='tabpanel']")).ToHaveCountAsync(1);
        await Panel("pv-keep-mounted").Locator("input").FillAsync("hello");

        await Tab("pv-keep-mounted", "Second").ClickAsync();
        await Expect(root.Locator("> [role='tabpanel']")).ToHaveCountAsync(2);
        await Expect(root.Locator("> [role='tabpanel'][hidden]")).ToHaveCountAsync(1);

        await Tab("pv-keep-mounted", "First").ClickAsync();
        await Expect(Panel("pv-keep-mounted").Locator("input")).ToHaveValueAsync("hello");
        await Expect(root.Locator("input[aria-label='Third']")).ToHaveCountAsync(0);
        Assert.IsNull(await Tab("pv-keep-mounted", "Third").GetAttributeAsync("aria-controls"), "No panel to point at.");
    }

    [TestMethod]
    public async Task MountAllAndHeaderOnly_RenderEveryPanel_OrNone()
    {
        await OpenAsync(Url);

        var all = Root("pv-mount-all").Locator("> [role='tabpanel']");
        await Expect(all).ToHaveCountAsync(3);
        await Expect(Root("pv-mount-all").Locator("> [role='tabpanel'][hidden]")).ToHaveCountAsync(2);
        await Expect(Root("pv-mount-all").Locator("> [role='tabpanel'][aria-hidden='true']")).ToHaveCountAsync(2);
        await Expect(Tab("pv-mount-all", "Third")).ToHaveAttributeAsync("aria-controls", (await all.Nth(2).GetAttributeAsync("id"))!);

        await Expect(Root("pv-header-only").Locator("[role='tabpanel']")).ToHaveCountAsync(0);
        Assert.IsNull(await Tab("pv-header-only", "Foo").GetAttributeAsync("aria-controls"));
        await Tab("pv-header-only", "Bar").ClickAsync();
        await Expect(Page.Locator("#detached-content")).ToHaveTextAsync("Hello, I am Bar.");
    }

    [TestMethod]
    public async Task Dismissible_TheButtonAndTheDeleteKeyDismissATab_AndTheNeighbourTakesOver()
    {
        await OpenAsync(Url);

        await Expect(Root("pv-editable")).ToHaveClassAsync(HasClass("bit-pvt-dsm"));
        await Expect(Tab("pv-editable", "Home").Locator(".bit-pvti-dbt")).ToHaveCountAsync(0);

        var dismiss = Tab("pv-editable", "Tab 2").Locator(".bit-pvti-dbt");
        await Expect(dismiss).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(dismiss).ToHaveAttributeAsync("title", "Remove");
        await Expect(dismiss).ToHaveAttributeAsync("aria-label", "Remove Tab 2");
        await Expect(dismiss.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Cancel"));
        await Expect(Tab("pv-editable", "Tab 2")).ToHaveAttributeAsync("aria-keyshortcuts", "Delete");

        await dismiss.ClickAsync();
        await Expect(Page.Locator("#dismissed")).ToHaveTextAsync("Tab 2");
        await Expect(Page.Locator("#editable-tabs")).ToHaveTextAsync("Tab 1,Tab 3");
        await Expect(Tab("pv-editable", "Tab 2")).ToHaveCountAsync(0);

        await Tab("pv-editable", "Tab 3").ClickAsync();
        await Expect(Page.Locator("#editable-key")).ToHaveTextAsync("Tab 3");
        await Page.Keyboard.PressAsync("Delete");
        await Expect(Page.Locator("#editable-tabs")).ToHaveTextAsync("Tab 1");
        await Expect(Page.Locator("#editable-key")).ToHaveTextAsync("Tab 1");
        await Expect(Tab("pv-editable", "Tab 1")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Tab("pv-editable", "Tab 1")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Addable_TheAddButtonRaisesOnAdd_AndTheCustomTextsAndIconsApply()
    {
        await OpenAsync(Url);

        var add = Root("pv-editable").Locator("> .bit-pvt-hwr > .bit-pvt-abt");
        await Expect(add).ToHaveAttributeAsync("title", "Add");
        await Expect(add).ToHaveAttributeAsync("aria-label", "Add");
        await Expect(add.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Add"));
        await Expect(Root("pv-editable").Locator("[role='tablist'] .bit-pvt-abt")).ToHaveCountAsync(0);

        await add.ClickAsync();
        await Expect(Page.Locator("#editable-tabs")).ToHaveTextAsync("Tab 1,Tab 2,Tab 3,Tab 4");
        await Expect(Tab("pv-editable", "Tab 4")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Panel("pv-editable")).ToContainTextAsync("Content of Tab 4");

        var customAdd = Root("pv-editable-custom").Locator(".bit-pvt-abt");
        await Expect(customAdd).ToHaveAttributeAsync("title", "New tab");
        await Expect(customAdd).ToHaveAttributeAsync("aria-label", "Open a new tab");
        await Expect(customAdd.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--CirclePlus"));
        await customAdd.ClickAsync();
        await Expect(Page.Locator("#custom-adds")).ToHaveTextAsync("1");

        var customDismiss = Tab("pv-editable-custom", "One").Locator(".bit-pvti-dbt");
        await Expect(customDismiss).ToHaveAttributeAsync("title", "Close");
        await Expect(customDismiss).ToHaveAttributeAsync("aria-label", "Close the One tab");
        await Expect(customDismiss.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--ChromeClose"));
        await customDismiss.ClickAsync();
        await Expect(Page.Locator("#custom-dismissed")).ToHaveTextAsync("One");
        await Expect(Page.Locator("#own-dismisses")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Reorderable_DraggingATabAndCtrlArrowsAskForANewOrder_AndAPinnedTabStays()
    {
        await OpenAsync(Url);

        await Expect(Root("pv-reorder")).ToHaveClassAsync(HasClass("bit-pvt-reo"));
        await Expect(Tab("pv-reorder", "Tab A")).ToHaveAttributeAsync("draggable", "true");
        await Expect(Tab("pv-reorder", "Tab A")).ToHaveAttributeAsync("aria-keyshortcuts", "Control+ArrowLeft Control+ArrowRight");
        Assert.IsNull(await Tab("pv-reorder", "Pinned").GetAttributeAsync("draggable"), "A pinned tab is not draggable.");

        await Tab("pv-reorder", "Tab A").DragToAsync(Tab("pv-reorder", "Tab C"));
        await Expect(Page.Locator("#reorder-last")).ToHaveTextAsync("Tab A:0->2");
        await Expect(Page.Locator("#reorder-order")).ToHaveTextAsync("Tab B,Tab C,Tab A,Tab D");
        await ExpectTabOrderAsync("pv-reorder", "Tab B,Tab C,Tab A,Tab D,Pinned");

        await Tab("pv-reorder", "Tab B").FocusAsync();
        await Page.Keyboard.PressAsync("Control+ArrowRight");
        await Expect(Page.Locator("#reorder-order")).ToHaveTextAsync("Tab C,Tab B,Tab A,Tab D");
        await Expect(Tab("pv-reorder", "Tab B")).ToBeFocusedAsync();

        // Nothing moves onto the pinned tab.
        await Tab("pv-reorder", "Tab D").FocusAsync();
        await Page.Keyboard.PressAsync("Control+ArrowRight");
        await Tab("pv-reorder", "Tab D").DragToAsync(Tab("pv-reorder", "Pinned"));
        await Page.Keyboard.PressAsync("Control+ArrowLeft");
        await Expect(Page.Locator("#reorder-last")).ToHaveTextAsync("Tab D:3->2");
        await ExpectTabOrderAsync("pv-reorder", "Tab C,Tab B,Tab D,Tab A,Pinned");
    }

    [TestMethod]
    public async Task Keyboard_TheHeaderIsOneTabStop_ArrowsSkipDisabledAndHiddenTabs_AndWrap()
    {
        await OpenAsync(Url);

        await Expect(Root("pv-keyboard").Locator("> .bit-pvt-hwr > [role='tablist']")).ToHaveAttributeAsync("aria-label", "Documents");
        await Expect(Tab("pv-keyboard", "Locked")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Tab("pv-keyboard", "Hidden")).ToBeHiddenAsync();
        await Expect(Tab("pv-keyboard", "Recent")).ToHaveAttributeAsync("aria-setsize", "4");
        await Expect(Tab("pv-keyboard", "Recent")).ToHaveAttributeAsync("aria-posinset", "4");

        await Page.Locator("#before-keyboard").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Tab("pv-keyboard", "File")).ToBeFocusedAsync();
        // The focus ring is drawn for the keyboard.
        await Expect(Tab("pv-keyboard", "File")).Not.ToHaveCSSAsync("box-shadow", "none");

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tab("pv-keyboard", "Shared")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tab("pv-keyboard", "Recent")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tab("pv-keyboard", "File")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Tab("pv-keyboard", "Recent")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(Tab("pv-keyboard", "File")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(Tab("pv-keyboard", "Recent")).ToBeFocusedAsync();
        await Expect(Tab("pv-keyboard", "File")).ToHaveAttributeAsync("aria-selected", "true");

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Tab("pv-keyboard", "Recent")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Tab("pv-keyboard", "Recent")).ToHaveAttributeAsync("tabindex", "0");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Tab("pv-keyboard", "Shared")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync(" ");
        await Expect(Tab("pv-keyboard", "Shared")).ToHaveAttributeAsync("aria-selected", "true");

        // The next Tab reaches the panel, and the one after leaves the pivot.
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Panel("pv-keyboard")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#after-keyboard")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Tab("pv-keyboard", "Shared")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Keyboard_SelectOnFocusLoopFalseNavigableFalseAndAVerticalHeader()
    {
        await OpenAsync(Url);

        await Expect(Root("pv-select-on-focus").Locator("[role='tablist']")).ToHaveAttributeAsync("aria-labelledby", "settings-label");
        await Tab("pv-select-on-focus", "Profile").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tab("pv-select-on-focus", "Security")).ToHaveAttributeAsync("aria-selected", "true");

        await Tab("pv-no-loop", "File").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Tab("pv-no-loop", "File")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("End");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tab("pv-no-loop", "Recent")).ToBeFocusedAsync();

        foreach (var text in new[] { "File", "Shared", "Recent" })
        {
            await Expect(Tab("pv-not-navigable", text)).ToHaveAttributeAsync("tabindex", "0");
        }

        await Tab("pv-not-navigable", "File").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Tab("pv-not-navigable", "Shared")).ToBeFocusedAsync();

        await Tab("pv-vertical-keys", "Up").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tab("pv-vertical-keys", "Up")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Tab("pv-vertical-keys", "Middle")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Tab("pv-vertical-keys", "Up")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Disabled_ThePivotTakesNoSelection_AndItsTabsDropThePagesHandlers()
    {
        await OpenAsync(Url);

        var root = Root("pv-disabled");
        var file = Tab("pv-disabled", "File");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(file).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Tab("pv-disabled", "Shared")).ToHaveAttributeAsync("aria-disabled", "true");

        var shared = await BoxAsync(Tab("pv-disabled", "Shared"));
        await Page.Mouse.ClickAsync(shared.X + shared.Width / 2, shared.Y + shared.Height / 2);

        await file.DispatchEventAsync("dblclick");
        await file.DispatchEventAsync("pointerdown");
        await file.DispatchEventAsync("mouseover");
        await Expect(Page.Locator("#tab-hovers")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#tab-activations")).ToHaveTextAsync("0");
        await Expect(Tab("pv-disabled", "File")).ToHaveAttributeAsync("aria-selected", "true");

        // The root hosts the open panel, which stays live, so it keeps the page's handlers.
        await root.DispatchEventAsync("dblclick");
        await Expect(Page.Locator("#root-activations")).ToHaveTextAsync("1");

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(file).Not.ToHaveAttributeAsync("aria-disabled", "true");
        await file.DispatchEventAsync("dblclick");
        await Expect(Page.Locator("#tab-activations")).ToHaveTextAsync("1");
        await Tab("pv-disabled", "Shared").ClickAsync();
        await Expect(Tab("pv-disabled", "Shared")).ToHaveAttributeAsync("aria-selected", "true");
    }

    [TestMethod]
    public async Task Disabled_ADisabledItemIsAnnouncedSkippedAndNotSelectable_AndATabPivotMutesItsFill()
    {
        await OpenAsync(Url);

        var off = Tab("pv-disabled-item", "Shared");
        await Expect(off).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(off).ToHaveClassAsync(HasClass("bit-dis"));
        await off.ClickAsync(new() { Force = true });
        await Expect(Tab("pv-disabled-item", "File")).ToHaveAttributeAsync("aria-selected", "true");

        await Tab("pv-disabled-item", "File").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tab("pv-disabled-item", "Recent")).ToBeFocusedAsync();

        await Expect(Tab("pv-disabled-tab", "File")).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-pri-dis)"));
    }

    [TestMethod]
    public async Task BitParams_CascadesItsDefaults_AndAPivotsOwnHeaderTypeWins()
    {
        await OpenAsync(Url);

        await Expect(Root("pv-params")).ToHaveClassAsync(HasClass("bit-pvt-tab"));
        await Expect(Root("pv-params")).ToHaveClassAsync(HasClass("bit-pvt-fwd"));
        await Expect(Root("pv-params-own")).ToHaveClassAsync(HasClass("bit-pvt-lnk"));
        await Expect(Root("pv-params-own")).Not.ToHaveClassAsync(HasClass("bit-pvt-tab"));
        await Expect(Root("pv-params-own")).ToHaveClassAsync(HasClass("bit-pvt-fwd"));
    }

    [TestMethod]
    public async Task Color_EveryColor_PaintsTheIndicatorAndTheTabFill()
    {
        await OpenAsync(Url);
        await Page.Mouse.MoveAsync(0, 0);

        foreach (var (color, suffix, main) in Colors)
        {
            var expected = await ResolveColorAsync($"var({main})");

            await Expect(Root($"pv-color-{color}")).ToHaveClassAsync(HasClass($"bit-pvt-{suffix}"));
            await ExpectPseudoAsync(Tab($"pv-color-{color}", "File"), "background-color", expected);

            await Expect(Tab($"pv-color-tab-{color}", "File")).ToHaveCSSAsync("background-color", expected);
        }
    }

    [TestMethod]
    public async Task ExternalIcons_RenderTheGivenClasses_AndIconWinsOverIconName()
    {
        await OpenAsync(Url);

        await Expect(Tab("pv-ext", "Home").Locator(".bit-pvti-icn i")).ToHaveClassAsync(HasClass("fa-house"));
        await Expect(Tab("pv-ext", "Heart").Locator(".bit-pvti-icn i")).ToHaveClassAsync(HasClass("fa-heart"));
        await Expect(Tab("pv-ext", "Rocket").Locator(".bit-pvti-icn i")).ToHaveClassAsync(HasClass("fa-rocket"));
        var gear = Tab("pv-ext", "Gear").Locator(".bit-pvti-icn i");
        await Expect(gear).ToHaveClassAsync(HasClass("bi-gear-fill"));
        await Expect(gear).Not.ToHaveClassAsync(HasClass("bit-icon--Settings"));
    }

    [TestMethod]
    [DataRow("Small", "bit-pvt-sm")]
    [DataRow("Medium", "bit-pvt-md")]
    [DataRow("Large", "bit-pvt-lg")]
    public async Task Size_EverySize_ScalesTheTabs(string size, string cls)
    {
        await OpenAsync(Url);

        await Expect(Root($"pv-size-{size}")).ToHaveClassAsync(HasClass(cls));
        await Expect(Root("pv-size-none")).Not.ToHaveClassAsync(new Regex(@"bit-pvt-(sm|md|lg)\b"));

        var heights = new Dictionary<string, double>();
        var fonts = new Dictionary<string, double>();
        foreach (var name in new[] { "Small", "Medium", "Large", "none" })
        {
            heights[name] = (await BoxAsync(Tab($"pv-size-{name}", "Shared"))).Height;
            fonts[name] = ParsePx(await Tab($"pv-size-{name}", "Shared").EvaluateAsync<string>("el => getComputedStyle(el).fontSize"));
        }

        switch (size)
        {
            case "Small":
                Assert.IsTrue(heights["Small"] < heights["Medium"] && fonts["Small"] < fonts["Medium"]);
                break;
            case "Medium":
                Assert.AreEqual(heights["none"], heights["Medium"], 0.5);
                Assert.AreEqual(fonts["none"], fonts["Medium"], 0.1);
                break;
            default:
                Assert.IsTrue(heights["Large"] > heights["Medium"] && fonts["Large"] > fonts["Medium"]);
                break;
        }
    }

    [TestMethod]
    public async Task StyleClassAndHtmlAttributes_LandOnTheRoot()
    {
        await OpenAsync(Url);

        await Expect(Root("pv-style")).ToHaveCSSAsync("border-top-color", "rgb(255, 99, 71)");
        await Expect(Root("pv-style")).ToHaveCSSAsync("margin-left", "9px");
        await Expect(Root("pv-class")).ToHaveClassAsync(HasClass("custom-class"));
        await Expect(Root("pv-class")).ToHaveClassAsync(HasClass("bit-pvt"));
        await Expect(Root("pv-class")).ToHaveAttributeAsync("data-test", "pivot-attr");
    }

    [TestMethod]
    public async Task Styles_ReachEveryPart()
    {
        await OpenAsync(Url);
        await Page.Mouse.MoveAsync(0, 0);

        var root = Root("pv-styles");
        var styled = Tab("pv-styles", "Styled");
        await Expect(root).ToHaveCSSAsync("padding-top", "2px");
        await Expect(root.Locator("> .bit-pvt-hwr > [role='tablist']")).ToHaveCSSAsync("padding-bottom", "3px");
        await Expect(root.Locator("> .bit-pvt-hwr")).ToHaveCSSAsync("padding-left", "4px");
        await Expect(Tab("pv-styles", "Alpha")).ToHaveCSSAsync("color", "rgb(10, 11, 12)");
        await Expect(styled).ToHaveCSSAsync("background-color", "rgb(13, 14, 15)");
        await Expect(styled.Locator(".bit-pvti-cnt")).ToHaveCSSAsync("padding-right", "1px");
        await Expect(styled.Locator(".bit-pvti-icn")).ToHaveCSSAsync("color", "rgb(16, 17, 18)");
        await Expect(styled.Locator(".bit-pvti-icn i")).ToHaveCSSAsync("color", "rgb(19, 20, 21)");
        await Expect(styled.Locator(".bit-pvti-txt")).ToHaveCSSAsync("color", "rgb(22, 23, 24)");
        await Expect(styled.Locator(".bit-pvti-cnr")).ToHaveCSSAsync("color", "rgb(25, 26, 27)");
        await Expect(root.Locator(".bit-pvt-hst")).ToHaveCSSAsync("color", "rgb(28, 29, 30)");
        await Expect(root.Locator(".bit-pvt-hnd")).ToHaveCSSAsync("color", "rgb(31, 32, 33)");
        await Expect(styled.Locator(".bit-pvti-dbt")).ToHaveCSSAsync("color", "rgb(34, 35, 36)");
        await Expect(styled.Locator(".bit-pvti-dsi")).ToHaveCSSAsync("color", "rgb(37, 38, 39)");
        await Expect(root.Locator(".bit-pvt-abt")).ToHaveCSSAsync("color", "rgb(40, 41, 42)");
        await Expect(root.Locator(".bit-pvt-abt i")).ToHaveCSSAsync("color", "rgb(43, 44, 45)");
        await Expect(Panel("pv-styles")).ToHaveCSSAsync("padding-top", "5px");
        await Expect(Panel("pv-styles")).ToHaveCSSAsync("margin-left", "6px");
        await Expect(Panel("pv-styles")).ToHaveClassAsync(HasClass("own-body"));

        var more = More("pv-styles");
        await Expect(more).ToBeVisibleAsync();
        await Expect(more).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await more.ClickAsync();
        var menu = Menu("pv-styles");
        await Expect(menu).ToHaveCSSAsync("border-top-width", "2px");
        await Expect(menu).ToHaveCSSAsync("border-top-color", "rgb(4, 5, 6)");
        await Expect(menu.Locator("[role='menuitemradio']").First).ToHaveCSSAsync("color", "rgb(7, 8, 9)");
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Root("pv-styles-slide").Locator(".bit-pvt-sbt").First).ToHaveCSSAsync("color", "rgb(46, 47, 48)");
    }

    [TestMethod]
    public async Task Classes_ReachEveryPart()
    {
        await OpenAsync(Url);

        var root = Root("pv-styles");
        var styled = Tab("pv-styles", "Styled");
        await Expect(root).ToHaveClassAsync(HasClass("c-root"));
        await Expect(root.Locator("> .bit-pvt-hwr > [role='tablist']")).ToHaveClassAsync(HasClass("c-header"));
        await Expect(root.Locator("> .bit-pvt-hwr")).ToHaveClassAsync(HasClass("c-header-container"));
        await Expect(Tab("pv-styles", "Alpha")).ToHaveClassAsync(HasClass("c-header-item"));
        await Expect(Tab("pv-styles", "Alpha")).Not.ToHaveClassAsync(HasClass("c-selected"));
        await Expect(styled).ToHaveClassAsync(HasClass("c-selected"));
        await Expect(styled.Locator(".bit-pvti-cnt")).ToHaveClassAsync(HasClass("c-item-content"));
        await Expect(styled.Locator(".bit-pvti-icn")).ToHaveClassAsync(HasClass("c-icon-container"));
        await Expect(styled.Locator(".bit-pvti-icn i")).ToHaveClassAsync(HasClass("c-icon"));
        await Expect(styled.Locator(".bit-pvti-txt")).ToHaveClassAsync(HasClass("c-text"));
        await Expect(styled.Locator(".bit-pvti-cnr")).ToHaveClassAsync(HasClass("c-count"));
        await Expect(root.Locator(".bit-pvt-hst")).ToHaveClassAsync(HasClass("c-start"));
        await Expect(root.Locator(".bit-pvt-hnd")).ToHaveClassAsync(HasClass("c-end"));
        await Expect(styled.Locator(".bit-pvti-dbt")).ToHaveClassAsync(HasClass("c-dismiss"));
        await Expect(styled.Locator(".bit-pvti-dsi")).ToHaveClassAsync(HasClass("c-dismiss-icon"));
        await Expect(root.Locator(".bit-pvt-abt")).ToHaveClassAsync(HasClass("c-add"));
        await Expect(root.Locator(".bit-pvt-abt i")).ToHaveClassAsync(HasClass("c-add-icon"));
        await Expect(Panel("pv-styles")).ToHaveClassAsync(HasClass("c-body"));
        await Expect(More("pv-styles")).ToHaveClassAsync(HasClass("c-overflow-button"));
        await Expect(Menu("pv-styles")).ToHaveClassAsync(HasClass("c-overflow-callout"));
        await Expect(Menu("pv-styles").Locator("[role='menuitemradio']").First).ToHaveClassAsync(HasClass("c-overflow-item"));
        await Expect(Root("pv-styles-slide").Locator(".bit-pvt-sbt").First).ToHaveClassAsync(HasClass("c-slide"));

        // SelectedItem follows the selection.
        await Tab("pv-styles", "Alpha").ClickAsync();
        await Expect(Tab("pv-styles", "Alpha")).ToHaveClassAsync(HasClass("c-selected"));
        await Expect(styled).Not.ToHaveClassAsync(HasClass("c-selected"));
    }

    [TestMethod]
    public async Task Visibility_AnItemHiddenOrCollapsedAtRuntime_AndThePivotItself()
    {
        await OpenAsync(Url);

        var two = Tab("pv-visibility", "Two");
        await Expect(Tab("pv-visibility", "Three")).ToHaveAttributeAsync("aria-setsize", "3");

        await Page.Locator("#btn-item-hidden").ClickAsync();
        await Expect(two).ToHaveCSSAsync("visibility", "hidden");

        await Page.Locator("#btn-item-collapsed").ClickAsync();
        await Expect(two).ToHaveCSSAsync("display", "none");
        await Expect(Tab("pv-visibility", "Three")).ToHaveAttributeAsync("aria-setsize", "2");
        await Expect(Tab("pv-visibility", "Three")).ToHaveAttributeAsync("aria-posinset", "2");
        await Tab("pv-visibility", "One").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tab("pv-visibility", "Three")).ToBeFocusedAsync();

        await Page.Locator("#btn-item-visible").ClickAsync();
        await Expect(two).ToBeVisibleAsync();
        await Expect(Tab("pv-visibility", "Three")).ToHaveAttributeAsync("aria-setsize", "3");

        await Page.Locator("#btn-root-collapsed").ClickAsync();
        await Expect(Root("pv-root-visibility")).ToBeHiddenAsync();
        await Page.Locator("#btn-root-visible").ClickAsync();
        await Expect(Root("pv-root-visibility")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheDefault()
    {
        await OpenAsync(Url);
        await Page.Mouse.MoveAsync(0, 0);

        var file = Tab("pv-css-vars", "File");
        var shared = Tab("pv-css-vars", "Shared");
        await ExpectPseudoAsync(file, "background-color", "rgb(46, 139, 87)");
        Assert.AreEqual("5px", await PseudoAsync(file, "height"));
        await Expect(shared).ToHaveCSSAsync("color", "rgb(10, 20, 30)");
        await Expect(file).ToHaveCSSAsync("font-weight", "800");
        await Expect(file).ToHaveCSSAsync("font-size", "18px");
        Assert.AreEqual(50, (await BoxAsync(file)).Height, 0.5);
        var a = await BoxAsync(file);
        var b = await BoxAsync(shared);
        Assert.AreEqual(13, b.X - (a.X + a.Width), 0.5);
        await Expect(Panel("pv-css-vars")).ToHaveCSSAsync("padding-top", "7px");

        await More("pv-css-vars-menu").ClickAsync();
        await Expect(Menu("pv-css-vars-menu")).ToHaveCSSAsync("background-color", "rgb(200, 210, 220)");
    }

    [TestMethod]
    public async Task CssVariables_AnExplicitColorGapAndSize_WinOverTheVariables()
    {
        await OpenAsync(Url);

        var file = Tab("pv-css-vars-params", "File");
        var shared = Tab("pv-css-vars-params", "Shared");
        await ExpectPseudoAsync(file, "background-color", await ResolveColorAsync("var(--bit-clr-err)"));
        var a = await BoxAsync(file);
        var b = await BoxAsync(shared);
        Assert.AreEqual(3, b.X - (a.X + a.Width), 0.5, "Gap wins over --bit-Pivot-gap.");
        Assert.IsTrue(a.Height < 50, "Size wins over --bit-Pivot-item-height.");
        Assert.IsTrue(ParsePx(await file.EvaluateAsync<string>("el => getComputedStyle(el).fontSize")) < 18);
    }

    [TestMethod]
    public async Task Rtl_AnExplicitDir_RunsTheTabsRightToLeft_AndMirrorsTheArrows()
    {
        await OpenAsync(Url);

        var root = Root("pv-rtl");
        await Expect(root).ToHaveAttributeAsync("dir", "rtl");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtl"));
        var tabs = root.Locator("> .bit-pvt-hwr > [role='tablist'] > [role='tab']");
        Assert.IsTrue((await BoxAsync(tabs.Nth(0))).X > (await BoxAsync(tabs.Nth(1))).X);

        await tabs.Nth(0).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(tabs.Nth(1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(tabs.Nth(0)).ToBeFocusedAsync();

        // Start is the right side, right to left.
        var start = Root("pv-rtl-start");
        var header = await BoxAsync(start.Locator("> .bit-pvt-hwr > [role='tablist']"));
        var panel = await BoxAsync(Panel("pv-rtl-start"));
        Assert.IsTrue(header.X >= panel.X + panel.Width - 1);

        // The slide buttons' arrows turn with the header.
        var slideIcon = Root("pv-rtl-slide").Locator(".bit-pvt-snx i");
        await Expect(slideIcon).ToHaveClassAsync(HasClass("bit-ico-frt"));
        StringAssert.StartsWith(await slideIcon.EvaluateAsync<string>("el => getComputedStyle(el).scale"), "-1");
        await Root("pv-rtl-slide").Locator(".bit-pvt-snx").ClickAsync();
        await Page.WaitForFunctionAsync("() => document.querySelector('#pv-rtl-slide [role=tablist]').scrollLeft < 0");
    }

    [TestMethod]
    public async Task Rtl_InheritedFromAnAncestor_IsReadOffTheHeader()
    {
        await OpenAsync(Url);

        var root = Root("pv-rtl-inherited");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-rtl"));
        Assert.IsTrue((await BoxAsync(Tab("pv-rtl-inherited", "One"))).X > (await BoxAsync(Tab("pv-rtl-inherited", "Two"))).X);

        await Tab("pv-rtl-inherited", "One").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Tab("pv-rtl-inherited", "Two")).ToBeFocusedAsync();

        // Left to right the slide arrow is not turned.
        var ltrIcon = Root("pv-slide").Locator(".bit-pvt-snx i");
        Assert.IsFalse((await ltrIcon.EvaluateAsync<string>("el => getComputedStyle(el).scale")).StartsWith("-1"));
    }

    [TestMethod]
    public async Task EscapeInsideADialog_ClosesTheOverflowMenuFirst_AndTheDialogOnTheNextPress()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-open-dialog").ClickAsync();
        await Expect(Page.Locator("#dialog-state")).ToHaveTextAsync("open");

        var more = More("pv-in-dialog");
        await Expect(more).ToBeVisibleAsync();
        await more.ClickAsync();
        var menu = Menu("pv-in-dialog");
        await Expect(menu).ToBeFocusedAsync();
        await Expect(menu).ToHaveAttributeAsync("data-bit-esc", "claim");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(menu).ToBeHiddenAsync();
        await Expect(more).ToBeFocusedAsync();
        await Expect(Page.Locator("#dialog-state")).ToHaveTextAsync("open");
        Assert.IsNull(await more.GetAttributeAsync("data-bit-esc"), "A closed menu claims no Escape.");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#dialog-state")).ToHaveTextAsync("closed");
    }

    [TestMethod]
    public async Task ReducedMotion_CollapsesTheIndicatorTransition_AndForceAnimationRestoresIt()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await OpenAsync(Url);

        var reduced = ParseDurations(await PseudoAsync(Tab("pv-motion", "File"), "transition-duration"));
        Assert.IsTrue(reduced.All(d => d < 0.01), $"Reduced: {string.Join(", ", reduced)}s.");

        await Expect(Root("pv-force-animation")).ToHaveClassAsync(HasClass("bit-fam"));
        var full = ParseDurations(await PseudoAsync(Tab("pv-force-animation", "File"), "transition-duration"));
        Assert.IsTrue(full.All(d => d >= 0.05), $"ForceAnimation: {string.Join(", ", full)}s.");

        // Without the preference the indicator moves at its full pace again (the page is opened afresh: WebKit does not
        // re-resolve the style of a pseudo element on a change of the emulated media alone).
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.NoPreference });
        await OpenAsync(Url);
        var normal = ParseDurations(await PseudoAsync(Tab("pv-motion", "File"), "transition-duration"));
        Assert.IsTrue(normal.All(d => d >= 0.05), $"No preference: {string.Join(", ", normal)}s.");
    }

    [TestMethod]
    public async Task Hover_ThePointerPaintsTheHoverBackground()
    {
        await OpenAsync(Url);

        var shared = Tab("pv-basic", "Recent");
        await shared.HoverAsync();
        await Expect(shared).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-bg-pri-hover)"));
    }

    [TestMethod]
    public async Task Unmount_ThePivotAndItsScriptsGoWithoutAnError()
    {
        await OpenAsync(Url);

        await Expect(More("pv-unmount")).ToBeVisibleAsync();

        await Page.Locator("#btn-toggle-mounted").ClickAsync();
        await Expect(Page.Locator("#mounted-state")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#pv-unmount")).ToHaveCountAsync(0);

        await Page.Locator("#btn-toggle-mounted").ClickAsync();
        await Expect(More("pv-unmount")).ToBeVisibleAsync();

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }



    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Tab(string id, string text)
    {
        return Page.Locator($"#{id} > .bit-pvt-hwr > .bit-pvt-hct > .bit-pvti").Filter(new() { HasText = text });
    }

    private ILocator Panel(string id) => Page.Locator($"#{id} > .bit-pvt-cct:not([hidden])");

    private ILocator More(string id) => Page.Locator($"#{id} > .bit-pvt-hwr > .bit-pvt-mor");

    private ILocator Menu(string id) => Page.Locator($"#{id} > .bit-pvt-hwr > [role='menu']");

    private static Task<string> PseudoAsync(ILocator element, string property)
    {
        return element.EvaluateAsync<string>($"el => getComputedStyle(el, '::before').getPropertyValue('{property}')");
    }

    // Waits for a computed style of the ::before of an element (the selection indicator) to reach a value, the way a
    // transition finishing is waited for: polled on the page's own frames, and compared once it settles or times out.
    private static async Task ExpectPseudoAsync(ILocator element, string property, string expected)
    {
        var value = await element.EvaluateAsync<string>(@"(el, [property, expected]) => new Promise(resolve => {
            const start = performance.now();
            const check = () => {
                const value = getComputedStyle(el, '::before').getPropertyValue(property);
                if (value === expected || performance.now() - start > 10000) resolve(value);
                else requestAnimationFrame(check);
            };
            check();
        })", new[] { property, expected });

        Assert.AreEqual(expected, value, $"The {property} of the ::before.");
    }

    // The menu keeps the focus and points at the item it is on through aria-activedescendant.
    private static async Task ExpectActiveAsync(ILocator menu, ILocator item)
    {
        var id = await item.GetAttributeAsync("id");
        await Expect(menu).ToHaveAttributeAsync("aria-activedescendant", id!);
        await Expect(item).ToHaveClassAsync(HasClass("bit-pvt-mni-act"));
    }

    private async Task ExpectTabOrderAsync(string id, string expected)
    {
        var tabs = Page.Locator($"#{id} > .bit-pvt-hwr > .bit-pvt-hct > .bit-pvti");
        await Expect(tabs).ToHaveTextAsync(expected.Split(',').Select(t => new Regex(Regex.Escape(t))).ToArray());
    }

    private static async Task<LocatorBoundingBoxResult> BoxAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.IsNotNull(box, "The element has no box.");
        return box;
    }

    private Task<string> ResolveColorAsync(string value)
    {
        return Page.EvaluateAsync<string>(@"v => {
            const probe = document.createElement('div');
            probe.style.backgroundColor = v;
            document.body.appendChild(probe);
            const color = getComputedStyle(probe).backgroundColor;
            probe.remove();
            return color;
        }", value);
    }

    private static double ParsePx(string value) => double.Parse(value.Replace("px", ""), CultureInfo.InvariantCulture);

    private static double[] ParseDurations(string value)
    {
        return value.Split(',').Select(v => v.Trim()).Select(v => v.EndsWith("ms", StringComparison.Ordinal)
            ? double.Parse(v[..^2], CultureInfo.InvariantCulture) / 1000
            : double.Parse(v[..^1], CultureInfo.InvariantCulture)).ToArray();
    }
}
