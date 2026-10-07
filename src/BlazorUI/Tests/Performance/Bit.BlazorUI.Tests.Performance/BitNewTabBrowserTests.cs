using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for the anchors that share BitNewTabUtils: BitLink, BitTag, BitButton, BitActionButton,
/// BitButtonGroup, BitMenuButton, BitBreadcrumb, BitNav, BitNavBar, BitBadge, BitPersona and BitCard.
///
/// bUnit pins the rel each of them renders, but not what that rel does: whether the page a new tab opens really
/// finds no window.opener - or finds one, where the rel asks for opener - and whether the new-tab sentence ends up
/// in the name the anchor is announced with. Every engine is asked, since each of them decides that on its own.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitNewTabBrowserTests
public class BitNewTabBrowserTests : PerformanceTestBase
{
    private readonly List<string> _errors = [];

    [TestMethod,
        DataRow("link-blank", "noopener"),
        DataRow("link-upper", "noopener"),
        DataRow("link-nofollow", "nofollow noopener"),
        DataRow("link-opener", "opener"),
        DataRow("link-labelled", "noopener"),
        DataRow("link-nohint", "noopener"),
        DataRow("link-splat", "nofollow noopener"),
        DataRow("link-self", null),
        DataRow("tag-blank", "noopener"),
        DataRow("tag-noreferrer", "nofollow noreferrer"),
        DataRow("tag-opener", "opener"),
        DataRow("tag-hint", "noopener"),
        DataRow("tag-nohint", "noopener"),
        DataRow("button-blank", "noopener"),
        DataRow("button-nofollow", "nofollow noopener"),
        DataRow("button-opener", "opener"),
        DataRow("button-splat-rel", "nofollow sponsored noopener"),
        DataRow("button-splat-opener", "opener"),
        DataRow("button-splat-target", "noopener"),
        DataRow("button-hash", null),
        DataRow("action-blank", "noopener"),
        DataRow("action-nofollow", "nofollow noopener"),
        DataRow("action-opener", "opener"),
        DataRow("badge-blank", "noopener"),
        DataRow("badge-nofollow", "nofollow noopener"),
        DataRow("persona-blank", "noopener"),
        DataRow("persona-opener", "opener"),
        DataRow("card-blank", "noopener"),
        DataRow("card-nofollow", "nofollow noopener"),
        DataRow("card-opener", "opener")
    ]
    public async Task NewTabAnchor_RendersTheSharedRel(string id, string? expectedRel)
    {
        await GoToThePage();

        Assert.AreEqual(expectedRel, await Anchor(id).GetAttributeAsync("rel"));
        AssertNoErrors();
    }

    [TestMethod]
    public async Task NewTabItems_RenderTheSharedRel()
    {
        await GoToThePage();

        var group = Page.Locator("#group a");
        Assert.AreEqual("noopener", await group.Nth(0).GetAttributeAsync("rel"));
        Assert.AreEqual("nofollow noopener", await group.Nth(1).GetAttributeAsync("rel"));
        Assert.AreEqual("opener", await group.Nth(2).GetAttributeAsync("rel"));

        var breadcrumb = Page.Locator("#breadcrumb a");
        Assert.AreEqual("noopener", await breadcrumb.Nth(0).GetAttributeAsync("rel"));
        Assert.AreEqual("opener", await breadcrumb.Nth(1).GetAttributeAsync("rel"));

        // A nav item opening a new tab is hardened whatever its origin, and one that does not is left alone,
        // external or not: the referrer is the app's to withhold.
        Assert.AreEqual("noopener", await NavItem("NavBlank").GetAttributeAsync("rel"));
        Assert.AreEqual("noopener", await NavItem("NavExternal").GetAttributeAsync("rel"));
        Assert.AreEqual("noreferrer", await NavItem("NavNoReferrer").GetAttributeAsync("rel"));
        Assert.AreEqual("opener", await NavItem("NavOpener").GetAttributeAsync("rel"));
        Assert.IsNull(await NavItem("NavSelf").GetAttributeAsync("rel"));

        Assert.AreEqual("noopener", await NavBarItem("BarBlank").GetAttributeAsync("rel"));
        Assert.AreEqual("opener", await NavBarItem("BarOpener").GetAttributeAsync("rel"));
        Assert.IsNull(await NavBarItem("BarSelf").GetAttributeAsync("rel"));

        await OpenTheMenu();
        Assert.AreEqual("noopener", await MenuItem("MenuBlank").GetAttributeAsync("rel"));
        Assert.AreEqual("opener", await MenuItem("MenuOpener").GetAttributeAsync("rel"));
        Assert.IsNull(await MenuItem("MenuSelf").GetAttributeAsync("rel"));

        AssertNoErrors();
    }

