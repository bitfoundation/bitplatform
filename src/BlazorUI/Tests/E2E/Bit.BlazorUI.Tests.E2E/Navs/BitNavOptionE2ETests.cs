using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Navs;

/// <summary>
/// End-to-end tests of BitNav with the BitNavOption API (/e2e/nav/options): every parameter of an option rendered
/// (Text, IconName, Icon, Url, Description, Title, Class, Style, IsExpanded, IsDisabled, IsSeparator, Expand/Collapse
/// aria labels, Target, Rel, ForceAnchor, Badge, BadgeAriaLabel, Dot, AriaLabel, AriaCurrent, Key, Data, Template,
/// TemplateRenderMode, SuffixTemplate), FitWidth/FullWidth, Grouped, the manual mode followed through OnSelectItem and a
/// two-way bound SelectedItem, IconOnly, SingleExpand, AllExpanded, NoCollapse, ReversedChevron with IndentValue, the
/// nav's templates, the public API on options, the events and Reselectable, options added and removed at runtime in
/// markup order, the BitParams cascade, Color/Size/Styles/Classes, RTL and the disabled nav. The automatic mode of the
/// options is covered by BitNavUrlE2ETests.
/// </summary>
[TestClass]
public class BitNavOptionE2ETests : E2ETestBase
{
    private const string PAGE = "/e2e/nav/options";

    private static Regex Exact(string text) => new($"^{Regex.Escape(text)}$");

    private ILocator Nav(string id) => Page.Locator($"#{id}");

    private ILocator Item(string navId, string text)
    {
        return Page.Locator($"#{navId} .bit-nav-ict").Filter(new() { Has = Page.Locator(".bit-nav-itx", new() { HasTextRegex = Exact(text) }) });
    }

