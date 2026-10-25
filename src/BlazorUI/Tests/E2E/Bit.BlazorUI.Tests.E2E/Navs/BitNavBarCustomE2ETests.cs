using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Navs;

/// <summary>
/// End-to-end tests of BitNavBar over a custom generic item type (/e2e/nav-bar/custom), every member of which is renamed
/// and found through the NameSelectors, by Name or by Selector: text, URL, icons and selected icons, disabled state, key,
/// title, target, rel, badge, dot, aria label, aria-current, class, style, data and an item's own template in both render
/// modes; DefaultSelectedItem and a two-way SelectedItem and SelectedKey kept in step; the bar's item template; the
/// events, the keyboard, the Pill indicator, Color, Size and InlineText; RTL and the disabled bar. The automatic mode of
/// custom items is covered by BitNavBarUrlE2ETests.
/// </summary>
[TestClass]
public class BitNavBarCustomE2ETests : E2ETestBase
{
    private const string PAGE = "/e2e/nav-bar/custom";

    private static Regex Exact(string text) => new($"^{Regex.Escape(text)}$");

    private ILocator Bar(string id) => Page.Locator($"#{id}");

    private ILocator Item(string barId, string text)
    {
        return Page.Locator($"#{barId} .bit-nbr-itm").Filter(new() { Has = Page.Locator(".bit-nbr-txc", new() { HasTextRegex = Exact(text) }) });
    }

    private async Task ExpectSelectedAsync(string barId, string text)
    {
        await Expect(Page.Locator($"#{barId} .bit-nbr-sel")).ToHaveCountAsync(1);
        await Expect(Item(barId, text)).ToHaveClassAsync(HasClass("bit-nbr-sel"));
        await Expect(Item(barId, text)).ToHaveAttributeAsync("aria-current", "page");
    }

    [TestMethod]
    public async Task Basic_EveryMemberIsReadThroughTheNameSelectors()
    {
        await OpenAsync(PAGE);

        await Expect(Bar("cnbr-basic")).ToHaveAttributeAsync("aria-label", "Custom bar");

        var home = Item("cnbr-basic", "Home");
        await Expect(home).ToHaveAttributeAsync("title", "Home tip");
        await Expect(home).ToHaveClassAsync(HasClass("home-class"));
        await Expect(home).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(home.Locator(".bit-nbr-ico")).ToHaveClassAsync(HasClass("bit-icon--Home"));

        var link = Item("cnbr-basic", "Link");
        await Expect(link).ToHaveAttributeAsync("href", "/e2e/nav-bar/url/link");
        await Expect(link).ToHaveAttributeAsync("target", "_blank");
        await Expect(link).ToHaveAttributeAsync("rel", "noreferrer");

        await Expect(Item("cnbr-basic", "Off")).ToBeDisabledAsync();
        await Expect(Item("cnbr-basic", "Inbox").Locator(".bit-nbr-bdg")).ToHaveTextAsync("9");
        await Expect(Item("cnbr-basic", "Inbox")).ToHaveAttributeAsync("aria-label", "Inbox (9 new)");
        await Expect(Item("cnbr-basic", "Alerts").Locator(".bit-nbr-bdt")).ToHaveCountAsync(1);
        await Expect(Item("cnbr-basic", "Alerts")).ToHaveAttributeAsync("aria-label", "Alerts (New alerts)");
        await Expect(Item("cnbr-basic", "Named")).ToHaveAttributeAsync("aria-label", "Custom name");
        await Expect(Item("cnbr-basic", "Pictured").Locator(".bit-nbr-ico")).ToHaveClassAsync(HasClass("my-picture"));

        await home.ClickAsync();
        await Expect(home.Locator(".bit-nbr-ico")).ToHaveClassAsync(HasClass("bit-icon--HomeSolid"));
        await Item("cnbr-basic", "Pictured").ClickAsync();
        await Expect(Item("cnbr-basic", "Pictured").Locator(".bit-nbr-ico")).ToHaveClassAsync(HasClass("my-picture-solid"));
        await Item("cnbr-basic", "Stepped").ClickAsync();
        await Expect(Item("cnbr-basic", "Stepped")).ToHaveAttributeAsync("aria-current", "step");
    }

    [TestMethod]
    public async Task Manual_DefaultSelectedItemAndTheTwoWaySelectedItemAndKeyKeptInStep()
    {
        await OpenAsync(PAGE);

        await ExpectSelectedAsync("cnbr-default", "Search");

        await Item("cnbr-bound", "Search").ClickAsync();
        await ExpectSelectedAsync("cnbr-bound", "Search");
        await Expect(Page.Locator("#cbound-selected")).ToHaveTextAsync("Search");
        await Expect(Page.Locator("#cbound-key")).ToHaveTextAsync("search");

        await Page.Locator("#cbtn-select-third").ClickAsync();
        await ExpectSelectedAsync("cnbr-bound", "Profile");
        await Expect(Page.Locator("#cbound-key")).ToHaveTextAsync("profile");

        await Page.Locator("#cbtn-key-first").ClickAsync();
        await ExpectSelectedAsync("cnbr-bound", "Home");
        await Expect(Page.Locator("#cbound-selected")).ToHaveTextAsync("Home");
    }

    [TestMethod]
    public async Task Templates_TheItemsOwnTemplateWinsInBothModes()
    {
        await OpenAsync(PAGE);

        var bar = Bar("cnbr-templates");
        await Expect(bar.Locator(".bit-nbr-itm .cnbr-item-template").First).ToHaveTextAsync("Item template: Plain");
        await Expect(bar.Locator(".bit-nbr-itm .cnbr-own")).ToHaveTextAsync("Own: Own");
        await Expect(bar.Locator("li > button.cnbr-replaced")).ToHaveTextAsync("Replaced: Replaced");
        await Expect(bar.Locator(".bit-nbr-itm")).ToHaveCountAsync(3);
    }

    [TestMethod]
    public async Task Events_TheKeyboardAndTheLook()
    {
        await OpenAsync(PAGE);

        var bar = Bar("cnbr-events");
        foreach (var cssClass in new[] { "bit-nbr-pil", "bit-nbr-wrn", "bit-nbr-sm", "bit-nbr-inl" })
        {
            await Expect(bar).ToHaveClassAsync(HasClass(cssClass));
        }

        await Item("cnbr-events", "Profile").ClickAsync();
        await Expect(Page.Locator("#cevent-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#cevent-last-clicked")).ToHaveTextAsync("Profile");
        await Expect(Page.Locator("#cevent-selects")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#cevent-last-selected")).ToHaveTextAsync("Profile");

        await Item("cnbr-events", "Profile").FocusAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(Item("cnbr-events", "Home")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync(" ");
        await ExpectSelectedAsync("cnbr-events", "Home");
        await Expect(Page.Locator("#cevent-selects")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task RtlAndDisabled()
    {
        await OpenAsync(PAGE);

        var home = (await Item("cnbr-rtl", "Home").BoundingBoxAsync())!;
        var profile = (await Item("cnbr-rtl", "Profile").BoundingBoxAsync())!;
        Assert.IsTrue(home.X > profile.X);

        await Expect(Bar("cnbr-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Item("cnbr-disabled", "Link")).Not.ToHaveAttributeAsync("href", new Regex(".*"));
        await Expect(Item("cnbr-disabled", "Button")).ToBeDisabledAsync();
    }
}
