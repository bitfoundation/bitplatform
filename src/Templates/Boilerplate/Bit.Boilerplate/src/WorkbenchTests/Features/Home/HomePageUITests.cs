namespace Boilerplate.WorkbenchTests.Features.Home;

/// <summary>The home page: what it promises, the way into the pages and its own language and theme.</summary>
[TestClass, TestCategory("UITest")]
public class HomePageUITests : WorkbenchPageTest
{
    private async Task OpenHome()
    {
        await Page.GotoAsync(Server.Address.ToString());
        await WaitUntilInteractive();
    }

    private ILocator Header => Page.Locator("header.shell-header");

    private ILocator Headline => Page.GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.HomeHeadline, Level = 1 });

    [TestMethod, EverySurface]
    public async Task TheHomePage_Should_SayWhatTheWorkbenchIs(WorkbenchSurface surface)
    {
        await OpenHome();

        await Expect(Page).ToHaveTitleAsync(WorkbenchStrings.HomePageTitle);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.HomeHeadline, Level = 1 })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.HomeWhyTitle, Level = 2 })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.HomeHowTitle, Level = 2 })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.HomeWhyCustomizeTitle, Level = 3 })).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheHomePage_Should_ListEveryPage(WorkbenchSurface surface)
    {
        await OpenHome();

        var cards = Page.Locator("a.page-card");

        await Expect(cards).ToHaveCountAsync(WorkbenchPageCatalog.Pages.Count);

        await Press(cards.Filter(new() { HasText = WorkbenchStrings.BuildingsTitle }));

        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/buildings"));
        await WaitForViews();
        await Expect(View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.BuildingsHeading })).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task StartHere_Should_OpenThePageWithTheTour(WorkbenchSurface surface)
    {
        await OpenHome();

        await Press(Page.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.HomeStartHere, Exact = true }));

        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/pos-dashboard"));
        await Expect(Page.Locator(".bit-bonboarding-popover").GetByText(WorkbenchStrings.HomeWhyAgentsTitle, new() { Exact = true })).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheCallToAction_Should_OpenTheFirstPage(WorkbenchSurface surface)
    {
        await OpenHome();

        await Press(Page.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.HomeCtaOpen, Exact = true }));

        await Expect(Page).ToHaveURLAsync(new Regex($"/workbench/{WorkbenchPageCatalog.Pages[0].Key}"));
        await WaitForViews();
    }

    [TestMethod, OnDesktops]
    public async Task TheHeadersLinks_Should_LeadToTheSectionsAndTheFlows(WorkbenchSurface surface)
    {
        await OpenHome();

        await Press(Header.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.Pages, Exact = true }));

        await Expect(Page).ToHaveURLAsync(new Regex("#pages$"));
        await Expect(Page.Locator("section#pages .section-head")).ToBeInViewportAsync();

        await Press(Header.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.HomeWhyButton, Exact = true }));

        await Expect(Page).ToHaveURLAsync(new Regex("#why$"));
        await Expect(Page.Locator("section#why .section-head")).ToBeInViewportAsync();

        await Press(Header.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.Flows, Exact = true }));

        await Expect(Page).ToHaveURLAsync(new Regex($"{WorkbenchUrls.Flows}$"));
    }

    [TestMethod, OnPhones]
    public async Task APhonesHeader_Should_FitTheScreenWithItsControlsInSight(WorkbenchSurface surface)
    {
        await OpenHome();

        var width = await Page.EvaluateAsync<int>("() => document.documentElement.scrollWidth");

        Assert.IsLessThanOrEqualTo(Page.ViewportSize!.Width, width);

        await Expect(Header.GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.Language })).ToBeInViewportAsync();
        await Expect(Header.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ToggleWorkbenchTheme, Exact = true })).ToBeInViewportAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheThemeSwitch_Should_ChangeThePageAndStayAfterAReload(WorkbenchSurface surface)
    {
        await OpenHome();

        var root = Page.Locator("html");

        await Press(Header.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ToggleWorkbenchTheme, Exact = true }));

        await Expect(root).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
        await Expect(Page.Locator("body")).ToHaveCSSAsync("background-color", new Regex(@"rgb\((\d|[1-5]\d), "));

        await OpenHome();

        await Expect(root).ToHaveAttributeAsync("bit-theme", new Regex("dark"));

        await Press(Header.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ToggleWorkbenchTheme, Exact = true }));

        await Expect(root).Not.ToHaveAttributeAsync("bit-theme", new Regex("dark"));
    }

    [TestMethod, EverySurface]
    [TestCategory("Localization")]
    public async Task ALanguage_Should_TranslateThePageAndStayAfterAReload(WorkbenchSurface surface)
    {
        await OpenHome();

        await Press(Header.GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.Language }));
        await Press(Page.GetByRole(AriaRole.Option, new() { Name = "فارسی", Exact = true }));

        var headline = InCulture("fa-IR", nameof(WorkbenchStrings.HomeHeadline));

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = headline, Level = 1 })).ToBeVisibleAsync();

        await OpenHome();

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = headline, Level = 1 })).ToBeVisibleAsync();

        // The shell speaks that language too, and the view stays in the one its scenario names.
        await OpenWorkbench("theme");

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(View().Locator("html")).Not.ToHaveAttributeAsync("dir", "rtl");
    }

    [TestMethod, EverySurface]
    public async Task TheThumbnails_Should_LoadInBothThemes(WorkbenchSurface surface)
    {
        await OpenHome();

        var thumbnail = Page.Locator("a.page-card").Filter(new() { HasText = WorkbenchStrings.BuildingsTitle }).Locator("img.light-only");

        await thumbnail.ScrollIntoViewIfNeededAsync();

        await WorkbenchTestReviewer.WaitFor(() => thumbnail.EvaluateAsync<int>("image => image.naturalWidth"), width => width > 0, TimeSpan.FromSeconds(90));
    }

    [TestMethod, EverySurface]
    public async Task TheFlowsSection_Should_LeadToTheSamples(WorkbenchSurface surface)
    {
        await OpenHome();

        var sample = Page.Locator("section#flows").GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.SampleFlowBuildings });

        await Press(sample);

        await Expect(Page).ToHaveURLAsync(new Regex($"/flows/{WorkbenchSampleFlows.BuildingsId}$"));
        await Expect(Page.Locator(".screen-card")).ToHaveCountAsync(3);
    }

    [TestMethod, EverySurface]
    public async Task TheFooter_Should_ListThePagesAndWhereToLearnMore(WorkbenchSurface surface)
    {
        await OpenHome();

        var footer = Page.Locator("footer.home-footer");

        await Expect(footer.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.ShiftsTitle, Exact = true })).ToHaveAttributeAsync("href", WorkbenchUrls.Workbench("shifts"));
        await Expect(footer.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.HomeFooterDocs })).ToHaveAttributeAsync("target", "_blank");
        await Expect(footer.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.HomeFooterSource })).ToHaveAttributeAsync("href", new Regex("github.com"));
    }

    [TestMethod, EverySurface]
    public async Task TheFootersLogo_Should_LeadToTheTopOfTheHomePage(WorkbenchSurface surface)
    {
        await OpenHome();

        var footer = Page.Locator("footer.home-footer");

        await footer.ScrollIntoViewIfNeededAsync();
        await Expect(Headline).Not.ToBeInViewportAsync();

        await Press(footer.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.WorkbenchTitle, Exact = true }));

        await Expect(Headline).ToBeInViewportAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheHeadersLogo_Should_LeadToTheTopOfTheHomePage(WorkbenchSurface surface)
    {
        await OpenHome();

        await Page.Locator("footer.home-footer").ScrollIntoViewIfNeededAsync();
        await Expect(Headline).Not.ToBeInViewportAsync();

        await Press(Header.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.WorkbenchTitle, Exact = true }));

        await Expect(Headline).ToBeInViewportAsync();
    }

    [TestMethod, EverySurface]
    public async Task ThePromptsCopyButton_Should_PutThePromptOnTheClipboard(WorkbenchSurface surface)
    {
        await Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);

        await OpenHome();

        await Press(Button(WorkbenchStrings.HomeCtaCopy));

        await Expect(Button(WorkbenchStrings.HomeCtaCopied)).ToBeVisibleAsync();

        await WorkbenchTestReviewer.WaitFor(() => Page.EvaluateAsync<string>("() => navigator.clipboard.readText()"), text => text == WorkbenchStrings.HomeCtaPrompt);

        await Expect(Button(WorkbenchStrings.HomeCtaCopy)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task ThePagesCard_Should_ShowItsReviewOnlyOnceItHasOne(WorkbenchSurface surface)
    {
        await OpenHome();

        var card = Page.Locator("a.page-card").Filter(new() { HasText = WorkbenchStrings.BuildingsTitle });

        await Expect(card.Locator(".page-title")).ToBeVisibleAsync();

        if (HasAgent is false)
        {
            await Expect(card.Locator(".page-review")).ToHaveCountAsync(0);
            return;
        }

        await using var reviewer = await WorkbenchTestReviewer.Open(Server, name: "Home reviewer");

        await reviewer.Comment(Server, "pos-dashboard", $"A comment for the home page {Guid.NewGuid():N}");

        await Expect(Page.Locator("a.page-card").Filter(new() { HasText = WorkbenchStrings.PosDashboardTitle }).Locator(".review-count")).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheSoundSwitch_Should_ShowOnlyWithAnAgent(WorkbenchSurface surface)
    {
        await OpenHome();

        var sound = Header.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.MuteReplySound, Exact = true });

        if (HasAgent is false)
        {
            await Expect(sound).ToHaveCountAsync(0);
            return;
        }

        await Expect(sound).ToHaveAttributeAsync("aria-pressed", "false");

        await Press(sound);

        await Expect(sound).ToHaveAttributeAsync("aria-pressed", "true");
        await WaitUntilSaved("IsReplySoundMuted");

        await OpenHome();

        await Expect(sound).ToHaveAttributeAsync("aria-pressed", "true");
    }
}
