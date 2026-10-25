using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Navs;

/// <summary>
/// End-to-end tests of BitNavBar with the BitNavBarItem API (/e2e/nav-bar): the rendered list of links and buttons, the
/// disabled contract (the bar and an item), the manual mode with DefaultSelectedItem, DefaultSelectedKey and two-way and
/// one-way bound SelectedItem and SelectedKey, IconOnly, HideUnselectedText and InlineText, FitWidth, FullWidth, Justified
/// and every Alignment, SelectedIconName, Filled, the Line and Pill indicators and FlipIndicator, Scrollable (the
/// selection scrolled into view, the mouse wheel, ScrollItemIntoView), the header and footer, the Vertical rail, badges and
/// dots, every template and its render modes, the events and Reselectable, the keyboard (arrows, Home/End, RTL,
/// SingleTabStop, WrapNavigation, SelectOnFocus), dynamic items, the BitParams cascade, every Color with and without
/// Filled, external icons, every Size, Style/Class/Styles/Classes, the public CSS variables, Visibility, HtmlAttributes,
/// RTL (explicit and inherited), the public API, aria-current, the new-tab hint, SafeArea, the lifecycle and reduced motion.
/// </summary>
[TestClass]
public class BitNavBarE2ETests : E2ETestBase
{
    private const string PAGE = "/e2e/nav-bar";

    // The Color switch of BitNavBar.razor.cs, in the order of BitColor.
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

    private ILocator Bar(string id) => Page.Locator($"#{id}");

    private ILocator Item(string barId, string text)
    {
        return Page.Locator($"#{barId} .bit-nbr-itm").Filter(new() { Has = Page.Locator(".bit-nbr-txc", new() { HasTextRegex = Exact(text) }) });
    }

    private ILocator Labeled(string barId, string label) => Page.Locator($"#{barId} .bit-nbr-itm[aria-label='{label}']");