    [TestMethod,
        DataRow("link-blank", false),
        DataRow("link-upper", false),
        DataRow("link-nofollow", false),
        DataRow("link-opener", true),
        DataRow("link-splat", false),
        DataRow("tag-blank", false),
        DataRow("tag-noreferrer", false),
        DataRow("tag-opener", true),
        DataRow("button-blank", false),
        DataRow("button-nofollow", false),
        DataRow("button-opener", true),
        DataRow("button-splat-rel", false),
        DataRow("button-splat-opener", true),
        DataRow("button-splat-target", false),
        DataRow("action-blank", false),
        DataRow("action-opener", true),
        DataRow("badge-blank", false),
        DataRow("badge-nofollow", false),
        DataRow("persona-blank", false),
        DataRow("persona-opener", true),
        DataRow("card-blank", false),
        DataRow("card-nofollow", false),
        DataRow("card-opener", true)
    ]
    public async Task NewTabAnchor_OpenedPageReachesTheOpenerOnlyWhenAskedTo(string id, bool expectOpener)
    {
        await GoToThePage();

        Assert.AreEqual(expectOpener, await OpenAndReadTheOpener(Anchor(id)));
        AssertNoErrors();
    }

    [TestMethod]
    public async Task NewTabItems_OpenedPageReachesTheOpenerOnlyWhenAskedTo()
    {
        await GoToThePage();

        var group = Page.Locator("#group a");
        Assert.IsFalse(await OpenAndReadTheOpener(group.Nth(0)));
        Assert.IsFalse(await OpenAndReadTheOpener(group.Nth(1)));
        Assert.IsTrue(await OpenAndReadTheOpener(group.Nth(2)));

        Assert.IsFalse(await OpenAndReadTheOpener(Page.Locator("#breadcrumb a").Nth(0)));
        Assert.IsTrue(await OpenAndReadTheOpener(Page.Locator("#breadcrumb a").Nth(1)));

        Assert.IsFalse(await OpenAndReadTheOpener(NavItem("NavBlank")));
        Assert.IsFalse(await OpenAndReadTheOpener(NavItem("NavExternal")));
        Assert.IsFalse(await OpenAndReadTheOpener(NavItem("NavNoReferrer")));
        Assert.IsTrue(await OpenAndReadTheOpener(NavItem("NavOpener")));

        Assert.IsFalse(await OpenAndReadTheOpener(NavBarItem("BarBlank")));
        Assert.IsTrue(await OpenAndReadTheOpener(NavBarItem("BarOpener")));

        await OpenTheMenu();
        Assert.IsFalse(await OpenAndReadTheOpener(MenuItem("MenuBlank")));
        await OpenTheMenu();
        Assert.IsTrue(await OpenAndReadTheOpener(MenuItem("MenuOpener")));

        AssertNoErrors();
    }

    [TestMethod]
    public async Task NewTabNavItem_SendsTheReferrerUnlessAskedNotTo()
    {
        // The nav used to add noreferrer to every external new-tab link; it is the app's choice now, so the page
        // an item opens learns where the reader came from unless the item asks for NoReferrer.
        await GoToThePage();

        Assert.AreNotEqual("", await OpenAndReadTheReferrer(NavItem("NavExternal")));
        Assert.AreEqual("", await OpenAndReadTheReferrer(NavItem("NavNoReferrer")));

        AssertNoErrors();
    }

