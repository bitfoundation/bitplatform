using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Navs;

/// <summary>
/// End-to-end tests of the automatic mode of BitNavBar (/e2e/nav-bar/url): the item matching the current URL is selected
/// in the prerendered HTML and after every real navigation inside the host (links, the bar's own anchors, back and
/// forward), with every BitNavMatch (Exact - ignoring case, a trailing slash, the query and the fragment -, Prefix on a
/// path boundary, Wildcard with ?, * and **, Regex), the Match of an item, AdditionalUrls, a bound SelectedKey reporting
/// the URL's item, OnSelectItem fired once per navigation, Reselectable, the Mode and Match switched at runtime, and the
/// same matching through the BitNavBarOption API and custom generic items.
/// </summary>
[TestClass]
public class BitNavBarUrlE2ETests : E2ETestBase
{
    private const string PAGE = "/e2e/nav-bar/url";

    private static Regex Exact(string text) => new($"^{Regex.Escape(text)}$");

    private ILocator Item(string barId, string text)
    {
        return Page.Locator($"#{barId} .bit-nbr-itm").Filter(new() { Has = Page.Locator(".bit-nbr-txc", new() { HasTextRegex = Exact(text) }) });
    }

    private async Task GoAsync(string linkId, string path)
    {
        await Page.Locator($"#{linkId}").ClickAsync();
        await Expect(Page.Locator("#current-path")).ToHaveTextAsync(path);
    }

    private async Task ExpectSelectedAsync(string barId, string? text)
    {
        if (text is null)
        {
            await Expect(Page.Locator($"#{barId} .bit-nbr-sel")).ToHaveCountAsync(0);
            await Expect(Page.Locator($"#{barId} [aria-current]")).ToHaveCountAsync(0);
            return;
        }

        await Expect(Page.Locator($"#{barId} .bit-nbr-sel")).ToHaveCountAsync(1);
        await Expect(Item(barId, text)).ToHaveClassAsync(HasClass("bit-nbr-sel"));
        await Expect(Item(barId, text)).ToHaveAttributeAsync("aria-current", "page");
    }

    private static string BarHtml(string html, string barId)
    {
        var start = html.IndexOf($"id=\"{barId}\"", StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"The prerendered HTML holds #{barId}.");
        var end = html.IndexOf("</nav>", start, StringComparison.Ordinal);
        return html[start..end];
    }

    [TestMethod]
    public async Task Prerender_TheHtmlAlreadyCarriesTheSelectionOfTheCurrentUrl()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{PAGE}/search");
        var html = await response.TextAsync();

        var selected = new Regex("href=\"/?e2e/nav-bar/url/search\"[^>]*class=\"bit-nbr-itm[^\"]*bit-nbr-sel[^\"]*\"[^>]*aria-current=\"page\"");
        StringAssert.Matches(BarHtml(html, "nbr-exact"), selected);
        StringAssert.Matches(BarHtml(html, "nbr-options-auto"), selected);
        StringAssert.Matches(BarHtml(html, "nbr-custom-auto"), selected);
        StringAssert.Contains(html, "Status: <span id=\"status\">Loading</span>");

