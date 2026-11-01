namespace Boilerplate.WorkbenchTests.Features.Shell;

/// <summary>The tour of what the home page promises, which shows each feature for real in the views and then puts them back.</summary>
[TestClass, TestCategory("UITest")]
public class TourUITests : WorkbenchPageTest
{
    private ILocator Next => Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourNext, Exact = true });

    private ILocator Popover => Page.Locator(".bit-bonboarding-popover");

    private ILocator Views => Page.Locator("iframe[data-pane-id]");

    private ILocator Mode(string name) => Page.Locator(".modes").GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private async Task Advance(string nextTitle)
    {
        await Next.ClickAsync();
        await Expect(Popover.GetByText(nextTitle, new() { Exact = true })).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task TheTour_Should_ShowEachFeatureAndPutTheViewsBack()
    {
        await Page.GotoAsync(Server.Address.ToString());
        await Page.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.HomeStartHere, Exact = true }).ClickAsync();

        await Expect(Popover.GetByText(WorkbenchStrings.HomeWhyAgentsTitle, new() { Exact = true })).ToBeVisibleAsync();

        await Advance(WorkbenchStrings.HomeWhyHeadStartTitle);

        await Advance(WorkbenchStrings.HomeWhyDevicesTitle);
        await Expect(Views).ToHaveCountAsync(2);
        await Expect(Page.Locator(".canvas").GetByText("Galaxy Tab S9").First).ToBeVisibleAsync();
        await Expect(Page.Locator(".canvas").GetByText("1024×640", new() { Exact = false })).ToBeVisibleAsync();

        await Advance(WorkbenchStrings.HomeWhyStatesTitle);
        await Expect(Views).ToHaveCountAsync(1);
        await Expect(Page).ToHaveURLAsync(new Regex("data=loading"));
        await Expect(Page.Locator("[data-bonboarding-anchor='data-states']")).ToBeInViewportAsync();

        await Advance(WorkbenchStrings.HomeWhyStoresTitle);
        await Expect(Page).ToHaveURLAsync(new Regex("backdrop=ocean"));

        await Advance(WorkbenchStrings.HomeWhyRealTitle);
        await Expect(Mode(WorkbenchStrings.ModeInspect)).ToHaveAttributeAsync("aria-pressed", "true");

        await Advance(WorkbenchStrings.HomeWhyLiveTitle);
        await Expect(Page.Locator("#wb-tab-review")).ToHaveAttributeAsync("aria-selected", "true");

        await Advance(WorkbenchStrings.Flows);
        await Expect(Page).ToHaveURLAsync(new Regex($"/flows/{WorkbenchSampleFlows.BuildingsId}$"));
        await Expect(Page.Locator(".screen-card")).ToHaveCountAsync(3);

        await Advance(WorkbenchStrings.HomeWhySafeTitle);
        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/pos-dashboard"));

        await Advance(WorkbenchStrings.HomeWhyCustomizeTitle);

        await Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourDone, Exact = true }).ClickAsync();

        await Expect(Popover).ToHaveCountAsync(0);
        await Expect(Views).ToHaveCountAsync(1);
        await Expect(Page.Locator("#wb-tab-view")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Mode(WorkbenchStrings.ModeBrowse)).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod]
    public async Task LeavingTheTourHalfway_Should_BringBackTheViewsItStartedFrom()
    {
        await OpenWorkbench("shifts", "?device=desktop-1536&theme=dark");

        await Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourStart, Exact = true }).ClickAsync();
        await Advance(WorkbenchStrings.HomeWhyHeadStartTitle);
        await Advance(WorkbenchStrings.HomeWhyDevicesTitle);
        await Expect(Views).ToHaveCountAsync(2);

        await Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourSkip, Exact = true }).ClickAsync();

        await Expect(Popover).ToHaveCountAsync(0);
        await Expect(Views).ToHaveCountAsync(1);
        await Expect(Page).ToHaveURLAsync(new Regex("theme=dark.*device=desktop-1536"));
    }

    [TestMethod]
    public async Task LeavingTheTourOnTheFlowsPage_Should_GoBackToTheViewsItStartedFrom()
    {
        await OpenWorkbench("shifts", "?device=desktop-1536&theme=dark");

        await Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourStart, Exact = true }).ClickAsync();

        foreach (var title in new[] { WorkbenchStrings.HomeWhyHeadStartTitle, WorkbenchStrings.HomeWhyDevicesTitle, WorkbenchStrings.HomeWhyStatesTitle, WorkbenchStrings.HomeWhyStoresTitle, WorkbenchStrings.HomeWhyRealTitle, WorkbenchStrings.HomeWhyLiveTitle, WorkbenchStrings.Flows })
        {
            await Advance(title);
        }

        await Expect(Page).ToHaveURLAsync(new Regex($"/flows/{WorkbenchSampleFlows.BuildingsId}$"));

        await Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourBack, Exact = true }).ClickAsync();
        await Expect(Popover.GetByText(WorkbenchStrings.HomeWhyLiveTitle, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.Locator("#wb-tab-review")).ToHaveAttributeAsync("aria-selected", "true");

        await Advance(WorkbenchStrings.Flows);
        await Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourSkip, Exact = true }).ClickAsync();

        await Expect(Popover).ToHaveCountAsync(0);
        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/shifts.*theme=dark.*device=desktop-1536"));
    }

    [TestMethod]
    public async Task StartHereOnAPhone_Should_OpenThePageWithoutTheTour()
    {
        await Page.SetViewportSizeAsync(390, 800);
        await Page.GotoAsync(Server.Address.ToString());

        await Page.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.HomeStartHere, Exact = true }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/pos-dashboard"));
        await Expect(Page.Locator(".inline-surface")).ToBeVisibleAsync();
        await Expect(Popover).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task TheGuides_Should_FollowTheAppsButtonsAndColors()
    {
        await OpenWorkbench("shifts");

        await Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourStart, Exact = true }).ClickAsync();
        await Expect(Next).ToHaveClassAsync(new Regex("bit-btn"));

        await Page.EvaluateAsync("document.documentElement.style.setProperty('--bit-clr-pri', 'rgb(200, 0, 120)')");

        await Expect(Next).ToHaveCSSAsync("background-color", "rgb(200, 0, 120)");
    }
}
