using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Utilities;

/// <summary>
/// BitMediaQuery in a real browser (/e2e/media-query), the viewport resized across every breakpoint: every
/// BitScreenQuery at both sides of each edge, a custom Query (OnChange once initially and once per flip,
/// @bind-IsMatched, a leading @media, a change at runtime, the ScreenQuery it falls back to, an invalid one,
/// orientation, prefers-color-scheme and prefers-reduced-motion), a one-way IsMatched, DefaultMatched before the
/// browser answers (the prerendered HTML), ChildContent, Template keeping its content across a flip, the focus kept
/// across a flip, Element and NoWrapper, the breakpoints of a scoped --bit-bp-*, of a BitThemeProvider and of the
/// document re-valued at runtime, the BitParams cascade, Id / Class / Style / AriaLabel / role / Dir / HtmlAttributes /
/// Visibility, and a media query that stops listening once removed.
/// </summary>
[TestClass]
public class BitMediaQueryE2ETests : E2ETestBase
{
    private const string Route = "/e2e/media-query";

    // The edges of the default scale, as BitScreenQuery documents them.
    private static readonly (string Query, int Min, int Max)[] ScreenQueries =
    [
        ("Xs", 0, 600), ("Sm", 600, 960), ("Md", 960, 1280), ("Lg", 1280, 1920), ("Xl", 1920, 2560), ("Xxl", 2560, int.MaxValue),
        ("LtSm", 0, 600), ("LtMd", 0, 960), ("LtLg", 0, 1280), ("LtXl", 0, 1920), ("LtXxl", 0, 2560),
        ("GtXs", 600, int.MaxValue), ("GtSm", 960, int.MaxValue), ("GtMd", 1280, int.MaxValue), ("GtLg", 1920, int.MaxValue), ("GtXl", 2560, int.MaxValue),
        ("SmToMd", 600, 1280), ("SmToLg", 600, 1920), ("SmToXl", 600, 2560), ("MdToLg", 960, 1920), ("MdToXl", 960, 2560), ("LgToXl", 1280, 2560)
    ];

    [TestMethod]
    [DataRow(400)]
    [DataRow(599)]
    [DataRow(600)]
    [DataRow(959)]
    [DataRow(960)]
    [DataRow(1279)]
    [DataRow(1280)]
    [DataRow(1919)]
    [DataRow(1920)]
    [DataRow(2559)]
    [DataRow(2560)]
    [DataRow(2800)]
    public async Task ScreenQuery_EveryQueryAtThisWidth_MatchesExactlyItsRange(int width)
    {
        await OpenAsync(Route);

        await Page.SetViewportSizeAsync(width, 800);

        foreach (var (query, min, max) in ScreenQueries)
        {
            var matched = width >= min && width < max;

            await Expect(Page.Locator($"#sq-{query}")).ToHaveTextAsync(matched ? "True" : "False");
            await Expect(Page.Locator($"#mdq-{query}")).ToHaveTextAsync(matched ? "yes" : "no");
        }
    }