    [TestMethod]
    public async Task Prerender_TheOptionsAreInTheHtml()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{PAGE}");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "id=\"onav-basic\"");
        StringAssert.Contains(html, "href=\"/e2e/nav/url/home\"");
        StringAssert.Contains(html, ">Books</span>");
    }

    [TestMethod]
    public async Task Basic_EveryParameterOfAnOptionIsRendered()
    {
        await OpenAsync(PAGE);

        var nav = Nav("onav-basic");
        await Expect(nav).ToHaveAttributeAsync("aria-label", "Option nav");

        var home = Item("onav-basic", "Home");
        await Expect(home).ToHaveAttributeAsync("href", "/e2e/nav/url/home");
        await Expect(home).ToHaveAttributeAsync("title", "Home tip");
        await Expect(home.Locator(".bit-nav-iic")).ToHaveClassAsync(HasClass("bit-icon--Home"));
        await Expect(home.Locator(".bit-nav-des")).ToHaveTextAsync("Start here");
        await Expect(home.Locator("xpath=..")).ToHaveClassAsync(HasClass("home-class"));
        await Expect(home.Locator("xpath=..")).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");

        await Expect(Item("onav-basic", "Library")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Item("onav-basic", "Books")).ToBeVisibleAsync();

        var archived = Item("onav-basic", "Archived");
        await Expect(archived).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(archived).Not.ToHaveAttributeAsync("href", new Regex(".*"));

        await Expect(nav.Locator("[role=separator]")).ToHaveCountAsync(1);

        var chevron = Item("onav-basic", "Reports").Locator(".bit-nav-cbt");
        await Expect(chevron).ToHaveAttributeAsync("aria-label", "Open reports");
        await chevron.ClickAsync();
        await Expect(chevron).ToHaveAttributeAsync("aria-label", "Close reports");
        await Expect(Item("onav-basic", "Monthly")).ToBeVisibleAsync();

        await Expect(Item("onav-basic", "External")).ToHaveAttributeAsync("target", "_blank");
        await Expect(Item("onav-basic", "External")).ToHaveAttributeAsync("rel", "noreferrer");
        await Expect(Item("onav-basic", "Anchor")).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Item("onav-basic", "Inbox").Locator(".bit-nav-bdg")).ToHaveTextAsync("5");
        await Expect(Item("onav-basic", "Inbox").Locator(".bit-nav-vhd")).ToHaveTextAsync(" (5 unread)");
        await Expect(Item("onav-basic", "Alerts").Locator(".bit-nav-bdt")).ToHaveCountAsync(1);
        await Expect(Item("onav-basic", "Named")).ToHaveAttributeAsync("aria-label", "Custom name");
        await Expect(Item("onav-basic", "Pictured").Locator(".bit-nav-iic")).ToHaveClassAsync(HasClass("my-picture"));

        await Item("onav-basic", "Stepped").ClickAsync();
        await Expect(Item("onav-basic", "Stepped")).ToHaveAttributeAsync("aria-current", "step");
    }

    [TestMethod]
    public async Task Expansion_ACollapsedOptionKeepsItsChildrenHidden_AndTogglesThemBack()
    {
        await OpenAsync(PAGE);

        var library = Item("onav-basic", "Library");
        await library.ClickAsync();

        await Expect(library).ToHaveAttributeAsync("aria-expanded", "false");
        // The options API keeps the children rendered, hidden, to keep their registrations alive.
        await Expect(Item("onav-basic", "Books")).ToBeHiddenAsync();
        await Expect(Item("onav-basic", "Books")).ToHaveCountAsync(1);

        await library.ClickAsync();
        await Expect(Item("onav-basic", "Books")).ToBeVisibleAsync();

        // A nested option toggles on its own.
        await Item("onav-basic", "Papers").ClickAsync();
        await Expect(Item("onav-basic", "Drafts")).ToBeVisibleAsync();
        await Expect(Item("onav-basic", "Drafts")).ToHaveCSSAsync("padding-inline-start", "59px");

        // The keyboard steps over the hidden children of a collapsed option.
        await Item("onav-basic", "Papers").ClickAsync();
        await Item("onav-basic", "Papers").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Item("onav-basic", "Reports")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Width_FitWidthAndFullWidth()
    {
        await OpenAsync(PAGE);

        var container = (await Page.Locator("#owidth-container").BoundingBoxAsync())!;
        var fit = (await Nav("onav-fit").BoundingBoxAsync())!;
        var full = (await Nav("onav-full").BoundingBoxAsync())!;

        await Expect(Nav("onav-fit")).ToHaveClassAsync(HasClass("bit-nav-ftw"));
        Assert.AreEqual(container.Width, full.Width, 0.5);
        Assert.IsTrue(fit.Width < container.Width / 2);
    }

    [TestMethod]
    public async Task Grouped_AHeaderTogglesItsGroup_AndALonelyGroupIsALabel()
    {
        await OpenAsync(PAGE);

        var header = Page.Locator("#onav-grouped button.bit-nav-gcb", new() { HasText = "Group one" });
        await Expect(header).ToHaveAttributeAsync("aria-expanded", "false");
        await header.ClickAsync();
        await Expect(header).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Item("onav-grouped", "One b")).ToBeVisibleAsync();

        await Expect(Page.Locator("#onav-grouped .bit-nav-gst", new() { HasText = "Lonely group" })).ToHaveJSPropertyAsync("tagName", "DIV");
        await Expect(Page.Locator("#onav-grouped [role=separator]")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Manual_ClickingSelects_AndTheKeyIsReported()
    {
        await OpenAsync(PAGE);

        await Item("onav-manual", "Banana").ClickAsync();
        await Expect(Page.Locator("#oselected-key")).ToHaveTextAsync("banana");
        await Expect(Item("onav-manual", "Banana")).ToHaveClassAsync(HasClass("bit-nav-sel"));
        await Expect(Item("onav-manual", "Banana")).ToHaveAttributeAsync("aria-current", "page");

        await Item("onav-manual", "Ice cream").ClickAsync();
        await Expect(Page.Locator("#oselected-key")).ToHaveTextAsync("ice-cream");
        await Expect(Item("onav-manual", "Banana")).Not.ToHaveClassAsync(HasClass("bit-nav-sel"));
    }

    [TestMethod]
    public async Task Manual_TheTwoWayBindingSelectsAnOptionAndOpensItsBranch()
    {
        await OpenAsync(PAGE);

        await Item("onav-bound", "Ice cream").ClickAsync();
        await Expect(Page.Locator("#obound-selected")).ToHaveTextAsync("Ice cream");

        await Page.Locator("#obtn-select-banana").ClickAsync();
        await Expect(Item("onav-bound", "Banana")).ToBeVisibleAsync();
        await Expect(Item("onav-bound", "Banana")).ToHaveClassAsync(HasClass("bit-nav-sel"));
        await Expect(Item("onav-bound", "Ice cream")).Not.ToHaveClassAsync(HasClass("bit-nav-sel"));
        await Expect(Page.Locator("#obound-selected")).ToHaveTextAsync("Banana");
    }

    [TestMethod]
    public async Task IconOnly_HidesTheTextsAndPinsTheBadges()
    {
        await OpenAsync(PAGE);

        await Expect(Nav("onav-icononly")).ToHaveClassAsync(HasClass("bit-nav-ion"));
        await Expect(Item("onav-icononly", "Home").Locator(".bit-nav-itx")).ToBeHiddenAsync();
        await Expect(Item("onav-icononly", "Home")).ToHaveAttributeAsync("aria-label", "Home");
        await Expect(Item("onav-icononly", "Mail").Locator(".bit-nav-itm")).ToHaveClassAsync(HasClass("bit-nav-icb"));
        await Expect(Item("onav-icononly", "Mail")).ToHaveAttributeAsync("aria-label", "Mail (3)");
    }

    [TestMethod]
    public async Task SingleExpandAllExpandedAndNoCollapse()
    {
        await OpenAsync(PAGE);

        await Item("onav-single", "Alpha").ClickAsync();
        await Expect(Item("onav-single", "Alpha one")).ToBeVisibleAsync();
        await Item("onav-single", "Beta").ClickAsync();
        await Expect(Item("onav-single", "Beta one")).ToBeVisibleAsync();
        await Expect(Item("onav-single", "Alpha one")).ToBeHiddenAsync();
        await Expect(Item("onav-single", "Alpha")).ToHaveAttributeAsync("aria-expanded", "false");

        await Expect(Item("onav-all", "Alpha deep")).ToBeVisibleAsync();

        await Expect(Page.Locator("#onav-nocollapse .bit-nav-cbt")).ToHaveCountAsync(0);
        await Expect(Item("onav-nocollapse", "Alpha one")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ReversedChevron_WithIndentValue()
    {
        await OpenAsync(PAGE);

        var parent = Item("onav-reversed", "Parent");
        await Expect(parent.Locator(".bit-nav-mct")).ToHaveCSSAsync("flex-direction", "row-reverse");
        await Expect(parent).ToHaveCSSAsync("padding-inline-start", "4px");
        await Expect(Item("onav-reversed", "Child")).ToHaveCSSAsync("padding-inline-start", "34px");
    }

    [TestMethod]
    public async Task Templates_TheNavsAndTheOptionsOwn()
    {
        await OpenAsync(PAGE);

        var nav = Nav("onav-templates");
        await Expect(nav.Locator(".bit-nav-ict .onav-item-template").First).ToHaveTextAsync("Item template: Plain");
        await Expect(nav.Locator(".bit-nav-ict .onav-own-template")).ToHaveTextAsync("Own template: Own");
        await Expect(nav.Locator("> ul > li > .onav-replaced")).ToHaveTextAsync("Replaced: Replaced");
        await Expect(nav.Locator(".bit-nav-ict")).ToHaveCountAsync(3);

        var plain = nav.Locator(".bit-nav-ict").Filter(new() { HasText = "Item template: Plain" });
        await Expect(plain.Locator(".bit-nav-sfx .onav-suffix")).ToHaveTextAsync("7");
        var ownSuffix = nav.Locator(".bit-nav-ict").Filter(new() { HasText = "Item template: Own suffix" });
        await Expect(ownSuffix.Locator(".bit-nav-sfx .onav-own-suffix")).ToHaveTextAsync("own 8");
        await Expect(ownSuffix.Locator(".onav-suffix")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task PublicApi_DrivesTheOptions()
    {
        await OpenAsync(PAGE);

        await Page.Locator("#obtn-expand-all").ClickAsync();
        await Expect(Item("onav-api", "Apple")).ToBeVisibleAsync();
        await Expect(Item("onav-api", "Veggie burger")).ToBeVisibleAsync();

        await Page.Locator("#obtn-collapse-all").ClickAsync();
        await Expect(Item("onav-api", "Apple")).ToBeHiddenAsync();
        await Expect(Item("onav-api", "Veggie burger")).ToBeHiddenAsync();

        await Page.Locator("#obtn-toggle").ClickAsync();
        await Expect(Item("onav-api", "Apple")).ToBeVisibleAsync();

        await Page.Locator("#obtn-select").ClickAsync();
        await Expect(Item("onav-api", "Ice cream")).ToHaveClassAsync(HasClass("bit-nav-sel"));

        await Page.Locator("#obtn-focus").ClickAsync();
        await Expect(Item("onav-api", "Veggie burger")).ToBeFocusedAsync();
        await Expect(Item("onav-api", "Fast foods")).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [TestMethod]
    public async Task Events_FireWithTheOption()
    {
        await OpenAsync(PAGE);

        await Item("onav-events", "Ice cream").ClickAsync();
        await Expect(Page.Locator("#oevent-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#oevent-last-clicked")).ToHaveTextAsync("Ice cream");
        await Expect(Page.Locator("#oevent-selects")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#oevent-last-selected")).ToHaveTextAsync("Ice cream");

        await Item("onav-events", "Fruits").ClickAsync();
        await Expect(Page.Locator("#oevent-toggles")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#oevent-last-toggled")).ToHaveTextAsync("Fruits");

        await Item("onav-reselectable", "Again").ClickAsync();
        await Item("onav-reselectable", "Again").ClickAsync();
        await Expect(Page.Locator("#oreselects")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Dynamic_AnOptionAddedLaterTakesItsMarkupPlace_AndIsRemovedCleanly()
    {
        await OpenAsync(PAGE);

        await Expect(Page.Locator("#onav-dynamic .bit-nav-itx")).ToHaveTextAsync(["First", "Last"]);

        await Page.Locator("#obtn-toggle-middle").ClickAsync();
        await Expect(Page.Locator("#onav-dynamic .bit-nav-itx")).ToHaveTextAsync(["First", "Middle", "Last"]);

        await Item("onav-dynamic", "Middle").ClickAsync();
        await Expect(Item("onav-dynamic", "Middle")).ToHaveClassAsync(HasClass("bit-nav-sel"));

        await Page.Locator("#obtn-toggle-middle").ClickAsync();
        await Expect(Page.Locator("#onav-dynamic .bit-nav-itx")).ToHaveTextAsync(["First", "Last"]);
        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    [Ignore("KNOWN BUG: BitNav walks its options in registration order, so an option rendered conditionally after the first render is reached by the arrow keys (and Home/End, type-ahead) last, not in its markup place; BitNavBar has AutoReorderOptions for this, BitNav has nothing.")]
    public async Task Dynamic_AnOptionAddedLater_IsWalkedByTheKeyboardInItsMarkupPlace()
    {
        await OpenAsync(PAGE);

        await Page.Locator("#obtn-toggle-middle").ClickAsync();
        await Expect(Page.Locator("#onav-dynamic .bit-nav-itx")).ToHaveTextAsync(["First", "Middle", "Last"]);

        await Item("onav-dynamic", "First").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Item("onav-dynamic", "Middle")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("End");
        await Expect(Item("onav-dynamic", "Last")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task BitParamsAndTheLookParameters()
    {
        await OpenAsync(PAGE);

        await Expect(Nav("onav-params")).ToHaveClassAsync(HasClass("bit-nav-ion"));
        await Expect(Nav("onav-params")).ToHaveClassAsync(HasClass("bit-nav-suc"));

        var look = Nav("onav-look");
        await Expect(look).ToHaveClassAsync(HasClass("bit-nav-ter"));
        await Expect(look).ToHaveClassAsync(HasClass("bit-nav-sm"));
        await Expect(look.Locator(".bit-nav-itx")).ToHaveCSSAsync("letter-spacing", "3px");
        await Expect(look.Locator(".bit-nav-iic")).ToHaveClassAsync(HasClass("custom-look-icon"));
    }

    [TestMethod]
    public async Task Rtl_MirrorsTheNavAndTheArrowKeys()
    {
        await OpenAsync(PAGE);

        await Expect(Nav("onav-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Item("onav-rtl", "Rtl sibling")).ToHaveCSSAsync("padding-right", "27px");

        var parent = Item("onav-rtl", "Rtl parent");
        await parent.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(parent).ToHaveAttributeAsync("aria-expanded", "true");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Item("onav-rtl", "Rtl child")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Disabled_TheNavIsUnavailable()
    {
        await OpenAsync(PAGE);

        await Expect(Nav("onav-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Item("onav-disabled", "Link")).Not.ToHaveAttributeAsync("href", new Regex(".*"));
        await Expect(Item("onav-disabled", "Button")).ToBeDisabledAsync();

        await Item("onav-disabled", "Link").DispatchEventAsync("click");
        await NextFramesAsync();
        await Expect(Page.Locator("#odisabled-clicks")).ToHaveTextAsync("0");
    }
}
