using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Drives BitNavPanel at /e2e/nav-panel (and /e2e/nav-panel/drawer) at a desktop viewport in a real browser: the
/// column of a wide screen and its rail (the toggle button, IsToggled two-way, ExpandOnHover, NoToggle, HideToggle,
/// the toggle's name and icon), Width / ToggledWidth / FitWidth / FullWidth, the logo, title and their link, the search
/// box filtering the items (SearchText two-way, OnSearch, SearchFilter, the empty message and template, the live
/// announcement, NoSearchBox, the rail's search button), navigation selecting the item (Automatic) and the Manual
/// selection with its binding, the expansion modes, custom items, the templates, the events, every public method, a
/// drawer on a wide screen (DrawerBreakpoint Always / Never: overlay, dialog role, focus trap and restore, Escape,
/// close button, Placement End, AutoFocus, NoOverlay), StickyEnds, the BitParams cascade, Accent / Color / Size,
/// external icons, Style / Class / Styles / Classes, every --bit-NavPanel-* variable, RTL, Disabled, prerendering and
/// unmounting. The phone viewport is covered by BitNavPanelPhoneE2ETests.
/// </summary>
[TestClass]
public class BitNavPanelE2ETests : E2ETestBase
{
    private const string Route = "/e2e/nav-panel";

    private ILocator Panel(string id) => Page.Locator($"#{id}");

    private ILocator Item(string panel, string text) => Page.Locator($"#{panel} .bit-nav-ict[title='{text}']");

    private ILocator Search(string panel) => Page.Locator($"#{panel} .bit-srb-inp");

    private ILocator ToggleButton(string panel) => Page.Locator($"#{panel} .bit-npn-tbn");

    private ILocator Overlay(string panel) => Panel(panel).Locator("xpath=preceding-sibling::div[contains(@class,'bit-npn-ovl')][1]");

    private ILocator Log => Page.Locator("#npn-log");

    private async Task ShowAsync(string id) => await Panel(id).ScrollIntoViewIfNeededAsync();

    private async Task<double> WidthAsync(string id) => (await Panel(id).BoundingBoxAsync())!.Width;

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

    // ---------- basic ----------