    [TestMethod]
    public async Task ScreenQuery_OpenedAtAPhoneWidth_ReportsItsFirstStateRightAway()
    {
        await Page.SetViewportSizeAsync(390, 800);
        await OpenAsync(Route);

        await Expect(Page.Locator("#sq-Xs")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#sq-GtXs")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#mdq-LtMd")).ToHaveTextAsync("yes");
    }

    [TestMethod]
    public async Task Query_OnChangeReportsTheFirstStateOnce_AndEveryFlip_TheBindingFollows()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#custom-changes")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#custom-last")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#custom-bound")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#mdq-custom")).ToHaveTextAsync("wide");

        await Page.SetViewportSizeAsync(700, 800);
        await Expect(Page.Locator("#custom-changes")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#custom-last")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#custom-bound")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#mdq-custom")).ToHaveTextAsync("narrow");

        // A resize that stays on the same side reports nothing.
        await Page.SetViewportSizeAsync(750, 800);
        await Page.SetViewportSizeAsync(900, 800);
        await Expect(Page.Locator("#custom-changes")).ToHaveTextAsync("3");
        await Expect(Page.Locator("#custom-last")).ToHaveTextAsync("True");
    }

    [TestMethod]
    public async Task Query_ChangedAtRuntime_ListensToTheNewOne_AndANullQueryFallsBackToScreenQuery()
    {
        await OpenAsync(Route);
        await Page.SetViewportSizeAsync(1000, 800);
        await Expect(Page.Locator("#custom-bound")).ToHaveTextAsync("True");

        await Page.ClickAsync("#btn-query-1200");
        await Expect(Page.Locator("#custom-bound")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#mdq-custom")).ToHaveTextAsync("narrow");

        await Page.SetViewportSizeAsync(1250, 800);
        await Expect(Page.Locator("#custom-bound")).ToHaveTextAsync("True");

        // No Query: the ScreenQuery (GtSm, from 960px) answers.
        await Page.SetViewportSizeAsync(1000, 800);
        await Expect(Page.Locator("#custom-bound")).ToHaveTextAsync("False");
        await Page.ClickAsync("#btn-query-null");
        await Expect(Page.Locator("#custom-query")).ToHaveTextAsync("null");
        await Expect(Page.Locator("#custom-bound")).ToHaveTextAsync("True");
        await Page.SetViewportSizeAsync(900, 800);
        await Expect(Page.Locator("#custom-bound")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Query_ALeadingAtMediaIsDropped_AndNonViewportFeaturesAreFollowed()
    {
        await OpenAsync(Route);

        await Page.SetViewportSizeAsync(1280, 720);
        await Expect(Page.Locator("#mdq-at-media")).ToHaveTextAsync("landscape");
        await Page.SetViewportSizeAsync(600, 900);
        await Expect(Page.Locator("#mdq-at-media")).ToHaveTextAsync("portrait");

        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await Expect(Page.Locator("#mdq-dark")).ToHaveTextAsync("dark");
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Light });
        await Expect(Page.Locator("#mdq-dark")).ToHaveTextAsync("light");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await Expect(Page.Locator("#mdq-reduce")).ToHaveTextAsync("reduce");
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.NoPreference });
        await Expect(Page.Locator("#mdq-reduce")).ToHaveTextAsync("full");
    }

    [TestMethod]
    public async Task Query_AnInvalidOne_NeverMatches_AndSaysSoInAWarning()
    {
        var warnings = new List<string>();
        Page.Console += (_, message) =>
        {
            if (message.Type == "warning") warnings.Add(message.Text);
        };

        await OpenAsync(Route);

        await Expect(Page.Locator("#mdq-invalid")).ToHaveTextAsync("never");
        await Expect(Page.Locator("#invalid-last")).ToHaveTextAsync("False");
        Assert.IsTrue(warnings.Any(w => w.Contains("is not a valid media query")), "No warning named the invalid query.");
    }

    [TestMethod]
    public async Task ChildContent_IsTheMatchedContent_AndNothingRendersWhenNotMatched()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#child-content")).ToBeVisibleAsync();

        await Page.SetViewportSizeAsync(700, 800);
        await Expect(Page.Locator("#childcontent-state")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#child-content")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#mdq-childcontent")).ToHaveTextAsync("");
    }

