using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Navs;

/// <summary>
/// End-to-end tests of the automatic mode of BitNav (/e2e/nav/url): the item matching the current URL is selected in the
/// prerendered HTML and after every real navigation inside the host (links, the nav's own anchors, back and forward),
/// with every BitNavMatch (Exact - ignoring case, a trailing slash, the query and the fragment -, Prefix on a path
/// boundary, Wildcard with ?, * and **, Regex), the Match of an item, AdditionalUrls, the path to a nested item opened,
/// OnSelectItem fired once per navigation, Reselectable, the Mode and Match switched at runtime, and the same matching
/// through the BitNavOption API and custom generic items.
/// </summary>
[TestClass]
public class BitNavUrlE2ETests : E2ETestBase
{
    private const string PAGE = "/e2e/nav/url";

    private static Regex Exact(string text) => new($"^{Regex.Escape(text)}$");

    private ILocator Item(string navId, string text)
    {
        return Page.Locator($"#{navId} .bit-nav-ict").Filter(new() { Has = Page.Locator(".bit-nav-itx", new() { HasTextRegex = Exact(text) }) });
    }

    private ILocator Selected(string navId) => Page.Locator($"#{navId} .bit-nav-sel");

    private async Task GoAsync(string linkId, string path)
    {
        await Page.Locator($"#{linkId}").ClickAsync();
        await Expect(Page.Locator("#current-path")).ToHaveTextAsync(path);
    }

    private async Task ExpectSelectedAsync(string navId, string? text)
    {
        if (text is null)
        {
            await Expect(Selected(navId)).ToHaveCountAsync(0);
            await Expect(Page.Locator($"#{navId} [aria-current]")).ToHaveCountAsync(0);
            return;
        }

        await Expect(Selected(navId)).ToHaveCountAsync(1);
        await Expect(Item(navId, text)).ToHaveClassAsync(HasClass("bit-nav-sel"));
        await Expect(Item(navId, text)).ToHaveAttributeAsync("aria-current", "page");
    }

    private static string NavHtml(string html, string navId)
    {
        var start = html.IndexOf($"id=\"{navId}\"", StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"The prerendered HTML holds #{navId}.");
        var end = html.IndexOf("</nav>", start, StringComparison.Ordinal);
        return html[start..end];
    }

