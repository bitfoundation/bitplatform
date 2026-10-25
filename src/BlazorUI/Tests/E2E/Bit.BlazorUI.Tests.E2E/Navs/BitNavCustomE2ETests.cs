using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Navs;

/// <summary>
/// End-to-end tests of BitNav over a custom generic item type (/e2e/nav/custom), every member of which is renamed and
/// found through the NameSelectors, by Name or by Selector: the rendered links, buttons, icons, descriptions, badges,
/// dots, separators, aria labels, aria-current, titles, targets, rels, classes and styles read off the items; the
/// expansion state written back to the item (and kept by the nav for a type without a member for it); the manual mode
/// with DefaultSelectedItem and a two-way binding; Grouped, IconOnly, SingleExpand, AllExpanded, NoCollapse,
/// ReversedChevron; the item, header and suffix templates and an item's own template in both render modes; the public
/// API; the events and Reselectable; the BitParams cascade; Color, Accent, Size, Style, Class, Styles and Classes; RTL;
/// and the disabled nav. The automatic mode of custom items is covered by BitNavUrlE2ETests.
/// </summary>
[TestClass]
public class BitNavCustomE2ETests : E2ETestBase
{
    private const string PAGE = "/e2e/nav/custom";

    private static Regex Exact(string text) => new($"^{Regex.Escape(text)}$");

    private ILocator Nav(string id) => Page.Locator($"#{id}");

    private ILocator Item(string navId, string text)
    {
        return Page.Locator($"#{navId} .bit-nav-ict").Filter(new() { Has = Page.Locator(".bit-nav-itx", new() { HasTextRegex = Exact(text) }) });
    }

    [TestMethod]
    public async Task Basic_EveryMemberIsReadThroughTheNameSelectors()
    {
        await OpenAsync(PAGE);

        var nav = Nav("cnav-basic");
        await Expect(nav).ToHaveAttributeAsync("aria-label", "Custom nav");

        // Text, Url, IconName, Description, Title, Class, Style.
        var home = Item("cnav-basic", "Home");
        await Expect(home).ToHaveAttributeAsync("href", "/e2e/nav/url/home");
        await Expect(home).ToHaveAttributeAsync("title", "Home tip");
        await Expect(home.Locator(".bit-nav-iic")).ToHaveClassAsync(HasClass("bit-icon--Home"));
        await Expect(home.Locator(".bit-nav-des")).ToHaveTextAsync("Start here");
        await Expect(home.Locator("xpath=..")).ToHaveClassAsync(HasClass("home-class"));
        await Expect(home.Locator("xpath=..")).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");

        // IsExpanded and ChildItems.
        await Expect(Item("cnav-basic", "Library")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Item("cnav-basic", "Books")).ToBeVisibleAsync();

        // IsDisabled.
        var archived = Item("cnav-basic", "Archived");
        await Expect(archived).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(archived).Not.ToHaveAttributeAsync("href", new Regex(".*"));

        // IsSeparator.
        await Expect(nav.Locator("[role=separator]")).ToHaveCountAsync(1);

        // ExpandAriaLabel and CollapseAriaLabel on the chevron of a linked parent.
        var chevron = Item("cnav-basic", "Reports").Locator(".bit-nav-cbt");
        await Expect(chevron).ToHaveAttributeAsync("aria-label", "Open reports");
        await chevron.ClickAsync();
        await Expect(chevron).ToHaveAttributeAsync("aria-label", "Close reports");
        await Expect(Item("cnav-basic", "Monthly")).ToBeVisibleAsync();

        // Target and Rel.
        var external = Item("cnav-basic", "External");
        await Expect(external).ToHaveAttributeAsync("target", "_blank");
        await Expect(external).ToHaveAttributeAsync("rel", "noreferrer");

        // ForceAnchor.
        await Expect(Item("cnav-basic", "Anchor")).ToHaveJSPropertyAsync("tagName", "A");
        await Expect(Item("cnav-basic", "Anchor")).ToHaveAttributeAsync("tabindex", "0");

        // Badge, BadgeAriaLabel and Dot.
        await Expect(Item("cnav-basic", "Inbox").Locator(".bit-nav-bdg")).ToHaveTextAsync("5");
        await Expect(Item("cnav-basic", "Inbox").Locator(".bit-nav-vhd")).ToHaveTextAsync(" (5 unread)");
        await Expect(Item("cnav-basic", "Alerts").Locator(".bit-nav-bdt")).ToHaveCountAsync(1);
        await Expect(Item("cnav-basic", "Alerts").Locator(".bit-nav-vhd")).ToHaveTextAsync(" (New alerts)");

        // AriaLabel and Icon.
        await Expect(Item("cnav-basic", "Named")).ToHaveAttributeAsync("aria-label", "Custom name");
        await Expect(Item("cnav-basic", "Pictured").Locator(".bit-nav-iic")).ToHaveClassAsync(HasClass("my-picture"));

        // AriaCurrent.
        await Item("cnav-basic", "Stepped").ClickAsync();
        await Expect(Item("cnav-basic", "Stepped")).ToHaveAttributeAsync("aria-current", "step");
    }