    [TestMethod]
    public async Task IsMatched_SetOneWay_FreezesTheContent_WhileOnChangeStillReports()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#oneway-last")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#mdq-oneway")).ToHaveTextAsync("frozen");
    }

    [TestMethod]
    public async Task DefaultMatched_IsWhatThePrerenderShows_UntilTheBrowserAnswers()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "<div id=\"mdq-default\" class=\"bit-mdq\"><b>guessed</b></div>");
        StringAssert.Contains(html, "<div id=\"mdq-nodefault\" class=\"bit-mdq\"><i>not yet</i></div>");
        StringAssert.Contains(html, "id=\"status\">Loading");

        await OpenAsync(Route);

        await Expect(Page.Locator("#mdq-default")).ToHaveTextAsync("answered");
        await Expect(Page.Locator("#mdq-nodefault")).ToHaveTextAsync("answered");
    }

    [TestMethod]
    public async Task Template_TheSameContentIsUpdatedAcrossTheFlip_ItsStateKept()
    {
        await OpenAsync(Route);

        var input = Page.Locator("#template-input");
        await Expect(input).ToHaveClassAsync("wide");
        await input.FillAsync("half filled");
        await input.EvaluateAsync("el => el.__e2eMarker = 'same element'");

        await Page.SetViewportSizeAsync(700, 800);

        await Expect(Page.Locator("#template-state")).ToHaveTextAsync("narrow");
        await Expect(input).ToHaveClassAsync("narrow");
        await Expect(input).ToHaveValueAsync("half filled");
        Assert.AreEqual("same element", await input.EvaluateAsync<string>("el => el.__e2eMarker"));
    }

    [TestMethod]
    public async Task Focus_AFlipReplacingTheFocusedButton_HandsTheFocusToItsTwin()
    {
        await OpenAsync(Route);

        await Page.FocusAsync("#focus-same");
        await Page.SetViewportSizeAsync(700, 800);

        await Expect(Page.Locator("#focus-same")).ToHaveTextAsync("Narrow");
        await Expect(Page.Locator("#focus-same")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Focus_WithoutATwin_TheFirstFocusableOfTheNewContentTakesIt()
    {
        await OpenAsync(Route);

        await Page.FocusAsync("#focus-wide");
        await Page.SetViewportSizeAsync(700, 800);

        await Expect(Page.Locator("#focus-narrow")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Focus_WithNothingFocusableLeft_TheRootHoldsItUntilItMovesOn()
    {
        await OpenAsync(Route);

        await Page.FocusAsync("#focus-gone");
        await Page.SetViewportSizeAsync(700, 800);

        var root = Page.Locator("#mdq-focus-none");
        await Expect(root).ToBeFocusedAsync();
        await Expect(root).ToHaveAttributeAsync("tabindex", "-1");

        // The next Tab carries on from there, and the borrowed tabindex goes.
        await Page.Keyboard.PressAsync("Tab");
        await Expect(root).Not.ToBeFocusedAsync();
        await Expect(root).Not.ToHaveAttributeAsync("tabindex", new Regex(".*"));
    }

    [TestMethod]
    public async Task Element_RendersTheTagAsked_FallingBackToADivForAnInvalidOrVoidName()
    {
        await OpenAsync(Route);

        Assert.AreEqual("SPAN", await TagAsync("#mdq-span"));
        Assert.AreEqual("P", await Page.Locator("#mdq-span").EvaluateAsync<string>("el => el.parentElement.tagName"));
        Assert.AreEqual("LI", await TagAsync("#mdq-li"));
        await Expect(Page.Locator("#mdq-li")).ToHaveCSSAsync("display", "list-item");
        Assert.AreEqual("DIV", await TagAsync("#mdq-invalid-element"));
        await Expect(Page.Locator("#mdq-invalid-element")).ToHaveTextAsync("invalid tag");
        Assert.AreEqual("DIV", await TagAsync("#mdq-void-element"));
        await Expect(Page.Locator("#mdq-void-element")).ToHaveTextAsync("void tag");
    }

    [TestMethod]
    public async Task NoWrapper_RendersTheContentAlone_AndACollapsedOneRendersNothing()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#mdq-nowrapper")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".ignored")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#nowrapper-host > b#nowrapper-wide")).ToHaveCountAsync(1);

        await Page.SetViewportSizeAsync(700, 800);
        await Expect(Page.Locator("#nowrapper-host > i#nowrapper-narrow")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#nowrapper-wide")).ToHaveCountAsync(0);

        Assert.AreEqual(0, await Page.Locator("#nowrapper-collapsed-host").EvaluateAsync<int>("el => el.children.length"));
    }

    [TestMethod]
    public async Task Breakpoints_AScopedVariableAndAThemeProvider_MoveTheEdge()
    {
        await OpenAsync(Route);
        await Page.SetViewportSizeAsync(1000, 800);

        // The default Md starts at 960px; the scoped and the provided ones at 1100px.
        await Expect(Page.Locator("#sq-Md")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#scoped-md")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#mdq-scoped-md")).ToHaveTextAsync("not md");
        await Expect(Page.Locator("#provider-gtsm")).ToHaveTextAsync("False");

        await Page.SetViewportSizeAsync(1150, 800);
        await Expect(Page.Locator("#scoped-md")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#mdq-scoped-md")).ToHaveTextAsync("md");
        await Expect(Page.Locator("#provider-gtsm")).ToHaveTextAsync("True");
    }

    [TestMethod]
    public async Task Breakpoints_TheDocumentReValuedAtRuntime_IsFollowedWithoutARender()
    {
        await OpenAsync(Route);
        await Page.SetViewportSizeAsync(1000, 800);
        await Expect(Page.Locator("#doc-gtxs")).ToHaveTextAsync("True");

        await Page.EvaluateAsync("() => document.body.style.setProperty('--bit-bp-sm', '1200px')");

        await Expect(Page.Locator("#doc-gtxs")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#mdq-doc-gtxs")).ToHaveTextAsync("xs");

        await Page.EvaluateAsync("() => document.body.style.removeProperty('--bit-bp-sm')");
        await Expect(Page.Locator("#doc-gtxs")).ToHaveTextAsync("True");
    }

    [TestMethod]
    public async Task Cascade_BitParamsSetsTheDefaults_AnOwnQueryWins_AndAChangeReachesThem()
    {
        await OpenAsync(Route);
        await Page.SetViewportSizeAsync(1000, 800);

        Assert.AreEqual("SECTION", await TagAsync("#mdq-cascade"));
        await Expect(Page.Locator("#cascade-state")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#mdq-cascade")).ToHaveTextAsync("cascaded match");

        // Its own ScreenQuery and Element: the cascaded Query and Element are not taken.
        Assert.AreEqual("DIV", await TagAsync("#mdq-cascade-own"));
        await Expect(Page.Locator("#cascade-own-state")).ToHaveTextAsync("False");

        await Page.ClickAsync("#btn-cascade-change");

        await Expect(Page.Locator("#cascade-state")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#mdq-cascade")).ToHaveTextAsync("cascaded miss");
        Assert.AreEqual("ASIDE", await TagAsync("#mdq-cascade"));
        Assert.AreEqual("DIV", await TagAsync("#mdq-cascade-own"));

        await Page.SetViewportSizeAsync(500, 800);
        await Expect(Page.Locator("#cascade-own-state")).ToHaveTextAsync("True");
    }

    [TestMethod]
    public async Task Base_IdClassStyleAttributesAriaLabelAndDir_LandOnTheRoot()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#mdq-base");
        await Expect(root).ToHaveClassAsync(HasClass("bit-mdq"));
        await Expect(root).ToHaveClassAsync(HasClass("e2e-mdq-class"));
        await Expect(root).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(root).ToHaveCSSAsync("color", "rgb(200, 0, 0)");
        await Expect(root).ToHaveAttributeAsync("aria-label", "Layout switch");
        await Expect(root).ToHaveAttributeAsync("role", "group");
        await Expect(root).ToHaveAttributeAsync("dir", "rtl");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(root).ToHaveCSSAsync("direction", "rtl");
        await Expect(root).ToHaveAttributeAsync("data-e2e", "mdq-attr");
        await Expect(root).ToHaveAttributeAsync("title", "mdq title");

        await Expect(Page.Locator("#mdq-labelledby")).ToHaveAttributeAsync("role", "group");
        await Expect(Page.Locator("#mdq-own-role")).ToHaveAttributeAsync("role", "region");
        await Expect(Page.Locator("#mdq-nav")).Not.ToHaveAttributeAsync("role", new Regex(".*"));
        await Expect(Page.Locator("#mdq-span")).Not.ToHaveAttributeAsync("role", new Regex(".*"));
    }

    [TestMethod]
    public async Task Visibility_HiddenAndCollapsed()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#mdq-vis");
        await Expect(root).ToBeVisibleAsync();

        await Page.ClickAsync("#btn-vis-hidden");
        await Expect(root).ToHaveCSSAsync("visibility", "hidden");

        await Page.ClickAsync("#btn-vis-collapsed");
        await Expect(root).ToHaveCSSAsync("display", "none");

        await Page.ClickAsync("#btn-vis-visible");
        await Expect(root).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Unmount_ARemovedMediaQueryStopsListening_WithoutAnError()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#unmount-changes")).ToHaveTextAsync("1");

        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#mdq-unmount")).ToHaveCountAsync(0);

        await Page.SetViewportSizeAsync(700, 800);
        await Expect(Page.Locator("#custom-bound")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#unmount-changes")).ToHaveTextAsync("1");

        // Mounted again: a new first report.
        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#mdq-unmount")).ToHaveTextAsync("narrow");
        await Expect(Page.Locator("#unmount-changes")).ToHaveTextAsync("2");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    private Task<string> TagAsync(string selector) => Page.Locator(selector).EvaluateAsync<string>("el => el.tagName");
}
