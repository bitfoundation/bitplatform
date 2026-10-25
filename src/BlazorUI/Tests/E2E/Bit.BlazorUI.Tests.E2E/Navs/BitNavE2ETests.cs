using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Navs;

/// <summary>
/// End-to-end tests of BitNav with the BitNavItem API (/e2e/nav): the rendered tree of links and buttons, expanding and
/// collapsing by pointer and keyboard (arrows, Home/End, the asterisk, type-ahead, Enter/Space on the chevron), the
/// Grouped render type and separators, the manual mode with DefaultSelectedItem and a two-way and a one-way bound
/// SelectedItem, IconOnly, SingleExpand, AllExpanded, NoCollapse, ReversedChevron and the indentation parameters, every
/// template and its render modes, the public API, the events and Reselectable, the accessibility attributes, the
/// BitParams cascade, badges and dots, every Color and Accent, external icons, every Size, Style/Class/Styles/Classes, the
/// public CSS variables, Visibility, HtmlAttributes, RTL (explicit and inherited), the disabled contract, dynamic items,
/// the lifecycle (prerender, re-render, unmount) and reduced motion.
/// </summary>
[TestClass]
public class BitNavE2ETests : E2ETestBase
{
    private const string PAGE = "/e2e/nav";

    // The Color switch of BitNav.razor.cs, in the order of BitColor.
    private static readonly (string Name, string Suffix)[] Colors =
    [
        ("primary", "pri"),
        ("secondary", "sec"),
        ("tertiary", "ter"),
        ("info", "inf"),
        ("success", "suc"),
        ("warning", "wrn"),
        ("severe-warning", "swr"),
        ("error", "err"),
        ("primary-background", "pbg"),
        ("secondary-background", "sbg"),
        ("tertiary-background", "tbg"),
        ("primary-foreground", "pfg"),
        ("secondary-foreground", "sfg"),
        ("tertiary-foreground", "tfg"),
        ("primary-border", "pbr"),
        ("secondary-border", "sbr"),
        ("tertiary-border", "tbr"),
    ];

    private static Regex Exact(string text) => new($"^{Regex.Escape(text)}$");

    private ILocator Nav(string id) => Page.Locator($"#{id}");

    // The anchor (or button) of the item whose text is exactly the given one.
    private ILocator Item(string navId, string text)
    {
        return Page.Locator($"#{navId} .bit-nav-ict").Filter(new() { Has = Page.Locator(".bit-nav-itx", new() { HasTextRegex = Exact(text) }) });
    }

    private ILocator Chevron(string navId, string text) => Item(navId, text).Locator(".bit-nav-cbt");

    private Task<string> FocusedTextAsync()
    {
        return Page.EvaluateAsync<string>("() => document.activeElement?.querySelector('.bit-nav-itx')?.textContent ?? document.activeElement?.textContent ?? ''");
    }

    // Resolves a color custom property the element carries (var() references substituted) to the rgb() a color
    // property painted with it computes to.
    private Task<string> ResolveColorVarAsync(string selector, string property)
    {
        return Page.Locator(selector).First.EvaluateAsync<string>(@"(el, prop) => {
            const value = getComputedStyle(el).getPropertyValue(prop).trim();
            const probe = document.createElement('span');
            probe.style.color = value;
            document.body.appendChild(probe);
            const color = getComputedStyle(probe).color;
            probe.remove();
            return color;
        }", property);
    }

    private Task<string> ResolveColorAsync(string cssColor)
    {
        return Page.EvaluateAsync<string>(@"value => {
            const probe = document.createElement('span');
            probe.style.color = value;
            document.body.appendChild(probe);
            const color = getComputedStyle(probe).color;
            probe.remove();
            return color;
        }", cssColor);
    }

    private Task<double> NumberStyleAsync(ILocator locator, string property)
    {
        return locator.EvaluateAsync<double>($"el => parseFloat(getComputedStyle(el).getPropertyValue('{property}'))");
    }

    private Task<LocatorBoundingBoxResult> BoxAsync(ILocator locator) => locator.BoundingBoxAsync().ContinueWith(t => t.Result!);

    // ---- Basic ----