    [TestMethod]
    public async Task Expansion_IsWrittenBackToTheItem_OrKeptByTheNavForATypeWithoutAMemberForIt()
    {
        await OpenAsync(PAGE);

        var library = Item("cnav-basic", "Library");
        await library.ClickAsync();
        await Expect(library).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Item("cnav-basic", "Books")).ToHaveCountAsync(0);
        await library.ClickAsync();
        await Expect(Item("cnav-basic", "Books")).ToBeVisibleAsync();

        // A type with no member to keep the state in still expands and collapses.
        var bare = Item("cnav-no-member", "Bare parent");
        await Expect(bare).ToHaveAttributeAsync("aria-expanded", "false");
        await bare.ClickAsync();
        await Expect(bare).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Item("cnav-no-member", "Bare child")).ToBeVisibleAsync();
        await bare.ClickAsync();
        await Expect(Item("cnav-no-member", "Bare child")).ToHaveCountAsync(0);

        // And from the keyboard.
        await bare.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(bare).ToHaveAttributeAsync("aria-expanded", "true");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Item("cnav-no-member", "Bare child")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Width_FitWidthAndFullWidth()
    {
        await OpenAsync(PAGE);

        var container = (await Page.Locator("#cwidth-container").BoundingBoxAsync())!;
        var fit = (await Nav("cnav-fit").BoundingBoxAsync())!;
        var full = (await Nav("cnav-full").BoundingBoxAsync())!;

        Assert.AreEqual(container.Width, full.Width, 0.5);
        Assert.IsTrue(fit.Width < container.Width / 2);
    }

    [TestMethod]
    public async Task Grouped_HeadersToggleTheirGroups_AndTheDividerIsASeparator()
    {
        await OpenAsync(PAGE);

        var header = Page.Locator("#cnav-grouped button.bit-nav-gcb", new() { HasText = "Group one" });
        await header.ClickAsync();
        await Expect(header).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Page.Locator("#cnav-grouped ul[aria-label='Group one'] .bit-nav-ict")).ToHaveCountAsync(2);
        await Expect(Page.Locator("#cnav-grouped [role=separator]")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Manual_DefaultSelectedItemAndTwoWayBinding()
    {
        await OpenAsync(PAGE);

        await Expect(Item("cnav-default", "Banana")).ToHaveClassAsync(HasClass("bit-nav-sel"));
        await Expect(Item("cnav-default", "Fruits")).ToHaveAttributeAsync("aria-expanded", "true");

        await Item("cnav-bound", "Ice cream").ClickAsync();
        await Expect(Page.Locator("#cbound-selected")).ToHaveTextAsync("Ice cream");
        await Expect(Item("cnav-bound", "Ice cream")).ToHaveAttributeAsync("aria-current", "page");

        await Page.Locator("#cbtn-select-banana").ClickAsync();
        await Expect(Item("cnav-bound", "Banana")).ToHaveClassAsync(HasClass("bit-nav-sel"));
        await Expect(Item("cnav-bound", "Ice cream")).Not.ToHaveClassAsync(HasClass("bit-nav-sel"));
    }

    [TestMethod]
    public async Task IconOnly_HidesTheTextsAndNamesTheItems()
    {
        await OpenAsync(PAGE);

        var home = Item("cnav-icononly", "Home");
        await Expect(home.Locator(".bit-nav-itx")).ToBeHiddenAsync();
        await Expect(home).ToHaveAttributeAsync("aria-label", "Home");
    }

    [TestMethod]
    public async Task SingleExpandAllExpandedAndNoCollapse()
    {
        await OpenAsync(PAGE);

        await Item("cnav-single", "Alpha").ClickAsync();
        await Expect(Item("cnav-single", "Alpha one")).ToBeVisibleAsync();
        await Item("cnav-single", "Beta").ClickAsync();
        await Expect(Item("cnav-single", "Beta one")).ToBeVisibleAsync();
        await Expect(Item("cnav-single", "Alpha one")).ToHaveCountAsync(0);

        await Expect(Item("cnav-all", "Alpha deep")).ToBeVisibleAsync();

        await Expect(Page.Locator("#cnav-nocollapse .bit-nav-cbt")).ToHaveCountAsync(0);
        await Expect(Item("cnav-nocollapse", "Alpha one")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ReversedChevron_MovesTheChevronAfterTheText()
    {
        await OpenAsync(PAGE);

        var parent = Item("cnav-reversed", "Parent");
        await Expect(parent.Locator(".bit-nav-mct")).ToHaveCSSAsync("flex-direction", "row-reverse");
        await Expect(parent).ToHaveCSSAsync("padding-inline-start", "4px");
        await Expect(Item("cnav-reversed", "Child")).ToHaveCSSAsync("padding-inline-start", "20px");
    }

    [TestMethod]
    public async Task Templates_TheItemsOwnTemplateWinsInBothModes_AndTheSuffixTemplatesRender()
    {
        await OpenAsync(PAGE);

        var nav = Nav("cnav-templates");
        await Expect(nav.Locator(".bit-nav-ict .cnav-item-template")).ToHaveTextAsync("Item template: Plain");
        await Expect(nav.Locator(".bit-nav-ict .cnav-own-view")).ToHaveTextAsync("Own view: Own view");
        await Expect(nav.Locator("> ul > li > .cnav-replaced-view")).ToHaveTextAsync("Replaced view: Replaced view");
        await Expect(nav.Locator(".bit-nav-ict")).ToHaveCountAsync(2);

        await Expect(Page.Locator("#cnav-header-template button.bit-nav-gcb .cnav-header-template")).ToHaveTextAsync("Header template: Cars");

        await Expect(Item("cnav-suffix", "Shared suffix").Locator(".bit-nav-sfx .cnav-suffix")).ToHaveTextAsync("11");
        await Expect(Item("cnav-suffix", "Own suffix").Locator(".bit-nav-sfx .cnav-own-suffix")).ToHaveTextAsync("own 22");
        await Expect(Item("cnav-suffix", "Own suffix").Locator(".cnav-suffix")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task PublicApi_DrivesTheTreeOfCustomItems()
    {
        await OpenAsync(PAGE);

        await Page.Locator("#cbtn-expand-all").ClickAsync();
        await Expect(Item("cnav-api", "Apple")).ToBeVisibleAsync();
        await Page.Locator("#cbtn-collapse-all").ClickAsync();
        await Expect(Item("cnav-api", "Apple")).ToHaveCountAsync(0);
        await Page.Locator("#cbtn-toggle").ClickAsync();
        await Expect(Item("cnav-api", "Apple")).ToBeVisibleAsync();
        await Page.Locator("#cbtn-toggle").ClickAsync();
        await Expect(Item("cnav-api", "Apple")).ToHaveCountAsync(0);

        await Page.Locator("#cbtn-select").ClickAsync();
        await Expect(Item("cnav-api", "Ice cream")).ToHaveClassAsync(HasClass("bit-nav-sel"));

        await Page.Locator("#cbtn-focus").ClickAsync();
        await Expect(Item("cnav-api", "Apple")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Events_FireWithTheCustomItem()
    {
        await OpenAsync(PAGE);

        await Item("cnav-events", "Ice cream").ClickAsync();
        await Expect(Page.Locator("#cevent-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#cevent-last-clicked")).ToHaveTextAsync("Ice cream");
        await Expect(Page.Locator("#cevent-selects")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#cevent-last-selected")).ToHaveTextAsync("Ice cream");

        await Item("cnav-events", "Fruits").ClickAsync();
        await Expect(Page.Locator("#cevent-toggles")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#cevent-last-toggled")).ToHaveTextAsync("Fruits:open");
        await Item("cnav-events", "Fruits").ClickAsync();
        await Expect(Page.Locator("#cevent-last-toggled")).ToHaveTextAsync("Fruits:closed");

        await Item("cnav-reselectable", "Again").ClickAsync();
        await Item("cnav-reselectable", "Again").ClickAsync();
        await Expect(Page.Locator("#creselects")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task BitParams_AndTheLookParameters()
    {
        await OpenAsync(PAGE);

        await Expect(Nav("cnav-params")).ToHaveClassAsync(HasClass("bit-nav-wrn"));
        await Expect(Nav("cnav-params")).ToHaveClassAsync(HasClass("bit-nav-sm"));

        var look = Nav("cnav-look");
        await Expect(look).ToHaveClassAsync(HasClass("bit-nav-err"));
        await Expect(look).ToHaveClassAsync(HasClass("bit-nav-ainf"));
        await Expect(look).ToHaveClassAsync(HasClass("bit-nav-lg"));
        await Expect(look).ToHaveClassAsync(HasClass("custom-look"));
        await Expect(look).ToHaveCSSAsync("border-top-color", "rgb(9, 8, 7)");
        await Expect(look.Locator(".bit-nav-itx").First).ToHaveCSSAsync("letter-spacing", "3px");
        await Expect(look.Locator(".bit-nav-iic").First).ToHaveClassAsync(HasClass("custom-look-icon"));
        await Expect(Item("cnav-look", "Home")).ToHaveClassAsync(HasClass("bit-nav-sel"));
    }

    [TestMethod]
    public async Task Rtl_MirrorsTheNavAndTheArrowKeys()
    {
        await OpenAsync(PAGE);

        await Expect(Nav("cnav-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Item("cnav-rtl", "Rtl sibling")).ToHaveCSSAsync("padding-right", "27px");

        var parent = Item("cnav-rtl", "Rtl parent");
        await parent.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(parent).ToHaveAttributeAsync("aria-expanded", "true");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(parent).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task Disabled_TheNavIsUnavailable()
    {
        await OpenAsync(PAGE);

        await Expect(Nav("cnav-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Item("cnav-disabled", "Link")).Not.ToHaveAttributeAsync("href", new Regex(".*"));
        await Expect(Item("cnav-disabled", "Button")).ToBeDisabledAsync();

        await Item("cnav-disabled", "Link").DispatchEventAsync("click");
        await NextFramesAsync();
        await Expect(Page.Locator("#cdisabled-clicks")).ToHaveTextAsync("0");
    }
}