    [TestMethod]
    public async Task Basic_AWideScreen_ShowsAColumnWithItsSearchBoxToggleAndItems()
    {
        await OpenAsync(Route);

        var panel = Panel("npn-basic");
        await Expect(panel).ToHaveClassAsync(HasClass("bit-npn"));
        await Expect(panel).ToHaveClassAsync(HasClass("bit-npn-col"));
        await Expect(panel).ToBeVisibleAsync();
        await Expect(panel).ToHaveCSSAsync("position", "sticky");
        await Expect(panel).Not.ToHaveAttributeAsync("role", new Regex(".*"));
        await Expect(Overlay("npn-basic")).ToHaveCountAsync(0);
        await Expect(panel.Locator("nav.bit-nav")).ToBeVisibleAsync();
        await Expect(Search("npn-basic")).ToHaveAttributeAsync("aria-label", "Search");
        await Expect(ToggleButton("npn-basic")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(ToggleButton("npn-basic")).ToHaveAttributeAsync("aria-controls", "npn-basic");
        await Expect(Item("npn-basic", "Home")).ToBeVisibleAsync();
        await Expect(Item("npn-basic", "Phones")).ToHaveCountAsync(0);
        Assert.AreEqual(240, await WidthAsync("npn-basic"), 1, "Without a Width the panel takes its container's.");

        // A group opens on its header.
        await Item("npn-basic", "Products").ClickAsync();
        await Expect(Item("npn-basic", "Products")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Item("npn-basic", "Phones")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Navigation_ClickingAnItem_NavigatesAndSelectsIt()
    {
        await OpenAsync(Route);

        await Item("npn-basic", "Settings").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/e2e/nav-panel/page/settings$"));
        await Expect(Page.Locator("#npn-section")).ToHaveTextAsync("settings");
        await Expect(Item("npn-basic", "Settings")).ToHaveAttributeAsync("aria-current", "page");
        await Expect(Item("npn-basic", "Home")).Not.ToHaveAttributeAsync("aria-current", new Regex(".*"));

        // A child's page selects the child and marks its group.
        await Item("npn-basic", "Products").ClickAsync();
        await Item("npn-basic", "Phones").ClickAsync();
        await Expect(Page.Locator("#npn-section")).ToHaveTextAsync("phones");
        await Expect(Item("npn-basic", "Phones")).ToHaveAttributeAsync("aria-current", "page");
        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    [TestMethod]
    public async Task Navigation_OpeningAPageUrl_SelectsItsItem()
    {
        await OpenAsync($"{Route}/page/contact");

        await Expect(Item("npn-basic", "Contact us")).ToHaveAttributeAsync("aria-current", "page");
        await Expect(Item("npn-basic", "Support")).ToHaveAttributeAsync("aria-expanded", "true");
    }

    // ---------- rail ----------

    [TestMethod]
    public async Task Toggle_TheButton_CollapsesThePanelIntoARailOfIconsAndBack()
    {
        await OpenAsync(Route);

        var full = await WidthAsync("npn-basic");
        await ToggleButton("npn-basic").ClickAsync();

        var panel = Panel("npn-basic");
        await Expect(panel).ToHaveClassAsync(HasClass("bit-npn-tgl"));
        await Expect(ToggleButton("npn-basic")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(ToggleButton("npn-basic")).ToHaveAttributeAsync("aria-label", "Expand the navigation panel");
        await Expect(Search("npn-basic")).ToBeHiddenAsync();
        await Expect(panel.Locator(".bit-npn-tsb")).ToBeVisibleAsync();
        await Expect(panel.Locator(".bit-nav-itx").First).ToBeHiddenAsync();
        await Expect(panel).ToHaveCSSAsync("max-width", "96px");
        await Page.WaitForFunctionAsync("([e, full]) => e.getBoundingClientRect().width < full - 50", new object[] { await panel.ElementHandleAsync(), full });
        Assert.IsTrue(full > 200);

        // The rail's search button opens the panel back out with the search box focused.
        await panel.Locator(".bit-npn-tsb").ClickAsync();
        await Expect(panel).Not.ToHaveClassAsync(HasClass("bit-npn-tgl"));
        await Expect(Search("npn-basic")).ToBeFocusedAsync();
        await Page.WaitForFunctionAsync("e => e.getBoundingClientRect().width >= 239", await panel.ElementHandleAsync());
    }

    [TestMethod]
    public async Task ExpandOnHover_TheRail_WidensWhileThePointerIsOverIt()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-hover");

        var panel = Panel("npn-hover");
        await Expect(panel).ToHaveClassAsync(HasClass("bit-npn-eoh"));
        await Expect(panel).ToHaveClassAsync(HasClass("bit-npn-tgl"));
        await Page.WaitForFunctionAsync("e => e.getBoundingClientRect().width < 150", await panel.ElementHandleAsync());

        await panel.HoverAsync();
        await Page.WaitForFunctionAsync("e => e.getBoundingClientRect().width >= 239", await panel.ElementHandleAsync());
        await Expect(panel.Locator(".bit-nav-itx").First).ToBeVisibleAsync();

        await Page.Mouse.MoveAsync(1200, 5);
        await Page.WaitForFunctionAsync("e => e.getBoundingClientRect().width < 150", await panel.ElementHandleAsync());
    }

    [TestMethod]
    public async Task NoToggleAndHideToggle_TakeTheButtonAwayAndIsToggledStillWorks()
    {
        await OpenAsync(Route);

        await Expect(ToggleButton("npn-notoggle")).ToHaveCountAsync(0);
        await Expect(ToggleButton("npn-hidetoggle")).ToHaveCountAsync(0);

        await Page.Locator("#btn-toggle-hidden").ClickAsync();
        await Expect(Panel("npn-hidetoggle")).ToHaveClassAsync(HasClass("bit-npn-tgl"));
        await Page.Locator("#btn-toggle-hidden").ClickAsync();
        await Expect(Panel("npn-hidetoggle")).Not.ToHaveClassAsync(HasClass("bit-npn-tgl"));
    }

    [TestMethod]
    public async Task ToggleAriaLabelAndIcon_NameAndDrawTheButton()
    {
        await OpenAsync(Route);

        var button = ToggleButton("npn-togglelabel");
        await Expect(button).ToHaveAttributeAsync("aria-label", "Fold the menu");
        await Expect(button).ToHaveAttributeAsync("title", "Fold the menu");
        await Expect(button.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--GlobalNavButton"));
        await button.ClickAsync();
        await Expect(button).ToHaveAttributeAsync("aria-label", "Fold the menu");
    }

    [TestMethod]
    public async Task IsToggled_TwoWay_FollowsTheToggleButtonAndThePage()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-api");

        await ToggleButton("npn-api").ClickAsync();
        await Expect(Page.Locator("#api-toggled")).ToHaveTextAsync("True");
        await Page.Locator("#btn-api-toggle").ClickAsync();
        await Expect(Page.Locator("#api-toggled")).ToHaveTextAsync("False");
        await Expect(Panel("npn-api")).Not.ToHaveClassAsync(HasClass("bit-npn-tgl"));
    }

    // ---------- width ----------

    [TestMethod]
    public async Task Width_TheWidthModes_SizeThePanelAndTheRail()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-width");

        // Width is the width of the panel's content box, inside its padding.
        await Expect(Panel("npn-width")).ToHaveCSSAsync("width", "200px");
        var padding = await Panel("npn-width").EvaluateAsync<double>("e => parseFloat(getComputedStyle(e).paddingLeft) + parseFloat(getComputedStyle(e).paddingRight)");
        Assert.AreEqual(200 + padding, await WidthAsync("npn-width"), 1);
        await Expect(Panel("npn-fit")).ToHaveClassAsync(HasClass("bit-npn-fiw"));
        var fit = await WidthAsync("npn-fit");
        Assert.IsTrue(fit < 300, $"FitWidth takes its widest item's width, was {fit}.");
        Assert.IsFalse(await Panel("npn-fit").EvaluateAsync<bool>("e => e.scrollWidth > e.clientWidth"), "Nothing of it is cut off.");
        await Expect(Panel("npn-full")).ToHaveClassAsync(HasClass("bit-npn-fuw"));
        // FullWidth fills its container, its padding included, and ignores Width.
        Assert.AreEqual(300, await WidthAsync("npn-full"), 1, "FullWidth stretches over its container and ignores Width.");
        await Expect(Panel("npn-full")).ToHaveCSSAsync("width", $"{300 - padding}px");

        await ToggleButton("npn-width").ClickAsync();
        await Expect(Panel("npn-width")).ToHaveCSSAsync("max-width", "72px");
        await Page.WaitForFunctionAsync("([e, p]) => Math.abs(e.getBoundingClientRect().width - 72 - p) < 1", new object[] { await Panel("npn-width").ElementHandleAsync(), padding });
    }

    [TestMethod]
    public async Task FullWidth_ThePanel_FillsItsContainerWithoutOverflowingIt()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-full");

        Assert.AreEqual(300, await WidthAsync("npn-full"), 1);
    }

    // ---------- logo ----------

    [TestMethod]
    public async Task Logo_ALogoLink_IsNamedByIconAriaLabelAndATitledOneByItsTitle()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-logo");

        var link = Panel("npn-logo").Locator("a.bit-npn-hln");
        await Expect(link).ToHaveAttributeAsync("href", "/e2e/nav-panel/page/home");
        await Expect(link).ToHaveAttributeAsync("aria-label", "bit platform home");
        await Expect(link.Locator("img.bit-npn-img")).ToHaveAttributeAsync("alt", "");
        await Expect(link.Locator("img.bit-npn-img")).ToBeVisibleAsync();

        var titled = Panel("npn-title").Locator("a.bit-npn-hln");
        await Expect(titled.Locator(".bit-npn-htx")).ToHaveTextAsync("BlazorUI");
        await Expect(titled).Not.ToHaveAttributeAsync("aria-label", new Regex(".*"));
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "BlazorUI", Exact = true })).ToHaveCountAsync(1);

        await Expect(Panel("npn-logo-nolink").Locator("img.bit-npn-img")).ToHaveAttributeAsync("alt", "Main menu");

        // The rail hides the brand but keeps the toggle.
        await ToggleButton("npn-title").ClickAsync();
        await Expect(titled).ToBeHiddenAsync();
        await Expect(ToggleButton("npn-title")).ToBeVisibleAsync();

        await link.ClickAsync();
        await Expect(Page.Locator("#npn-section")).ToHaveTextAsync("home");
    }

    // ---------- search ----------

    [TestMethod]
    public async Task Search_TypingFiltersTheItemsAndAnnouncesTheCount()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-search");

        var search = Search("npn-search");
        await Expect(search).ToHaveAttributeAsync("placeholder", "Search in menu items...");
        await search.FillAsync("phon");

        await Expect(Item("npn-search", "Phones")).ToBeVisibleAsync();
        await Expect(Item("npn-search", "Home")).ToHaveCountAsync(0);
        await Expect(Item("npn-search", "Settings")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#search-text")).ToHaveTextAsync("phon");
        await Expect(Page.Locator("#last-search")).ToHaveTextAsync("phon");
        await Expect(Panel("npn-search").Locator(".bit-npn-lvr")).ToHaveTextAsync("1 found");
        await Expect(Panel("npn-search").Locator(".bit-npn-lvr")).ToHaveAttributeAsync("role", "status");

        // The data and the description match too; a matched group keeps its children.
        await search.FillAsync("catalog");
        await Expect(Item("npn-search", "Products")).ToBeVisibleAsync();
        await search.FillAsync("start page");
        await Expect(Item("npn-search", "Home")).ToBeVisibleAsync();
        await Expect(Item("npn-search", "Products")).ToHaveCountAsync(0);

        await search.FillAsync("zzz");
        await Expect(Panel("npn-search")).ToContainTextAsync("There is no item found.");
        await Expect(Panel("npn-search").Locator(".bit-npn-lvr")).ToHaveTextAsync("0 found");

        await search.FillAsync("");
        await Expect(Item("npn-search", "Home")).ToBeVisibleAsync();
        await Expect(Item("npn-search", "Settings")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task SearchText_SetByThePage_FiltersTheItems()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-search");

        await Page.Locator("#btn-set-search").ClickAsync();
        await Expect(Search("npn-search")).ToHaveValueAsync("set");
        await Expect(Item("npn-search", "Settings")).ToBeVisibleAsync();
        await Expect(Item("npn-search", "Home")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task SearchFilter_ReplacesTheMatchingAndEmptyListTemplateTheMessage()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-search-filter");

        var search = Search("npn-search-filter");
        await search.FillAsync("se");
        await Expect(Item("npn-search-filter", "Settings")).ToBeVisibleAsync();

        // "ings" is in Settings but not at its start.
        await search.FillAsync("ings");
        await Expect(Panel("npn-search-filter").Locator(".npn-empty-tpl")).ToHaveTextAsync("Nothing here");
        await Expect(Item("npn-search-filter", "Settings")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Search_Escape_ClearsTheSearchFirst()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-search-filter");

        var search = Search("npn-search-filter");
        await search.FillAsync("se");
        await Expect(Item("npn-search-filter", "Home")).ToHaveCountAsync(0);
        await search.PressAsync("Escape");
        await Expect(search).ToHaveValueAsync("");
        await Expect(Item("npn-search-filter", "Home")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task NoSearchBox_RemovesIt()
    {
        await OpenAsync(Route);

        await Expect(Search("npn-nosearch")).ToHaveCountAsync(0);
        await Expect(Item("npn-nosearch", "Home")).ToBeVisibleAsync();
    }

    // ---------- selection ----------

    [TestMethod]
    public async Task ManualSelection_TheClickedItemIsSelectedAndBoundBothWays()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-manual");

        await Expect(Item("npn-manual", "Home")).ToHaveAttributeAsync("aria-current", "page");
        await Item("npn-manual", "Settings").ClickAsync();
        await Expect(Page.Locator("#selected")).ToHaveTextAsync("Settings");
        await Expect(Item("npn-manual", "Settings")).ToHaveAttributeAsync("aria-current", "page");
        await Expect(Log).ToHaveTextAsync("select:Settings");

        // Reselectable reports the click on the selected item again.
        await Item("npn-manual", "Settings").ClickAsync();
        await Expect(Log).ToHaveTextAsync("select:Settings,select:Settings");

        await Item("npn-manual", "Home").ClickAsync();
        await Expect(Page.Locator("#selected")).ToHaveTextAsync("Home");
        await Page.Locator("#btn-select-settings").ClickAsync();
        await Expect(Item("npn-manual", "Settings")).ToHaveAttributeAsync("aria-current", "page");
    }

    // ---------- expansion ----------

    [TestMethod]
    public async Task Expansion_AllExpandedSingleExpandAndNoCollapse()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-allexpanded");

        await Expect(Item("npn-allexpanded", "Phones")).ToBeVisibleAsync();
        await Expect(Item("npn-allexpanded", "Contact us")).ToBeVisibleAsync();

        await Item("npn-single", "Products").ClickAsync();
        await Expect(Item("npn-single", "Phones")).ToBeVisibleAsync();
        await Item("npn-single", "Support").ClickAsync();
        await Expect(Item("npn-single", "Contact us")).ToBeVisibleAsync();
        await Expect(Item("npn-single", "Phones")).ToHaveCountAsync(0);

        await Expect(Item("npn-nocollapse", "Phones")).ToBeVisibleAsync();
        await Expect(Panel("npn-nocollapse").Locator(".bit-nav-cbt")).ToHaveCountAsync(0);
    }

    // ---------- custom items & templates ----------

    [TestMethod]
    public async Task CustomItems_TheNameSelectors_MapTheItems()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-custom");

        await Expect(Item("npn-custom", "The home")).ToContainTextAsync("Custom home");
        await Expect(Item("npn-custom", "The home").Locator(".bit-nav-iic")).ToHaveClassAsync(HasClass("bit-icon--Home"));
        await Panel("npn-custom").GetByText("Custom group").ClickAsync();
        await Expect(Panel("npn-custom").GetByText("Custom child")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Templates_HeaderFooterItemAndSuffix()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-templates");

        var panel = Panel("npn-templates");
        await Expect(panel.Locator(".npn-custom-header")).ToContainTextAsync("Custom header");
        await Expect(panel.Locator(".bit-npn-hdr")).ToHaveCountAsync(0);
        await Expect(panel.Locator(".npn-custom-footer")).ToHaveTextAsync("Logout");
        await Expect(panel.Locator(".npn-item-tpl").First).ToHaveTextAsync("Home!");

        // A custom header calls Toggle itself.
        await Page.Locator("#btn-custom-toggle").ClickAsync();
        await Expect(panel).ToHaveClassAsync(HasClass("bit-npn-tgl"));

        await Expect(Panel("npn-suffix").Locator(".npn-suffix").First).ToHaveTextAsync("*");
    }

    // ---------- events ----------

    [TestMethod]
    public async Task Events_OnItemClickAndOnItemToggle()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-events");

        await Item("npn-events", "Products").ClickAsync();
        await Expect(Log).ToHaveTextAsync(new Regex("toggle:Products"));
        await Item("npn-events", "Phones").ClickAsync();
        await Expect(Log).ToHaveTextAsync(new Regex("click:Phones$"));
        await Item("npn-events", "Home").ClickAsync();
        await Expect(Log).ToHaveTextAsync(new Regex("click:Home$"));
    }

    // ---------- public APIs ----------

    [TestMethod]
    public async Task Apis_ExpandCollapseToggleAndFocusTheItems()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-api");

        await Page.Locator("#btn-api-expand-all").ClickAsync();
        await Expect(Item("npn-api", "Phones")).ToBeVisibleAsync();
        await Expect(Item("npn-api", "Contact us")).ToBeVisibleAsync();
        await Page.Locator("#btn-api-collapse-all").ClickAsync();
        await Expect(Item("npn-api", "Phones")).ToHaveCountAsync(0);

        await Page.Locator("#btn-api-expand-item").ClickAsync();
        await Expect(Item("npn-api", "Phones")).ToBeVisibleAsync();
        await Page.Locator("#btn-api-read").ClickAsync();
        await Expect(Page.Locator("#api-state")).ToHaveTextAsync("True");
        await Page.Locator("#btn-api-collapse-item").ClickAsync();
        await Expect(Item("npn-api", "Phones")).ToHaveCountAsync(0);
        await Page.Locator("#btn-api-toggle-item").ClickAsync();
        await Expect(Item("npn-api", "Phones")).ToBeVisibleAsync();
        await Page.Locator("#btn-api-read").ClickAsync();
        await Expect(Page.Locator("#api-state")).ToHaveTextAsync("True");

        await Page.Locator("#btn-api-focus-item").ClickAsync();
        await Expect(Item("npn-api", "Support")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Apis_FocusSearchBoxOpensTheRailAndClearSearchEmptiesIt()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-api");

        await Page.Locator("#btn-api-toggle").ClickAsync();
        await Expect(Panel("npn-api")).ToHaveClassAsync(HasClass("bit-npn-tgl"));
        await Page.Locator("#btn-api-focus-search").ClickAsync();
        await Expect(Panel("npn-api")).Not.ToHaveClassAsync(HasClass("bit-npn-tgl"));
        await Expect(Search("npn-api")).ToBeFocusedAsync();

        await Search("npn-api").FillAsync("set");
        await Expect(Item("npn-api", "Home")).ToHaveCountAsync(0);
        await Page.Locator("#btn-api-clear-search").ClickAsync();
        await Expect(Search("npn-api")).ToHaveValueAsync("");
        await Expect(Item("npn-api", "Home")).ToBeVisibleAsync();
    }

    // ---------- drawer on a wide screen ----------

    [TestMethod]
    public async Task Drawer_Always_IsAModalDrawerWithAnOverlayThatTrapsAndRestoresTheFocus()
    {
        await OpenAsync(Route);

        var panel = Panel("npn-drawer");
        await Expect(panel).ToHaveClassAsync(HasClass("bit-npn-drw"));
        await Expect(panel).ToHaveClassAsync(HasClass("bit-npn-cls"));
        await Expect(panel).ToBeHiddenAsync();

        var opener = Page.Locator("#btn-open-drawer");
        await opener.ClickAsync();
        await Expect(Page.Locator("#drawer-open")).ToHaveTextAsync("True");
        await Expect(panel).ToBeVisibleAsync();
        await Expect(panel).ToHaveCSSAsync("position", "fixed");
        await Expect(panel).ToHaveAttributeAsync("role", "dialog");
        await Expect(panel).ToHaveAttributeAsync("aria-modal", "true");
        await Expect(panel).ToHaveAttributeAsync("aria-label", new Regex(".+"));
        await Expect(Overlay("npn-drawer")).ToBeVisibleAsync();
        await Expect(Overlay("npn-drawer")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(panel.Locator(".bit-npn-tbn")).ToBeHiddenAsync();

        // The focus is taken into the drawer and held there.
        await Page.WaitForFunctionAsync("p => p.contains(document.activeElement)", await panel.ElementHandleAsync());
        for (var i = 0; i < 8; i++)
        {
            await Page.Keyboard.PressAsync("Tab");
            Assert.IsTrue(await panel.EvaluateAsync<bool>("p => p.contains(document.activeElement)"), $"Tab {i} left the drawer.");
        }

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#drawer-open")).ToHaveTextAsync("False");
        await Expect(panel).ToBeHiddenAsync();
        await Expect(opener).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Drawer_TheOverlayAndTheCloseButton_CloseIt()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-open-drawer").ClickAsync();
        await Expect(Panel("npn-drawer")).ToBeVisibleAsync();
        await Overlay("npn-drawer").ClickAsync(new() { Position = new() { X = 1200, Y = 300 } });
        await Expect(Page.Locator("#drawer-open")).ToHaveTextAsync("False");

        await Page.Locator("#btn-open-drawer").ClickAsync();
        var close = Panel("npn-drawer").Locator(".bit-npn-cbn");
        await Expect(close).ToBeVisibleAsync();
        await Expect(close).ToHaveAttributeAsync("aria-label", "Shut the menu");
        await Expect(close.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--ChromeClose"));
        await close.ClickAsync();
        await Expect(Page.Locator("#drawer-open")).ToHaveTextAsync("False");

        await Page.Locator("#btn-api-open").ClickAsync();
        await Expect(Page.Locator("#drawer-open")).ToHaveTextAsync("True");
        await Page.Locator("#btn-api-close").DispatchEventAsync("click");
        await Expect(Page.Locator("#drawer-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Drawer_ClickingAnItem_NavigatesAndClosesIt()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-open-drawer").ClickAsync();
        await Item("npn-drawer", "Settings").ClickAsync();
        await Expect(Page.Locator("#npn-section")).ToHaveTextAsync("settings");
        await Expect(Page.Locator("#drawer-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Drawer_PlacementEnd_ComesFromTheRightAndAutoFocusLandsOnTheSearchBox()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-open-end").ClickAsync();
        var panel = Panel("npn-drawer-end");
        await Expect(panel).ToHaveClassAsync(HasClass("bit-npn-end"));
        await Expect(panel).ToBeVisibleAsync();
        await Expect(Search("npn-drawer-end")).ToBeFocusedAsync();
        // Docked to the right edge once it has slid in.
        await Page.WaitForFunctionAsync("e => Math.abs(e.getBoundingClientRect().right - innerWidth) < 2", await panel.ElementHandleAsync());
    }

    [TestMethod]
    public async Task Drawer_NoOverlay_IsNotModalAndStaysOpenOnNavigation()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-open-nonmodal").ClickAsync();
        var panel = Panel("npn-drawer-nonmodal");
        await Expect(panel).ToBeVisibleAsync();
        await Expect(Overlay("npn-drawer-nonmodal")).ToHaveCountAsync(0);
        await Expect(panel).Not.ToHaveAttributeAsync("aria-modal", new Regex(".*"));

        await Item("npn-drawer-nonmodal", "Home").ClickAsync();
        await Expect(Page.Locator("#npn-section")).ToHaveTextAsync("home");
        await Expect(Page.Locator("#nonmodal-open")).ToHaveTextAsync("True");
        await Expect(panel).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task DrawerBreakpointNever_IsAColumnEvenWhereADrawerWouldBe()
    {
        await Page.SetViewportSizeAsync(400, 800);
        await OpenAsync(Route);

        await Expect(Panel("npn-never")).ToHaveClassAsync(HasClass("bit-npn-col"));
        await Expect(Panel("npn-basic")).ToHaveClassAsync(HasClass("bit-npn-drw"));
        await ShowAsync("npn-never");
        await Expect(Panel("npn-never")).ToBeVisibleAsync();
        await Expect(Panel("npn-basic")).ToBeHiddenAsync();
    }

    // ---------- sticky ends ----------

    [TestMethod]
    public async Task StickyEnds_OnlyTheItemsScroll()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-sticky");

        var panel = Panel("npn-sticky");
        await Expect(panel).ToHaveClassAsync(HasClass("bit-npn-ste"));
        var footer = panel.Locator(".npn-sticky-footer");
        await Expect(footer).ToBeVisibleAsync();
        var panelBox = await panel.BoundingBoxAsync();
        var footerBox = await footer.BoundingBoxAsync();
        Assert.IsTrue(footerBox!.Y + footerBox.Height <= panelBox!.Y + panelBox.Height + 1, "The footer is pinned inside the panel.");

        var nav = panel.Locator("nav.bit-nav");
        Assert.IsTrue(await nav.EvaluateAsync<bool>("n => n.scrollHeight > n.clientHeight"), "The items scroll between the ends.");
        await nav.EvaluateAsync("n => n.scrollTop = 200");
        await Expect(Search("npn-sticky")).ToBeInViewportAsync();
    }

    // ---------- cascading ----------

    [TestMethod]
    public async Task Cascading_BitParams_SetTheDefaultsAndThePanelsOwnWin()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-cas");

        await Expect(Search("npn-cas")).ToHaveAttributeAsync("placeholder", "Cascaded placeholder");
        await Expect(ToggleButton("npn-cas")).ToHaveCountAsync(0);
        await Expect(Panel("npn-cas")).ToHaveClassAsync(HasClass("bit-nav-asuc"));
        await Expect(Search("npn-cas-own")).ToHaveAttributeAsync("placeholder", "Own placeholder");
    }

    // ---------- color, icons, size ----------

    [DataTestMethod]
    [DataRow("primary", "apri", "--bit-clr-pri")]
    [DataRow("secondary", "asec", "--bit-clr-sec")]
    [DataRow("tertiary", "ater", "--bit-clr-ter")]
    [DataRow("info", "ainf", "--bit-clr-inf")]
    [DataRow("success", "asuc", "--bit-clr-suc")]
    [DataRow("warning", "awrn", "--bit-clr-wrn")]
    [DataRow("severewarning", "aswr", "--bit-clr-swr")]
    [DataRow("error", "aerr", "--bit-clr-err")]
    [DataRow("primarybackground", "apbg", "--bit-clr-bg-pri")]
    [DataRow("secondarybackground", "asbg", "--bit-clr-bg-sec")]
    [DataRow("tertiarybackground", "atbg", "--bit-clr-bg-ter")]
    [DataRow("primaryforeground", "apfg", "--bit-clr-fg-pri")]
    [DataRow("secondaryforeground", "asfg", "--bit-clr-fg-sec")]
    [DataRow("tertiaryforeground", "atfg", "--bit-clr-fg-ter")]
    [DataRow("primaryborder", "apbr", "--bit-clr-brd-pri")]
    [DataRow("secondaryborder", "asbr", "--bit-clr-brd-sec")]
    [DataRow("tertiaryborder", "atbr", "--bit-clr-brd-ter")]
    public async Task Accent_EachColor_PaintsThePanel(string name, string suffix, string token)
    {
        await OpenAsync(Route);
        var id = $"npn-accent-{name}";
        await ShowAsync(id);

        await Expect(Panel(id)).ToHaveClassAsync(HasClass($"bit-nav-{suffix}"));
        // The accent fills the content of the panel and its selected item.
        await Expect(Panel(id).Locator(".bit-npn-cnt")).ToHaveCSSAsync("background-color", await ResolveColorAsync($"var({token})"));
        await Expect(Item(id, "Home")).ToHaveAttributeAsync("aria-current", "page");
    }

    [TestMethod]
    public async Task Color_PaintsTheNavAndTheButtons()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-color");

        await Expect(ToggleButton("npn-color")).ToHaveClassAsync(HasClass("bit-btn-err"));
        await Expect(Panel("npn-color").Locator(".bit-srb")).ToHaveClassAsync(HasClass("bit-srb-err"));
        await Expect(Panel("npn-color").Locator("nav.bit-nav")).ToHaveClassAsync(HasClass("bit-nav-err"));
    }

    [TestMethod]
    public async Task ExternalIcons_TheToggleSearchAndChevronIcons()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-icons");

        await Expect(ToggleButton("npn-icons").Locator("i")).ToHaveClassAsync(HasClass("bi-list"));
        await Expect(Panel("npn-icons").Locator(".bit-npn-tsb i")).ToHaveClassAsync(HasClass("bi-search"));
        await Expect(Panel("npn-icons-names").Locator(".bit-npn-tsb i")).ToHaveClassAsync(HasClass("bit-icon--Zoom"));

        await Page.Locator("#btn-icons-toggle").ClickAsync();
        await Expect(Panel("npn-icons").Locator(".bit-nav-cbt i").First).ToHaveClassAsync(HasClass("bi-chevron-down"));
    }

    [TestMethod]
    public async Task Size_ScalesTheItems()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-size-small");

        var small = (await Item("npn-size-small", "Home").BoundingBoxAsync())!.Height;
        var medium = (await Item("npn-size-medium", "Home").BoundingBoxAsync())!.Height;
        var large = (await Item("npn-size-large", "Home").BoundingBoxAsync())!.Height;
        Assert.IsTrue(small < medium && medium < large, $"{small} < {medium} < {large}");
        await Expect(Panel("npn-size-small").Locator("nav.bit-nav")).ToHaveClassAsync(HasClass("bit-nav-sm"));
    }

    // ---------- style ----------

    [TestMethod]
    public async Task StylesAndClasses_EveryPart_TakesItsOwn()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-styled");

        var panel = Panel("npn-styled");
        await Expect(panel).ToHaveClassAsync(HasClass("npn-root-class"));
        await Expect(panel).ToHaveClassAsync(HasClass("c-root"));
        await Expect(panel).ToHaveCSSAsync("border-top-width", "2px");
        await Expect(panel).ToHaveCSSAsync("padding-top", "3px");
        await Expect(panel.Locator(".bit-npn-cnt")).ToHaveClassAsync(HasClass("c-container"));
        await Expect(panel.Locator(".bit-npn-cnt")).ToHaveCSSAsync("padding-left", "4px");
        await Expect(panel.Locator(".bit-npn-hdr")).ToHaveClassAsync(HasClass("c-header"));
        await Expect(panel.Locator(".bit-npn-hdr")).ToHaveCSSAsync("padding-right", "5px");
        await Expect(panel.Locator(".bit-npn-img")).ToHaveClassAsync(HasClass("c-hicon"));
        await Expect(panel.Locator(".bit-npn-img")).ToHaveCSSAsync("opacity", "0.5");
        await Expect(panel.Locator(".bit-npn-htx")).ToHaveClassAsync(HasClass("c-htext"));
        await Expect(panel.Locator(".bit-npn-htx")).ToHaveCSSAsync("color", "rgb(10, 20, 30)");
        await Expect(ToggleButton("npn-styled")).ToHaveClassAsync(HasClass("c-toggle"));
        await Expect(ToggleButton("npn-styled")).ToHaveCSSAsync("margin-left", "6px");
        await Expect(panel.Locator(".bit-npn-cbn")).ToHaveClassAsync(HasClass("c-close"));
        await Expect(panel.Locator(".bit-srb")).ToHaveClassAsync(HasClass("c-search"));
        await Expect(panel.Locator(".bit-srb")).ToHaveClassAsync(HasClass("c-sb-root"));
        await Expect(panel.Locator(".bit-srb")).ToHaveCSSAsync("margin-top", "7px");
        await Expect(panel.Locator("nav.bit-nav")).ToHaveClassAsync(HasClass("c-nav"));
        await Expect(panel.Locator("nav.bit-nav")).ToHaveCSSAsync("margin-bottom", "9px");
        await Expect(Item("npn-styled", "Home")).ToHaveClassAsync(HasClass("c-nav-item"));
        await Expect(Item("npn-styled", "Home")).ToHaveCSSAsync("letter-spacing", "1px");

        // A column hides its drawer-only close button.
        await Expect(panel.Locator(".bit-npn-cbn")).ToBeHiddenAsync();

        await Search("npn-styled").FillAsync("zzz");
        await Expect(panel.Locator(".c-empty")).ToHaveCSSAsync("color", "rgb(40, 50, 60)");

        await Page.Locator("#btn-styled-toggle").ClickAsync();
        await Expect(panel).ToHaveClassAsync(HasClass("c-toggled"));
        await Expect(panel).ToHaveCSSAsync("outline-color", "rgb(9, 9, 9)");
        await Expect(panel.Locator(".bit-npn-tsb")).ToHaveClassAsync(HasClass("c-tsearch"));
        await Expect(panel.Locator(".bit-npn-tsb")).ToHaveCSSAsync("margin-top", "8px");
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_RestyleThePanelTheRailAndTheDrawer()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-vars");

        var panel = Panel("npn-vars");
        await Expect(panel).ToHaveCSSAsync("background-color", "rgb(250, 240, 230)");
        await Expect(panel).ToHaveCSSAsync("width", "210px");
        await Expect(panel).ToHaveCSSAsync("padding-top", "11px");
        await Expect(panel).ToHaveCSSAsync("border-top-right-radius", "19px");
        await Expect(panel).ToHaveCSSAsync("border-right-width", "3px");
        await Expect(panel).ToHaveCSSAsync("border-right-color", "rgb(0, 0, 255)");
        await Expect(panel).ToHaveCSSAsync("box-shadow", "rgb(0, 0, 0) 1px 1px 2px 0px");
        await Expect(panel.Locator(".bit-npn-cnt")).ToHaveCSSAsync("padding-top", "13px");
        await Expect(panel.Locator(".bit-npn-cnt")).ToHaveCSSAsync("row-gap", "17px");
        await Expect(panel.Locator(".bit-npn-img")).ToHaveCSSAsync("max-width", "21px");

        await Page.Locator("#btn-vars-toggle").ClickAsync();
        await Expect(panel).ToHaveCSSAsync("max-width", "77px");

        await Page.Locator("#btn-vars-drawer").ClickAsync();
        var drawer = Panel("npn-vars-drawer");
        await Expect(drawer).ToBeVisibleAsync();
        await Expect(drawer).ToHaveCSSAsync("box-shadow", "rgb(255, 0, 0) 2px 2px 3px 0px");
        await Expect(Overlay("npn-vars-drawer")).ToHaveCSSAsync("background-color", "rgba(0, 128, 0, 0.5)");
        await Expect(Overlay("npn-vars-drawer")).ToHaveCSSAsync("backdrop-filter", "blur(2px)");
    }

    [TestMethod]
    public async Task NoPadAndTop_RemoveTheInsetAndOffsetThePanel()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-nopad");

        await Expect(Panel("npn-nopad")).ToHaveClassAsync(HasClass("bit-npn-npd"));
        await Expect(Panel("npn-nopad")).ToHaveCSSAsync("padding-top", "0px");
        await Expect(Panel("npn-nopad")).ToHaveCSSAsync("top", "40px");
    }

    // ---------- RTL ----------

    [TestMethod]
    public async Task Rtl_TheColumnAndTheDrawerMirror()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-rtl");

        await Expect(Panel("npn-rtl")).ToHaveAttributeAsync("dir", "rtl");
        var icon = await Item("npn-rtl", "Home").Locator(".bit-nav-iic").BoundingBoxAsync();
        var text = await Item("npn-rtl", "Home").Locator(".bit-nav-itx").BoundingBoxAsync();
        Assert.IsTrue(icon!.X > text!.X, "The icon leads on the right.");

        await Page.Locator("#btn-open-rtl-drawer").ClickAsync();
        var drawer = Panel("npn-rtl-drawer");
        await Expect(drawer).ToBeVisibleAsync();
        // An RTL drawer comes from the right.
        await Page.WaitForFunctionAsync("e => Math.abs(e.getBoundingClientRect().right - innerWidth) < 2", await drawer.ElementHandleAsync());
    }

    // ---------- disabled ----------

    [TestMethod]
    public async Task Disabled_ThePanel_TurnsItsControlsOff()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-disabled");

        var panel = Panel("npn-disabled");
        await Expect(panel).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(ToggleButton("npn-disabled")).ToBeDisabledAsync();
        await Expect(ToggleButton("npn-disabled")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Search("npn-disabled")).ToBeDisabledAsync();
        await Expect(Item("npn-disabled", "Home")).ToHaveAttributeAsync("aria-disabled", "true");
        await ToggleButton("npn-disabled").DispatchEventAsync("click");
        await Expect(panel).Not.ToHaveClassAsync(HasClass("bit-npn-tgl"));
    }

    // ---------- lifecycle ----------

    [TestMethod]
    public async Task Prerender_ThePagesHtml_ContainsThePanel()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}/page/settings");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Matches(html, new Regex(@"<div id=""npn-basic""[^>]*class=""[^""]*\bbit-npn\b"));
        StringAssert.Matches(html, new Regex(@"href=""/e2e/nav-panel/page/settings""[^>]*aria-current=""page""|aria-current=""page""[^>]*href=""/e2e/nav-panel/page/settings"""));
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Unmount_ThePanel_LeavesNoErrorBehind()
    {
        await OpenAsync(Route);
        await ShowAsync("npn-mounted");

        await Page.Locator("#btn-unmount").ClickAsync();
        await Expect(Panel("npn-mounted")).ToHaveCountAsync(0);
        await Page.Locator("#btn-unmount").ClickAsync();
        await Expect(Panel("npn-mounted")).ToBeVisibleAsync();

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }
}