    [TestMethod]
    public async Task NewTabItems_AreAnnouncedAsOpeningANewTab()
    {
        await GoToThePage();

        await Expect(NavItem("NavBlank")).ToHaveAccessibleNameAsync("NavBlank (opens in a new tab)");
        await Expect(NavItem("NavDocs")).ToHaveAccessibleNameAsync("NavLabelled (opens in a new tab)");
        await Expect(NavItem("NavSelf")).ToHaveAccessibleNameAsync("NavSelf");
        await Expect(NavBarItem("BarBlank")).ToHaveAccessibleNameAsync("BarBlank (opens in a new tab)");
        await Expect(NavBarItem("BarSelf")).ToHaveAccessibleNameAsync("BarSelf");
        await Expect(Page.Locator("#breadcrumb a").Nth(0)).ToHaveAccessibleNameAsync("Home (opens in a new tab)");

        await OpenTheMenu();
        await Expect(MenuItem("MenuBlank")).ToHaveAccessibleNameAsync("MenuBlank (opens in a new tab)");
        await Expect(MenuItem("MenuSelf")).ToHaveAccessibleNameAsync("MenuSelf");

        AssertNoErrors();
    }

    [TestMethod,
        DataRow("link-blank", "Docs (opens in a new tab)"),
        DataRow("link-upper", "Upper (opens in a new tab)"),
        DataRow("link-opener", "Opener (opens in a new tab)"),
        DataRow("link-labelled", "Labelled (opens in a new tab)"),
        DataRow("link-nohint", "NoHint"),
        DataRow("link-splat", "Splat (opens in a new tab)"),
        DataRow("link-self", "Self"),
        DataRow("tag-blank", "Tag (opens in a new tab)"),
        DataRow("tag-opener", "TagOpener (opens in a new tab)"),
        DataRow("tag-hint", "TagHint (new window)"),
        DataRow("tag-nohint", "TagNoHint")
    ]
    public async Task NewTabAnchor_IsAnnouncedAsOpeningANewTab(string id, string expectedName)
    {
        await GoToThePage();

        await Expect(Anchor(id)).ToHaveAccessibleNameAsync(expectedName);
        AssertNoErrors();
    }

    private ILocator Anchor(string id) => Page.Locator($"a#{id}, #{id} a").First;

    private ILocator MenuItem(string text) => Page.Locator("a.bit-mnb-itm", new() { HasText = text });

    // A nav item is titled after its text, which finds it whatever its accessible name is made of.
    private ILocator NavItem(string text) => Page.Locator($"#nav a.bit-nav-ict[title='{text}']");

    private ILocator NavBarItem(string text) => Page.Locator("#navbar a.bit-nbr-itm", new() { HasText = text });

    private async Task OpenTheMenu()
    {
        await Page.Locator("#menu button").First.ClickAsync();
        await Expect(MenuItem("MenuBlank")).ToBeVisibleAsync();
    }

    // Opens the anchor's new tab and asks the page it opened whether it can still reach this one. A noopener
    // context is a page of its own rather than a popup of this one, so it is waited for on the browser context.
    private async Task<bool> OpenAndReadTheOpener(ILocator anchor)
    {
        var opened = await Page.Context.RunAndWaitForPageAsync(() => anchor.ClickAsync());

        try
        {
            await opened.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            return await opened.EvaluateAsync<bool>("() => window.opener !== null");
        }
        finally
        {
            await opened.CloseAsync();
        }
    }

    // Opens the anchor's new tab and reads the referrer the page it opened was handed.
    private async Task<string> OpenAndReadTheReferrer(ILocator anchor)
    {
        var opened = await Page.Context.RunAndWaitForPageAsync(() => anchor.ClickAsync());

        try
        {
            await opened.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            return await opened.EvaluateAsync<string>("() => document.referrer");
        }
        finally
        {
            await opened.CloseAsync();
        }
    }

    private async Task GoToThePage()
    {
        Page.Console += (_, message) =>
        {
            if (message.Type == "error") _errors.Add(message.Text);
        };
        Page.PageError += (_, error) => _errors.Add(error);

        await Page.GotoAsync($"{BaseUrl}/regression/new-tab-anchors");
        await WaitForStatus("Ready");

        // Leaving the home page the base class opened aborts the circuit it was still negotiating, which some
        // engines log as an error; only what goes wrong on this page counts.
        _errors.Clear();
    }

    private void AssertNoErrors()
    {
        Assert.IsEmpty(_errors, string.Join("\n", _errors));
    }
}
