using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiSourceCard at /e2e/aisourcecard in a real browser: the link laid over a card whose source has a web
/// address (a new tab, no opener and no referrer, named by what the card says), the site line's fallbacks, the
/// addresses that never become an href (javascript:, data:, relative, protocol-relative), the favicons that are loaded
/// and the ones that are refused (SVG above all), the button a card becomes with OnActivate (pointer and keyboard),
/// Disabled, the number, a title cut with an ellipsis, the theme tokens and every --bit-AiSourceCard-* variable, Styles
/// and Classes, and RTL.
/// </summary>
[TestClass]
public class BitAiSourceCardE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aisourcecard";

    private const string RenderModesUrl = "https://learn.microsoft.com/aspnet/core/blazor/components/render-modes";

    private ILocator Card(string id) => Page.Locator($"#{id}");

    private ILocator Link(string id) => Page.Locator($"#{id} a.bit-crd-lnk");

    // ---------- a link ----------

    [TestMethod]
    public async Task Link_AWebAddress_IsALinkOverTheCardIntoANewTabWithoutOpenerOrReferrer()
    {
        await OpenAsync(Route);

        var link = Link("src-link");
        await Expect(link).ToHaveCountAsync(1);
        await Expect(link).ToHaveAttributeAsync("href", RenderModesUrl);
        await Expect(link).ToHaveAttributeAsync("target", "_blank");
        await Expect(link).ToHaveAttributeAsync("rel", new Regex(@"(?=.*\bnoopener\b)(?=.*\bnoreferrer\b)"));

        // The link is named after everything the card says.
        await Expect(link).ToHaveAccessibleNameAsync(new Regex(@"1.*Blazor render modes.*Microsoft Learn"));

        await Expect(Card("src-link").Locator(".bit-aisrc-idx")).ToHaveTextAsync("1");
        await Expect(Card("src-link").Locator(".bit-aisrc-ttl")).ToHaveTextAsync("Blazor render modes");
        await Expect(Card("src-link").Locator(".bit-aisrc-sit")).ToHaveTextAsync("Microsoft Learn");

        // It is laid over the whole card, so the card is the link wherever it is pressed.
        var card = await Card("src-link").Locator(".bit-crd").RectAsync();
        var over = await link.RectAsync();
        Assert.AreEqual(card.Left, over.Left, 1.5);
        Assert.AreEqual(card.Right, over.Right, 1.5);
        Assert.AreEqual(card.Top, over.Top, 1.5);
        Assert.AreEqual(card.Bottom, over.Bottom, 1.5);
        await Expect(Card("src-link").Locator("button")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Link_TheKeyboard_ReachesTheLinkOnce()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit leaves links out of the Tab order unless the user turns that on (Safari's \"Press Tab to highlight each item\").");

        await OpenAsync(Route);

        await Link("src-link").FocusAsync();
        await Expect(Link("src-link")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Link("src-host")).ToBeFocusedAsync();

        // A card with no address is no stop at all.
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Link("src-trimmed")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Link_TheSiteLine_FallsBackToTheHostThenToTheKind()
    {
        await OpenAsync(Route);

        await Expect(Card("src-host").Locator(".bit-aisrc-sit")).ToHaveTextAsync("learn.microsoft.com");
        await Expect(Card("src-kind").Locator(".bit-aisrc-sit")).ToHaveTextAsync("Document");
        await Expect(Card("src-kind").Locator("a, button")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Link_AnAddressPaddedWithBlanks_IsTrimmedAndAnAriaLabelNamesTheLink()
    {
        await OpenAsync(Route);

        await Expect(Link("src-trimmed")).ToHaveAttributeAsync("href", "https://example.com/padded");
        await Expect(Card("src-trimmed").Locator(".bit-aisrc-sit")).ToHaveTextAsync("example.com");
        await Expect(Link("src-aria")).ToHaveAccessibleNameAsync("Open the render modes guide");
    }

    // ---------- unsafe addresses ----------

    [TestMethod]
    public async Task Unsafe_AnAddressThatIsNoWebAddress_NeverBecomesAnHref()
    {
        await OpenAsync(Route);

        foreach (var id in new[] { "src-js", "src-js-upper", "src-data-url", "src-relative", "src-protocol-relative" })
        {
            await Expect(Card(id).Locator("a")).ToHaveCountAsync(0);
            await Expect(Card(id).Locator("[href]")).ToHaveCountAsync(0);
            await Expect(Card(id).Locator("button")).ToHaveCountAsync(0);
        }

        // The site line is still the name the source gave.
        await Expect(Card("src-js").Locator(".bit-aisrc-sit")).ToHaveTextAsync("Script");
        await Expect(Card("src-relative").Locator(".bit-aisrc-sit")).ToHaveTextAsync("Relative");

        // Nothing anywhere on the page is a script address, and pressing a card runs nothing.
        Assert.AreEqual(0, await Page.EvaluateAsync<int>("() => [...document.querySelectorAll('[href],[src]')].filter(e => /^\\s*javascript:/i.test(e.getAttribute('href') ?? e.getAttribute('src'))).length"));

        var dialogs = 0;
        Page.Dialog += (_, dialog) => { dialogs++; _ = dialog.DismissAsync(); };
        var url = Page.Url;
        await Card("src-js").ClickAsync();
        await Card("src-data-url").ClickAsync();
        await NextFramesAsync();
        Assert.AreEqual(0, dialogs);
        Assert.AreEqual(url, Page.Url);
    }

    // ---------- favicons ----------

    [TestMethod]
    public async Task Favicon_AWebAddressOrAnInlineRasterImage_IsLoadedQuietly()
    {
        await OpenAsync(Route);

        var web = Card("src-fav-http").Locator("img.bit-aisrc-fav");
        await Expect(web).ToHaveAttributeAsync("src", new Regex(@"^http://localhost:\d+/samples/persona-sample-72\.png$"));
        await Expect(web).ToHaveAttributeAsync("alt", "");
        await Expect(web).ToHaveAttributeAsync("loading", "lazy");
        await Expect(web).ToHaveAttributeAsync("referrerpolicy", "no-referrer");

        await web.ScrollIntoViewIfNeededAsync();
        await Page.WaitForFunctionAsync("() => { const i = document.querySelector('#src-fav-http img'); return i && i.complete && i.naturalWidth > 0; }");

        var inline = Card("src-fav-png").Locator("img.bit-aisrc-fav");
        await Expect(inline).ToHaveAttributeAsync("src", new Regex("^data:image/png;base64,"));
        await Expect(inline).ToHaveAttributeAsync("alt", "");

        // The favicon is decorative: the site line beside it says the same.
        await Expect(web).ToHaveAccessibleNameAsync("");
    }

    [TestMethod]
    public async Task Favicon_AnSvgAScriptOrAnotherKindOfData_IsNeverLoaded()
    {
        await OpenAsync(Route);

        foreach (var id in new[] { "src-fav-svg", "src-fav-svg-base64", "src-fav-js", "src-fav-html" })
        {
            await Expect(Card(id).Locator("img")).ToHaveCountAsync(0);
            await Expect(Card(id).Locator(".bit-aisrc-sit")).ToHaveTextAsync("Microsoft Learn");
        }
    }

    // ---------- a button ----------

    [TestMethod]
    public async Task Button_OnActivate_MakesTheCardAButtonThatHandsTheSourceOver()
    {
        await OpenAsync(Route);

        var button = Card("src-button").Locator("button.bit-crd-lnk");
        await Expect(button).ToHaveCountAsync(1);
        await Expect(Card("src-button").Locator("a")).ToHaveCountAsync(0);
        await Expect(button).ToHaveAccessibleNameAsync(new Regex(@"Blazor render modes.*Microsoft Learn"));

        await Card("src-button").Locator(".bit-aisrc-ttl").ClickAsync();
        await Expect(Page.Locator("#src-activated")).ToHaveTextAsync("src-1");
        await Expect(Page.Locator("#src-activations")).ToHaveTextAsync("1");

        await button.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#src-activations")).ToHaveTextAsync("2");
        await Page.Keyboard.PressAsync("Space");
        await Expect(Page.Locator("#src-activations")).ToHaveTextAsync("3");
    }

    [TestMethod]
    public async Task Button_Disabled_HandsNothingOver()
    {
        await OpenAsync(Route);

        var button = Card("src-button-disabled").Locator("button.bit-crd-lnk");
        await Expect(button).ToBeDisabledAsync();
        await Expect(Card("src-button-disabled")).ToHaveClassAsync(HasClass("bit-dis"));

        await Card("src-button-disabled").ClickAsync(new() { Force = true });
        await button.DispatchEventAsync("click");
        await Card("src-button").Locator(".bit-aisrc-ttl").ClickAsync();
        await Expect(Page.Locator("#src-activated")).ToHaveTextAsync("src-1");
        await Expect(Page.Locator("#src-activations")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Disabled_ALinkCard_LeadsNowhereAndSaysSo()
    {
        await OpenAsync(Route);

        var link = Link("src-disabled");
        await Expect(link).ToHaveCountAsync(1);
        await Expect(link).Not.ToHaveAttributeAsync("href", new Regex(".*"));
        await Expect(link).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(link).ToHaveAttributeAsync("role", "link");
        await Expect(link).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Card("src-disabled")).ToHaveClassAsync(HasClass("bit-dis"));

        // Every part is drawn in the disabled text of the card.
        var color = await ComputedStyleAsync("#src-disabled .bit-crd", "color");
        foreach (var part in new[] { ".bit-aisrc-idx", ".bit-aisrc-ttl", ".bit-aisrc-sit" })
        {
            await Expect(Card("src-disabled").Locator(part)).ToHaveCSSAsync("color", color);
        }
    }

    // ---------- number, length ----------

    [TestMethod]
    public async Task Number_OnlyARealNumber_IsShown()
    {
        await OpenAsync(Route);

        await Expect(Card("src-no-number").Locator(".bit-aisrc-idx")).ToHaveCountAsync(0);
        await Expect(Card("src-max-number").Locator(".bit-aisrc-idx")).ToHaveCountAsync(0);
        await Expect(Card("src-number-12").Locator(".bit-aisrc-idx")).ToHaveTextAsync("12");
        await Expect(Card("src-number-12").Locator(".bit-aisrc-idx")).ToHaveCSSAsync("font-variant-numeric", "tabular-nums");
    }

    [TestMethod]
    public async Task Long_ATitleAndASiteTooLong_AreCutWithAnEllipsis()
    {
        await OpenAsync(Route);

        // The title is cut itself; the site line cuts the words beside its favicon.
        foreach (var part in new[] { ".bit-aisrc-ttl", ".bit-aisrc-sit > span" })
        {
            var element = Card("src-long").Locator(part);
            await Expect(element).ToHaveCSSAsync("text-overflow", "ellipsis");
            await Expect(element).ToHaveCSSAsync("white-space", "nowrap");
            await Expect(element).ToHaveCSSAsync("overflow-x", "hidden");
            Assert.IsTrue(await element.EvaluateAsync<bool>("e => e.scrollWidth > e.clientWidth"), $"{part} overflows its box.");
        }

        // The card keeps the width of its box rather than growing to its title.
        var box = await Page.Locator("#long > div").RectAsync();
        var card = await Card("src-long").RectAsync();
        Assert.IsTrue(card.Right <= box.Right + 1, "The card stays inside its box.");
    }

    // ---------- look ----------

    [TestMethod]
    public async Task Theme_TheCard_ReadsTheTypeRampAndTheSecondaryText()
    {
        await OpenAsync(Route);

        var card = Card("src-link");
        var secondary = await Page.ResolveColorAsync("var(--bit-clr-fg-sec)");
        await Expect(card.Locator(".bit-aisrc-ttl")).ToHaveCSSAsync("font-size", await card.ResolveLengthInAsync("var(--bit-tpg-fs-sm)"));
        await Expect(card.Locator(".bit-aisrc-ttl")).ToHaveCSSAsync("font-weight", await FontWeightAsync("var(--bit-tpg-fw-medium)"));
        await Expect(card.Locator(".bit-aisrc-sit")).ToHaveCSSAsync("font-size", await card.ResolveLengthInAsync("var(--bit-tpg-fs-xs)"));
        await Expect(card.Locator(".bit-aisrc-sit")).ToHaveCSSAsync("color", secondary);
        await Expect(card.Locator(".bit-aisrc-idx")).ToHaveCSSAsync("color", secondary);

        var favicon = Card("src-fav-png").Locator(".bit-aisrc-fav");
        await Expect(favicon).ToHaveCSSAsync("width", await Card("src-fav-png").ResolveLengthInAsync("var(--bit-siz-icon-sm)"));
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheCard()
    {
        await OpenAsync(Route);

        var card = Card("src-vars");
        await Expect(card.Locator(".bit-aisrc-ctn")).ToHaveCSSAsync("column-gap", "17px");
        await Expect(card.Locator(".bit-aisrc-idx")).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(card.Locator(".bit-aisrc-idx")).ToHaveCSSAsync("font-size", "11px");
        await Expect(card.Locator(".bit-aisrc-ttl")).ToHaveCSSAsync("color", "rgb(4, 5, 6)");
        await Expect(card.Locator(".bit-aisrc-ttl")).ToHaveCSSAsync("font-size", "19px");
        await Expect(card.Locator(".bit-aisrc-sit")).ToHaveCSSAsync("color", "rgb(7, 8, 9)");
        await Expect(card.Locator(".bit-aisrc-sit")).ToHaveCSSAsync("font-size", "11px");
        await Expect(card.Locator(".bit-aisrc-fav")).ToHaveCSSAsync("width", "22px");
        await Expect(card.Locator(".bit-aisrc-fav")).ToHaveCSSAsync("height", "22px");
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachTheParts()
    {
        await OpenAsync(Route);

        var card = Card("src-styles");
        await Expect(card.Locator(".bit-aisrc-ttl")).ToHaveCSSAsync("color", "rgb(40, 41, 42)");
        await Expect(card.Locator(".bit-aisrc-idx")).ToHaveCSSAsync("font-weight", "300");
        await Expect(card.Locator(".bit-aisrc-sit")).ToHaveClassAsync(HasClass("e2e-src-site"));
        await Expect(card.Locator(".bit-crd")).ToHaveClassAsync(HasClass("e2e-src-card"));
    }

    [TestMethod]
    public async Task Rtl_AnExplicitOrAnInheritedDirection_MirrorsTheCard()
    {
        await OpenAsync(Route);

        await Expect(Card("src-rtl")).ToHaveAttributeAsync("dir", "rtl");

        foreach (var id in new[] { "src-rtl", "src-rtl-inherited" })
        {
            var index = await Card(id).Locator(".bit-aisrc-idx").RectAsync();
            var body = await Card(id).Locator(".bit-aisrc-bdy").RectAsync();
            Assert.IsTrue(index.Left > body.Left, $"{id}: the number leads from the right.");

            var favicon = await Card(id).Locator(".bit-aisrc-fav").RectAsync();
            var site = await Card(id).Locator(".bit-aisrc-sit > span").RectAsync();
            Assert.IsTrue(favicon.Left > site.Left, $"{id}: the favicon leads the site line from the right.");
        }

        var ltrIndex = await Card("src-ltr").Locator(".bit-aisrc-idx").RectAsync();
        var ltrBody = await Card("src-ltr").Locator(".bit-aisrc-bdy").RectAsync();
        Assert.IsTrue(ltrIndex.Left < ltrBody.Left, "Left to right, the number leads from the left.");
    }

    // ---------- helpers ----------

    private Task<string> FontWeightAsync(string cssWeight)
    {
        return Page.EvaluateAsync<string>(
            """
            value => {
                const probe = document.createElement('div');
                probe.style.fontWeight = value;
                document.body.appendChild(probe);
                const resolved = getComputedStyle(probe).fontWeight;
                probe.remove();
                return resolved;
            }
            """, cssWeight);
    }
}