    [TestMethod]
    public async Task Prerender_TheHtmlAlreadyCarriesTheSelectionOfTheCurrentUrl()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{PAGE}/team/alice");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "Status: <span id=\"status\">Loading</span>");

        // The Items API: the nested item is selected and the branch leading to it is open.
        StringAssert.Matches(NavHtml(html, "nav-exact"), new Regex("href=\"/e2e/nav/url/team/alice\"[^>]*class=\"bit-nav-ict bit-nav-sel[^\"]*\"[^>]*aria-current=\"page\""));

        // The options and the custom items do the same.
        StringAssert.Matches(NavHtml(html, "nav-options-auto"), new Regex("href=\"/e2e/nav/url/team/alice\"[^>]*class=\"bit-nav-ict bit-nav-sel[^\"]*\"[^>]*aria-current=\"page\""));
        StringAssert.Matches(NavHtml(html, "nav-custom-auto"), new Regex("href=\"/e2e/nav/url/team/alice\"[^>]*class=\"bit-nav-ict bit-nav-sel[^\"]*\"[^>]*aria-current=\"page\""));

        // And the interactive render keeps it.
        await OpenAsync($"{PAGE}/team/alice");
        await ExpectSelectedAsync("nav-exact", "Alice");
        await ExpectSelectedAsync("nav-options-auto", "Alice");
        await ExpectSelectedAsync("nav-custom-auto", "Alice");
        await Expect(Page.Locator("#option-selected")).ToHaveTextAsync("Alice");
    }

    [TestMethod]
    public async Task Exact_TheSelectionFollowsEveryNavigation_AndIsReportedOncePerNavigation()
    {
        await OpenAsync(PAGE);

        await ExpectSelectedAsync("nav-exact", "Root");
        // The interactive first render reports the selection the prerender showed, once.
        await Expect(Page.Locator("#exact-select-count")).ToHaveTextAsync("1");

        await GoAsync("go-products", "/products");
        await ExpectSelectedAsync("nav-exact", "Products");
        await Expect(Item("nav-exact", "Root")).Not.ToHaveClassAsync(HasClass("bit-nav-sel"));
        await Expect(Page.Locator("#exact-select-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#exact-selected")).ToHaveTextAsync("Products");

        // Exact is exact: a deeper page selects the item of that page, opening the branch it is in.
        await GoAsync("go-products-a", "/products/a");
        await ExpectSelectedAsync("nav-exact", "Product A");
        await Expect(Item("nav-exact", "Products")).ToHaveAttributeAsync("aria-expanded", "true");

        // A page nothing points at selects nothing.
        await GoAsync("go-products-a-b", "/products/a/b");
        await ExpectSelectedAsync("nav-exact", null);
        await Expect(Page.Locator("#exact-selected")).ToHaveTextAsync("none");
        await Expect(Page.Locator("#exact-select-count")).ToHaveTextAsync("4");
    }

    [TestMethod]
    public async Task Exact_ClickingAnItemsLink_NavigatesAndSelectsIt()
    {
        await OpenAsync(PAGE);

        await Item("nav-exact", "Contact").ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/e2e/nav/url/contact$"));
        await Expect(Page.Locator("#current-path")).ToHaveTextAsync("/contact");
        await ExpectSelectedAsync("nav-exact", "Contact");
        await Expect(Page.Locator("#exact-clicked")).ToHaveTextAsync("Contact");
    }

    [TestMethod]
    public async Task Exact_IgnoresTheCaseATrailingSlashTheQueryAndTheFragment()
    {
        await OpenAsync(PAGE);

        await GoAsync("go-products-upper", "/PRODUCTS/");
        await ExpectSelectedAsync("nav-exact", "Products");

        await GoAsync("go-contact-hash", "/contact");
        await ExpectSelectedAsync("nav-exact", "Contact");
    }

    [TestMethod]
    public async Task Exact_ANestedItemOpensItsBranch_WhichBorrowsTheSelectionOnceCollapsed()
    {
        await OpenAsync(PAGE);

        await Expect(Item("nav-exact", "Alice")).ToHaveCountAsync(0);

        await GoAsync("go-team-alice", "/team/alice");

        await ExpectSelectedAsync("nav-exact", "Alice");
        var team = Item("nav-exact", "Team");
        await Expect(team).ToHaveAttributeAsync("aria-expanded", "true");

        await team.ClickAsync();
        await Expect(team).ToHaveClassAsync(HasClass("bit-nav-sca"));
        await Expect(Item("nav-exact", "Alice")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task BackAndForward_TheSelectionFollowsTheHistory()
    {
        await OpenAsync(PAGE);

        await GoAsync("go-products", "/products");
        await ExpectSelectedAsync("nav-exact", "Products");

        await Page.GoBackAsync();
        await Expect(Page.Locator("#current-path")).ToHaveTextAsync("/");
        await ExpectSelectedAsync("nav-exact", "Root");

        await Page.GoForwardAsync();
        await Expect(Page.Locator("#current-path")).ToHaveTextAsync("/products");
        await ExpectSelectedAsync("nav-exact", "Products");
    }

    [TestMethod]
    public async Task Prefix_MatchesTheItemAndEverythingUnderIt_OnAPathBoundary()
    {
        await OpenAsync(PAGE);

        await ExpectSelectedAsync("nav-prefix", null);

        await GoAsync("go-products", "/products");
        await ExpectSelectedAsync("nav-prefix", "Products");

        await GoAsync("go-products-a-b", "/products/a/b");
        await ExpectSelectedAsync("nav-prefix", "Products");

        // "/product" is no prefix of "/products": the match stops at a segment boundary.
        await GoAsync("go-product", "/product");
        await ExpectSelectedAsync("nav-prefix", "Product");
        await Expect(Item("nav-prefix", "Products")).Not.ToHaveClassAsync(HasClass("bit-nav-sel"));

        await GoAsync("go-blog-42", "/blog/42");
        await ExpectSelectedAsync("nav-prefix", "Blog");
    }

    [TestMethod]
    public async Task Wildcard_QuestionMarkStarAndDoubleStar()
    {
        await OpenAsync(PAGE);

        // * is any run of characters within one segment.
        await GoAsync("go-products-a", "/products/a");
        await ExpectSelectedAsync("nav-wildcard", "One product");
        await GoAsync("go-products-a-b", "/products/a/b");
        await ExpectSelectedAsync("nav-wildcard", null);

        // ** is anything, across segments.
        await GoAsync("go-docs-deep", "/docs/x/y/z");
        await ExpectSelectedAsync("nav-wildcard", "Docs");

        // ? is exactly one character.
        await GoAsync("go-files-ab", "/files/ab");
        await ExpectSelectedAsync("nav-wildcard", "Files");
        await GoAsync("go-files-abc", "/files/abc");
        await ExpectSelectedAsync("nav-wildcard", null);
    }

    [TestMethod]
    public async Task Regex_TheItemsPatternDecides()
    {
        await OpenAsync(PAGE);

        await GoAsync("go-blog-42", "/blog/42");
        await ExpectSelectedAsync("nav-regex", "Numbered post");

        await GoAsync("go-blog-abc", "/blog/abc");
        await ExpectSelectedAsync("nav-regex", "Named post");

        await GoAsync("go-products", "/products");
        await ExpectSelectedAsync("nav-regex", null);
    }

    [TestMethod]
    public async Task TheMatchOfAnItem_WinsOverTheMatchOfTheNav()
    {
        await OpenAsync(PAGE);

        await GoAsync("go-products-a-b", "/products/a/b");
        await ExpectSelectedAsync("nav-item-match", "Products section");

        await GoAsync("go-contact-hash", "/contact");
        await ExpectSelectedAsync("nav-item-match", "Contact");
    }

    [TestMethod]
    public async Task AdditionalUrls_LightTheItemUpOnEveryOneOfThem()
    {
        await OpenAsync(PAGE);

        await GoAsync("go-settings", "/settings");
        await ExpectSelectedAsync("nav-additional", "Settings");

        await GoAsync("go-preferences", "/preferences");
        await ExpectSelectedAsync("nav-additional", "Settings");

        await GoAsync("go-options", "/options");
        await ExpectSelectedAsync("nav-additional", "Settings");

        await GoAsync("go-nowhere", "/nowhere");
        await ExpectSelectedAsync("nav-additional", null);
    }

    [TestMethod]
    public async Task Reselectable_ANavigationBackToTheSelectedItemIsReportedAgain()
    {
        await OpenAsync($"{PAGE}/products");

        await ExpectSelectedAsync("nav-auto-reselectable", "Products");
        await Expect(Page.Locator("#auto-reselect-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#exact-select-count")).ToHaveTextAsync("1");

        await Item("nav-auto-reselectable", "Products").ClickAsync();

        await Expect(Page.Locator("#auto-reselect-count")).ToHaveTextAsync("2");
        // A nav that is not reselectable swallows the navigation to where it already is.
        await Expect(Page.Locator("#exact-select-count")).ToHaveTextAsync("1");
        await ExpectSelectedAsync("nav-auto-reselectable", "Products");
    }

    [TestMethod]
    public async Task ModeAndMatch_ChangedAtRuntime_ReRunTheMatch()
    {
        await OpenAsync($"{PAGE}/products/a");

        // Manual: the default selection holds whatever the URL says.
        await ExpectSelectedAsync("nav-mode-switch", "Manual pick");
        await Page.Locator("#btn-to-automatic").ClickAsync();
        await ExpectSelectedAsync("nav-mode-switch", null);
        await GoAsync("go-products", "/products");
        await ExpectSelectedAsync("nav-mode-switch", "Products");

        await GoAsync("go-products-a", "/products/a");
        await ExpectSelectedAsync("nav-match-switch", null);
        await Page.Locator("#btn-to-prefix").ClickAsync();
        await ExpectSelectedAsync("nav-match-switch", "Products");
    }

    [TestMethod]
    public async Task Options_FollowTheUrlWithTheirOwnMatchAndAdditionalUrls()
    {
        await OpenAsync(PAGE);

        await ExpectSelectedAsync("nav-options-auto", "Root");

        await GoAsync("go-team-alice", "/team/alice");
        await ExpectSelectedAsync("nav-options-auto", "Alice");
        await Expect(Page.Locator("#option-selected")).ToHaveTextAsync("Alice");
        await Expect(Item("nav-options-auto", "Team")).ToHaveAttributeAsync("aria-expanded", "true");

        await GoAsync("go-preferences", "/preferences");
        await ExpectSelectedAsync("nav-options-auto", "Settings");

        await GoAsync("go-blog-42", "/blog/42");
        await ExpectSelectedAsync("nav-options-auto", "Posts");

        await GoAsync("go-nowhere", "/nowhere");
        await ExpectSelectedAsync("nav-options-auto", null);
    }

    [TestMethod]
    public async Task CustomItems_FollowTheUrlThroughTheirNameSelectors()
    {
        await OpenAsync(PAGE);

        await ExpectSelectedAsync("nav-custom-auto", "Root");

        await GoAsync("go-team-alice", "/team/alice");
        await ExpectSelectedAsync("nav-custom-auto", "Alice");

        await GoAsync("go-options", "/options");
        await ExpectSelectedAsync("nav-custom-auto", "Settings");

        await Item("nav-custom-auto", "Products").ClickAsync();
        await Expect(Page.Locator("#current-path")).ToHaveTextAsync("/products");
        await ExpectSelectedAsync("nav-custom-auto", "Products");
    }
}
