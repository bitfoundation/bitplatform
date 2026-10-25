using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Navs;

/// <summary>
/// End-to-end tests of BitNavBar with the BitNavBarOption API (/e2e/nav-bar/options): every parameter of an option
/// rendered (Text, IconName, SelectedIconName, Icon, SelectedIcon, Url, Target, Rel, IsDisabled, Key, Title, Class, Style,
/// Badge, BadgeAriaLabel, Dot, AriaLabel, AriaCurrent, Template, TemplateRenderMode), DefaultSelectedKey and a two-way
/// SelectedKey (the way the options API binds its selection), AutoReorderOptions keeping the keyboard in markup order,
/// an option removed while it holds the roving tab stop, the templates, Vertical/Filled/Indicator/Color/Size/Styles/
/// Classes, the events and the keyboard, the BitParams cascade, RTL and the disabled bar. The automatic mode of the
/// options is covered by BitNavBarUrlE2ETests.
/// </summary>
[TestClass]
public class BitNavBarOptionE2ETests : E2ETestBase
{
    private const string PAGE = "/e2e/nav-bar/options";

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
    public async Task Prerender_TheOptionsAreInTheHtml_WithTheDefaultSelectedKeySelected()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{PAGE}");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "id=\"onbr-basic\"");
        var start = html.IndexOf("id=\"onbr-default-key\"", StringComparison.Ordinal);
        var bar = html[start..html.IndexOf("</nav>", start, StringComparison.Ordinal)];
        StringAssert.Matches(bar, new Regex("class=\"bit-nbr-itm bit-nbr-sel\"[^>]*aria-current=\"page\"[^>]*>.*?>Search<", RegexOptions.Singleline));
    }

    [TestMethod]
    public async Task Basic_EveryParameterOfAnOptionIsRendered()
    {
        await OpenAsync(PAGE);

        var bar = Bar("onbr-basic");
        await Expect(bar).ToHaveAttributeAsync("aria-label", "Option bar");

        var home = Item("onbr-basic", "Home");
        await Expect(home).ToHaveAttributeAsync("title", "Home tip");
        await Expect(home).ToHaveClassAsync(HasClass("home-class"));
        await Expect(home).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(home.Locator(".bit-nbr-ico")).ToHaveClassAsync(HasClass("bit-icon--Home"));

        var link = Item("onbr-basic", "Link");
        await Expect(link).ToHaveAttributeAsync("href", "/e2e/nav-bar/url/link");
        await Expect(link).ToHaveAttributeAsync("target", "_blank");
        await Expect(link).ToHaveAttributeAsync("rel", "noreferrer");
        await Expect(link.Locator(".bit-nbr-vhd")).ToHaveTextAsync("(opens in a new tab)");

        await Expect(Item("onbr-basic", "Off")).ToBeDisabledAsync();
        await Expect(Item("onbr-basic", "Inbox").Locator(".bit-nbr-bdg")).ToHaveTextAsync("9");
        await Expect(Item("onbr-basic", "Inbox")).ToHaveAttributeAsync("aria-label", "Inbox (9 new)");
        await Expect(Item("onbr-basic", "Alerts").Locator(".bit-nbr-bdt")).ToHaveCountAsync(1);
        await Expect(Item("onbr-basic", "Alerts")).ToHaveAttributeAsync("aria-label", "Alerts (New alerts)");
        await Expect(Item("onbr-basic", "Named")).ToHaveAttributeAsync("aria-label", "Custom name");

        var pictured = Item("onbr-basic", "Pictured");
        await Expect(pictured.Locator(".bit-nbr-ico")).ToHaveClassAsync(HasClass("my-picture"));

        // Selecting swaps in the selected icons and sets the option's aria-current.
        await home.ClickAsync();
        await Expect(home.Locator(".bit-nbr-ico")).ToHaveClassAsync(HasClass("bit-icon--HomeSolid"));
        await pictured.ClickAsync();
        await Expect(pictured.Locator(".bit-nbr-ico")).ToHaveClassAsync(HasClass("my-picture-solid"));
        await Expect(home.Locator(".bit-nbr-ico")).ToHaveClassAsync(HasClass("bit-icon--Home"));
        await Item("onbr-basic", "Stepped").ClickAsync();
        await Expect(Item("onbr-basic", "Stepped")).ToHaveAttributeAsync("aria-current", "step");
    }

    [TestMethod]
    public async Task Keys_DefaultSelectedKeyAndATwoWaySelectedKey()
    {
        await OpenAsync(PAGE);

        await ExpectSelectedAsync("onbr-default-key", "Search");

        await Expect(Page.Locator("#onbr-selected-key")).ToHaveTextAsync("none");
        await Item("onbr-key", "Search").ClickAsync();
        await ExpectSelectedAsync("onbr-key", "Search");
        await Expect(Page.Locator("#onbr-selected-key")).ToHaveTextAsync("search");
        await Expect(Page.Locator("#onbr-selected-text")).ToHaveTextAsync("Search");

        await Page.Locator("#obtn-key-profile").ClickAsync();
        await ExpectSelectedAsync("onbr-key", "Profile");
    }

    [TestMethod]
    public async Task AutoReorderOptions_AnOptionShownLaterIsWalkedInItsMarkupPlace()
    {
        await OpenAsync(PAGE);

        await Page.Locator("#obtn-toggle-middle").ClickAsync();
        await Expect(Page.Locator("#onbr-reorder .bit-nbr-txc")).ToHaveTextAsync(["First", "Middle", "Last"]);
        await Expect(Page.Locator("#onbr-reorder .bit-nbr-lit").First).ToHaveAttributeAsync("data-bit-nbr-opt", new Regex(".+"));

        await Item("onbr-reorder", "First").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Item("onbr-reorder", "Middle")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(Item("onbr-reorder", "Last")).ToBeFocusedAsync();

        // Without it the options are walked in the order they registered in, and the marker is not rendered.
        await Expect(Page.Locator("#onbr-no-reorder .bit-nbr-txc")).ToHaveTextAsync(["First", "Middle", "Last"]);
        await Expect(Page.Locator("#onbr-no-reorder [data-bit-nbr-opt]")).ToHaveCountAsync(0);
        await Item("onbr-no-reorder", "First").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Item("onbr-no-reorder", "Last")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Removal_TheRovingTabStopMovesOffAnOptionThatIsRemoved()
    {
        await OpenAsync(PAGE);

        await Expect(Item("onbr-removal", "One")).ToHaveAttributeAsync("tabindex", "0");
        await Item("onbr-removal", "One").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Item("onbr-removal", "Two")).ToBeFocusedAsync();
        await Expect(Item("onbr-removal", "Two")).ToHaveAttributeAsync("tabindex", "0");

        await Page.Locator("#obtn-remove-second").ClickAsync();
        await Expect(Item("onbr-removal", "Two")).ToHaveCountAsync(0);

        // One of the options left holds the stop, so the bar stays reachable with Tab.
        await Expect(Page.Locator("#onbr-removal .bit-nbr-itm[tabindex='0']")).ToHaveCountAsync(1);
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task Templates_TheBarsTheOptionsOwnAndTheReplaceMode()
    {
        await OpenAsync(PAGE);

        var bar = Bar("onbr-templates");
        await Expect(bar.Locator(".bit-nbr-itm .onbr-item-template").First).ToHaveTextAsync("Item template: Plain");
        await Expect(bar.Locator(".bit-nbr-itm .onbr-own")).ToHaveTextAsync("Own: Own");
        await Expect(bar.Locator("li > button.onbr-replaced")).ToHaveTextAsync("Replaced: Replaced");
        await Expect(bar.Locator(".bit-nbr-itm")).ToHaveCountAsync(3);

        await bar.Locator(".bit-nbr-itm").Nth(1).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(bar.Locator(".bit-nbr-itm").Nth(2)).ToBeFocusedAsync();
        await Expect(bar.Locator(".bit-nbr-itm").Nth(2)).ToContainTextAsync("After");
    }

    [TestMethod]
    public async Task Look_VerticalFilledLineColorSizeStylesAndClasses()
    {
        await OpenAsync(PAGE);

        var bar = Bar("onbr-look");
        foreach (var cssClass in new[] { "bit-nbr-vrt", "bit-nbr-fil", "bit-nbr-lin", "bit-nbr-suc", "bit-nbr-lg", "bit-nbr-ftw" })
        {
            await Expect(bar).ToHaveClassAsync(HasClass(cssClass));
        }

        await ExpectSelectedAsync("onbr-look", "Home");
        var fill = await bar.EvaluateAsync<string>(@"el => {
            const probe = document.createElement('span');
            probe.style.color = getComputedStyle(el).getPropertyValue('--bit-nbr-clr-acc-active');
            document.body.appendChild(probe);
            const color = getComputedStyle(probe).color;
            probe.remove();
            return color;
        }");
        await Expect(Item("onbr-look", "Home")).ToHaveCSSAsync("background-color", fill);
        await Expect(Item("onbr-look", "Home").Locator(".bit-nbr-txt")).ToHaveCSSAsync("letter-spacing", "3px");
        await Expect(Item("onbr-look", "Home").Locator(".bit-nbr-ico")).ToHaveClassAsync(HasClass("custom-look-icon"));

        var home = (await Item("onbr-look", "Home").BoundingBoxAsync())!;
        var search = (await Item("onbr-look", "Search").BoundingBoxAsync())!;
        Assert.IsTrue(search.Y > home.Y, "A vertical rail stacks its options.");
    }

    [TestMethod]
    public async Task Events_FireWithTheOption_AndTheKeyboardWalksTheOptions()
    {
        await OpenAsync(PAGE);

        await Item("onbr-events", "Search").ClickAsync();
        await Expect(Page.Locator("#oevent-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#oevent-last-clicked")).ToHaveTextAsync("Search");
        await Expect(Page.Locator("#oevent-selects")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#oevent-last-selected")).ToHaveTextAsync("Search");

        await Item("onbr-events", "Search").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Item("onbr-events", "Profile")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(Item("onbr-events", "Home")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await ExpectSelectedAsync("onbr-events", "Home");
        await Expect(Page.Locator("#oevent-selects")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task BitParams_AndRtl()
    {
        await OpenAsync(PAGE);

        await Expect(Bar("onbr-params")).ToHaveClassAsync(HasClass("bit-nbr-ion"));
        await Expect(Bar("onbr-params")).ToHaveClassAsync(HasClass("bit-nbr-pil"));
        await Expect(Bar("onbr-params").Locator(".bit-nbr-txt")).ToHaveCountAsync(0);

        await Expect(Bar("onbr-rtl")).ToHaveAttributeAsync("dir", "rtl");
        var home = (await Item("onbr-rtl", "Home").BoundingBoxAsync())!;
        var search = (await Item("onbr-rtl", "Search").BoundingBoxAsync())!;
        Assert.IsTrue(home.X > search.X);
        await Item("onbr-rtl", "Home").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Item("onbr-rtl", "Search")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Disabled_TheBarIsUnavailable()
    {
        await OpenAsync(PAGE);

        await Expect(Bar("onbr-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        var link = Item("onbr-disabled", "Link");
        await Expect(link).Not.ToHaveAttributeAsync("href", new Regex(".*"));
        await Expect(link).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Item("onbr-disabled", "Button")).ToBeDisabledAsync();

        await link.DispatchEventAsync("click");
        await NextFramesAsync();
        await Expect(Page.Locator("#odisabled-clicks")).ToHaveTextAsync("0");
    }
}
