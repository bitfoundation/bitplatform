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
        await Expect(Headline).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.HomeWhyTitle, Level = 2 })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.HomeHowTitle, Level = 2 })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.HomeWhyCustomizeTitle, Level = 3 })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.HomeWhyManyAppsTitle, Level = 3 })).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheHomePage_Should_ShowItsContentBeforeItBecomesInteractive(WorkbenchSurface surface)
    {
        await Context.RouteAsync(url => url.Contains("blazor.web", StringComparison.Ordinal), route => route.AbortAsync());

        await Page.GotoAsync(Server.Address.ToString());

        await Page.WaitForFunctionAsync("() => window.Blazor === undefined && document.readyState === 'complete'");
        await Expect(Headline).ToHaveCSSAsync("opacity", "1");
        await Expect(Page.Locator(".hero-actions")).ToHaveCSSAsync("opacity", "1");
        await Expect(Page.Locator("article.reason").First).ToHaveCSSAsync("opacity", "1");

        if (IsPhone is false)
        {
            await Expect(Page.Locator(".laptop-holder")).ToHaveCSSAsync("opacity", "1");
        }
    }

    [TestMethod, EverySurface]
    public async Task TheThumbnails_Should_LeaveThePageAndTheBrowsersConnectionsAlone(WorkbenchSurface surface)
    {
        var inFlight = 0;
        var mostInFlight = 0;
        var isNegotiating = false;
        var askedBeforeInteractive = false;
        var askedForTheHiddenTheme = false;

        static bool IsThumbnail(IRequest request) => request.Url.Contains("/api/thumbnails/", StringComparison.Ordinal);

        Page.Request += (_, request) =>
        {
            isNegotiating |= request.Url.Contains("/_blazor/negotiate", StringComparison.Ordinal);

            if (IsThumbnail(request))
            {
                askedBeforeInteractive |= isNegotiating is false;
                askedForTheHiddenTheme |= request.Url.Contains("theme=Dark", StringComparison.Ordinal);
                mostInFlight = Math.Max(mostInFlight, Interlocked.Increment(ref inFlight));
            }
        };
        Page.RequestFinished += (_, request) =>
        {
            if (IsThumbnail(request)) Interlocked.Decrement(ref inFlight);
        };
        Page.RequestFailed += (_, request) =>
        {
            if (IsThumbnail(request)) Interlocked.Decrement(ref inFlight);
        };

        await Context.RouteAsync(url => url.Contains("/api/thumbnails/", StringComparison.Ordinal), async route =>
        {
            await Task.Delay(300);
            await route.ContinueAsync();
        });

        await OpenHome();

        var thumbnail = Page.Locator("a.page-card img.light-only").Last;

        await thumbnail.ScrollIntoViewIfNeededAsync();
        await WorkbenchTestReviewer.WaitFor(() => thumbnail.EvaluateAsync<int>("image => image.naturalWidth"), width => width > 0, TimeSpan.FromSeconds(120));

        Assert.IsFalse(askedBeforeInteractive, "A thumbnail was asked for before the page started to become interactive.");
        Assert.IsFalse(askedForTheHiddenTheme, "A thumbnail of the theme that isn't showing was asked for.");
        Assert.IsLessThanOrEqualTo(2, mostInFlight);
    }

    [TestMethod, OnDesktops]
    public async Task ACardInSightWhenThePageBecomesInteractive_Should_NotBlink(WorkbenchSurface surface)
    {
        await Page.SetViewportSizeAsync(1600, 2400);
        await Page.AddInitScriptAsync("""
            window.__lowestOpacity = 1;
            window.__wasShown = false;
            const watch = () => {
                const card = document.querySelector('article.reason');
                if (card) {
                    const opacity = Number(getComputedStyle(card).opacity);
                    window.__wasShown ||= opacity > 0.99;
                    if (window.__wasShown) window.__lowestOpacity = Math.min(window.__lowestOpacity, opacity);
                }
                requestAnimationFrame(watch);
            };
            requestAnimationFrame(watch);
            """);

        await OpenHome();
        await Expect(Page.Locator(".home.reveals-live")).ToBeAttachedAsync();
        await Page.WaitForTimeoutAsync(500);

        Assert.IsGreaterThanOrEqualTo(0.99, await Page.EvaluateAsync<double>("() => window.__lowestOpacity"));
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
    public async Task TheFootersPageLinks_Should_OpenThatPage(WorkbenchSurface surface)
    {
        await OpenHome();

        await Press(Page.Locator("footer.home-footer").GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.ShiftsTitle, Exact = true }));

        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/shifts$"));
        await WaitForViews();
        await Expect(Page).ToHaveTitleAsync($"{WorkbenchStrings.ShiftsTitle} · {WorkbenchStrings.WorkbenchTitle}");
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
    public async Task TheAllFlowsCard_Should_LeadToTheFlows(WorkbenchSurface surface)
    {
        await OpenHome();

        await Press(Page.Locator("a.all-flows"));

        await Expect(Page).ToHaveURLAsync(new Regex($"{WorkbenchUrls.Flows}$"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.Flows, Level = 1 })).ToBeVisibleAsync();
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