        await OpenAsync($"{PAGE}/search");
        await ExpectSelectedAsync("nbr-exact", "Search");
        await ExpectSelectedAsync("nbr-options-auto", "Search");
        await ExpectSelectedAsync("nbr-custom-auto", "Search");
        await Expect(Page.Locator("#option-selected")).ToHaveTextAsync("Search");
    }

    [TestMethod]
    public async Task Exact_TheSelectionFollowsEveryNavigation_AndIsReportedOncePerNavigation()
    {
        await OpenAsync(PAGE);

        await ExpectSelectedAsync("nbr-exact", "Root");
        await Expect(Page.Locator("#exact-select-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#exact-key")).ToHaveTextAsync("root");

        await GoAsync("go-home", "/home");
        await ExpectSelectedAsync("nbr-exact", "Home");
        await Expect(Page.Locator("#exact-select-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#exact-selected")).ToHaveTextAsync("Home");
        // A bound SelectedKey reports the item the URL selected.
        await Expect(Page.Locator("#exact-key")).ToHaveTextAsync("home");

        await GoAsync("go-search-deep", "/search/a/b");
        await ExpectSelectedAsync("nbr-exact", null);
        await Expect(Page.Locator("#exact-selected")).ToHaveTextAsync("none");
        await Expect(Page.Locator("#exact-key")).ToHaveTextAsync("none");
    }

    [TestMethod]
    public async Task Exact_ClickingAnItemsLink_NavigatesAndSelectsIt()
    {
        await OpenAsync(PAGE);

        await Item("nbr-exact", "Profile").ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/e2e/nav-bar/url/profile$"));
        await ExpectSelectedAsync("nbr-exact", "Profile");
        await Expect(Page.Locator("#exact-clicked")).ToHaveTextAsync("Profile");
    }

    [TestMethod]
    public async Task Exact_IgnoresTheCaseATrailingSlashTheQueryAndTheFragment()
    {
        await OpenAsync(PAGE);

        await GoAsync("go-home-upper", "/HOME/");
        await ExpectSelectedAsync("nbr-exact", "Home");

        // An item URL written without its leading slash matches all the same.
        await GoAsync("go-search", "/search");
        await ExpectSelectedAsync("nbr-exact", "Search");
    }

    [TestMethod]
    public async Task BackAndForward_TheSelectionFollowsTheHistory()
    {
        await OpenAsync(PAGE);

        await GoAsync("go-home", "/home");
        await GoAsync("go-profile", "/profile");
        await ExpectSelectedAsync("nbr-exact", "Profile");

        await Page.GoBackAsync();
        await Expect(Page.Locator("#current-path")).ToHaveTextAsync("/home");
        await ExpectSelectedAsync("nbr-exact", "Home");

        await Page.GoForwardAsync();
        await Expect(Page.Locator("#current-path")).ToHaveTextAsync("/profile");
        await ExpectSelectedAsync("nbr-exact", "Profile");
    }

    [TestMethod]
    public async Task Prefix_MatchesOnAPathBoundary()
    {
        await OpenAsync(PAGE);

        await GoAsync("go-search", "/search");
        await ExpectSelectedAsync("nbr-prefix", "Search");

        await GoAsync("go-search-deep", "/search/a/b");
        await ExpectSelectedAsync("nbr-prefix", "Search");

        await GoAsync("go-searches", "/searches");
        await ExpectSelectedAsync("nbr-prefix", null);

        await GoAsync("go-docs-deep", "/docs/one/two");
        await ExpectSelectedAsync("nbr-prefix", "Docs");
    }

    [TestMethod]
    public async Task Wildcard_QuestionMarkStarAndDoubleStar()
    {
        await OpenAsync(PAGE);

        await GoAsync("go-docs-one", "/docs/one");
        await ExpectSelectedAsync("nbr-wildcard", "One doc");

        await GoAsync("go-docs-deep", "/docs/one/two");
        await ExpectSelectedAsync("nbr-wildcard", null);

        await GoAsync("go-search-deep", "/search/a/b");
        await ExpectSelectedAsync("nbr-wildcard", "Any search");

        await GoAsync("go-post-7", "/post/7");
        await ExpectSelectedAsync("nbr-wildcard", "Post");

        await GoAsync("go-search", "/search");
        await ExpectSelectedAsync("nbr-wildcard", null);
    }

    [TestMethod]
    public async Task Regex_TheItemsPatternDecides()
    {
        await OpenAsync(PAGE);

        await GoAsync("go-post-7", "/post/7");
        await ExpectSelectedAsync("nbr-regex", "Numbered post");

        await GoAsync("go-post-x", "/post/x");
        await ExpectSelectedAsync("nbr-regex", "Named post");

        await GoAsync("go-home", "/home");
        await ExpectSelectedAsync("nbr-regex", null);
    }

    [TestMethod]
    public async Task TheMatchOfAnItemAndAdditionalUrls()
    {
        await OpenAsync(PAGE);

        await GoAsync("go-search-deep", "/search/a/b");
        await ExpectSelectedAsync("nbr-item-match", "Search area");

        await GoAsync("go-home", "/home");
        await ExpectSelectedAsync("nbr-item-match", "Home");
        await ExpectSelectedAsync("nbr-additional", "Home");

        await GoAsync("go-me", "/me");
        await ExpectSelectedAsync("nbr-additional", "Profile");

        await GoAsync("go-nowhere", "/nowhere");
        await ExpectSelectedAsync("nbr-additional", null);
    }

    [TestMethod]
    public async Task Reselectable_ANavigationBackToTheSelectedItemIsReportedAgain()
    {
        await OpenAsync($"{PAGE}/home");

        await ExpectSelectedAsync("nbr-auto-reselectable", "Home");
        await Expect(Page.Locator("#auto-reselect-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#exact-select-count")).ToHaveTextAsync("1");

        await Item("nbr-auto-reselectable", "Home").ClickAsync();

        await Expect(Page.Locator("#auto-reselect-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#exact-select-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task ModeAndMatch_ChangedAtRuntime_ReRunTheMatch()
    {
        await OpenAsync($"{PAGE}/search/a/b");

        await ExpectSelectedAsync("nbr-mode-switch", "Manual pick");
        await Page.Locator("#btn-to-automatic").ClickAsync();
        await ExpectSelectedAsync("nbr-mode-switch", null);
        await GoAsync("go-search", "/search");
        await ExpectSelectedAsync("nbr-mode-switch", "Search");

        await GoAsync("go-search-deep", "/search/a/b");
        await ExpectSelectedAsync("nbr-match-switch", null);
        await Page.Locator("#btn-to-prefix").ClickAsync();
        await ExpectSelectedAsync("nbr-match-switch", "Search");
    }

    [TestMethod]
    public async Task Options_FollowTheUrlWithTheirOwnMatchAndAdditionalUrls()
    {
        await OpenAsync(PAGE);

        await ExpectSelectedAsync("nbr-options-auto", null);

        await GoAsync("go-home", "/home");
        await ExpectSelectedAsync("nbr-options-auto", "Home");
        await Expect(Page.Locator("#option-selected")).ToHaveTextAsync("Home");

        await GoAsync("go-search-deep", "/search/a/b");
        await ExpectSelectedAsync("nbr-options-auto", "Search");

        await GoAsync("go-post-7", "/post/7");
        await ExpectSelectedAsync("nbr-options-auto", "Posts");

        await GoAsync("go-me", "/me");
        await ExpectSelectedAsync("nbr-options-auto", "Profile");
    }

    [TestMethod]
    public async Task CustomItems_FollowTheUrlThroughTheirNameSelectors()
    {
        await OpenAsync(PAGE);

        await GoAsync("go-search-deep", "/search/a/b");
        await ExpectSelectedAsync("nbr-custom-auto", "Search");

        await GoAsync("go-me", "/me");
        await ExpectSelectedAsync("nbr-custom-auto", "Profile");

        await Item("nbr-custom-auto", "Home").ClickAsync();
        await Expect(Page.Locator("#current-path")).ToHaveTextAsync("/home");
        await ExpectSelectedAsync("nbr-custom-auto", "Home");
    }
}