    private async Task ExpectSelectedAsync(string barId, string text)
    {
        await Expect(Page.Locator($"#{barId} .bit-nbr-sel")).ToHaveCountAsync(1);
        await Expect(Item(barId, text)).ToHaveClassAsync(HasClass("bit-nbr-sel"));
        await Expect(Item(barId, text)).ToHaveAttributeAsync("aria-current", "page");
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

    private static Task<string> AfterStyleAsync(ILocator locator, string property)
    {
        return locator.EvaluateAsync<string>($"el => getComputedStyle(el, '::after').getPropertyValue('{property}')");
    }

    private static Task<double> NumberStyleAsync(ILocator locator, string property)
    {
        return locator.EvaluateAsync<double>($"el => parseFloat(getComputedStyle(el).getPropertyValue('{property}'))");
    }

    private static async Task<LocatorBoundingBoxResult> BoxAsync(ILocator locator) => (await locator.BoundingBoxAsync())!;

    private Task WaitForAfterAsync(string selector, string property, string value)
    {
        return Page.WaitForFunctionAsync(
            "([s, p, v]) => { const el = document.querySelector(s); return !!el && getComputedStyle(el, '::after').getPropertyValue(p) === v; }",
            new object[] { selector, property, value });
    }

    // ---- Basic ----

    [TestMethod]
    public async Task Basic_Prerender_ContainsTheBarBeforeTheCircuitConnects()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{PAGE}");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "id=\"nbr-basic\"");
        StringAssert.Contains(html, "aria-label=\"Main\"");
        StringAssert.Contains(html, "href=\"/e2e/nav-bar/url/home\"");
        StringAssert.Contains(html, "Status: <span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Basic_Renders_ANavLandmarkHoldingAListOfLinksAndButtons()
    {
        await OpenAsync(PAGE);

        var bar = Bar("nbr-basic");
        await Expect(bar).ToHaveJSPropertyAsync("tagName", "NAV");
        await Expect(bar).ToHaveAttributeAsync("aria-label", "Main");
        await Expect(bar.Locator("> ul.bit-nbr-cnt")).ToHaveAttributeAsync("role", "list");
        await Expect(bar.Locator("> ul > li.bit-nbr-lit")).ToHaveCountAsync(4);

        var home = Item("nbr-basic", "Home");
        await Expect(home).ToHaveJSPropertyAsync("tagName", "A");
        await Expect(home).ToHaveAttributeAsync("href", "/e2e/nav-bar/url/home");
        await Expect(home.Locator(".bit-nbr-ico")).ToHaveClassAsync(HasClass("bit-icon--Home"));
        await Expect(home.Locator(".bit-nbr-txt")).ToHaveAttributeAsync("data-content", "Home");
        await Expect(Item("nbr-basic", "Search")).ToHaveAttributeAsync("title", "Search tip");

        var alerts = Item("nbr-basic", "Alerts");
        await Expect(alerts).ToHaveJSPropertyAsync("tagName", "BUTTON");
        await Expect(alerts).ToHaveAttributeAsync("type", "button");

        // Nothing on this page matches an item's URL, so nothing is selected.
        await Expect(bar.Locator(".bit-nbr-sel")).ToHaveCountAsync(0);

        // A horizontal bar: one row, the icon stacked over the text, the items spread over the width.
        var boxes = new List<LocatorBoundingBoxResult>();
        foreach (var text in new[] { "Home", "Search", "Alerts", "Profile" }) boxes.Add(await BoxAsync(Item("nbr-basic", text)));
        for (var i = 1; i < boxes.Count; i++)
        {
            Assert.AreEqual(boxes[0].Y, boxes[i].Y, 1);
            Assert.IsTrue(boxes[i].X > boxes[i - 1].X + boxes[i - 1].Width - 1);
        }
        var icon = await BoxAsync(home.Locator(".bit-nbr-ico"));
        var text0 = await BoxAsync(home.Locator(".bit-nbr-txt"));
        Assert.IsTrue(icon.Y + icon.Height <= text0.Y + 1, "The icon sits over the text.");
        await Expect(bar.Locator("> ul")).ToHaveCSSAsync("justify-content", "space-around");
    }

    // ---- Disabled ----

    [TestMethod]
    public async Task Disabled_TheWholeBarIsUnavailable_AndThePagesHandlersAreDropped()
    {
        await OpenAsync(PAGE);

        var bar = Bar("nbr-disabled");
        await Expect(bar).ToHaveClassAsync(HasClass("bit-dis"));

        var link = Item("nbr-disabled", "Link");
        await Expect(link).ToHaveJSPropertyAsync("tagName", "A");
        await Expect(link).Not.ToHaveAttributeAsync("href", new Regex(".*"));
        await Expect(link).Not.ToHaveAttributeAsync("target", new Regex(".*"));
        await Expect(link).ToHaveAttributeAsync("role", "link");
        await Expect(link).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(link).ToHaveClassAsync(HasClass("bit-nbr-dis"));
        await Expect(link.Locator(".bit-nbr-vhd")).ToHaveCountAsync(0);

        var button = Item("nbr-disabled", "Button");
        await Expect(button).ToBeDisabledAsync();
        await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");

        await link.DispatchEventAsync("click");
        await bar.DispatchEventAsync("click");
        await bar.DispatchEventAsync("keydown", new Dictionary<string, object> { ["key"] = "ArrowRight" });
        // The header is page content the disabled bar takes the pointer away from.
        await Expect(Page.Locator("#nbr-disabled .bit-nbr-hdr")).ToHaveCSSAsync("pointer-events", "none");
        await Page.Locator("#disabled-header-button").ClickAsync(new() { Force = true });
        await NextFramesAsync();

        await Expect(Page.Locator("#disabled-item-clicks")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#disabled-root-clicks")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#disabled-root-keys")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#disabled-header-clicks")).ToHaveTextAsync("0");

        // Enabled again, everything is back.
        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(bar).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(link).ToHaveAttributeAsync("href", "/e2e/nav-bar/url/link");
        await Expect(link).Not.ToHaveAttributeAsync("role", new Regex(".*"));

        await button.ClickAsync();
        await Expect(Page.Locator("#disabled-item-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#disabled-root-clicks")).ToHaveTextAsync("1");
        await Page.Locator("#disabled-header-button").ClickAsync();
        await Expect(Page.Locator("#disabled-header-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Disabled_AnItem_LosesItsLinkAndTabStop_AndIsDimmed()
    {
        await OpenAsync(PAGE);

        var offLink = Item("nbr-item-disabled", "Off link");
        await Expect(offLink).Not.ToHaveAttributeAsync("href", new Regex(".*"));
        await Expect(offLink).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(offLink).ToHaveAttributeAsync("role", "link");
        await Expect(Item("nbr-item-disabled", "Off button")).ToBeDisabledAsync();

        var enabledColor = await Item("nbr-item-disabled", "Home").EvaluateAsync<string>("el => getComputedStyle(el).color");
        await Expect(offLink).Not.ToHaveCSSAsync("color", enabledColor);
        await Expect(offLink).ToHaveCSSAsync("cursor", "default");

        await offLink.DispatchEventAsync("click");
        await NextFramesAsync();
        await Expect(Page.Locator("#nbr-item-disabled .bit-nbr-sel")).ToHaveCountAsync(0);

        // Neither Tab nor the arrows stop on them.
        await Item("nbr-item-disabled", "Home").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Item("nbr-item-disabled", "Profile")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Item("nbr-item-disabled", "Home")).ToBeFocusedAsync();
    }

    // ---- Manual ----

    [TestMethod]
    public async Task Manual_DefaultSelectedItemAndDefaultSelectedKey()
    {
        await OpenAsync(PAGE);

        await ExpectSelectedAsync("nbr-manual-default", "Search");
        await ExpectSelectedAsync("nbr-default-key", "Alerts");

        // A default is only where the bar starts: a click moves it.
        await Item("nbr-default-key", "Home").ClickAsync();
        await ExpectSelectedAsync("nbr-default-key", "Home");
    }

    [TestMethod]
    public async Task Manual_ClickingSelects_AndTheTwoWaySelectedItemFollows()
    {
        await OpenAsync(PAGE);

        await Item("nbr-manual-bound", "Alerts").ClickAsync();
        await ExpectSelectedAsync("nbr-manual-bound", "Alerts");
        await Expect(Page.Locator("#bound-selected")).ToHaveTextAsync("Alerts");
        await Expect(Page.Locator("#bound-select-count")).ToHaveTextAsync("1");

        await Item("nbr-manual-bound", "Alerts").ClickAsync();
        await NextFramesAsync();
        await Expect(Page.Locator("#bound-select-count")).ToHaveTextAsync("1");

        await Page.Locator("#btn-select-profile").ClickAsync();
        await ExpectSelectedAsync("nbr-manual-bound", "Profile");

        await Page.Locator("#btn-clear-selection").ClickAsync();
        await Expect(Page.Locator("#nbr-manual-bound .bit-nbr-sel")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#nbr-manual-bound [aria-current]")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Manual_TheTwoWaySelectedKeyFollowsBothWays()
    {
        await OpenAsync(PAGE);

        await Expect(Page.Locator("#selected-key")).ToHaveTextAsync("none");

        await Item("nbr-selected-key", "Profile").ClickAsync();
        await ExpectSelectedAsync("nbr-selected-key", "Profile");
        await Expect(Page.Locator("#selected-key")).ToHaveTextAsync("profile");

        await Page.Locator("#btn-key-search").ClickAsync();
        await ExpectSelectedAsync("nbr-selected-key", "Search");

        // A key no item carries waits for one; meanwhile the selection stays where it was.
        await Page.Locator("#btn-key-unknown").ClickAsync();
        await Expect(Page.Locator("#selected-key")).ToHaveTextAsync("unknown");
        await ExpectSelectedAsync("nbr-selected-key", "Search");
    }

    [TestMethod]
    public async Task Manual_AOneWayBoundSelectionIsHeld()
    {
        await OpenAsync(PAGE);

        await ExpectSelectedAsync("nbr-oneway", "Home");
        await Item("nbr-oneway", "Alerts").ClickAsync();
        await NextFramesAsync();
        await ExpectSelectedAsync("nbr-oneway", "Home");

        await ExpectSelectedAsync("nbr-oneway-key", "Profile");
        await Item("nbr-oneway-key", "Search").ClickAsync();
        await NextFramesAsync();
        await ExpectSelectedAsync("nbr-oneway-key", "Profile");
    }

    // ---- Labels ----

    [TestMethod]
    public async Task IconOnly_RendersNoTexts_AndNamesEveryItem()
    {
        await OpenAsync(PAGE);

        var bar = Bar("nbr-icononly");
        await Expect(bar).ToHaveClassAsync(HasClass("bit-nbr-ion"));
        await Expect(bar.Locator(".bit-nbr-txt")).ToHaveCountAsync(0);

        var home = Labeled("nbr-icononly", "Home");
        await Expect(home).ToHaveAttributeAsync("title", "Home");
        await Expect(home.Locator(".bit-nbr-ico")).ToBeVisibleAsync();
        await Expect(bar.GetByRole(AriaRole.Button, new() { Name = "Profile", Exact = true })).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task HideUnselectedText_ShowsTheTextOfTheSelectedItemOnly()
    {
        await OpenAsync(PAGE);

        var bar = Bar("nbr-hide-unselected");
        await Expect(bar).ToHaveClassAsync(HasClass("bit-nbr-hut"));

        var home = Item("nbr-hide-unselected", "Home");
        var search = Item("nbr-hide-unselected", "Search");
        await Expect(home.Locator(".bit-nbr-txt")).ToHaveCSSAsync("opacity", "1");
        await Expect(search.Locator(".bit-nbr-txt")).ToHaveCSSAsync("opacity", "0");
        await Expect(home).Not.ToHaveAttributeAsync("aria-label", new Regex(".*"));
        await Expect(search).ToHaveAttributeAsync("aria-label", "Search");
        await Expect(search).ToHaveAttributeAsync("title", "Search");

        await search.ClickAsync();

        await Expect(search.Locator(".bit-nbr-txt")).ToHaveCSSAsync("opacity", "1");
        await Expect(home.Locator(".bit-nbr-txt")).ToHaveCSSAsync("opacity", "0");
        await Expect(search).Not.ToHaveAttributeAsync("aria-label", new Regex(".*"));
        await Expect(home).ToHaveAttributeAsync("aria-label", "Home");
    }

    [TestMethod]
    public async Task InlineText_PutsTheTextBesideTheIcon()
    {
        await OpenAsync(PAGE);

        var home = Item("nbr-inline", "Home");
        await Expect(Bar("nbr-inline")).ToHaveClassAsync(HasClass("bit-nbr-inl"));
        await Expect(home).ToHaveCSSAsync("flex-direction", "row");

        var icon = await BoxAsync(home.Locator(".bit-nbr-ico"));
        var text = await BoxAsync(home.Locator(".bit-nbr-txt"));
        Assert.IsTrue(text.X >= icon.X + icon.Width - 1, "The text follows the icon.");
        Assert.IsTrue(Math.Abs((icon.Y + icon.Height / 2) - (text.Y + text.Height / 2)) < 4, "Both sit on one line.");
    }

    // ---- Width & Alignment ----

    [TestMethod]
    public async Task Width_FitWidthFullWidthAndJustified()
    {
        await OpenAsync(PAGE);

        var container = await BoxAsync(Page.Locator("#width-container"));
        var fit = await BoxAsync(Bar("nbr-fit"));
        var full = await BoxAsync(Bar("nbr-full"));

        await Expect(Bar("nbr-fit")).ToHaveClassAsync(HasClass("bit-nbr-ftw"));
        await Expect(Bar("nbr-full")).ToHaveClassAsync(HasClass("bit-nbr-flw"));
        Assert.AreEqual(container.Width, full.Width, 0.5);
        Assert.IsTrue(fit.Width < container.Width / 2, $"A FitWidth bar ({fit.Width}px) takes the room of its items only.");

        // Justified gives every item an equal share, whatever its label.
        await Expect(Bar("nbr-justified")).ToHaveClassAsync(HasClass("bit-nbr-jst"));
        var widths = await Page.Locator("#nbr-justified .bit-nbr-lit").EvaluateAllAsync<double[]>("els => els.map(e => e.getBoundingClientRect().width)");
        Assert.AreEqual(3, widths.Length);
        Assert.AreEqual(widths[0], widths[1], 1);
        Assert.AreEqual(widths[1], widths[2], 1);
    }

    [TestMethod]
    [DataRow("start", "bit-nbr-str", "flex-start")]
    [DataRow("end", "bit-nbr-end", "flex-end")]
    [DataRow("center", "bit-nbr-ctr", "center")]
    [DataRow("space-between", "bit-nbr-sbt", "space-between")]
    [DataRow("space-around", "bit-nbr-sar", "space-around")]
    [DataRow("space-evenly", "bit-nbr-sev", "space-evenly")]
    [DataRow("baseline", null, "space-around")]
    [DataRow("stretch", null, "space-around")]
    public async Task Alignment_DistributesTheItemsAlongTheBar(string name, string? cssClass, string justify)
    {
        await OpenAsync(PAGE);

        var bar = Bar($"nbr-align-{name}");
        if (cssClass is not null)
        {
            await Expect(bar).ToHaveClassAsync(HasClass(cssClass));
        }
        else
        {
            await Expect(bar).ToHaveAttributeAsync("class", "bit-nbr");
        }

        await Expect(bar.Locator("> ul")).ToHaveCSSAsync("justify-content", justify);

        var list = await BoxAsync(bar.Locator("> ul"));
        var first = await BoxAsync(bar.Locator(".bit-nbr-lit").First);
        var last = await BoxAsync(bar.Locator(".bit-nbr-lit").Last);
        var startGap = first.X - list.X;
        var endGap = list.X + list.Width - (last.X + last.Width);

        switch (justify)
        {
            case "flex-start":
            case "space-between":
                Assert.AreEqual(0, startGap, 1);
                break;
            case "flex-end":
                Assert.AreEqual(0, endGap, 1);
                Assert.IsTrue(startGap > 50);
                break;
            case "center":
                Assert.AreEqual(startGap, endGap, 1.5);
                Assert.IsTrue(startGap > 50);
                break;
            default:
                Assert.AreEqual(startGap, endGap, 1.5);
                Assert.IsTrue(startGap > 1);
                break;
        }
    }

    // ---- Selection ----

    [TestMethod]
    public async Task SelectedIconName_SwapsTheSelectedItemToItsOwnGlyph()
    {
        await OpenAsync(PAGE);

        var home = Item("nbr-selected-icon", "Home").Locator(".bit-nbr-ico");
        var favorite = Item("nbr-selected-icon", "Favorite").Locator(".bit-nbr-ico");
        await Expect(home).ToHaveClassAsync(HasClass("bit-icon--HomeSolid"));
        await Expect(favorite).ToHaveClassAsync(HasClass("bit-icon--FavoriteStar"));

        await Item("nbr-selected-icon", "Favorite").ClickAsync();

        await Expect(favorite).ToHaveClassAsync(HasClass("bit-icon--FavoriteStarFill"));
        await Expect(home).ToHaveClassAsync(HasClass("bit-icon--Home"));

        // An item without a selected icon keeps its own.
        await Item("nbr-selected-icon", "Plain").ClickAsync();
        await Expect(Item("nbr-selected-icon", "Plain").Locator(".bit-nbr-ico")).ToHaveClassAsync(HasClass("bit-icon--Ringer"));
    }

    [TestMethod]
    public async Task Filled_FillsTheSelectedItem_UnfilledColorsItOnly()
    {
        await OpenAsync(PAGE);

        var primary = await ResolveColorAsync("var(--bit-clr-pri)");
        var primaryActive = await ResolveColorAsync("var(--bit-clr-pri-active)");
        var primaryText = await ResolveColorAsync("var(--bit-clr-pri-text)");

        await Expect(Bar("nbr-filled")).ToHaveClassAsync(HasClass("bit-nbr-fil"));
        await Expect(Item("nbr-filled", "Home")).ToHaveCSSAsync("background-color", primaryActive);
        await Expect(Item("nbr-filled", "Home")).ToHaveCSSAsync("color", primaryText);

        await Expect(Item("nbr-indicator-none", "Home")).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
        await Expect(Item("nbr-indicator-none", "Home")).ToHaveCSSAsync("color", primary);
        await Expect(Item("nbr-indicator-none", "Home")).ToHaveCSSAsync("font-weight", "600");
        await Expect(Item("nbr-indicator-none", "Search")).Not.ToHaveCSSAsync("color", primary);

        // Filled also fills the hovered item.
        var hover = await ResolveColorAsync("var(--bit-clr-pri-hover)");
        await Item("nbr-filled", "Search").HoverAsync();
        await Expect(Item("nbr-filled", "Search")).ToHaveCSSAsync("background-color", hover);
    }

    [TestMethod]
    public async Task LineIndicator_DrawsALineAlongTheBottomOfTheSelectedItem_OrTheTopWhenFlipped()
    {
        await OpenAsync(PAGE);

        await Expect(Bar("nbr-line")).ToHaveClassAsync(HasClass("bit-nbr-lin"));
        var selected = Item("nbr-line", "Home");
        var other = Item("nbr-line", "Search");

        Assert.AreEqual("none", await AfterStyleAsync(selected, "transform"));
        Assert.AreEqual("matrix(0, 0, 0, 1, 0, 0)", await AfterStyleAsync(other, "transform"));
        Assert.AreEqual("0px", await AfterStyleAsync(selected, "bottom"));
        Assert.AreNotEqual("0px", await AfterStyleAsync(selected, "top"));
        Assert.AreEqual(await ResolveColorAsync("var(--bit-clr-pri)"), await AfterStyleAsync(selected, "background-color"));

        // The line moves with the selection.
        await other.ClickAsync();
        await WaitForAfterAsync("#nbr-line .bit-nbr-sel", "transform", "none");
        await ExpectSelectedAsync("nbr-line", "Search");
        await Page.WaitForFunctionAsync("() => getComputedStyle(document.querySelector('#nbr-line .bit-nbr-itm'), '::after').transform === 'matrix(0, 0, 0, 1, 0, 0)'");

        var flipped = Item("nbr-line-flip", "Home");
        await Expect(Bar("nbr-line-flip")).ToHaveClassAsync(HasClass("bit-nbr-fli"));
        Assert.AreEqual("0px", await AfterStyleAsync(flipped, "top"));
        Assert.AreNotEqual("0px", await AfterStyleAsync(flipped, "bottom"));
    }

    [TestMethod]
    public async Task PillIndicator_FillsAPillBehindTheIconOfTheSelectedItem()
    {
        await OpenAsync(PAGE);

        await Expect(Bar("nbr-pill")).ToHaveClassAsync(HasClass("bit-nbr-pil"));
        var pill = Item("nbr-pill", "Home").Locator(".bit-nbr-icc");
        await Expect(pill).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-bg-ter)"));
        await Expect(Item("nbr-pill", "Home")).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
        await Expect(Item("nbr-pill", "Search").Locator(".bit-nbr-icc")).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");

        // The pill is a pill: fully rounded and wider than tall.
        var box = await BoxAsync(pill);
        Assert.IsTrue(box.Width > box.Height);
        Assert.AreNotEqual("0px", await pill.EvaluateAsync<string>("el => getComputedStyle(el).borderTopLeftRadius"));

        await Expect(Item("nbr-pill-filled", "Home").Locator(".bit-nbr-icc")).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-pri-active)"));
        await Expect(Item("nbr-pill-filled", "Home")).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
    }

    // ---- Scrollable ----

    [TestMethod]
    public async Task Scrollable_TheItemsScroll_AndTheSelectionIsScrolledIntoView()
    {
        await OpenAsync(PAGE);

        var bar = Bar("nbr-scroll");
        var list = bar.Locator("> ul");
        await Expect(bar).ToHaveClassAsync(HasClass("bit-nbr-scr"));
        await Expect(list).ToHaveCSSAsync("overflow-x", "auto");
        Assert.IsTrue(await list.EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth + 100"), "The items keep their size and overflow the bar.");
        Assert.AreEqual(0, await list.EvaluateAsync<double>("el => el.scrollLeft"));

        await Page.Locator("#btn-select-last").ClickAsync();
        await ExpectSelectedAsync("nbr-scroll", "Item 15");

        await Page.WaitForFunctionAsync(@"() => {
            const list = document.querySelector('#nbr-scroll > ul');
            const item = document.querySelector('#nbr-scroll .bit-nbr-sel');
            const l = list.getBoundingClientRect(), i = item.getBoundingClientRect();
            return list.scrollLeft > 0 && i.right <= l.right + 1 && i.left >= l.left - 1;
        }");

        // The page under the bar did not move along with it.
        Assert.AreEqual(0, await Page.EvaluateAsync<double>("() => document.scrollingElement.scrollLeft"));

        await Page.Locator("#btn-select-first").ClickAsync();
        await Page.WaitForFunctionAsync("() => document.querySelector('#nbr-scroll > ul').scrollLeft === 0");

        // Without Scrollable the same items are squeezed into the bar instead, and the list does not scroll.
        await Expect(Page.Locator("#nbr-squeezed > ul")).ToHaveCSSAsync("overflow-x", "visible");
        var squeezed = (await BoxAsync(Page.Locator("#nbr-squeezed .bit-nbr-itm").First)).Width;
        var scrolled = (await BoxAsync(Page.Locator("#nbr-scroll .bit-nbr-itm").First)).Width;
        Assert.IsTrue(squeezed < scrolled, $"A squeezed item ({squeezed}px) is narrower than a scrolling one ({scrolled}px).");
    }

    [TestMethod]
    public async Task Scrollable_TheMouseWheelScrollsTheBarSideways()
    {
        await OpenAsync(PAGE);

        var list = Page.Locator("#nbr-scroll > ul");
        await list.HoverAsync();
        await Page.Mouse.WheelAsync(0, 200);

        await Page.WaitForFunctionAsync("() => document.querySelector('#nbr-scroll > ul').scrollLeft > 50");

        // Back to the start, the wheel is handed back to the page rather than trapped.
        await Page.Mouse.WheelAsync(0, -2000);
        await Page.WaitForFunctionAsync("() => document.querySelector('#nbr-scroll > ul').scrollLeft === 0");
    }

    [TestMethod]
    public async Task Scrollable_ScrollItemIntoView_BringsAnyItemIntoTheScrolledArea()
    {
        await OpenAsync(PAGE);

        await Page.Locator("#btn-scroll-into-view").ClickAsync();

        await Page.WaitForFunctionAsync(@"() => {
            const list = document.querySelector('#nbr-scroll > ul');
            const item = [...list.querySelectorAll('.bit-nbr-txc')].find(e => e.textContent === 'Item 10').closest('.bit-nbr-itm');
            const l = list.getBoundingClientRect(), i = item.getBoundingClientRect();
            return list.scrollLeft > 0 && i.right <= l.right + 1;
        }");
        await Expect(Page.Locator("#nbr-scroll .bit-nbr-sel")).ToHaveCountAsync(0);
    }

    // ---- Header & Footer ----

    [TestMethod]
    public async Task HeaderAndFooter_RenderAroundTheItems_OutsideTheirList()
    {
        await OpenAsync(PAGE);

        var bar = Bar("nbr-hf");
        await Expect(bar.Locator("> .bit-nbr-hdr #hf-header")).ToHaveTextAsync("Logo");
        await Expect(bar.Locator("> .bit-nbr-ftr #hf-footer")).ToHaveTextAsync("More");
        await Expect(bar.Locator("ul #hf-header, ul #hf-footer")).ToHaveCountAsync(0);

        var order = await bar.EvaluateAsync<string>("el => [...el.children].map(c => c.className.split(' ')[0]).join(',')");
        Assert.AreEqual("bit-nbr-hdr,bit-nbr-cnt,bit-nbr-ftr", order);

        var header = await BoxAsync(bar.Locator(".bit-nbr-hdr"));
        var list = await BoxAsync(bar.Locator("> ul"));
        var footer = await BoxAsync(bar.Locator(".bit-nbr-ftr"));
        Assert.IsTrue(header.X + header.Width <= list.X + 1 && list.X + list.Width <= footer.X + 1);
    }

    // ---- Vertical ----

    [TestMethod]
    public async Task Vertical_StacksTheItemsInARail_AndTheArrowsWalkIt()
    {
        await OpenAsync(PAGE);

        var bar = Bar("nbr-vertical");
        await Expect(bar).ToHaveClassAsync(HasClass("bit-nbr-vrt"));
        await Expect(bar).ToHaveCSSAsync("flex-direction", "column");
        await Expect(bar.Locator("> ul")).ToHaveCSSAsync("flex-direction", "column");

        var home = await BoxAsync(Item("nbr-vertical", "Home"));
        var search = await BoxAsync(Item("nbr-vertical", "Search"));
        Assert.AreEqual(home.X, search.X, 1);
        Assert.IsTrue(search.Y >= home.Y + home.Height - 1);

        await Item("nbr-vertical", "Home").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Item("nbr-vertical", "Search")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Item("nbr-vertical", "Home")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Item("nbr-vertical", "Search")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Vertical_TheLineRunsDownTheLeadingEdge_OrTheTrailingOneWhenFlipped()
    {
        await OpenAsync(PAGE);

        var line = Item("nbr-vertical-line", "Home");
        Assert.AreEqual("0px", await AfterStyleAsync(line, "left"));
        Assert.AreNotEqual("0px", await AfterStyleAsync(line, "right"));
        Assert.AreEqual("none", await AfterStyleAsync(line, "transform"));
        Assert.AreEqual("matrix(1, 0, 0, 0, 0, 0)", await AfterStyleAsync(Item("nbr-vertical-line", "Search"), "transform"));
        var width = double.Parse((await AfterStyleAsync(line, "width")).Replace("px", ""), CultureInfo.InvariantCulture);
        var height = double.Parse((await AfterStyleAsync(line, "height")).Replace("px", ""), CultureInfo.InvariantCulture);
        Assert.IsTrue(height > width, "The line runs along the height of the item.");

        var flipped = Item("nbr-vertical-line-flip", "Home");
        Assert.AreEqual("0px", await AfterStyleAsync(flipped, "right"));
        Assert.AreNotEqual("0px", await AfterStyleAsync(flipped, "left"));
    }

    [TestMethod]
    public async Task Vertical_AlignmentCentersTheItemsBetweenTheHeaderAndTheFooter()
    {
        await OpenAsync(PAGE);

        var bar = Bar("nbr-vertical-center");
        var list = await BoxAsync(bar.Locator("> ul"));
        var first = await BoxAsync(bar.Locator(".bit-nbr-lit").First);
        var last = await BoxAsync(bar.Locator(".bit-nbr-lit").Last);
        Assert.AreEqual(first.Y - list.Y, list.Y + list.Height - (last.Y + last.Height), 1.5);

        var header = await BoxAsync(Page.Locator("#vc-header"));
        var footer = await BoxAsync(Page.Locator("#vc-footer"));
        Assert.IsTrue(header.Y < first.Y && footer.Y > last.Y);
    }

    [TestMethod]
    public async Task Vertical_AScrollableRailScrollsDown_ToTheSelection()
    {
        await OpenAsync(PAGE);

        var list = Page.Locator("#nbr-vertical-scroll > ul");
        Assert.IsTrue(await list.EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight"));

        await Page.Locator("#btn-vertical-last").ClickAsync();
        await ExpectSelectedAsync("nbr-vertical-scroll", "Rail 12");
        await Page.WaitForFunctionAsync(@"() => {
            const list = document.querySelector('#nbr-vertical-scroll > ul');
            const item = list.querySelector('.bit-nbr-sel');
            return list.scrollTop > 0 && item.getBoundingClientRect().bottom <= list.getBoundingClientRect().bottom + 1;
        }");
        Assert.AreEqual(0, await list.EvaluateAsync<double>("el => el.scrollLeft"));
    }

    // ---- Badge ----

    [TestMethod]
    public async Task Badge_SitsOnTheIcon_AndIsFoldedIntoTheName()
    {
        await OpenAsync(PAGE);

        var inbox = Item("nbr-badge", "Inbox");
        var badge = inbox.Locator(".bit-nbr-bdg");
        await Expect(badge).ToHaveTextAsync("12");
        await Expect(badge).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(badge).ToHaveCSSAsync("position", "absolute");
        await Expect(badge).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-err)"));
        await Expect(inbox).ToHaveAttributeAsync("aria-label", "Inbox (12)");

        var icon = await BoxAsync(inbox.Locator(".bit-nbr-ico"));
        var badgeBox = await BoxAsync(badge);
        Assert.IsTrue(badgeBox.X > icon.X && badgeBox.Y < icon.Y + icon.Height / 2, "The badge overlaps the top end of the icon.");

        await Expect(Item("nbr-badge", "Chat")).ToHaveAttributeAsync("aria-label", "Chat (3 unread chats)");

        var alerts = Item("nbr-badge", "Alerts");
        await Expect(alerts.Locator(".bit-nbr-bdg.bit-nbr-bdt")).ToHaveTextAsync("");
        await Expect(alerts).ToHaveAttributeAsync("aria-label", "Alerts (New alerts)");
        var dot = await BoxAsync(alerts.Locator(".bit-nbr-bdt"));
        Assert.AreEqual(dot.Width, dot.Height, 0.5);
        Assert.IsTrue(dot.Height < badgeBox.Height);

        // A dot with no description is not announced at all.
        await Expect(Item("nbr-badge", "Silent")).Not.ToHaveAttributeAsync("aria-label", new Regex(".*"));

        // An item without an icon lays its badge out in its own flow.
        var noIcon = Item("nbr-badge", "No icon");
        await Expect(noIcon.Locator(".bit-nbr-icc")).ToHaveClassAsync(HasClass("bit-nbr-nic"));
        await Expect(noIcon.Locator(".bit-nbr-bdg")).ToHaveCSSAsync("position", "static");

        await Expect(Item("nbr-badge", "Named")).ToHaveAttributeAsync("aria-label", "Custom (1)");
    }

    // ---- Templates ----

    [TestMethod]
    public async Task Templates_ItemTemplateTheItemsOwnAndTheReplaceMode()
    {
        await OpenAsync(PAGE);

        var templated = Page.Locator("#nbr-item-template .bit-nbr-itm .custom-item", new() { HasText = "Custom: Search" });
        await Expect(templated).ToBeVisibleAsync();
        await Expect(Page.Locator("#nbr-item-template .bit-nbr-txt")).ToHaveCountAsync(0);
        await templated.ClickAsync();
        await Expect(Page.Locator("#template-clicked")).ToHaveTextAsync("Search");
        await Expect(Page.Locator("#nbr-item-template .bit-nbr-sel .custom-item")).ToHaveTextAsync("Custom: Search");

        var own = Bar("nbr-own-template");
        await Expect(own.Locator(".bit-nbr-itm .own-template")).ToHaveTextAsync("Own: Own");
        await Expect(own.Locator("li > button.own-replaced")).ToHaveTextAsync("New");
        await Expect(own.Locator(".bit-nbr-itm")).ToHaveCountAsync(3);

        // The replaced item is left out of the keyboard navigation.
        await own.Locator(".bit-nbr-itm").Nth(1).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Item("nbr-own-template", "Profile")).ToBeFocusedAsync();

        var replaced = Bar("nbr-replace-template");
        await Expect(replaced.Locator(".bit-nbr-itm")).ToHaveCountAsync(0);
        await Expect(replaced.Locator("li.bit-nbr-lit > button.replaced-control")).ToHaveCountAsync(4);
    }

    // ---- Events ----

    [TestMethod]
    public async Task Events_ClickAndSelect_AndReselectable()
    {
        await OpenAsync(PAGE);

        await Item("nbr-events", "Search").ClickAsync();
        await Expect(Page.Locator("#event-click-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#event-last-clicked")).ToHaveTextAsync("Search");
        await Expect(Page.Locator("#event-select-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#event-last-selected")).ToHaveTextAsync("Search");

        await Item("nbr-events", "Search").ClickAsync();
        await NextFramesAsync();
        await Expect(Page.Locator("#event-click-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#event-select-count")).ToHaveTextAsync("1");

        await Page.Locator("#btn-toggle-reselectable").ClickAsync();
        await Item("nbr-events", "Search").ClickAsync();
        await Expect(Page.Locator("#event-click-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#event-select-count")).ToHaveTextAsync("2");

        await Item("nbr-events", "Profile").ClickAsync();
        await Expect(Page.Locator("#event-click-count")).ToHaveTextAsync("3");
        await Expect(Page.Locator("#event-last-selected")).ToHaveTextAsync("Profile");
    }

    // ---- Keyboard ----

    [TestMethod]
    public async Task Keyboard_TheArrowsHomeAndEnd_MoveAlongTheBar_SkippingDisabledItems()
    {
        await OpenAsync(PAGE);

        await Page.EvaluateAsync("() => { window.__barKeys = []; document.addEventListener('keydown', e => window.__barKeys.push(e.key + ':' + e.defaultPrevented)); }");

        await Item("nbr-keyboard", "Home").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Item("nbr-keyboard", "Search")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Item("nbr-keyboard", "Alerts")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(Item("nbr-keyboard", "Profile")).ToBeFocusedAsync();

        // The ends do not wrap by default.
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Item("nbr-keyboard", "Profile")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Home");
        await Expect(Item("nbr-keyboard", "Home")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Item("nbr-keyboard", "Search")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Item("nbr-keyboard", "Home")).ToBeFocusedAsync();

        // By default every item is a tab stop of its own.
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Item("nbr-keyboard", "Search")).ToBeFocusedAsync();

        var keys = await Page.EvaluateAsync<string[]>("() => window.__barKeys");
        CollectionAssert.AreEqual(
            new[] { "ArrowRight:true", "ArrowRight:true", "End:true", "ArrowDown:true", "Home:true", "ArrowDown:true", "ArrowUp:true", "Tab:false" },
            keys);

        // Enter and Space activate the focused item.
        await Page.Keyboard.PressAsync("Enter");
        await ExpectSelectedAsync("nbr-keyboard", "Search");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Item("nbr-keyboard", "Alerts")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync(" ");
        await ExpectSelectedAsync("nbr-keyboard", "Alerts");

        // Left at the first item stays there. (Last: a horizontal arrow reads the direction in a round trip, and a key
        // pressed before it returns would race the focus it puts back.)
        await Page.Keyboard.PressAsync("Home");
        await Expect(Item("nbr-keyboard", "Home")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await NextFramesAsync();
        await Expect(Item("nbr-keyboard", "Home")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task SingleTabStop_TheBarIsOneTabStop_ThatFollowsTheFocus()
    {
        await OpenAsync(PAGE);

        var tabIndexes = await Page.Locator("#nbr-single-tab .bit-nbr-itm").EvaluateAllAsync<string[]>("els => els.map(e => e.getAttribute('tabindex'))");
        CollectionAssert.AreEqual(new[] { "-1", "0", "-1", "-1" }, tabIndexes, "The selected item holds the stop.");

        // Tab enters the bar on the stop, and leaves it in one press.
        await Item("nbr-keyboard", "Profile").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Item("nbr-single-tab", "Search")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Item("nbr-single-tab", "Alerts")).ToBeFocusedAsync();
        await Expect(Item("nbr-single-tab", "Alerts")).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Item("nbr-single-tab", "Search")).ToHaveAttributeAsync("tabindex", "-1");

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Bar("nbr-single-tab").Locator(":focus")).ToHaveCountAsync(0);

        // Shift+Tab comes back to where the reader left the bar.
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Item("nbr-single-tab", "Alerts")).ToBeFocusedAsync();

        // The arrows do not select.
        await ExpectSelectedAsync("nbr-single-tab", "Search");
    }

    [TestMethod]
    public async Task WrapNavigation_TheArrowsWrapAroundAtBothEnds()
    {
        await OpenAsync(PAGE);

        await Item("nbr-wrap", "Search").FocusAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(Item("nbr-wrap", "Profile")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Item("nbr-wrap", "Home")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Item("nbr-wrap", "Profile")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task SelectOnFocus_TheSelectionFollowsTheFocus_InTheManualModeOnly()
    {
        await OpenAsync(PAGE);

        await Item("nbr-select-on-focus", "Search").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        await Expect(Item("nbr-select-on-focus", "Alerts")).ToBeFocusedAsync();
        await ExpectSelectedAsync("nbr-select-on-focus", "Alerts");
        await Expect(Page.Locator("#focus-selected")).ToHaveTextAsync("Alerts");

        await Page.Keyboard.PressAsync("Home");
        await ExpectSelectedAsync("nbr-select-on-focus", "Home");

        // In the automatic mode the URL selects, never the focus.
        await Item("nbr-select-on-focus-auto", "Search").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Item("nbr-select-on-focus-auto", "Alerts")).ToBeFocusedAsync();
        await Expect(Page.Locator("#nbr-select-on-focus-auto .bit-nbr-sel")).ToHaveCountAsync(0);
    }

    // ---- Dynamic items ----

    [TestMethod]
    public async Task DynamicItems_AddRemoveAndReverse_KeepTheSelection()
    {
        await OpenAsync(PAGE);

        await Item("nbr-dynamic", "Profile").ClickAsync();
        await ExpectSelectedAsync("nbr-dynamic", "Profile");

        await Page.Locator("#btn-add-item").ClickAsync();
        await Expect(Page.Locator("#nbr-dynamic .bit-nbr-txc")).ToHaveTextAsync(["Home", "Search", "Alerts", "Profile", "Added 1"]);

        await Page.Locator("#btn-reverse-items").ClickAsync();
        await Expect(Page.Locator("#nbr-dynamic .bit-nbr-txc")).ToHaveTextAsync(["Added 1", "Profile", "Alerts", "Search", "Home"]);
        await ExpectSelectedAsync("nbr-dynamic", "Profile");

        // The keyboard follows the new order.
        await Item("nbr-dynamic", "Added 1").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Item("nbr-dynamic", "Profile")).ToBeFocusedAsync();

        await Page.Locator("#btn-remove-item").ClickAsync();
        await Expect(Page.Locator("#nbr-dynamic .bit-nbr-txc")).ToHaveTextAsync(["Added 1", "Profile", "Alerts", "Search"]);
        await ExpectSelectedAsync("nbr-dynamic", "Profile");
        await Expect(Page.Locator("#dynamic-selected")).ToHaveTextAsync("Profile");
    }

    // ---- Cascading parameters ----

    [TestMethod]
    public async Task BitParams_HandsItsDefaults_AndABarKeepsWhatItSetsItself()
    {
        await OpenAsync(PAGE);

        var all = Bar("nbr-params-all");
        await Expect(all).ToHaveClassAsync(HasClass("bit-nbr-fil"));
        await Expect(all).ToHaveClassAsync(HasClass("bit-nbr-pil"));
        await Expect(all).ToHaveClassAsync(HasClass("bit-nbr-err"));
        await Expect(all).ToHaveClassAsync(HasClass("bit-nbr-ion"));
        // The cascaded Manual mode is what lets DefaultSelectedItem select.
        await Expect(all.Locator(".bit-nbr-sel")).ToHaveCountAsync(1);
        await Expect(all.Locator(".bit-nbr-txt")).ToHaveCountAsync(0);

        var own = Bar("nbr-params-own");
        await Expect(own).ToHaveClassAsync(HasClass("bit-nbr-lin"));
        await Expect(own).Not.ToHaveClassAsync(HasClass("bit-nbr-pil"));
        await Expect(own).ToHaveClassAsync(HasClass("bit-nbr-fil"));
        await Expect(own).ToHaveClassAsync(HasClass("bit-nbr-err"));
    }

    // ---- Color ----

    [TestMethod]
    public async Task Color_EveryValuePaintsTheSelectedItemAndItsLine()
    {
        await OpenAsync(PAGE);

        var primary = await ResolveColorAsync("var(--bit-clr-pri)");
        await Expect(Page.Locator("#nbr-color-default .bit-nbr-sel")).ToHaveCSSAsync("color", primary);

        var painted = new HashSet<string>();

        foreach (var (name, suffix) in Colors)
        {
            var bar = Bar($"nbr-color-{name}");
            await Expect(bar).ToHaveClassAsync(HasClass($"bit-nbr-{suffix}"));

            var color = await ResolveColorVarAsync($"#nbr-color-{name}", "--bit-nbr-clr");
            var line = await ResolveColorVarAsync($"#nbr-color-{name}", "--bit-nbr-clr-ind");
            var selected = bar.Locator(".bit-nbr-sel");

            await Expect(selected).ToHaveCSSAsync("color", color);
            Assert.AreEqual(line, await AfterStyleAsync(selected, "background-color"), name);

            painted.Add(color);
        }

        Assert.IsTrue(painted.Count >= 10, $"The roles paint in distinct colors ({painted.Count} of {Colors.Length}).");
    }

    [TestMethod]
    public async Task Color_WithFilled_EveryValueFillsTheSelectedItem()
    {
        await OpenAsync(PAGE);

        var fills = new HashSet<string>();

        foreach (var (name, suffix) in Colors)
        {
            var bar = Bar($"nbr-filled-{name}");
            await Expect(bar).ToHaveClassAsync(HasClass($"bit-nbr-{suffix}"));
            await Expect(bar).ToHaveClassAsync(HasClass("bit-nbr-fil"));

            var fill = await ResolveColorVarAsync($"#nbr-filled-{name}", "--bit-nbr-clr-acc-active");
            var text = await ResolveColorVarAsync($"#nbr-filled-{name}", "--bit-nbr-clr-acc-text");

            await Expect(bar.Locator(".bit-nbr-sel")).ToHaveCSSAsync("background-color", fill);
            await Expect(bar.Locator(".bit-nbr-sel")).ToHaveCSSAsync("color", text);

            fills.Add(fill);
        }

        Assert.IsTrue(fills.Count >= 10, $"The roles fill in distinct colors ({fills.Count} of {Colors.Length}).");
    }

    // ---- External icons ----

    [TestMethod]
    public async Task ExternalIcons_RenderTheirClasses_AndTheSelectedIconSwapsIn()
    {
        await OpenAsync(PAGE);

        var css = Item("nbr-external", "Css").Locator(".bit-nbr-ico");
        await Expect(css).ToHaveClassAsync(new Regex(@"(^|\s)my-icon my-icon-house(\s|$)"));

        var bootstrap = Item("nbr-external", "Bootstrap").Locator(".bit-nbr-ico");
        await Expect(bootstrap).ToHaveClassAsync(new Regex(@"(^|\s)bi bi-gear(\s|$)"));
        await Expect(bootstrap).Not.ToHaveClassAsync(HasClass("bit-icon--Home"));

        await Expect(Item("nbr-external", "FontAwesome").Locator(".bit-nbr-ico")).ToHaveClassAsync(new Regex(@"(^|\s)fa-solid fa-house(\s|$)"));

        await Item("nbr-external", "Css").ClickAsync();
        await Expect(css).ToHaveClassAsync(new Regex(@"(^|\s)my-icon my-icon-house-solid(\s|$)"));

        await Item("nbr-external", "Bootstrap").ClickAsync();
        await Expect(bootstrap).ToHaveClassAsync(HasClass("bit-icon--HomeSolid"));
        await Expect(css).ToHaveClassAsync(new Regex(@"(^|\s)my-icon my-icon-house(\s|$)"));
    }

    // ---- Size ----

    [TestMethod]
    public async Task Size_EveryValueScalesTheItems()
    {
        await OpenAsync(PAGE);

        await Expect(Bar("nbr-size-small")).ToHaveClassAsync(HasClass("bit-nbr-sm"));
        await Expect(Bar("nbr-size-medium")).ToHaveClassAsync(HasClass("bit-nbr-md"));
        await Expect(Bar("nbr-size-large")).ToHaveClassAsync(HasClass("bit-nbr-lg"));
        await Expect(Bar("nbr-size-default")).Not.ToHaveClassAsync(new Regex(@"(^|\s)bit-nbr-(sm|md|lg)(\s|$)"));

        async Task<double[]> MetricsAsync(string id)
        {
            var home = Item(id, "Home");
            return
            [
                await NumberStyleAsync(home, "min-height"),
                await NumberStyleAsync(home.Locator(".bit-nbr-ico"), "font-size"),
                await NumberStyleAsync(home.Locator(".bit-nbr-txt"), "font-size"),
                await NumberStyleAsync(home, "padding-top"),
                (await BoxAsync(home)).Height,
            ];
        }

        var small = await MetricsAsync("nbr-size-small");
        var medium = await MetricsAsync("nbr-size-medium");
        var large = await MetricsAsync("nbr-size-large");
        var unset = await MetricsAsync("nbr-size-default");

        string[] names = ["touch target", "icon size", "text size", "padding", "height"];
        for (var i = 0; i < names.Length; i++)
        {
            Assert.AreEqual(medium[i], unset[i], 0.01, $"Unset is the medium {names[i]}.");
            Assert.IsTrue(small[i] < medium[i] && medium[i] < large[i], $"{names[i]}: {small[i]} < {medium[i]} < {large[i]}");
        }
    }

    // ---- Style & Class ----

    [TestMethod]
    public async Task StyleClassAndTheItemsStyleAndClass()
    {
        await OpenAsync(PAGE);

        await Expect(Bar("nbr-style")).ToHaveCSSAsync("border-top-color", "rgb(255, 99, 71)");
        await Expect(Bar("nbr-class")).ToHaveClassAsync(HasClass("custom-class"));
        await Expect(Item("nbr-item-style", "Styled")).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(Item("nbr-item-style", "Classed")).ToHaveClassAsync(HasClass("item-class"));
    }

    [TestMethod]
    public async Task Styles_ReachEveryPartOfTheBar()
    {
        await OpenAsync(PAGE);

        var bar = Bar("nbr-styles");
        await Expect(bar).ToHaveCSSAsync("background-color", "rgb(42, 42, 42)");
        await Expect(bar.Locator("> ul")).ToHaveCSSAsync("column-gap", "9px");
        await Expect(bar.Locator(".bit-nbr-hdr")).ToHaveCSSAsync("color", "rgb(1, 1, 1)");
        await Expect(bar.Locator(".bit-nbr-ftr")).ToHaveCSSAsync("color", "rgb(2, 2, 2)");
        await Expect(Item("nbr-styles", "Other")).ToHaveCSSAsync("border-top-color", "rgb(3, 3, 3)");
        await Expect(Item("nbr-styles", "Inbox")).ToHaveCSSAsync("border-bottom-color", "rgb(4, 4, 4)");
        await Expect(Item("nbr-styles", "Other")).Not.ToHaveCSSAsync("border-bottom-color", "rgb(4, 4, 4)");
        await Expect(Item("nbr-styles", "Inbox").Locator(".bit-nbr-ico")).ToHaveCSSAsync("color", "rgb(5, 5, 5)");
        await Expect(Item("nbr-styles", "Inbox").Locator(".bit-nbr-icc")).ToHaveCSSAsync("opacity", "0.75");
        await Expect(Item("nbr-styles", "Inbox").Locator(".bit-nbr-txt")).ToHaveCSSAsync("letter-spacing", "2px");
        await Expect(Item("nbr-styles", "Inbox").Locator(".bit-nbr-bdg")).ToHaveCSSAsync("background-color", "rgb(6, 6, 6)");
        await Expect(bar.Locator(".bit-nbr-lit").First).ToHaveCSSAsync("outline-color", "rgb(7, 7, 7)");
    }

    [TestMethod]
    public async Task Classes_ReachEveryPartOfTheBar()
    {
        await OpenAsync(PAGE);

        var bar = Bar("nbr-classes");
        await Expect(bar).ToHaveClassAsync(HasClass("custom-root"));
        await Expect(bar.Locator("> ul")).ToHaveClassAsync(HasClass("custom-container"));
        await Expect(bar.Locator(".bit-nbr-hdr")).ToHaveClassAsync(HasClass("custom-header"));
        await Expect(bar.Locator(".bit-nbr-ftr")).ToHaveClassAsync(HasClass("custom-footer"));
        await Expect(Item("nbr-classes", "Other")).ToHaveClassAsync(HasClass("custom-item"));
        await Expect(Item("nbr-classes", "Other")).Not.ToHaveClassAsync(HasClass("custom-selected-item"));
        await Expect(Item("nbr-classes", "Inbox")).ToHaveClassAsync(HasClass("custom-selected-item"));
        await Expect(Item("nbr-classes", "Inbox").Locator(".bit-nbr-ico")).ToHaveClassAsync(HasClass("custom-item-icon"));
        await Expect(Item("nbr-classes", "Inbox").Locator(".bit-nbr-icc")).ToHaveClassAsync(HasClass("custom-icon-container"));
        await Expect(Item("nbr-classes", "Inbox").Locator(".bit-nbr-txt")).ToHaveClassAsync(HasClass("custom-item-text"));
        await Expect(Item("nbr-classes", "Inbox").Locator(".bit-nbr-bdg")).ToHaveClassAsync(HasClass("custom-item-badge"));
        await Expect(bar.Locator(".bit-nbr-lit").First).ToHaveClassAsync(HasClass("custom-item-wrapper"));
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheBar()
    {
        await OpenAsync(PAGE);

        var bar = Bar("nbr-vars");
        await Expect(bar).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(bar).ToHaveCSSAsync("border-top-left-radius", "11px");
        await Expect(bar).ToHaveCSSAsync("padding-top", "5px");
        await Expect(bar).ToHaveCSSAsync("padding-left", "6px");
        await Expect(bar).ToHaveCSSAsync("box-shadow", "rgb(255, 0, 0) 0px 0px 4px 0px");
        await Expect(bar.Locator("> ul")).ToHaveCSSAsync("column-gap", "7px");

        var other = Item("nbr-vars", "Other");
        await Expect(other).ToHaveCSSAsync("color", "rgb(40, 50, 60)");
        await Expect(other).ToHaveCSSAsync("border-top-left-radius", "13px");
        await Expect(other).ToHaveCSSAsync("padding-top", "9px");
        await Expect(other).ToHaveCSSAsync("min-height", "70px");
        await Expect(other).ToHaveCSSAsync("min-width", "70px");
        await Expect(other.Locator(".bit-nbr-ico")).ToHaveCSSAsync("font-size", "25px");
        await Expect(other.Locator(".bit-nbr-txt")).ToHaveCSSAsync("font-size", "15px");

        var selected = Item("nbr-vars", "Selected");
        await Expect(selected).ToHaveCSSAsync("color", "rgb(200, 0, 0)");
        await Expect(selected).ToHaveCSSAsync("background-color", "rgb(0, 0, 100)");
        await Expect(selected).ToHaveCSSAsync("font-weight", "800");
        Assert.AreEqual("rgb(0, 150, 0)", await AfterStyleAsync(selected, "background-color"));
        Assert.AreEqual("5px", await AfterStyleAsync(selected, "height"));
        Assert.AreEqual("10px", await AfterStyleAsync(selected, "left"));
        await Expect(selected.Locator(".bit-nbr-bdg")).ToHaveCSSAsync("color", "rgb(255, 255, 0)");
        await Expect(selected.Locator(".bit-nbr-bdg")).ToHaveCSSAsync("background-color", "rgb(0, 128, 128)");
        await Expect(Item("nbr-vars", "Off")).ToHaveCSSAsync("color", "rgb(120, 120, 120)");

        await other.HoverAsync();
        await Expect(other).ToHaveCSSAsync("color", "rgb(0, 200, 200)");
        await Expect(other).ToHaveCSSAsync("background-color", "rgb(220, 220, 0)");
    }

    [TestMethod]
    public async Task CssVariables_AnExplicitColorAndSize_WinOverThem()
    {
        await OpenAsync(PAGE);

        var success = await ResolveColorAsync("var(--bit-clr-suc)");
        var selected = Item("nbr-vars-explicit", "Selected");

        await Expect(selected).ToHaveCSSAsync("color", success);
        Assert.AreEqual(success, await AfterStyleAsync(selected, "background-color"));
        await Expect(selected).Not.ToHaveCSSAsync("min-height", "70px");
        await Expect(selected.Locator(".bit-nbr-ico")).Not.ToHaveCSSAsync("font-size", "25px");

        // The unfilled fill and the indicator's shape are the variables' alone.
        await Expect(selected).ToHaveCSSAsync("background-color", "rgb(0, 0, 100)");
        Assert.AreEqual("5px", await AfterStyleAsync(selected, "height"));
    }

    [TestMethod]
    public async Task FocusColor_TheFocusRingTakesTheVariablesColor()
    {
        await OpenAsync(PAGE);

        // The focus moved by the keyboard is the one the ring is drawn for (:focus-visible).
        await Item("nbr-focus-color", "Home").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        var item = Item("nbr-focus-color", "Search");
        await Expect(item).ToBeFocusedAsync();

        var shadow = await item.EvaluateAsync<string>("el => getComputedStyle(el).boxShadow");
        StringAssert.Contains(shadow, "rgb(255, 0, 255)");
        Assert.AreEqual("none", await Item("nbr-focus-color", "Home").EvaluateAsync<string>("el => getComputedStyle(el).boxShadow"));

        // Without the variable the ring is the library's own.
        await Item("nbr-keyboard", "Home").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Item("nbr-keyboard", "Search")).ToBeFocusedAsync();
        var plain = await Item("nbr-keyboard", "Search").EvaluateAsync<string>("el => getComputedStyle(el).boxShadow");
        Assert.AreNotEqual("none", plain);
        Assert.IsFalse(plain.Contains("rgb(255, 0, 255)"));
    }

    [TestMethod]
    public async Task Visibility_HiddenKeepsTheRoom_CollapsedRemovesIt()
    {
        await OpenAsync(PAGE);

        var bar = Bar("nbr-visibility");
        await Page.Locator("#btn-cycle-visibility").ClickAsync();
        await Expect(Page.Locator("#visibility-state")).ToHaveTextAsync("Hidden");
        await Expect(bar).ToHaveCSSAsync("visibility", "hidden");
        Assert.IsTrue((await BoxAsync(bar)).Height > 0);

        await Page.Locator("#btn-cycle-visibility").ClickAsync();
        await Expect(bar).ToHaveCSSAsync("display", "none");

        await Page.Locator("#btn-cycle-visibility").ClickAsync();
        await Expect(bar).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task HtmlAttributes_LandOnTheRoot()
    {
        await OpenAsync(PAGE);

        await Expect(Bar("nbr-html-attributes")).ToHaveAttributeAsync("data-test", "bar-data");
        await Expect(Bar("nbr-html-attributes")).ToHaveAttributeAsync("data-extra", "extra-value");
    }

    // ---- RTL ----

    [TestMethod]
    public async Task Rtl_MirrorsTheBarAndTheArrowKeys()
    {
        await OpenAsync(PAGE);

        var bar = Bar("nbr-rtl");
        await Expect(bar).ToHaveAttributeAsync("dir", "rtl");
        await Expect(bar).ToHaveClassAsync(HasClass("bit-rtl"));

        var home = await BoxAsync(Item("nbr-rtl", "Home"));
        var profile = await BoxAsync(Item("nbr-rtl", "Profile"));
        Assert.IsTrue(home.X > profile.X, "The first item is on the right.");

        await Item("nbr-rtl", "Home").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Item("nbr-rtl", "Search")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Item("nbr-rtl", "Home")).ToBeFocusedAsync();
        // Down and Up keep their meaning.
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Item("nbr-rtl", "Search")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Rtl_ADirectionInheritedFromAnAncestor_MirrorsTheBarToo()
    {
        await OpenAsync(PAGE);

        var bar = Bar("nbr-rtl-inherited");
        await Expect(bar).Not.ToHaveAttributeAsync("dir", new Regex(".*"));

        var home = await BoxAsync(Item("nbr-rtl-inherited", "Home"));
        var profile = await BoxAsync(Item("nbr-rtl-inherited", "Profile"));
        Assert.IsTrue(home.X > profile.X);

        await Item("nbr-rtl-inherited", "Home").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Item("nbr-rtl-inherited", "Search")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Rtl_TheWheelScrollsARightToLeftBarTowardItsEnd()
    {
        await OpenAsync(PAGE);

        var list = Page.Locator("#nbr-rtl-scroll > ul");
        await list.HoverAsync();
        await Page.Mouse.WheelAsync(0, 200);

        await Page.WaitForFunctionAsync("() => document.querySelector('#nbr-rtl-scroll > ul').scrollLeft < -50");
    }

    // ---- Public API ----

    [TestMethod]
    public async Task PublicApi_SelectItemAndFocusItem()
    {
        await OpenAsync(PAGE);

        await Page.Locator("#btn-api-select").ClickAsync();
        await ExpectSelectedAsync("nbr-api", "Alerts");
        await Expect(Page.Locator("#api-selected")).ToHaveTextAsync("Alerts");

        await Page.Locator("#btn-api-select-none").ClickAsync();
        await Expect(Page.Locator("#nbr-api .bit-nbr-sel")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#api-selected")).ToHaveTextAsync("none");

        await Page.Locator("#btn-api-focus").ClickAsync();
        await Expect(Item("nbr-api", "Profile")).ToBeFocusedAsync();
    }

    // ---- Accessibility ----

    [TestMethod]
    [DataRow("Page", "page")]
    [DataRow("Step", "step")]
    [DataRow("Location", "location")]
    [DataRow("Date", "date")]
    [DataRow("Time", "time")]
    [DataRow("True", "true")]
    public async Task AriaCurrent_TheSelectedItemCarriesItsOwnToken(string text, string token)
    {
        await OpenAsync(PAGE);

        var item = Item("nbr-aria-current", text);
        await item.ClickAsync();

        await Expect(item).ToHaveAttributeAsync("aria-current", token);
        await Expect(Page.Locator("#nbr-aria-current [aria-current]")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task NewTab_LinksCarryNoopenerAndAnnounceThemselves()
    {
        await OpenAsync(PAGE);

        var blank = Item("nbr-newtab", "Blank");
        await Expect(blank).ToHaveAttributeAsync("target", "_blank");
        await Expect(blank).ToHaveAttributeAsync("rel", "noopener");
        await Expect(blank.Locator(".bit-nbr-vhd")).ToHaveTextAsync("(opens in a new tab)");

        await Expect(Item("nbr-newtab", "Rel")).ToHaveAttributeAsync("rel", "noreferrer");
        await Expect(Labeled("nbr-newtab", "Labeled link (opens in a new tab)")).ToHaveCountAsync(1);

        await Expect(Item("nbr-newtab-custom", "Blank").Locator(".bit-nbr-vhd")).ToHaveTextAsync("(new window)");
        await Expect(Item("nbr-newtab-none", "Blank").Locator(".bit-nbr-vhd")).ToHaveCountAsync(0);
        await Expect(Item("nbr-newtab-none", "Blank")).ToHaveAttributeAsync("rel", "noopener");
    }

    [TestMethod]
    public async Task SafeArea_ReservesTheDevicesInsetUnderTheBar()
    {
        await OpenAsync(PAGE);

        var bar = Bar("nbr-safearea");
        await Expect(bar).ToHaveClassAsync(HasClass("bit-nbr-sfa"));
        // A desktop has no inset, so env(safe-area-inset-bottom) falls back to nothing.
        await Expect(bar.Locator("> ul")).ToHaveCSSAsync("margin-bottom", "0px");
        Assert.IsTrue(await bar.EvaluateAsync<bool>("el => [...document.styleSheets].some(s => { try { return [...s.cssRules].some(r => r.cssText.includes('.bit-nbr-sfa') && r.cssText.includes('safe-area-inset-bottom')); } catch { return false; } })"));
    }

    // ---- Lifecycle ----

    [TestMethod]
    public async Task Lifecycle_ReRendersOnParameterChanges_AndUnmountsCleanly()
    {
        await OpenAsync(PAGE);

        var bar = Bar("nbr-lifecycle");
        await Expect(bar).ToBeVisibleAsync();
        await Page.Locator("#btn-toggle-vertical").ClickAsync();
        await Expect(bar).ToHaveClassAsync(HasClass("bit-nbr-vrt"));
        await Page.Locator("#btn-toggle-vertical").ClickAsync();
        await Expect(bar).Not.ToHaveClassAsync(HasClass("bit-nbr-vrt"));

        await Item("nbr-lifecycle", "Search").ClickAsync();
        await Item("nbr-lifecycle", "Search").FocusAsync();

        await Page.Locator("#btn-toggle-mount").ClickAsync();
        await Expect(Page.Locator("#lifecycle-mounted")).ToHaveTextAsync("False");
        await Expect(bar).ToHaveCountAsync(0);

        await Page.Locator("#btn-toggle-mount").ClickAsync();
        await Expect(bar).ToBeVisibleAsync();

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    // ---- Forced colors ----

    [TestMethod]
    public async Task ForcedColors_TheSelectedItemIsHighlightedAndUnderlined()
    {
        if (IsChromium is false) Assert.Inconclusive("Playwright emulates forced colors in Chromium only.");

        await OpenAsync(PAGE);
        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });

        var highlight = await ResolveColorAsync("Highlight");

        // Without an indicator of its own, the selected item takes a Highlight line, since the fill is gone.
        var selected = Item("nbr-indicator-none", "Home");
        await Expect(selected).ToHaveCSSAsync("color", highlight);
        Assert.AreEqual(highlight, await AfterStyleAsync(selected, "background-color"));
        Assert.AreEqual("0px", await AfterStyleAsync(selected, "bottom"));

        // The Line indicator is drawn in Highlight, and a disabled item in GrayText.
        Assert.AreEqual(highlight, await AfterStyleAsync(Item("nbr-line", "Home"), "background-color"));
        await Expect(Item("nbr-item-disabled", "Off link")).ToHaveCSSAsync("color", await ResolveColorAsync("GrayText"));
    }

    // ---- Motion ----

    [TestMethod]
    public async Task Motion_TheLineSlidesOverTheMotionDuration_WhichReducedMotionCollapses()
    {
        await OpenAsync(PAGE);

        var item = Item("nbr-motion", "Search");
        var duration = await AfterStyleAsync(item, "transition-duration");
        Assert.IsTrue(double.Parse(duration.TrimEnd('s'), CultureInfo.InvariantCulture) > 0.01, $"The line moves over a duration ({duration}).");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        var reduced = await AfterStyleAsync(item, "transition-duration");
        Assert.IsTrue(double.Parse(reduced.TrimEnd('s'), CultureInfo.InvariantCulture) < 0.01, $"Reduced motion collapses it ({reduced}).");
        var forced = await AfterStyleAsync(Item("nbr-motion-force", "Search"), "transition-duration");
        Assert.AreEqual(duration, forced, "ForceAnimation brings it back.");

        // A scrolling bar stops scrolling smoothly too.
        await Expect(Page.Locator("#nbr-scroll > ul")).ToHaveCSSAsync("scroll-behavior", "auto");
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.NoPreference });
        await Expect(Page.Locator("#nbr-scroll > ul")).ToHaveCSSAsync("scroll-behavior", "smooth");
    }
}