    [TestMethod]
    public async Task Basic_Prerender_ContainsTheNavBeforeTheCircuitConnects()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{PAGE}");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "id=\"nav-basic\"");
        StringAssert.Contains(html, "aria-label=\"Basic nav\"");
        StringAssert.Contains(html, "href=\"/e2e/nav/url/home\"");
        StringAssert.Contains(html, "Status: <span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Basic_Renders_ANavLandmarkOfListsLinksAndButtons()
    {
        await OpenAsync(PAGE);

        var nav = Nav("nav-basic");
        await Expect(nav).ToHaveJSPropertyAsync("tagName", "NAV");
        await Expect(nav).ToHaveAttributeAsync("aria-label", "Basic nav");
        await Expect(nav).ToHaveClassAsync(HasClass("bit-nav"));
        await Expect(nav.Locator("> ul.bit-nav-lst")).ToHaveAttributeAsync("role", "list");
        await Expect(nav.Locator("> ul > li[role=listitem]")).ToHaveCountAsync(8);

        var home = Item("nav-basic", "Home");
        await Expect(home).ToHaveJSPropertyAsync("tagName", "A");
        await Expect(home).ToHaveAttributeAsync("href", "/e2e/nav/url/home");
        await Expect(home).ToHaveAttributeAsync("title", "Home");
        await Expect(home.Locator("i.bit-nav-iic")).ToHaveClassAsync(HasClass("bit-icon--Home"));

        // The description is announced as the description it is.
        var description = home.Locator(".bit-nav-des");
        await Expect(description).ToHaveTextAsync("Start here");
        var descriptionId = await description.GetAttributeAsync("id");
        await Expect(home).ToHaveAttributeAsync("aria-describedby", descriptionId!);
        await Expect(home).ToHaveAttributeAsync("aria-label", "Home");

        // An item without a URL is a button, and a parent reports its state.
        var docs = Item("nav-basic", "Docs");
        await Expect(docs).ToHaveJSPropertyAsync("tagName", "BUTTON");
        await Expect(docs).ToHaveAttributeAsync("type", "button");
        await Expect(docs).ToHaveAttributeAsync("aria-expanded", "false");

        // A leaf reports no expansion state at all.
        await Expect(home).Not.ToHaveAttributeAsync("aria-expanded", new Regex(".*"));

        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    [TestMethod]
    public async Task Basic_ClickingAParentWithoutUrl_TogglesItsChildren()
    {
        await OpenAsync(PAGE);

        var docs = Item("nav-basic", "Docs");
        var chevronIcon = docs.Locator(".bit-nav-cbt i");

        await Expect(Item("nav-basic", "Intro")).ToHaveCountAsync(0);
        await Expect(chevronIcon).ToHaveClassAsync(HasClass("bit-ico-r90"));
        await Expect(chevronIcon).ToHaveCSSAsync("transform", "matrix(0, 1, -1, 0, 0, 0)");

        await docs.ClickAsync();

        await Expect(docs).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Item("nav-basic", "Intro")).ToBeVisibleAsync();
        await Expect(Item("nav-basic", "Guide")).ToBeVisibleAsync();
        await Expect(chevronIcon).ToHaveClassAsync(HasClass("bit-nav-exp"));
        await Expect(chevronIcon).ToHaveCSSAsync("transform", "matrix(0, -1, 1, 0, 0, 0)");

        // A nested branch opens on its own.
        await Item("nav-basic", "Guide").ClickAsync();
        await Expect(Item("nav-basic", "Step one")).ToBeVisibleAsync();

        // The child is indented one level deeper than its parent.
        var parentPadding = await NumberStyleAsync(Item("nav-basic", "Guide"), "padding-inline-start");
        var childPadding = await NumberStyleAsync(Item("nav-basic", "Step one"), "padding-inline-start");
        Assert.AreEqual(16 + 27, childPadding - parentPadding + 0, 0.5, "A childless item at depth 2 is indented by 2 x 16 + 27 against the 16 of its parent.");

        await docs.ClickAsync();

        await Expect(docs).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Item("nav-basic", "Intro")).ToHaveCountAsync(0);
        Assert.IsTrue(Page.Url.EndsWith(PAGE, StringComparison.Ordinal), $"A parent without a URL navigates nowhere ({Page.Url}).");
    }

    [TestMethod]
    public async Task Basic_TheChevronOfALinkedParent_TogglesWithoutNavigating()
    {
        await OpenAsync(PAGE);

        var reports = Item("nav-basic", "Reports");
        var chevron = Chevron("nav-basic", "Reports");

        await Expect(reports).ToHaveJSPropertyAsync("tagName", "A");
        await Expect(chevron).ToHaveAttributeAsync("role", "button");
        await Expect(chevron).ToHaveAttributeAsync("tabindex", "0");
        await Expect(chevron).ToHaveAttributeAsync("aria-label", "Expand reports");
        await Expect(chevron).ToHaveAttributeAsync("aria-expanded", "false");

        await chevron.ClickAsync();

        await Expect(chevron).ToHaveAttributeAsync("aria-label", "Collapse reports");
        await Expect(chevron).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(reports).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Item("nav-basic", "Monthly")).ToBeVisibleAsync();
        Assert.IsTrue(Page.Url.EndsWith(PAGE, StringComparison.Ordinal), $"The chevron click must not follow the link ({Page.Url}).");

        // Enter and Space on the focused chevron toggle it, and neither follows the link around it.
        await chevron.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(chevron).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Item("nav-basic", "Monthly")).ToHaveCountAsync(0);

        await Page.Keyboard.PressAsync(" ");
        await Expect(chevron).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Item("nav-basic", "Yearly")).ToBeVisibleAsync();

        await NextFramesAsync();
        Assert.IsTrue(Page.Url.EndsWith(PAGE, StringComparison.Ordinal), $"Enter and Space on the chevron must not follow the link ({Page.Url}).");
    }

    [TestMethod]
    public async Task Basic_DisabledItems_AreNeitherLinksNorFocusable()
    {
        await OpenAsync(PAGE);

        var archived = Item("nav-basic", "Archived");
        await Expect(archived).ToHaveJSPropertyAsync("tagName", "A");
        await Expect(archived).Not.ToHaveAttributeAsync("href", new Regex(".*"));
        await Expect(archived).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(archived).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(archived).ToHaveClassAsync(HasClass("bit-nav-dis"));
        await Expect(archived).ToHaveCSSAsync("pointer-events", "none");

        var locked = Item("nav-basic", "Locked");
        await Expect(locked).ToBeDisabledAsync();
        await Expect(locked).ToHaveAttributeAsync("aria-disabled", "true");

        // A disabled item is drawn in the disabled color, not the item color.
        var enabledColor = await ComputedStyleAsync("#nav-basic .bit-nav-ict:not(.bit-nav-dis) .bit-nav-itm", "color");
        var disabledColor = await archived.Locator(".bit-nav-itm").EvaluateAsync<string>("el => getComputedStyle(el).color");
        Assert.AreNotEqual(enabledColor, disabledColor);

        // A click dispatched straight at it (the way a screen reader activates) goes nowhere.
        await archived.DispatchEventAsync("click");
        await NextFramesAsync();
        Assert.IsTrue(Page.Url.EndsWith(PAGE, StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Basic_TabStepsOverTheDisabledItems()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit's default keyboard navigation leaves links out of the Tab order, so Tab cannot reach the linked item after the disabled ones.");

        await OpenAsync(PAGE);

        await Item("nav-basic", "Docs").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");

        Assert.AreEqual("Reports", await FocusedTextAsync());
    }

    [TestMethod]
    public async Task Basic_ForceAnchor_RendersAnAnchorWithoutHrefInTheTabOrder()
    {
        await OpenAsync(PAGE);

        var anchor = Item("nav-basic", "Anchor");
        await Expect(anchor).ToHaveJSPropertyAsync("tagName", "A");
        await Expect(anchor).Not.ToHaveAttributeAsync("href", new Regex(".*"));
        await Expect(anchor).ToHaveAttributeAsync("tabindex", "0");

        await anchor.FocusAsync();
        await Expect(anchor).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Basic_ANewTabLink_CarriesNoopenerAndAnnouncesItself()
    {
        await OpenAsync(PAGE);

        var external = Item("nav-basic", "External");
        await Expect(external).ToHaveAttributeAsync("target", "_blank");
        await Expect(external).ToHaveAttributeAsync("rel", "noopener");
        await Expect(external).ToHaveAttributeAsync("title", "External site");
        await Expect(external.Locator(".bit-nav-vhd")).ToHaveTextAsync("(opens in a new tab)");

        // The hint is read by assistive technologies and drawn nowhere.
        var box = await external.Locator(".bit-nav-vhd").BoundingBoxAsync();
        Assert.IsTrue(box is null || (box.Width <= 1 && box.Height <= 1), "The new-tab hint is visually hidden.");

        // A rel that already decides the opener relationship is kept as it is.
        await Expect(Item("nav-basic", "Referrerless")).ToHaveAttributeAsync("rel", "noreferrer");
    }

    // ---- FitWidth & FullWidth ----

    [TestMethod]
    public async Task Width_FitWidthShrinksToTheWidestItem_FullWidthFillsTheContainer()
    {
        await OpenAsync(PAGE);

        await Expect(Nav("nav-fit")).ToHaveClassAsync(HasClass("bit-nav-ftw"));
        await Expect(Nav("nav-full")).ToHaveClassAsync(HasClass("bit-nav-flw"));

        var fit = await BoxAsync(Nav("nav-fit"));
        var full = await BoxAsync(Nav("nav-full"));
        var container = await BoxAsync(Page.Locator("#width-container"));

        Assert.AreEqual(container.Width, full.Width, 0.5);
        Assert.IsTrue(fit.Width < container.Width / 2, $"A FitWidth nav ({fit.Width}px) only takes the room of its widest item.");

        var longest = await BoxAsync(Item("nav-fit", "A much longer item text"));
        Assert.AreEqual(fit.Width, longest.Width, 1);

        // FitWidth reserves the room of the chevron at the end of every item as well.
        await Expect(Item("nav-fit", "Short")).ToHaveCSSAsync("padding-inline-end", "27px");
        await Expect(Item("nav-full", "Short")).Not.ToHaveCSSAsync("padding-inline-end", "27px");
    }

    // ---- Grouped & Separator ----

    [TestMethod]
    public async Task Grouped_AHeaderTogglesItsGroup_AndAGroupWithoutChildrenIsALabel()
    {
        await OpenAsync(PAGE);

        var header = Page.Locator("#nav-grouped button.bit-nav-gcb", new() { HasText = "Fruits" });
        await Expect(header).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(header).ToHaveAttributeAsync("title", "Fruit group");
        await Expect(header.Locator(".bit-nav-ghd")).ToHaveTextAsync("Fruits");
        await Expect(Item("nav-grouped", "Apple")).ToHaveCountAsync(0);

        await header.ClickAsync();

        await Expect(header).ToHaveAttributeAsync("aria-expanded", "true");
        var group = Page.Locator("#nav-grouped ul.bit-nav-lst[aria-label='Fruits']");
        await Expect(group).ToBeVisibleAsync();
        await Expect(group.Locator(".bit-nav-ict")).ToHaveCountAsync(2);

        // The header is a neutral surface with a rule under it.
        Assert.AreNotEqual("0px", await header.EvaluateAsync<string>("el => getComputedStyle(el).borderBottomWidth"));

        // A group without children is its group's label: no button, no tab stop.
        var label = Page.Locator("#nav-grouped .bit-nav-gcb", new() { HasText = "Empty group" });
        await Expect(label).ToHaveJSPropertyAsync("tagName", "DIV");
        await Expect(label).ToHaveClassAsync(HasClass("bit-nav-gst"));
        await Expect(label).ToHaveCSSAsync("cursor", "default");

        // A separator draws a rule.
        var separator = Page.Locator("#nav-grouped [role=separator]");
        await Expect(separator).ToHaveClassAsync(HasClass("bit-nav-sep"));
        Assert.AreNotEqual("0px", await separator.EvaluateAsync<string>("el => getComputedStyle(el).borderTopWidth"));

        await header.ClickAsync();
        await Expect(header).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(group).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Separator_TheKeyboardStepsOverIt()
    {
        await OpenAsync(PAGE);

        await Expect(Page.Locator("#nav-separator [role=separator]")).ToHaveCountAsync(1);

        await Item("nav-separator", "Before").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");

        await Expect(Item("nav-separator", "After")).ToBeFocusedAsync();
    }

    // ---- Manual mode ----

    [TestMethod]
    public async Task Manual_DefaultSelectedItem_SelectsAndOpensThePathToIt()
    {
        await OpenAsync(PAGE);

        var cherry = Item("nav-manual-default", "Cherry");
        await Expect(cherry).ToBeVisibleAsync();
        await Expect(cherry).ToHaveClassAsync(HasClass("bit-nav-sel"));
        await Expect(cherry).ToHaveAttributeAsync("aria-current", "page");
        await Expect(Item("nav-manual-default", "Fruits")).ToHaveAttributeAsync("aria-expanded", "true");

        // The selected item is drawn apart: its own fill and the bar along its leading edge.
        var selectedBg = await cherry.EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");
        var otherBg = await Item("nav-manual-default", "Apple").EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");
        Assert.AreNotEqual(otherBg, selectedBg);
        await Expect(cherry.Locator(".bit-nav-itm")).ToHaveCSSAsync("font-weight", "600");
    }

    [TestMethod]
    public async Task Manual_ClickingSelects_AndTheTwoWayBindingFollows()
    {
        await OpenAsync(PAGE);

        await Expect(Page.Locator("#bound-selected")).ToHaveTextAsync("none");

        await Item("nav-manual-bound", "Ice cream").ClickAsync();

        await Expect(Page.Locator("#bound-selected")).ToHaveTextAsync("Ice cream");
        await Expect(Page.Locator("#bound-select-count")).ToHaveTextAsync("1");
        await Expect(Item("nav-manual-bound", "Ice cream")).ToHaveClassAsync(HasClass("bit-nav-sel"));
        await Expect(Item("nav-manual-bound", "Ice cream")).ToHaveAttributeAsync("aria-current", "page");

        // Clicking the selected item again selects nothing new.
        await Item("nav-manual-bound", "Ice cream").ClickAsync();
        await NextFramesAsync();
        await Expect(Page.Locator("#bound-select-count")).ToHaveTextAsync("1");

        // The page writes the binding: the nav selects the item and opens the branch it is in.
        await Page.Locator("#btn-select-carrot").ClickAsync();
        await Expect(Item("nav-manual-bound", "Carrot")).ToBeVisibleAsync();
        await Expect(Item("nav-manual-bound", "Carrot")).ToHaveClassAsync(HasClass("bit-nav-sel"));
        await Expect(Item("nav-manual-bound", "Ice cream")).Not.ToHaveClassAsync(HasClass("bit-nav-sel"));
        await Expect(Page.Locator("#bound-selected")).ToHaveTextAsync("Carrot");

        await Page.Locator("#btn-clear-selection").ClickAsync();
        await Expect(Page.Locator("#nav-manual-bound .bit-nav-sel")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#nav-manual-bound [aria-current]")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Manual_AOneWayBoundSelection_IsHeldWhereThePageSetIt()
    {
        await OpenAsync(PAGE);

        var fruits = Item("nav-manual-oneway", "Fruits");
        await Expect(fruits).ToHaveClassAsync(HasClass("bit-nav-sel"));

        await Item("nav-manual-oneway", "Ice cream").ClickAsync();
        await NextFramesAsync();

        await Expect(Item("nav-manual-oneway", "Ice cream")).Not.ToHaveClassAsync(HasClass("bit-nav-sel"));
        await Expect(fruits).ToHaveClassAsync(HasClass("bit-nav-sel"));
    }

    // ---- IconOnly ----

    [TestMethod]
    public async Task IconOnly_HidesTheTexts_KeepsTheNames_AndFollowsTheParameter()
    {
        await OpenAsync(PAGE);

        var nav = Nav("nav-icononly");
        var home = Item("nav-icononly", "Home");

        await Expect(nav).ToHaveClassAsync(HasClass("bit-nav-ion"));
        await Expect(home.Locator(".bit-nav-itx")).ToBeHiddenAsync();
        await Expect(home.Locator(".bit-nav-iic")).ToBeVisibleAsync();
        await Expect(home).ToHaveAttributeAsync("aria-label", "Home");
        await Expect(home).ToHaveAttributeAsync("title", "Home");
        var railWidth = (await BoxAsync(nav.Locator(".bit-nav-ict").First)).Width;

        await Page.Locator("#btn-toggle-icononly").ClickAsync();

        await Expect(nav).Not.ToHaveClassAsync(HasClass("bit-nav-ion"));
        await Expect(home.Locator(".bit-nav-itx")).ToBeVisibleAsync();
        await Expect(home).Not.ToHaveAttributeAsync("aria-label", new Regex(".*"));
        var fullWidth = (await BoxAsync(nav.Locator(".bit-nav-ict").First)).Width;
        Assert.IsTrue(fullWidth > railWidth, $"The item widens from the rail ({railWidth}px) to the full row ({fullWidth}px).");
    }

    // ---- Expand & Collapse ----

    [TestMethod]
    public async Task SingleExpand_OpeningABranchClosesTheOtherOne()
    {
        await OpenAsync(PAGE);

        await Item("nav-single", "Alpha").ClickAsync();
        await Item("nav-single", "Alpha nested").ClickAsync();
        await Expect(Item("nav-single", "Alpha deep")).ToBeVisibleAsync();

        await Item("nav-single", "Beta").ClickAsync();

        await Expect(Item("nav-single", "Beta one")).ToBeVisibleAsync();
        await Expect(Item("nav-single", "Alpha")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Item("nav-single", "Alpha one")).ToHaveCountAsync(0);

        // ExpandAll does nothing in the single-expand mode either.
        await Page.Locator("#btn-single-expand-all").ClickAsync();
        await NextFramesAsync();
        await Expect(Item("nav-single", "Alpha")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Item("nav-single", "Beta one")).ToBeVisibleAsync();

        // The asterisk, which opens every sibling, is inert in the single-expand mode.
        await Item("nav-single", "Beta").FocusAsync();
        await Page.Keyboard.PressAsync("*");
        await NextFramesAsync();
        await Expect(Item("nav-single", "Alpha")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task AllExpanded_OpensEveryBranchOnTheFirstRender()
    {
        await OpenAsync(PAGE);

        await Expect(Item("nav-allexpanded", "Alpha deep")).ToBeVisibleAsync();
        await Expect(Item("nav-allexpanded", "Beta one")).ToBeVisibleAsync();
        await Expect(Page.Locator("#nav-allexpanded [aria-expanded=false]")).ToHaveCountAsync(0);

        // And the reader can still collapse them.
        await Item("nav-allexpanded", "Beta").ClickAsync();
        await Expect(Item("nav-allexpanded", "Beta one")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task IsExpanded_OfAnItem_OpensItOnTheFirstRender()
    {
        await OpenAsync(PAGE);

        await Expect(Item("nav-initial-expanded", "Open child")).ToBeVisibleAsync();
        await Expect(Item("nav-initial-expanded", "Closed child")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task NoCollapse_ShowsEveryBranch_WithoutChevronsOrToggles()
    {
        await OpenAsync(PAGE);

        await Expect(Item("nav-nocollapse", "Alpha deep")).ToBeVisibleAsync();
        await Expect(Page.Locator("#nav-nocollapse .bit-nav-cbt")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#nav-nocollapse [aria-expanded]")).ToHaveCountAsync(0);

        await Item("nav-nocollapse", "Alpha").ClickAsync();
        await NextFramesAsync();
        await Expect(Item("nav-nocollapse", "Alpha one")).ToBeVisibleAsync();

        // Left on an open parent cannot collapse it either.
        await Item("nav-nocollapse", "Alpha").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Item("nav-nocollapse", "Alpha one")).ToBeVisibleAsync();

        // Without the chevron's room, every item is indented by half a level.
        await Expect(Item("nav-nocollapse", "Alpha")).ToHaveCSSAsync("padding-inline-start", "8px");
        await Expect(Item("nav-nocollapse", "Alpha one")).ToHaveCSSAsync("padding-inline-start", "24px");

        // A Grouped header turns into the label of its group.
        var header = Page.Locator("#nav-nocollapse-grouped .bit-nav-gcb");
        await Expect(header).ToHaveJSPropertyAsync("tagName", "DIV");
        await Expect(header).ToHaveClassAsync(HasClass("bit-nav-gst"));
        await Expect(Item("nav-nocollapse-grouped", "One b")).ToBeVisibleAsync();
    }

    // ---- Chevron & Indentation ----

    [TestMethod]
    public async Task ReversedChevron_MovesTheChevronToTheEnd_AndUsesTheReversedPadding()
    {
        await OpenAsync(PAGE);

        var parent = Item("nav-reversed", "Parent");
        await Expect(parent.Locator(".bit-nav-mct")).ToHaveCSSAsync("flex-direction", "row-reverse");

        var chevron = await BoxAsync(parent.Locator(".bit-nav-cbt"));
        var text = await BoxAsync(parent.Locator(".bit-nav-itx"));
        Assert.IsTrue(chevron.X > text.X + text.Width - 1, "The reversed chevron sits after the text.");

        // depth x IndentValue + IndentReversedPadding, whatever the item holds.
        await Expect(parent).ToHaveCSSAsync("padding-inline-start", "4px");
        await Expect(Item("nav-reversed", "Child")).ToHaveCSSAsync("padding-inline-start", "20px");
        await Expect(Item("nav-reversed", "Sibling")).ToHaveCSSAsync("padding-inline-start", "4px");

        await Expect(Item("nav-reversed-padding", "Parent")).ToHaveCSSAsync("padding-inline-start", "10px");
        await Expect(Item("nav-reversed-padding", "Child")).ToHaveCSSAsync("padding-inline-start", "26px");
    }

    [TestMethod]
    public async Task ChevronDownIconName_ReplacesTheChevronGlyph_WhichStillRotates()
    {
        await OpenAsync(PAGE);

        var icon = Chevron("nav-chevron-icon", "Parent").Locator("i");
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--CircleAdditionSolid"));
        await Expect(icon).Not.ToHaveClassAsync(HasClass("bit-icon--ChevronRight"));
        await Expect(icon).ToHaveClassAsync(HasClass("bit-ico-r90"));

        await Item("nav-chevron-icon", "Parent").ClickAsync();

        await Expect(icon).ToHaveClassAsync(HasClass("bit-nav-exp"));
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--CircleAdditionSolid"));
    }

    [TestMethod]
    public async Task Indentation_FollowsIndentValueAndIndentPadding()
    {
        await OpenAsync(PAGE);

        // The defaults: 16px per level, and the 27px of the chevron that a childless item keeps in its place.
        await Expect(Item("nav-indent-default", "Parent")).ToHaveCSSAsync("padding-inline-start", "0px");
        await Expect(Item("nav-indent-default", "Sibling")).ToHaveCSSAsync("padding-inline-start", "27px");
        await Expect(Item("nav-indent-default", "Child")).ToHaveCSSAsync("padding-inline-start", "43px");
        await Expect(Item("nav-indent-default", "Nested")).ToHaveCSSAsync("padding-inline-start", "16px");
        await Expect(Item("nav-indent-default", "Deep")).ToHaveCSSAsync("padding-inline-start", "59px");
        await Expect(Chevron("nav-indent-default", "Parent")).ToHaveCSSAsync("min-width", "27px");

        // IndentValue="40" IndentPadding="40".
        await Expect(Item("nav-indent", "Parent")).ToHaveCSSAsync("padding-inline-start", "0px");
        await Expect(Item("nav-indent", "Sibling")).ToHaveCSSAsync("padding-inline-start", "40px");
        await Expect(Item("nav-indent", "Child")).ToHaveCSSAsync("padding-inline-start", "80px");
        await Expect(Chevron("nav-indent", "Parent")).ToHaveCSSAsync("min-width", "40px");

        // So the texts of a parent and its childless sibling line up.
        var parentText = await BoxAsync(Item("nav-indent", "Parent").Locator(".bit-nav-itx"));
        var siblingText = await BoxAsync(Item("nav-indent", "Sibling").Locator(".bit-nav-itx"));
        Assert.AreEqual(parentText.X, siblingText.X, 1);
    }

    // ---- Templates ----

    [TestMethod]
    public async Task HeaderTemplate_RendersInsideTheHeaderButton_OrInItsPlaceInReplaceMode()
    {
        await OpenAsync(PAGE);

        var header = Page.Locator("#nav-header-template button.bit-nav-gcb");
        await Expect(header.Locator(".custom-header")).ToHaveTextAsync("Header: Cars");
        await Expect(header.Locator(".bit-nav-ghd")).ToHaveCountAsync(0);

        await header.ClickAsync();
        await Expect(Item("nav-header-template", "Audi")).ToBeVisibleAsync();

        // Replace: no header button around the template at all.
        await Expect(Page.Locator("#nav-header-replace .bit-nav-gcb")).ToHaveCountAsync(0);
        var replaced = Page.Locator("#nav-header-replace > ul > li > .custom-header-replaced");
        await Expect(replaced).ToHaveTextAsync("Replaced header: Bikes");
    }

    [TestMethod]
    public async Task ItemTemplate_NormalModeKeepsTheItemsClick_ReplaceModeTakesTheItemsPlace()
    {
        await OpenAsync(PAGE);

        var templated = Page.Locator("#nav-item-template .bit-nav-ict .custom-item", new() { HasText = "Custom: Sent" });
        await Expect(templated).ToBeVisibleAsync();
        await Expect(Page.Locator("#nav-item-template .bit-nav-itx")).ToHaveCountAsync(0);

        await templated.ClickAsync();

        await Expect(Page.Locator("#template-clicked")).ToHaveTextAsync("Sent");
        await Expect(Page.Locator("#nav-item-template .bit-nav-sel .custom-item")).ToHaveTextAsync("Custom: Sent");

        // Replace: the template renders in the list item, with no anchor or button of the nav around it.
        await Expect(Page.Locator("#nav-item-replace .bit-nav-ict")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#nav-item-replace > ul > li > .custom-item-replaced")).ToHaveCountAsync(3);
        await Expect(Page.Locator("#nav-item-replace .custom-item-replaced").Nth(1)).ToHaveTextAsync("Replaced: Green");
    }

    [TestMethod]
    public async Task TheTemplateOfAnItem_WinsOverTheItemTemplateOfTheNav()
    {
        await OpenAsync(PAGE);

        var nav = Nav("nav-item-own-template");
        await Expect(nav.Locator(".bit-nav-ict .custom-item")).ToHaveTextAsync("Nav template: Plain");
        await Expect(nav.Locator(".bit-nav-ict .own-template")).ToHaveTextAsync("Own template: Own");

        // An item's Replace template takes its place, the others keep their anchor or button.
        await Expect(nav.Locator("> ul > li > .own-replaced")).ToHaveTextAsync("Own replaced: Own replaced");
        await Expect(nav.Locator(".bit-nav-ict")).ToHaveCountAsync(2);
    }

    [TestMethod]
    public async Task AnItemReplacedByItsTemplate_IsLeftOutOfTheKeyboardNavigation()
    {
        await OpenAsync(PAGE);

        // The replaced item renders the template's own control in place of the nav's anchor or button.
        await Expect(Page.Locator("#nav-replace-keyboard > ul > li > input.replaced-input")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#nav-replace-keyboard .bit-nav-ict")).ToHaveCountAsync(2);

        await Item("nav-replace-keyboard", "Before").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Item("nav-replace-keyboard", "After")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Item("nav-replace-keyboard", "Before")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("End");
        await Expect(Item("nav-replace-keyboard", "After")).ToBeFocusedAsync();

        // The template's control keeps its own keys.
        var input = Page.Locator("#nav-replace-keyboard input.replaced-input");
        await input.FillAsync("abc");
        await input.PressAsync("Home");
        await Expect(input).ToBeFocusedAsync();
        Assert.AreEqual(0, await input.EvaluateAsync<int>("el => el.selectionStart"));
    }

    [TestMethod]
    public async Task TheTemplateOfAnItem_KeepsItsOwnNormalMode_UnderAReplacingNavTemplate()
    {
        await OpenAsync(PAGE);

        var nav = Nav("nav-template-precedence");
        await Expect(nav.Locator("> ul > li > .nav-replacing-template")).ToHaveTextAsync("Nav replaces: Replaced by the nav");
        await Expect(nav.Locator(".bit-nav-ict .own-normal-template")).ToHaveTextAsync("Own normal: Own normal");

        var header = Nav("nav-header-precedence");
        await Expect(header.Locator("> ul > li > .nav-replacing-header")).ToHaveTextAsync("Nav replaces header: Replaced group");
        var ownHeader = header.Locator("button.bit-nav-gcb .own-header-template");
        await Expect(ownHeader).ToHaveTextAsync("Own header: Own header");
        await ownHeader.ClickAsync();
        await Expect(Item("nav-header-precedence", "Inside own")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task SuffixTemplate_RendersAtTheEndOfTheItem_AheadOfItsBadge()
    {
        await OpenAsync(PAGE);

        var inbox = Item("nav-suffix", "Inbox");
        await Expect(inbox.Locator(".bit-nav-sfx .nav-suffix")).ToHaveTextAsync("42");
        await Expect(inbox.Locator(".bit-nav-itx")).ToHaveTextAsync("Inbox");

        // The suffix sits at the far end of the row.
        var item = await BoxAsync(inbox.Locator(".bit-nav-itm"));
        var suffix = await BoxAsync(inbox.Locator(".bit-nav-sfx"));
        Assert.AreEqual(item.X + item.Width, suffix.X + suffix.Width, 1.5);

        // The item's own SuffixTemplate wins over the nav's.
        var drafts = Item("nav-suffix", "Drafts");
        await Expect(drafts.Locator(".bit-nav-sfx .own-suffix")).ToHaveTextAsync("own 7");
        await Expect(drafts.Locator(".nav-suffix")).ToHaveCountAsync(0);

        // The badge follows the suffix.
        var badged = Item("nav-suffix", "Badged");
        var order = await badged.Locator(".bit-nav-sfx").EvaluateAsync<string>("el => [...el.children].map(c => c.className).join('|')");
        StringAssert.StartsWith(order, "nav-suffix");
        StringAssert.Contains(order, "bit-nav-bdg");
    }

    // ---- Public API ----

    [TestMethod]
    public async Task PublicApi_ExpandAllCollapseAllAndToggleItem_DriveTheTree()
    {
        await OpenAsync(PAGE);

        await Page.Locator("#btn-expand-all").ClickAsync();
        await Expect(Item("nav-api", "Apple")).ToBeVisibleAsync();
        await Expect(Item("nav-api", "Veggie burger")).ToBeVisibleAsync();

        await Page.Locator("#btn-collapse-all").ClickAsync();
        await Expect(Item("nav-api", "Apple")).ToHaveCountAsync(0);
        await Expect(Item("nav-api", "Burger")).ToHaveCountAsync(0);

        // ExpandAll and CollapseAll of one item reach its subtree only.
        await Page.Locator("#btn-expand-all-fruits").ClickAsync();
        await Expect(Item("nav-api", "Apple")).ToBeVisibleAsync();
        await Expect(Item("nav-api", "Burger")).ToHaveCountAsync(0);
        await Page.Locator("#btn-collapse-all-fruits").ClickAsync();
        await Expect(Item("nav-api", "Apple")).ToHaveCountAsync(0);

        // ToggleItem reports through OnItemToggle; ExpandAll and CollapseAll do not.
        await Expect(Page.Locator("#api-toggle-count")).ToHaveTextAsync("0");
        await Page.Locator("#btn-toggle-fruits").ClickAsync();
        await Expect(Item("nav-api", "Banana")).ToBeVisibleAsync();
        await Expect(Page.Locator("#api-toggle-count")).ToHaveTextAsync("1");
        await Page.Locator("#btn-toggle-fruits").ClickAsync();
        await Expect(Item("nav-api", "Banana")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#api-toggle-count")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task PublicApi_ExpandItemCollapseItemAndIsItemExpanded()
    {
        await OpenAsync(PAGE);

        await Page.Locator("#btn-is-expanded").ClickAsync();
        await Expect(Page.Locator("#api-fastfoods-expanded")).ToHaveTextAsync("False");

        await Page.Locator("#btn-expand-fastfoods").ClickAsync();
        await Expect(Item("nav-api", "Burger")).ToBeVisibleAsync();
        await Page.Locator("#btn-is-expanded").ClickAsync();
        await Expect(Page.Locator("#api-fastfoods-expanded")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#api-fruits-expanded")).ToHaveTextAsync("False");

        // Expanding an expanded item does nothing, and reports nothing.
        await Page.Locator("#btn-expand-fastfoods").ClickAsync();
        await Expect(Item("nav-api", "Burger")).ToBeVisibleAsync();
        await Expect(Page.Locator("#api-toggle-count")).ToHaveTextAsync("1");

        await Page.Locator("#btn-collapse-fastfoods").ClickAsync();
        await Expect(Item("nav-api", "Burger")).ToHaveCountAsync(0);
        await Page.Locator("#btn-is-expanded").ClickAsync();
        await Expect(Page.Locator("#api-fastfoods-expanded")).ToHaveTextAsync("False");

        await Page.Locator("#btn-collapse-fastfoods").ClickAsync();
        await Expect(Page.Locator("#api-toggle-count")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task PublicApi_SelectItemAndFocusItem()
    {
        await OpenAsync(PAGE);

        await Page.Locator("#btn-select-icecream").ClickAsync();
        await Expect(Item("nav-api", "Ice cream")).ToHaveClassAsync(HasClass("bit-nav-sel"));
        await Expect(Page.Locator("#api-selected")).ToHaveTextAsync("Ice cream");

        await Page.Locator("#btn-select-none").ClickAsync();
        await Expect(Page.Locator("#nav-api .bit-nav-sel")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#api-selected")).ToHaveTextAsync("none");

        // FocusItem opens the branches the item is nested in, then moves the focus onto it.
        await Expect(Item("nav-api", "Veggie burger")).ToHaveCountAsync(0);
        await Page.Locator("#btn-focus-veggie").ClickAsync();
        await Expect(Item("nav-api", "Veggie burger")).ToBeFocusedAsync();
        await Expect(Item("nav-api", "Fast foods")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Item("nav-api", "Vegetarian")).ToHaveAttributeAsync("aria-expanded", "true");

        // An item already on screen is focused right away.
        await Page.Locator("#btn-focus-veggie").ClickAsync();
        await Expect(Item("nav-api", "Veggie burger")).ToBeFocusedAsync();
    }

    // ---- Events ----

    [TestMethod]
    public async Task Events_ClickSelectAndToggle_FireOnceEachWithTheirItem()
    {
        await OpenAsync(PAGE);

        await Item("nav-events", "Ice cream").ClickAsync();

        await Expect(Page.Locator("#event-click-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#event-last-clicked")).ToHaveTextAsync("Ice cream");
        await Expect(Page.Locator("#event-select-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#event-last-selected")).ToHaveTextAsync("Ice cream");

        // The selected item swallows the next click.
        await Item("nav-events", "Ice cream").ClickAsync();
        await NextFramesAsync();
        await Expect(Page.Locator("#event-click-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#event-select-count")).ToHaveTextAsync("1");

        // A parent toggles (and is clicked), and is not selected.
        await Item("nav-events", "Fruits").ClickAsync();
        await Expect(Page.Locator("#event-toggle-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#event-last-toggled")).ToHaveTextAsync("Fruits:expanded");
        await Expect(Page.Locator("#event-click-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#event-last-clicked")).ToHaveTextAsync("Fruits");
        await Expect(Page.Locator("#event-select-count")).ToHaveTextAsync("1");

        await Item("nav-events", "Fruits").ClickAsync();
        await Expect(Page.Locator("#event-toggle-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#event-last-toggled")).ToHaveTextAsync("Fruits:collapsed");
    }

    [TestMethod]
    public async Task Reselectable_FiresTheClickAndTheSelectAgainForTheSelectedItem()
    {
        await OpenAsync(PAGE);

        var again = Item("nav-reselectable", "Again");
        await again.ClickAsync();
        await Expect(Page.Locator("#reselect-click-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#reselect-select-count")).ToHaveTextAsync("1");

        await again.ClickAsync();
        await Expect(Page.Locator("#reselect-click-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#reselect-select-count")).ToHaveTextAsync("2");
        await Expect(again).ToHaveClassAsync(HasClass("bit-nav-sel"));
    }

    // ---- Accessibility ----

    [TestMethod]
    [DataRow("Page item", "page")]
    [DataRow("Step item", "step")]
    [DataRow("Location item", "location")]
    [DataRow("Date item", "date")]
    [DataRow("Time item", "time")]
    [DataRow("True item", "true")]
    public async Task AriaCurrent_TheSelectedItemCarriesItsOwnToken(string text, string token)
    {
        await OpenAsync(PAGE);

        await Expect(Nav("nav-a11y")).ToHaveAttributeAsync("aria-label", "Components");

        var item = Item("nav-a11y", text);
        await Expect(item).Not.ToHaveAttributeAsync("aria-current", new Regex(".*"));

        await item.ClickAsync();

        await Expect(item).ToHaveAttributeAsync("aria-current", token);
        await Expect(Page.Locator("#nav-a11y [aria-current]")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Accessibility_AriaLabelAndDescriptionNameAndDescribeTheItem()
    {
        await OpenAsync(PAGE);

        var labeled = Item("nav-a11y", "Labeled item");
        await Expect(labeled).ToHaveAttributeAsync("aria-label", "Custom accessible name");
        await Expect(Nav("nav-a11y").GetByRole(AriaRole.Button, new() { Name = "Custom accessible name", Exact = true })).ToHaveCountAsync(1);

        var described = Item("nav-a11y", "Described item");
        var descriptionId = await described.Locator(".bit-nav-des").GetAttributeAsync("id");
        await Expect(described).ToHaveAttributeAsync("aria-describedby", descriptionId!);
        await Expect(described).ToHaveAttributeAsync("aria-label", "Described item");
    }

    [TestMethod]
    public async Task Accessibility_TheChevronIsNamedByTheNavsOrTheItemsExpandAndCollapseLabels()
    {
        await OpenAsync(PAGE);

        var chevron = Chevron("nav-aria-labels", "Linked parent");
        await Expect(chevron).ToHaveAttributeAsync("aria-label", "Open branch");
        await chevron.ClickAsync();
        await Expect(chevron).ToHaveAttributeAsync("aria-label", "Close branch");

        // The item's own labels win over the nav's.
        var own = Chevron("nav-aria-labels", "Own labels");
        await Expect(own).ToHaveAttributeAsync("aria-label", "Show own");
        await own.ClickAsync();
        await Expect(own).ToHaveAttributeAsync("aria-label", "Hide own");

        // Without any label the chevron is named after the item.
        await Expect(Chevron("nav-basic", "Reports")).ToHaveAttributeAsync("aria-label", "Expand reports");
    }

    [TestMethod]
    public async Task NewTabHint_IsReplacedByNewTabHintAndRemovedByNoNewTabHint()
    {
        await OpenAsync(PAGE);

        await Expect(Item("nav-newtab", "Blank").Locator(".bit-nav-vhd")).ToHaveTextAsync("(opens in a new tab)");

        // An item named through an aria-label carries the sentence in it instead.
        var labeled = Page.Locator("#nav-newtab .bit-nav-ict").Nth(1);
        await Expect(labeled).ToHaveAttributeAsync("aria-label", "Labeled (opens in a new tab)");
        await Expect(labeled.Locator(".bit-nav-vhd")).ToHaveCountAsync(0);

        await Expect(Item("nav-newtab-custom", "Blank").Locator(".bit-nav-vhd")).ToHaveTextAsync("(new window)");

        var none = Item("nav-newtab-none", "Blank");
        await Expect(none).ToHaveAttributeAsync("target", "_blank");
        await Expect(none).ToHaveAttributeAsync("rel", "noopener");
        await Expect(none.Locator(".bit-nav-vhd")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task ACollapsedBranchHoldingTheSelection_BorrowsTheSelectedLook()
    {
        await OpenAsync(PAGE);

        var branch = Item("nav-sca", "Branch");
        var leaf = Item("nav-sca", "Second leaf");
        await Expect(leaf).ToHaveClassAsync(HasClass("bit-nav-sel"));
        await Expect(branch).Not.ToHaveClassAsync(HasClass("bit-nav-sca"));
        var selectedBg = await leaf.EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");

        await branch.ClickAsync();
        // The pointer leaves, so the branch is read at rest rather than in its hovered state.
        await Page.Mouse.MoveAsync(0, 0);

        await Expect(leaf).ToHaveCountAsync(0);
        await Expect(branch).ToHaveClassAsync(HasClass("bit-nav-sca"));
        await Expect(branch).ToHaveCSSAsync("background-color", selectedBg);
        // Only the look is borrowed: aria-current stays with the selected item alone.
        await Expect(branch).Not.ToHaveAttributeAsync("aria-current", new Regex(".*"));

        await branch.ClickAsync();
        await Expect(branch).Not.ToHaveClassAsync(HasClass("bit-nav-sca"));
        await Expect(leaf).ToHaveClassAsync(HasClass("bit-nav-sel"));

        // A collapsed group header holding the selection does the same.
        var header = Page.Locator("#nav-sca-grouped button.bit-nav-gcb", new() { HasText = "Group" }).First;
        await Expect(header).ToHaveAttributeAsync("aria-expanded", "true");
        await header.ClickAsync();
        await Expect(header).ToHaveClassAsync(HasClass("bit-nav-sca"));
        await Expect(Page.Locator("#nav-sca-grouped button.bit-nav-gcb", new() { HasText = "Other group" })).Not.ToHaveClassAsync(HasClass("bit-nav-sca"));
    }

    // ---- Cascading parameters ----

    [TestMethod]
    public async Task BitParams_HandsItsDefaults_AndANavKeepsWhatItSetsItself()
    {
        await OpenAsync(PAGE);

        var all = Nav("nav-params-all");
        await Expect(all).ToHaveClassAsync(HasClass("bit-nav-ion"));
        await Expect(all).ToHaveClassAsync(HasClass("bit-nav-err"));
        await Expect(all).ToHaveClassAsync(HasClass("bit-nav-lg"));
        await Expect(all).ToHaveClassAsync(HasClass("bit-nav-ftw"));
        await Expect(all.Locator(".bit-nav-itx").First).ToBeHiddenAsync();
        var error = await ResolveColorAsync("var(--bit-clr-err)");
        await Expect(all.Locator(".bit-nav-iic").First).ToHaveCSSAsync("color", error);

        var own = Nav("nav-params-own");
        await Expect(own).Not.ToHaveClassAsync(HasClass("bit-nav-ion"));
        await Expect(own).ToHaveClassAsync(HasClass("bit-nav-err"));
        await Expect(own.Locator(".bit-nav-itx").First).ToBeVisibleAsync();
    }

    // ---- Badges ----

    [TestMethod]
    public async Task Badges_AreDrawnAfterTheText_AndFoldedIntoTheName()
    {
        await OpenAsync(PAGE);

        var inbox = Item("nav-badges", "Inbox");
        var badge = inbox.Locator(".bit-nav-bdg");
        await Expect(badge).ToHaveTextAsync("3");
        await Expect(badge).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(inbox.Locator(".bit-nav-iic")).ToBeVisibleAsync();
        await Expect(inbox.Locator(".bit-nav-vhd")).ToHaveTextAsync(" (3)");
        await Expect(Nav("nav-badges").GetByRole(AriaRole.Button, new() { Name = "Inbox (3)", Exact = true })).ToHaveCountAsync(1);

        // The badge is a filled pill in the error colors.
        await Expect(badge).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-err)"));
        var badgeBox = await BoxAsync(badge);
        var textBox = await BoxAsync(inbox.Locator(".bit-nav-itx"));
        Assert.IsTrue(badgeBox.X > textBox.X + textBox.Width, "The badge follows the text.");

        await Expect(Item("nav-badges", "Drafts").Locator(".bit-nav-vhd")).ToHaveTextAsync(" (12 drafts)");

        // A dot carries no text: it is announced through its BadgeAriaLabel only.
        var alerts = Item("nav-badges", "Alerts");
        await Expect(alerts.Locator(".bit-nav-bdg.bit-nav-bdt")).ToHaveTextAsync("");
        await Expect(alerts.Locator(".bit-nav-vhd")).ToHaveTextAsync(" (New alerts)");
        var dot = await BoxAsync(alerts.Locator(".bit-nav-bdt"));
        Assert.AreEqual(dot.Width, dot.Height, 0.5, "The dot is round.");
        Assert.IsTrue(dot.Width < badgeBox.Height, "The dot is smaller than a badge.");

        await Expect(Item("nav-badges", "Silent dot").Locator(".bit-nav-bdt")).ToHaveCountAsync(1);
        await Expect(Item("nav-badges", "Silent dot").Locator(".bit-nav-vhd")).ToHaveCountAsync(0);

        // Named through an aria-label, the count goes into it.
        await Expect(Item("nav-badges", "Named")).ToHaveAttributeAsync("aria-label", "Named item (4)");

        // A badge wins over a dot.
        await Expect(Item("nav-badges", "Both").Locator(".bit-nav-bdg")).ToHaveTextAsync("2");
        await Expect(Item("nav-badges", "Both").Locator(".bit-nav-bdt")).ToHaveCountAsync(0);

        // A disabled item dims its badge rather than repainting it.
        await Expect(Item("nav-badges", "Off").Locator(".bit-nav-bdg")).Not.ToHaveCSSAsync("opacity", "1");
    }

    [TestMethod]
    public async Task Badges_TheRailPinsTheBadgeToTheCornerOfTheIcon()
    {
        await OpenAsync(PAGE);

        var inbox = Page.Locator("#nav-badges-icononly .bit-nav-ict").First;
        await Expect(inbox.Locator(".bit-nav-itm")).ToHaveClassAsync(HasClass("bit-nav-icb"));
        await Expect(inbox.Locator(".bit-nav-sfx")).ToHaveCSSAsync("position", "absolute");
        await Expect(inbox).ToHaveAttributeAsync("aria-label", "Inbox (3)");

        var item = await BoxAsync(inbox.Locator(".bit-nav-itm"));
        var badge = await BoxAsync(inbox.Locator(".bit-nav-bdg"));
        Assert.IsTrue(badge.Y < item.Y + item.Height / 2, "The badge sits at the top of the rail item.");
    }

    // ---- Color & Accent ----

    [TestMethod]
    public async Task Color_EveryValuePaintsTheIconsAndTheIndicatorInItsRole()
    {
        await OpenAsync(PAGE);

        // Unset, the nav is primary.
        var primary = await ResolveColorAsync("var(--bit-clr-pri)");
        await Expect(Page.Locator("#nav-color-default .bit-nav-iic").First).ToHaveCSSAsync("color", primary);

        var painted = new HashSet<string>();

        foreach (var (name, suffix) in Colors)
        {
            var nav = Nav($"nav-color-{name}");
            await Expect(nav).ToHaveClassAsync(HasClass($"bit-nav-{suffix}"));

            var expected = await ResolveColorVarAsync($"#nav-color-{name}", "--bit-nav-clr");
            Assert.AreNotEqual("", expected, name);

            await Expect(nav.Locator(".bit-nav-iic").Nth(1)).ToHaveCSSAsync("color", expected);
            await Expect(nav.Locator(".bit-nav-sel")).ToHaveCSSAsync("border-left-color", expected);

            painted.Add(expected);
        }

        Assert.IsTrue(painted.Count >= 10, $"The roles paint in distinct colors ({painted.Count} distinct of {Colors.Length}).");
    }

    [TestMethod]
    public async Task Accent_EveryValueFillsTheSelectedItemAndRecolorsWhatItHolds()
    {
        await OpenAsync(PAGE);

        var fills = new HashSet<string>();

        foreach (var (name, suffix) in Colors)
        {
            var nav = Nav($"nav-accent-{name}");
            await Expect(nav).ToHaveClassAsync(HasClass($"bit-nav-a{suffix}"));

            var fill = await ResolveColorVarAsync($"#nav-accent-{name}", "--bit-nav-clr-active");
            var text = await ResolveColorVarAsync($"#nav-accent-{name}", "--bit-nav-clr-text");
            var icon = await ResolveColorVarAsync($"#nav-accent-{name}", "--bit-nav-clr-icon");

            var selected = nav.Locator(".bit-nav-sel");
            await Expect(selected).ToHaveCSSAsync("background-color", fill);
            await Expect(selected.Locator(".bit-nav-itm")).ToHaveCSSAsync("color", text);
            await Expect(selected.Locator(".bit-nav-iic")).ToHaveCSSAsync("color", icon);

            fills.Add(fill);
        }

        Assert.IsTrue(fills.Count >= 10, $"The accents fill in distinct colors ({fills.Count} distinct of {Colors.Length}).");

        // Unset, the selection takes the neutral PrimaryBackground fill.
        var neutral = await Page.Locator("#nav-accent-default .bit-nav-sel").EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");
        var primaryBackgroundActive = await ResolveColorVarAsync("#nav-accent-primary-background", "--bit-nav-clr-active");
        Assert.AreEqual(primaryBackgroundActive, neutral);
    }

    [TestMethod]
    public async Task Accent_TheHoveredItemTakesTheAccentsHoverFill()
    {
        await OpenAsync(PAGE);

        var other = Item("nav-accent-success", "Settings");
        var restBg = await other.EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");
        var hover = await ResolveColorVarAsync("#nav-accent-success", "--bit-nav-clr-hover");
        var text = await ResolveColorVarAsync("#nav-accent-success", "--bit-nav-clr-text");

        await other.HoverAsync();

        await Expect(other).ToHaveCSSAsync("background-color", hover);
        await Expect(other.Locator(".bit-nav-itm")).ToHaveCSSAsync("color", text);
        Assert.AreNotEqual(restBg, hover);
    }

    // ---- External icons ----

    [TestMethod]
    public async Task ExternalIcons_RenderTheirClasses_AndWinOverIconName()
    {
        await OpenAsync(PAGE);

        await Expect(Item("nav-external", "Css icon").Locator("i.bit-nav-iic")).ToHaveClassAsync(new Regex(@"(^|\s)my-icon my-icon-house(\s|$)"));

        var bootstrap = Item("nav-external", "Bootstrap icon").Locator("i.bit-nav-iic");
        await Expect(bootstrap).ToHaveClassAsync(new Regex(@"(^|\s)bi bi-gear-fill(\s|$)"));
        await Expect(bootstrap).Not.ToHaveClassAsync(HasClass("bit-icon--Home"));

        await Expect(Item("nav-external", "FontAwesome icon").Locator("i.bit-nav-iic")).ToHaveClassAsync(new Regex(@"(^|\s)fa-solid fa-house(\s|$)"));

        // ChevronDownIcon replaces the chevron, and rotates it all the same.
        var chevron = Chevron("nav-external", "Branch").Locator("i");
        await Expect(chevron).ToHaveClassAsync(new Regex(@"(^|\s)bi bi-chevron-right(\s|$)"));
        await Expect(chevron).ToHaveClassAsync(HasClass("bit-ico-r90"));
        await Item("nav-external", "Branch").ClickAsync();
        await Expect(chevron).ToHaveClassAsync(HasClass("bit-nav-exp"));
    }

    // ---- Size ----

    [TestMethod]
    public async Task Size_EveryValueScalesTheItemsAndTheHeaders()
    {
        await OpenAsync(PAGE);

        await Expect(Nav("nav-size-small")).ToHaveClassAsync(HasClass("bit-nav-sm"));
        await Expect(Nav("nav-size-medium")).ToHaveClassAsync(HasClass("bit-nav-md"));
        await Expect(Nav("nav-size-large")).ToHaveClassAsync(HasClass("bit-nav-lg"));
        await Expect(Nav("nav-size-default")).Not.ToHaveClassAsync(new Regex(@"(^|\s)bit-nav-(sm|md|lg)(\s|$)"));

        async Task<double[]> MetricsAsync(string id)
        {
            var home = Item(id, "Home");
            return
            [
                await NumberStyleAsync(home, "min-height"),
                await NumberStyleAsync(home.Locator(".bit-nav-itm"), "font-size"),
                await NumberStyleAsync(home.Locator(".bit-nav-iic"), "font-size"),
                await NumberStyleAsync(home.Locator(".bit-nav-des"), "font-size"),
                await NumberStyleAsync(home.Locator(".bit-nav-bdg"), "height"),
                await NumberStyleAsync(Page.Locator($"#{id} .bit-nav-gcb"), "min-height"),
                await NumberStyleAsync(Page.Locator($"#{id} .bit-nav-ghd"), "font-size"),
                await NumberStyleAsync(Page.Locator($"#{id} .bit-nav-bdt"), "width"),
                await NumberStyleAsync(Chevron(id, "Branch"), "font-size"),
            ];
        }

        var small = await MetricsAsync("nav-size-small");
        var medium = await MetricsAsync("nav-size-medium");
        var large = await MetricsAsync("nav-size-large");
        var unset = await MetricsAsync("nav-size-default");

        string[] names = ["item height", "font size", "icon size", "description size", "badge height", "header height", "header size", "dot size", "chevron size"];

        for (var i = 0; i < names.Length; i++)
        {
            Assert.AreEqual(medium[i], unset[i], 0.01, $"Unset is the medium {names[i]}.");
            Assert.IsTrue(small[i] <= medium[i] && medium[i] <= large[i], $"{names[i]}: {small[i]} <= {medium[i]} <= {large[i]}");
        }

        Assert.IsTrue(small[0] < medium[0] && medium[0] < large[0], "The item heights differ per size.");
        Assert.IsTrue(small[1] < medium[1] && medium[1] < large[1], "The font sizes differ per size.");
        Assert.IsTrue(small[5] < medium[5] && medium[5] < large[5], "The header heights differ per size.");
    }

    // ---- Style & Class ----

    [TestMethod]
    public async Task StyleAndClass_ReachTheRoot_AndTheItemsStyleAndClassTheirListItem()
    {
        await OpenAsync(PAGE);

        await Expect(Nav("nav-style")).ToHaveCSSAsync("border-top-color", "rgb(255, 99, 71)");
        await Expect(Nav("nav-style")).ToHaveCSSAsync("max-width", "320px");
        await Expect(Nav("nav-class")).ToHaveClassAsync(HasClass("custom-class"));
        await Expect(Nav("nav-class")).ToHaveClassAsync(HasClass("bit-nav"));

        var styled = Item("nav-item-style", "Styled").Locator("xpath=..");
        await Expect(styled).ToHaveJSPropertyAsync("tagName", "LI");
        await Expect(styled).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(Item("nav-item-style", "Classed").Locator("xpath=..")).ToHaveClassAsync(HasClass("item-class"));
    }

    [TestMethod]
    public async Task Styles_ReachEveryPartOfTheNav()
    {
        await OpenAsync(PAGE);

        var nav = Nav("nav-styles");
        await Expect(nav).ToHaveCSSAsync("background-color", "rgb(42, 42, 42)");
        await Expect(Item("nav-styles", "Plain")).ToHaveCSSAsync("margin-top", "3px");
        await Expect(Item("nav-styles", "Selected")).ToHaveCSSAsync("border-left-color", "rgb(255, 215, 0)");
        await Expect(Chevron("nav-styles", "Parent")).ToHaveCSSAsync("color", "rgb(0, 255, 255)");
        await Expect(Chevron("nav-styles", "Parent").Locator("i")).ToHaveCSSAsync("font-size", "9px");
        await Expect(Item("nav-styles", "Plain").Locator(".bit-nav-itm")).ToHaveCSSAsync("color", "rgb(255, 69, 0)");
        await Expect(Item("nav-styles", "Selected").Locator(".bit-nav-itm")).ToHaveCSSAsync("color", "rgb(255, 215, 0)");
        await Expect(Item("nav-styles", "Plain").Locator(".bit-nav-iic")).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(Item("nav-styles", "Plain").Locator(".bit-nav-itx")).ToHaveCSSAsync("letter-spacing", "2px");
        await Expect(Item("nav-styles", "Selected").Locator(".bit-nav-bdg")).ToHaveCSSAsync("background-color", "rgb(4, 5, 6)");
        await Expect(Item("nav-styles", "Selected").Locator(".bit-nav-sfx")).ToHaveCSSAsync("opacity", "0.5");
        await Expect(Item("nav-styles", "Selected").Locator(".bit-nav-des")).ToHaveCSSAsync("color", "rgb(143, 188, 143)");
        await Expect(Page.Locator("#nav-styles .bit-nav-sep")).ToHaveCSSAsync("border-top-color", "rgb(7, 8, 9)");

        await Expect(Page.Locator("#nav-styles-grouped .bit-nav-gcb")).ToHaveCSSAsync("background-color", "rgb(10, 11, 12)");
        await Expect(Page.Locator("#nav-styles-grouped .bit-nav-ghd")).ToHaveCSSAsync("color", "rgb(13, 14, 15)");
    }

    [TestMethod]
    public async Task Classes_ReachEveryPartOfTheNav()
    {
        await OpenAsync(PAGE);

        await Expect(Nav("nav-classes")).ToHaveClassAsync(HasClass("custom-root"));
        await Expect(Item("nav-classes", "Plain")).ToHaveClassAsync(HasClass("custom-item-container"));
        await Expect(Item("nav-classes", "Plain")).Not.ToHaveClassAsync(HasClass("custom-selected-container"));
        await Expect(Item("nav-classes", "Selected")).ToHaveClassAsync(HasClass("custom-selected-container"));
        await Expect(Chevron("nav-classes", "Parent")).ToHaveClassAsync(HasClass("custom-toggle-button"));
        await Expect(Chevron("nav-classes", "Parent").Locator("i")).ToHaveClassAsync(HasClass("custom-toggle-icon"));
        await Expect(Item("nav-classes", "Plain").Locator(".bit-nav-itm")).ToHaveClassAsync(HasClass("custom-item-part"));
        await Expect(Item("nav-classes", "Plain").Locator(".bit-nav-itm")).Not.ToHaveClassAsync(HasClass("custom-selected-item"));
        await Expect(Item("nav-classes", "Selected").Locator(".bit-nav-itm")).ToHaveClassAsync(HasClass("custom-selected-item"));
        await Expect(Item("nav-classes", "Plain").Locator(".bit-nav-iic")).ToHaveClassAsync(HasClass("custom-item-icon"));
        await Expect(Item("nav-classes", "Plain").Locator(".bit-nav-itx")).ToHaveClassAsync(HasClass("custom-item-text"));
        await Expect(Item("nav-classes", "Selected").Locator(".bit-nav-bdg")).ToHaveClassAsync(HasClass("custom-item-badge"));
        await Expect(Item("nav-classes", "Selected").Locator(".bit-nav-sfx")).ToHaveClassAsync(HasClass("custom-item-suffix"));
        await Expect(Item("nav-classes", "Selected").Locator(".bit-nav-des")).ToHaveClassAsync(HasClass("custom-description"));
        await Expect(Page.Locator("#nav-classes .bit-nav-sep")).ToHaveClassAsync(HasClass("custom-separator"));

        await Expect(Page.Locator("#nav-classes-grouped .bit-nav-gcb")).ToHaveClassAsync(HasClass("custom-header"));
        await Expect(Page.Locator("#nav-classes-grouped .bit-nav-ghd")).ToHaveClassAsync(HasClass("custom-header-text"));
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheNav()
    {
        await OpenAsync(PAGE);

        var selected = Item("nav-vars", "Selected");
        var other = Item("nav-vars", "Other");

        await Expect(other).ToHaveCSSAsync("border-top-left-radius", "13px");
        await Expect(other).ToHaveCSSAsync("min-height", "50px");
        await Expect(other.Locator(".bit-nav-iic")).ToHaveCSSAsync("color", "rgb(200, 0, 0)");
        await Expect(other.Locator(".bit-nav-itm")).ToHaveCSSAsync("color", "rgb(30, 60, 90)");
        await Expect(other.Locator(".bit-nav-itm")).ToHaveCSSAsync("font-size", "19px");
        await Expect(other.Locator(".bit-nav-des")).ToHaveCSSAsync("color", "rgb(70, 80, 90)");
        await Expect(selected).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(selected.Locator(".bit-nav-itm")).ToHaveCSSAsync("color", "rgb(240, 240, 240)");
        await Expect(selected).ToHaveCSSAsync("border-left-color", "rgb(0, 128, 0)");
        await Expect(selected).ToHaveCSSAsync("border-left-width", "6px");
        await Expect(selected.Locator(".bit-nav-bdg")).ToHaveCSSAsync("background-color", "rgb(0, 100, 0)");
        await Expect(selected.Locator(".bit-nav-bdg")).ToHaveCSSAsync("color", "rgb(255, 255, 0)");
        await Expect(Page.Locator("#nav-vars .bit-nav-sep")).ToHaveCSSAsync("border-top-color", "rgb(50, 60, 70)");
        await Expect(Page.Locator("#nav-vars > ul")).ToHaveCSSAsync("row-gap", "7px");

        var header = Page.Locator("#nav-vars-grouped .bit-nav-gcb");
        await Expect(header).ToHaveCSSAsync("color", "rgb(11, 22, 33)");
        await Expect(header).ToHaveCSSAsync("border-bottom-color", "rgb(44, 55, 66)");
        await Expect(header).ToHaveCSSAsync("min-height", "61px");
        await Expect(header.Locator(".bit-nav-ghd")).ToHaveCSSAsync("font-size", "23px");
    }

    [TestMethod]
    public async Task CssVariables_TheStateAndMetricVariables_RestyleTheNav()
    {
        await OpenAsync(PAGE);

        var selected = Item("nav-vars-state", "Selected");
        var other = Item("nav-vars-state", "Other");

        // The metrics.
        await Expect(other).ToHaveCSSAsync("padding-top", "7px");
        await Expect(other.Locator(".bit-nav-iic")).ToHaveCSSAsync("font-size", "21px");
        await Expect(other.Locator(".bit-nav-des")).ToHaveCSSAsync("font-size", "9px");
        await Expect(other.Locator(".bit-nav-bdg")).ToHaveCSSAsync("font-size", "13px");
        await Expect(other.Locator(".bit-nav-bdg")).ToHaveCSSAsync("height", "22px");
        await Expect(Item("nav-vars-state", "Dotted").Locator(".bit-nav-bdt")).ToHaveCSSAsync("width", "11px");

        // The selected and the disabled colors.
        await Expect(selected.Locator(".bit-nav-iic")).ToHaveCSSAsync("color", "rgb(9, 100, 9)");
        await Expect(selected.Locator(".bit-nav-des")).ToHaveCSSAsync("color", "rgb(10, 100, 10)");
        await Expect(Item("nav-vars-state", "Off").Locator(".bit-nav-itm")).ToHaveCSSAsync("color", "rgb(16, 100, 16)");
        await Expect(Item("nav-vars-state", "Off").Locator(".bit-nav-iic")).ToHaveCSSAsync("color", "rgb(16, 100, 16)");

        // An item under the pointer.
        await other.HoverAsync();
        await Expect(other).ToHaveCSSAsync("background-color", "rgb(1, 100, 1)");
        await Expect(other.Locator(".bit-nav-itm")).ToHaveCSSAsync("color", "rgb(2, 100, 2)");
        await Expect(other.Locator(".bit-nav-iic")).ToHaveCSSAsync("color", "rgb(3, 100, 3)");
        await Expect(other.Locator(".bit-nav-des")).ToHaveCSSAsync("color", "rgb(4, 100, 4)");

        // The selected item under the pointer, then pressed.
        await selected.HoverAsync();
        await Expect(selected).ToHaveCSSAsync("background-color", "rgb(11, 100, 11)");
        await Expect(selected.Locator(".bit-nav-itm")).ToHaveCSSAsync("color", "rgb(13, 100, 13)");
        await Expect(selected.Locator(".bit-nav-iic")).ToHaveCSSAsync("color", "rgb(14, 100, 14)");
        await Expect(selected.Locator(".bit-nav-des")).ToHaveCSSAsync("color", "rgb(15, 100, 15)");
        await Page.Mouse.DownAsync();
        await Expect(selected).ToHaveCSSAsync("background-color", "rgb(12, 100, 12)");
        await Page.Mouse.UpAsync();

        // An item pressed.
        await other.HoverAsync();
        await Page.Mouse.DownAsync();
        await Expect(other).ToHaveCSSAsync("background-color", "rgb(5, 100, 5)");
        await Expect(other.Locator(".bit-nav-itm")).ToHaveCSSAsync("color", "rgb(6, 100, 6)");
        await Expect(other.Locator(".bit-nav-iic")).ToHaveCSSAsync("color", "rgb(7, 100, 7)");
        await Expect(other.Locator(".bit-nav-des")).ToHaveCSSAsync("color", "rgb(8, 100, 8)");
        await Page.Mouse.UpAsync();

        // A group header under the pointer.
        var header = Page.Locator("#nav-vars-state-grouped .bit-nav-gcb");
        await header.HoverAsync();
        await Expect(header).ToHaveCSSAsync("background-color", "rgb(17, 100, 17)");
    }

    [TestMethod]
    public async Task CssVariables_AnExplicitColorAccentAndSize_WinOverThem()
    {
        await OpenAsync(PAGE);

        var success = await ResolveColorAsync("var(--bit-clr-suc)");
        var errorActive = await ResolveColorVarAsync("#nav-vars-color", "--bit-nav-clr-active");

        await Expect(Item("nav-vars-color", "Other").Locator(".bit-nav-iic")).Not.ToHaveCSSAsync("color", "rgb(200, 0, 0)");
        await Expect(Item("nav-vars-color", "Selected")).ToHaveCSSAsync("border-left-color", success);
        await Expect(Item("nav-vars-color", "Selected")).ToHaveCSSAsync("background-color", errorActive);
        await Expect(Item("nav-vars-color", "Other")).Not.ToHaveCSSAsync("min-height", "50px");
        await Expect(Item("nav-vars-color", "Other").Locator(".bit-nav-itm")).Not.ToHaveCSSAsync("font-size", "19px");

        // What the parameters do not set stays the variables'.
        await Expect(Item("nav-vars-color", "Other")).ToHaveCSSAsync("border-top-left-radius", "13px");
    }

    [TestMethod]
    public async Task Visibility_HiddenKeepsTheRoom_CollapsedRemovesIt()
    {
        await OpenAsync(PAGE);

        var nav = Nav("nav-visibility");
        await Expect(nav).ToBeVisibleAsync();

        await Page.Locator("#btn-cycle-visibility").ClickAsync();
        await Expect(Page.Locator("#visibility-state")).ToHaveTextAsync("Hidden");
        await Expect(nav).ToHaveCSSAsync("visibility", "hidden");
        Assert.IsTrue((await BoxAsync(nav)).Height > 0);

        await Page.Locator("#btn-cycle-visibility").ClickAsync();
        await Expect(Page.Locator("#visibility-state")).ToHaveTextAsync("Collapsed");
        await Expect(nav).ToHaveCSSAsync("display", "none");

        await Page.Locator("#btn-cycle-visibility").ClickAsync();
        await Expect(nav).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task HtmlAttributes_AndUnmatchedAttributes_LandOnTheRoot()
    {
        await OpenAsync(PAGE);

        var nav = Nav("nav-html-attributes");
        await Expect(nav).ToHaveAttributeAsync("data-test", "nav-data");
        await Expect(nav).ToHaveAttributeAsync("data-extra", "extra-value");
        await Expect(nav).ToHaveAttributeAsync("title", "Nav title");
    }

    // ---- RTL ----

    [TestMethod]
    public async Task Rtl_MirrorsTheNavAndItsIndentation()
    {
        await OpenAsync(PAGE);

        var nav = Nav("nav-rtl");
        await Expect(nav).ToHaveAttributeAsync("dir", "rtl");
        await Expect(nav).ToHaveClassAsync(HasClass("bit-rtl"));

        var navBox = await BoxAsync(nav);
        var sibling = Item("nav-rtl", "Rtl sibling");
        var text = await BoxAsync(sibling.Locator(".bit-nav-itx"));
        Assert.IsTrue(text.X + text.Width > navBox.X + navBox.Width - 60, "The text starts at the right edge of a right-to-left nav.");
        Assert.IsTrue(text.X > navBox.X + navBox.Width / 2, "And stays in its right half.");

        // The indentation is on the right.
        await Expect(sibling).ToHaveCSSAsync("padding-right", "27px");
        await Expect(sibling).ToHaveCSSAsync("padding-left", "4px");

        // The chevron of the parent sits right of its text.
        var parent = Item("nav-rtl", "Rtl parent");
        var chevron = await BoxAsync(parent.Locator(".bit-nav-cbt"));
        var parentText = await BoxAsync(parent.Locator(".bit-nav-itx"));
        Assert.IsTrue(chevron.X > parentText.X);

        var ltrText = await BoxAsync(Item("nav-ltr", "Ltr sibling").Locator(".bit-nav-itx"));
        var ltrNav = await BoxAsync(Nav("nav-ltr"));
        Assert.IsTrue(ltrText.X < ltrNav.X + 60, "A left-to-right nav keeps its text at the left.");
    }

    [TestMethod]
    public async Task Rtl_TheArrowKeysAreMirrored()
    {
        await OpenAsync(PAGE);

        var parent = Item("nav-rtl", "Rtl parent");
        await parent.FocusAsync();

        // Left steps in: it opens the branch, then moves into it.
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(parent).ToHaveAttributeAsync("aria-expanded", "true");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Item("nav-rtl", "Rtl child")).ToBeFocusedAsync();

        // Right steps out, then closes it.
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(parent).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(parent).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task Rtl_ADirectionInheritedFromAnAncestor_MirrorsTheArrowKeysToo()
    {
        await OpenAsync(PAGE);

        var nav = Nav("nav-rtl-inherited");
        await Expect(nav).Not.ToHaveAttributeAsync("dir", new Regex(".*"));
        await Expect(nav).Not.ToHaveClassAsync(HasClass("bit-rtl"));

        var parent = Item("nav-rtl-inherited", "Inherited parent");
        await Expect(parent).ToHaveCSSAsync("padding-right", "0px");
        await Expect(Item("nav-rtl-inherited", "Inherited sibling")).ToHaveCSSAsync("padding-right", "27px");

        await parent.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(parent).ToHaveAttributeAsync("aria-expanded", "true");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Item("nav-rtl-inherited", "Inherited child")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(parent).ToBeFocusedAsync();

        // A left-to-right nav keeps Right for stepping in.
        var ltrParent = Item("nav-ltr", "Ltr parent");
        await ltrParent.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(ltrParent).ToHaveAttributeAsync("aria-expanded", "true");
    }

    // ---- Disabled ----

    [TestMethod]
    public async Task Disabled_TheWholeNavIsUnavailable_AndThePagesHandlersAreDropped()
    {
        await OpenAsync(PAGE);

        var nav = Nav("nav-disabled");
        await Expect(nav).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(nav).ToHaveCSSAsync("pointer-events", "none");

        var link = Item("nav-disabled", "Disabled link");
        await Expect(link).Not.ToHaveAttributeAsync("href", new Regex(".*"));
        await Expect(link).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(link).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Item("nav-disabled", "Disabled button")).ToBeDisabledAsync();
        await Expect(Item("nav-disabled", "Disabled button")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Item("nav-disabled", "Disabled parent")).ToBeDisabledAsync();

        // A click a screen reader dispatches reaches neither the item nor the page's handler on the root.
        await link.DispatchEventAsync("click");
        await nav.DispatchEventAsync("click");
        await nav.DispatchEventAsync("keydown", new Dictionary<string, object> { ["key"] = "Enter" });
        await NextFramesAsync();
        await Expect(Page.Locator("#disabled-item-clicks")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#disabled-root-clicks")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#disabled-root-keys")).ToHaveTextAsync("0");
        await Expect(Item("nav-disabled", "Disabled child")).ToHaveCountAsync(0);

        // Enabled again, everything is back.
        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(nav).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(link).ToHaveAttributeAsync("href", "/e2e/nav/url/somewhere");

        await Item("nav-disabled", "Disabled button").ClickAsync();
        await Expect(Page.Locator("#disabled-item-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#disabled-root-clicks")).ToHaveTextAsync("1");

        await Item("nav-disabled", "Disabled parent").ClickAsync();
        await Expect(Item("nav-disabled", "Disabled child")).ToBeVisibleAsync();
    }

    // ---- Keyboard ----

    [TestMethod]
    public async Task Keyboard_UpDownHomeEnd_WalkTheVisibleItems_SkippingDisabledAndSeparators()
    {
        await OpenAsync(PAGE);

        await Item("nav-keyboard", "Alpha").FocusAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Item("nav-keyboard", "Beta")).ToBeFocusedAsync();

        // The separator and the disabled Gamma are no stops; the children of the collapsed Beta are not on screen.
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Item("nav-keyboard", "Delta")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("End");
        await Expect(Item("nav-keyboard", "Echo")).ToBeFocusedAsync();

        // The ends do not wrap.
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Item("nav-keyboard", "Echo")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Home");
        await Expect(Item("nav-keyboard", "Alpha")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Item("nav-keyboard", "Alpha")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Item("nav-keyboard", "Alpha")).ToBeFocusedAsync();

        // The focus ring is drawn around the item the keyboard is on.
        Assert.AreNotEqual("none", await Item("nav-keyboard", "Alpha").EvaluateAsync<string>("el => getComputedStyle(el).boxShadow"));
    }

    [TestMethod]
    public async Task Keyboard_TheNavKeys_DoNotScrollThePage()
    {
        await OpenAsync(PAGE);

        await Page.EvaluateAsync("() => { window.__navKeys = []; document.addEventListener('keydown', e => window.__navKeys.push(e.key + ':' + e.defaultPrevented)); }");

        await Item("nav-keyboard", "Alpha").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("End");
        await Page.Keyboard.PressAsync("Home");
        await Page.Keyboard.PressAsync("Tab");

        var keys = await Page.EvaluateAsync<string[]>("() => window.__navKeys");
        CollectionAssert.AreEqual(new[] { "ArrowDown:true", "End:true", "Home:true", "Tab:false" }, keys);
    }

    [TestMethod]
    public async Task Keyboard_RightOpensThenStepsIn_LeftStepsOutThenCloses()
    {
        await OpenAsync(PAGE);

        var beta = Item("nav-keyboard", "Beta");
        await beta.FocusAsync();

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(beta).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(beta).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Item("nav-keyboard", "Beta one")).ToBeFocusedAsync();

        // Down skips the disabled Beta two.
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Item("nav-keyboard", "Beta three")).ToBeFocusedAsync();

        // Right on a leaf does nothing.
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Item("nav-keyboard", "Beta three")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(beta).ToBeFocusedAsync();
        await Expect(beta).ToHaveAttributeAsync("aria-expanded", "true");

        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(beta).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Item("nav-keyboard", "Beta one")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Keyboard_TheAsteriskOpensEverySibling_AndTypingJumpsToAnItem()
    {
        await OpenAsync(PAGE);

        await Item("nav-keyboard", "Alpha").FocusAsync();
        await Page.Keyboard.PressAsync("*");

        await Expect(Item("nav-keyboard", "Beta")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Item("nav-keyboard", "Delta")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Item("nav-keyboard", "Alpha")).ToBeFocusedAsync();

        // Typing jumps to the next item starting with the letter, and repeating it walks those items.
        await Page.Keyboard.PressAsync("d");
        await Expect(Item("nav-keyboard", "Delta")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("d");
        await Expect(Item("nav-keyboard", "Delta one")).ToBeFocusedAsync();

        // Keys typed in a row build one term: "dde" matches nothing, so the focus stays.
        await Page.Keyboard.PressAsync("e");
        await NextFramesAsync();
        await Expect(Item("nav-keyboard", "Delta one")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Keyboard_TypeAhead_WrapsAroundToTheFirstMatch()
    {
        await OpenAsync(PAGE);

        await Item("nav-keyboard", "Delta").FocusAsync();
        await Page.Keyboard.PressAsync("a");

        await Expect(Item("nav-keyboard", "Alpha")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Keyboard_EnterAndSpaceActivateTheFocusedItem()
    {
        await OpenAsync(PAGE);

        await Item("nav-keyboard", "Alpha").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#keyboard-selected")).ToHaveTextAsync("Alpha");
        await Expect(Item("nav-keyboard", "Alpha")).ToHaveClassAsync(HasClass("bit-nav-sel"));

        await Page.Keyboard.PressAsync("End");
        await Expect(Item("nav-keyboard", "Echo")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync(" ");
        await Expect(Page.Locator("#keyboard-selected")).ToHaveTextAsync("Echo");

        // On a parent without a URL they toggle it.
        await Item("nav-keyboard", "Delta").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Item("nav-keyboard", "Delta")).ToHaveAttributeAsync("aria-expanded", "true");
        await Page.Keyboard.PressAsync(" ");
        await Expect(Item("nav-keyboard", "Delta")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task Keyboard_TabReachesEveryEnabledItemInTurn()
    {
        await OpenAsync(PAGE);

        await Item("nav-keyboard", "Alpha").FocusAsync();
        var visited = new List<string> { await FocusedTextAsync() };

        for (var i = 0; i < 3; i++)
        {
            await Page.Keyboard.PressAsync("Tab");
            visited.Add(await FocusedTextAsync());
        }

        CollectionAssert.AreEqual(new[] { "Alpha", "Beta", "Delta", "Echo" }, visited);
    }

    [TestMethod]
    public async Task Keyboard_InSingleExpand_RightOpensOneBranchAndClosesTheOther()
    {
        await OpenAsync(PAGE);

        await Item("nav-keyboard-single", "First").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Item("nav-keyboard-single", "First child")).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Item("nav-keyboard-single", "First child")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Item("nav-keyboard-single", "Second")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        await Expect(Item("nav-keyboard-single", "Second child")).ToBeVisibleAsync();
        await Expect(Item("nav-keyboard-single", "First child")).ToHaveCountAsync(0);
    }

    // ---- Dynamic items ----

    [TestMethod]
    public async Task DynamicItems_ChangesToTheListShowUp_AndTheExpansionStateSurvives()
    {
        await OpenAsync(PAGE);

        var parent = Item("nav-dynamic", "Dynamic parent");
        await parent.ClickAsync();
        await Expect(Item("nav-dynamic", "Dynamic child")).ToBeVisibleAsync();

        await Page.Locator("#btn-add-root").ClickAsync();
        await Expect(Item("nav-dynamic", "Added 1")).ToBeVisibleAsync();
        await Expect(parent).ToHaveAttributeAsync("aria-expanded", "true");

        // A child appended in place to a nested list shows up too.
        await Page.Locator("#btn-add-child").ClickAsync();
        await Expect(Item("nav-dynamic", "Added child 2")).ToBeVisibleAsync();

        await Page.Locator("#btn-remove-root").ClickAsync();
        await Expect(Item("nav-dynamic", "Added 1")).ToHaveCountAsync(0);

        // A tree rebuilt out of new instances keeps the state the reader left it in.
        await Page.Locator("#btn-rebuild").ClickAsync();
        await Expect(parent).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Item("nav-dynamic", "Added child 2")).ToBeVisibleAsync();

        await parent.ClickAsync();
        await Expect(Item("nav-dynamic", "Dynamic child")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task DynamicItems_TheKeyOfAnItemCarriesItsStateAcrossARebuildThatReordersIt()
    {
        await OpenAsync(PAGE);

        await Item("nav-keyed", "Keyed A").ClickAsync();
        await Expect(Item("nav-keyed", "A child")).ToBeVisibleAsync();

        await Page.Locator("#btn-keyed-reverse").ClickAsync();

        await Expect(Page.Locator("#nav-keyed > ul > li > .bit-nav-ict .bit-nav-itx")).ToHaveTextAsync(["Keyed C", "Keyed B", "Keyed A"]);
        await Expect(Item("nav-keyed", "Keyed A")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Item("nav-keyed", "A child")).ToBeVisibleAsync();
        await Expect(Item("nav-keyed", "Keyed C")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    // ---- Lifecycle ----

    [TestMethod]
    public async Task Lifecycle_ReRendersOnParameterChanges_AndUnmountsCleanly()
    {
        await OpenAsync(PAGE);

        var nav = Nav("nav-lifecycle");
        await Expect(nav).ToBeVisibleAsync();

        await Page.Locator("#btn-cycle-size").ClickAsync();
        await Expect(nav).ToHaveClassAsync(HasClass("bit-nav-sm"));
        await Page.Locator("#btn-cycle-size").ClickAsync();
        await Expect(nav).ToHaveClassAsync(HasClass("bit-nav-md"));

        await Page.Locator("#btn-toggle-render-type").ClickAsync();
        await Expect(nav.Locator("button.bit-nav-gcb")).ToHaveCountAsync(1);
        await Page.Locator("#btn-toggle-render-type").ClickAsync();
        await Expect(nav.Locator(".bit-nav-gcb")).ToHaveCountAsync(0);

        await Item("nav-lifecycle", "Life parent").ClickAsync();
        await Item("nav-lifecycle", "Life child").FocusAsync();

        await Page.Locator("#btn-toggle-mount").ClickAsync();
        await Expect(Page.Locator("#lifecycle-mounted")).ToHaveTextAsync("False");
        await Expect(nav).ToHaveCountAsync(0);

        await Page.Locator("#btn-toggle-mount").ClickAsync();
        await Expect(nav).ToBeVisibleAsync();

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    // ---- Forced colors ----

    [TestMethod]
    public async Task ForcedColors_TheSelectionIsMarkedByAHighlightBar()
    {
        if (IsChromium is false) Assert.Inconclusive("Playwright emulates forced colors in Chromium only.");

        await OpenAsync(PAGE);
        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });

        var highlight = await ResolveColorAsync("Highlight");
        var selected = Item("nav-manual-default", "Cherry");
        var other = Item("nav-manual-default", "Apple");

        await Expect(selected).ToHaveCSSAsync("border-left-color", highlight);
        await Expect(other).Not.ToHaveCSSAsync("border-left-color", highlight);

        // The bar is never thinner than the thick stroke, and every item reserves the same room so nothing moves.
        var selectedWidth = await NumberStyleAsync(selected, "border-left-width");
        Assert.IsTrue(selectedWidth >= 2, $"The bar is {selectedWidth}px wide.");
        Assert.AreEqual(selectedWidth, await NumberStyleAsync(other, "border-left-width"), 0.01);

        // A disabled item is drawn in GrayText.
        await Expect(Item("nav-basic", "Archived").Locator(".bit-nav-itm")).ToHaveCSSAsync("color", await ResolveColorAsync("GrayText"));
    }

    // ---- Motion ----

    [TestMethod]
    public async Task Motion_TheChevronTurnsOverTheMotionDuration_WhichReducedMotionCollapses()
    {
        await OpenAsync(PAGE);

        var duration = await Chevron("nav-motion", "Motion parent").Locator("i").EvaluateAsync<string>("el => getComputedStyle(el).transitionDuration");
        Assert.IsTrue(double.Parse(duration.TrimEnd('s'), System.Globalization.CultureInfo.InvariantCulture) > 0.01, $"The chevron turns over a duration ({duration}).");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        var reduced = await Chevron("nav-motion", "Motion parent").Locator("i").EvaluateAsync<string>("el => getComputedStyle(el).transitionDuration");
        Assert.IsTrue(double.Parse(reduced.TrimEnd('s'), System.Globalization.CultureInfo.InvariantCulture) < 0.01, $"Reduced motion collapses it ({reduced}).");

        // ForceAnimation brings it back.
        var forced = await Chevron("nav-motion-force", "Forced parent").Locator("i").EvaluateAsync<string>("el => getComputedStyle(el).transitionDuration");
        Assert.AreEqual(duration, forced);
    }
}
