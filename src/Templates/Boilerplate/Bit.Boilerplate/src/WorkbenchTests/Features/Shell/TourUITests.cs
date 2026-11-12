namespace Boilerplate.WorkbenchTests.Features.Shell;

/// <summary>The tour of what the home page promises, which shows each feature for real in the views and then puts them back.</summary>
[TestClass, TestCategory("UITest")]
public class TourUITests : WorkbenchPageTest
{
    private ILocator Next => Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourNext, Exact = true });

    private ILocator Popover => Page.Locator(".bit-bonboarding-popover");

    private ILocator Views => Page.Locator("iframe[data-pane-id]");

    private ILocator Mode(string name) => Page.Locator(".modes").GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private ILocator ArrowEditor => Page.GetByRole(AriaRole.Dialog, new() { Name = WorkbenchStrings.ArrowLabel });

    private static readonly Regex onTheSample = new($"/flows/{WorkbenchSampleFlows.BuildingsId}$");

    private static readonly Regex onTheSamplesArrow = new($"/flows/{WorkbenchSampleFlows.BuildingsId}\\?{WorkbenchUrls.ArrowQueryKey}={WorkbenchSampleFlows.BuildingsAddArrowId}$");

    /// <summary>The steps up to and including the Flows one, after the first.</summary>
    private static readonly string[] stepsToFlows = [WorkbenchStrings.HomeWhyHeadStartTitle, WorkbenchStrings.HomeWhyDevicesTitle, WorkbenchStrings.HomeWhyStatesTitle, WorkbenchStrings.HomeWhyStoresTitle, WorkbenchStrings.HomeWhyRealTitle, WorkbenchStrings.HomeWhyLiveTitle, WorkbenchStrings.HomeWhyProgressTitle, WorkbenchStrings.Flows];

    /// <summary>The steps on the building sample's board: the arrow's one only where an arrow can be changed.</summary>
    private string[] BoardSteps => HasAgent ? [WorkbenchStrings.Flows, WorkbenchStrings.TourArrowsTitle] : [WorkbenchStrings.Flows];

    /// <summary>The arrow step's editor, open on the sample's Add building arrow.</summary>
    private async Task ExpectTheSamplesArrowEditor()
    {
        await Expect(Page).ToHaveURLAsync(onTheSamplesArrow);
        await Expect(ArrowEditor).ToBeVisibleAsync();
        await Expect(ArrowEditor.GetByRole(AriaRole.Textbox, new() { Name = WorkbenchStrings.ArrowLabel })).ToHaveValueAsync(WorkbenchStrings.BuildingsAdd);
    }

    private async Task Advance(string nextTitle)
    {
        await Press(Next);
        await Expect(Popover.GetByText(nextTitle, new() { Exact = true })).ToBeVisibleAsync();
    }

    [TestMethod, OnDesktops]
    public async Task TheTour_Should_ShowEachFeatureAndPutTheViewsBack(WorkbenchSurface surface)
    {
        await Page.GotoAsync(Server.Address.ToString());
        await WaitUntilInteractive();
        await Press(Page.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.HomeStartHere, Exact = true }));

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
        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/shifts"));
        await Expect(View().Locator(".shifts")).ToBeVisibleAsync();

        await Advance(WorkbenchStrings.HomeWhyLiveTitle);
        await Expect(Page.Locator("#wb-tab-review")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/pos-dashboard"));

        await Advance(WorkbenchStrings.HomeWhyProgressTitle);
        await Expect(Page).ToHaveURLAsync(new Regex($"{WorkbenchUrls.Progress}$"));
        await Expect(Page.Locator(".task-list")).ToBeVisibleAsync();

        await Advance(WorkbenchStrings.Flows);
        await Expect(Page).ToHaveURLAsync(onTheSample);
        await Expect(Page.Locator(".screen-card")).ToHaveCountAsync(3);

        if (HasAgent)
        {
            await Advance(WorkbenchStrings.TourArrowsTitle);
            await ExpectTheSamplesArrowEditor();
        }

        await Advance(WorkbenchStrings.HomeWhyManyAppsTitle);
        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/landing-minimal"));
        await Expect(View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.NotchHeroTitle, Level = 1, Exact = true })).ToBeVisibleAsync();

        await Advance(WorkbenchStrings.HomeWhySafeTitle);
        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/pos-dashboard"));

        await Advance(WorkbenchStrings.HomeWhyCustomizeTitle);

        await Press(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourDone, Exact = true }));

        await Expect(Popover).ToHaveCountAsync(0);
        await Expect(Views).ToHaveCountAsync(1);
        await Expect(Page.Locator("#wb-tab-view")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Mode(WorkbenchStrings.ModeBrowse)).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod, OnDesktops]
    public async Task LeavingTheTourHalfway_Should_BringBackTheViewsItStartedFrom(WorkbenchSurface surface)
    {
        await OpenWorkbench("shifts", "?device=desktop-1536&theme=dark");

        await Press(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourStart, Exact = true }));
        await Advance(WorkbenchStrings.HomeWhyHeadStartTitle);
        await Advance(WorkbenchStrings.HomeWhyDevicesTitle);
        await Expect(Views).ToHaveCountAsync(2);

        await Press(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourSkip, Exact = true }));

        await Expect(Popover).ToHaveCountAsync(0);
        await Expect(Views).ToHaveCountAsync(1);
        await Expect(Page).ToHaveURLAsync(new Regex("theme=dark.*device=desktop-1536"));
    }

    [TestMethod, OnDesktops]
    public async Task LeavingTheTourOnTheShiftPlanner_Should_GoBackToThePageItStartedFrom(WorkbenchSurface surface)
    {
        await OpenWorkbench("pos-dashboard", "?device=desktop-1536");

        await Press(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourStart, Exact = true }));

        foreach (var title in new[] { WorkbenchStrings.HomeWhyHeadStartTitle, WorkbenchStrings.HomeWhyDevicesTitle, WorkbenchStrings.HomeWhyStatesTitle, WorkbenchStrings.HomeWhyStoresTitle, WorkbenchStrings.HomeWhyRealTitle })
        {
            await Advance(title);
        }

        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/shifts"));

        await Press(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourSkip, Exact = true }));

        await Expect(Popover).ToHaveCountAsync(0);
        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/pos-dashboard.*device=desktop-1536"));
        await Expect(Mode(WorkbenchStrings.ModeBrowse)).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod, OnDesktops]
    public async Task TheLandingPageStep_Should_GoBackAStepAndLeaveForThePageItStartedFrom(WorkbenchSurface surface)
    {
        await OpenWorkbench("pos-dashboard", "?device=desktop-1536");

        await Press(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourStart, Exact = true }));

        foreach (var title in (string[])[.. stepsToFlows[..^1], .. BoardSteps, WorkbenchStrings.HomeWhyManyAppsTitle])
        {
            await Advance(title);
        }

        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/landing-minimal"));
        await Expect(View().Locator(".lp-minimal")).ToBeVisibleAsync();

        await Press(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourBack, Exact = true }));

        await Expect(Popover.GetByText(BoardSteps[^1], new() { Exact = true })).ToBeVisibleAsync();

        if (HasAgent)
        {
            await ExpectTheSamplesArrowEditor();
        }
        else
        {
            await Expect(Page).ToHaveURLAsync(onTheSample);
        }

        await Advance(WorkbenchStrings.HomeWhyManyAppsTitle);
        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/landing-minimal"));

        await Press(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourSkip, Exact = true }));

        await Expect(Popover).ToHaveCountAsync(0);
        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/pos-dashboard.*device=desktop-1536"));
    }

    [TestMethod, OnDesktops]
    public async Task LeavingTheTourOnTheFlowsPage_Should_GoBackToTheViewsItStartedFrom(WorkbenchSurface surface)
    {
        await OpenWorkbench("shifts", "?device=desktop-1536&theme=dark");

        await Press(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourStart, Exact = true }));

        foreach (var title in new[] { WorkbenchStrings.HomeWhyHeadStartTitle, WorkbenchStrings.HomeWhyDevicesTitle, WorkbenchStrings.HomeWhyStatesTitle, WorkbenchStrings.HomeWhyStoresTitle, WorkbenchStrings.HomeWhyRealTitle, WorkbenchStrings.HomeWhyLiveTitle, WorkbenchStrings.HomeWhyProgressTitle, WorkbenchStrings.Flows })
        {
            await Advance(title);
        }

        await Expect(Page).ToHaveURLAsync(new Regex($"/flows/{WorkbenchSampleFlows.BuildingsId}$"));

        await Press(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourBack, Exact = true }));
        await Expect(Popover.GetByText(WorkbenchStrings.HomeWhyProgressTitle, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page).ToHaveURLAsync(new Regex($"{WorkbenchUrls.Progress}$"));

        await Press(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourBack, Exact = true }));
        await Expect(Popover.GetByText(WorkbenchStrings.HomeWhyLiveTitle, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.Locator("#wb-tab-review")).ToHaveAttributeAsync("aria-selected", "true");

        await Advance(WorkbenchStrings.HomeWhyProgressTitle);
        await Advance(WorkbenchStrings.Flows);
        await Press(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourSkip, Exact = true }));

        await Expect(Popover).ToHaveCountAsync(0);
        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/shifts.*theme=dark.*device=desktop-1536"));
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task TheArrowStep_Should_OpenTheSamplesArrowAndCloseItGoingBackWithoutACopy(WorkbenchSurface surface)
    {
        var flows = (await Server.Agent.GetFlows(CancellationToken.None)).Count;

        await OpenWorkbench("shifts", "?device=desktop-1536");

        await Press(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourStart, Exact = true }));

        foreach (var title in (string[])[.. stepsToFlows, WorkbenchStrings.TourArrowsTitle])
        {
            await Advance(title);
        }

        await ExpectTheSamplesArrowEditor();

        await Press(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourBack, Exact = true }));

        await Expect(Popover.GetByText(WorkbenchStrings.Flows, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page).ToHaveURLAsync(onTheSample);
        await Expect(ArrowEditor).ToHaveCountAsync(0);
        await Expect(Page.Locator(".screen-card")).ToHaveCountAsync(3);

        await Advance(WorkbenchStrings.TourArrowsTitle);
        await ExpectTheSamplesArrowEditor();

        await Press(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourSkip, Exact = true }));

        await Expect(Popover).ToHaveCountAsync(0);
        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/shifts.*device=desktop-1536"));

        Assert.HasCount(flows, await Server.Agent.GetFlows(CancellationToken.None), "The tour changes no flow.");
    }

    private async Task ExpectAboveTheCard(ILocator target)
    {
        await Expect(target).ToBeVisibleAsync();

        var box = await target.BoundingBoxAsync();
        var card = await Popover.BoundingBoxAsync();

        Assert.IsLessThanOrEqualTo(card!.Y, box!.Y + box.Height);
        Assert.IsGreaterThanOrEqualTo(0, box.X);
        Assert.IsLessThanOrEqualTo(Page.ViewportSize!.Width, box.X + box.Width);
    }

    [TestMethod, OnPhones]
    public async Task TheTourOnAPhone_Should_ShowEachFeatureAboveItsCardAndGiveThePhoneLayoutBack(WorkbenchSurface surface)
    {
        await Page.GotoAsync(Server.Address.ToString());
        await WaitUntilInteractive();
        await Press(Page.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.HomeStartHere, Exact = true }));

        await Expect(Popover.GetByText(WorkbenchStrings.HomeWhyAgentsTitle, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.Locator(".phone-layout")).ToHaveCountAsync(0);

        await Advance(WorkbenchStrings.HomeWhyHeadStartTitle);
        await ExpectAboveTheCard(Page.Locator("#wb-navigator"));

        await Advance(WorkbenchStrings.HomeWhyDevicesTitle);
        await Expect(Views).ToHaveCountAsync(2);
        await ExpectAboveTheCard(Views.First);
        await ExpectAboveTheCard(Views.Last);

        await Advance(WorkbenchStrings.HomeWhyStatesTitle);
        await Expect(Page).ToHaveURLAsync(new Regex("data=loading"));
        await ExpectAboveTheCard(Views.First);
        await ExpectAboveTheCard(Page.Locator("[data-bonboarding-anchor='data-states']"));
        await Press(Page.Locator("[data-bonboarding-anchor='data-states']").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.DataError }));
        await Expect(Page).ToHaveURLAsync(new Regex("data=error"));

        await Advance(WorkbenchStrings.HomeWhyStoresTitle);
        await Expect(Page).ToHaveURLAsync(new Regex("backdrop=ocean"));
        await ExpectAboveTheCard(Views.First);
        await ExpectAboveTheCard(Page.Locator("[data-bonboarding-anchor='screenshot']"));

        await Advance(WorkbenchStrings.HomeWhyRealTitle);
        await Expect(Mode(WorkbenchStrings.ModeInspect)).ToHaveAttributeAsync("aria-pressed", "true");
        await ExpectAboveTheCard(Page.Locator(".modes"));
        await ExpectAboveTheCard(Page.Locator("#wb-inspector"));
        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/shifts"));

        await Advance(WorkbenchStrings.HomeWhyLiveTitle);
        await ExpectAboveTheCard(Page.Locator("#wb-inspector"));
        await Expect(Page.Locator("#wb-tab-review")).ToHaveAttributeAsync("aria-selected", "true");

        await Advance(WorkbenchStrings.HomeWhyProgressTitle);
        await Expect(Page).ToHaveURLAsync(new Regex($"{WorkbenchUrls.Progress}$"));
        await ExpectAboveTheCard(Page.Locator(".list-title"));

        await Advance(WorkbenchStrings.Flows);
        await Expect(Page).ToHaveURLAsync(onTheSample);
        await Expect(Page.Locator(".screen-card")).ToHaveCountAsync(3);
        await ExpectAboveTheCard(Page.Locator(".flow-board"));

        if (HasAgent)
        {
            await Advance(WorkbenchStrings.TourArrowsTitle);
            await ExpectTheSamplesArrowEditor();
            await ExpectAboveTheCard(ArrowEditor);
        }

        await Advance(WorkbenchStrings.HomeWhyManyAppsTitle);
        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/landing-minimal"));
        await ExpectAboveTheCard(Views.First);

        await Advance(WorkbenchStrings.HomeWhySafeTitle);
        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/pos-dashboard"));

        await Advance(WorkbenchStrings.HomeWhyCustomizeTitle);

        await Press(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourDone, Exact = true }));

        await Expect(Popover).ToHaveCountAsync(0);
        await Expect(Page.Locator(".phone-view iframe")).ToBeVisibleAsync();
        await Expect(Page.Locator(".workbench")).ToHaveCountAsync(0);
    }

    [TestMethod, OnPhones]
    public async Task TheBackButtonOnAPhonesFlowsStep_Should_GoBackAStepAndKeepTheViewsDrawn(WorkbenchSurface surface)
    {
        await Page.GotoAsync(new Uri(Server.Address, "workbench/pos-dashboard?tour=true").ToString());
        await Expect(Popover.GetByText(WorkbenchStrings.HomeWhyAgentsTitle, new() { Exact = true })).ToBeVisibleAsync();

        foreach (var title in new[] { WorkbenchStrings.HomeWhyHeadStartTitle, WorkbenchStrings.HomeWhyDevicesTitle, WorkbenchStrings.HomeWhyStatesTitle, WorkbenchStrings.HomeWhyStoresTitle, WorkbenchStrings.HomeWhyRealTitle, WorkbenchStrings.HomeWhyLiveTitle, WorkbenchStrings.HomeWhyProgressTitle, WorkbenchStrings.Flows })
        {
            await Advance(title);
        }

        await Expect(Page).ToHaveURLAsync(new Regex($"/flows/{WorkbenchSampleFlows.BuildingsId}$"));

        await Page.GoBackAsync();

        await Expect(Popover.GetByText(WorkbenchStrings.HomeWhyProgressTitle, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page).ToHaveURLAsync(new Regex($"{WorkbenchUrls.Progress}$"));

        // The card's own Back reloads the view during the step that hides it: it has to keep a size there, or its page draws nothing.
        await Advance(WorkbenchStrings.Flows);
        await Press(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourBack, Exact = true }));
        await Expect(Popover.GetByText(WorkbenchStrings.HomeWhyProgressTitle, new() { Exact = true })).ToBeVisibleAsync();
        await Press(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourBack, Exact = true }));
        await Expect(Popover.GetByText(WorkbenchStrings.HomeWhyLiveTitle, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Views).ToHaveCountAsync(1);
        Assert.IsNotNull(await Views.First.BoundingBoxAsync());
    }

    [TestMethod, OnPhones]
    public async Task TheToolsPanelOnAPhone_Should_StartTheTourAndSkippingIt_Should_GiveThePhoneLayoutBack(WorkbenchSurface surface)
    {
        await Page.GotoAsync(new Uri(Server.Address, "workbench/shifts").ToString());

        await Press(Page.Locator(".tools-button"));
        await Press(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourStart, Exact = true }));

        await Expect(Popover.GetByText(WorkbenchStrings.HomeWhyAgentsTitle, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.Locator(".tools-panel")).ToHaveCountAsync(0);

        await Advance(WorkbenchStrings.HomeWhyHeadStartTitle);
        await Press(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourSkip, Exact = true }));

        await Expect(Popover).ToHaveCountAsync(0);
        await Expect(Page.Locator(".phone-view iframe")).ToBeVisibleAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/shifts"));
    }

    [TestMethod, OnPhones]
    public async Task TheTourOnASidewaysPhone_Should_ShowTheCardBesideTheViews(WorkbenchSurface surface)
    {
        await Page.SetViewportSizeAsync(844, 390);
        await Page.GotoAsync(new Uri(Server.Address, "workbench/pos-dashboard?tour=true").ToString());
        await Expect(Popover.GetByText(WorkbenchStrings.HomeWhyAgentsTitle, new() { Exact = true })).ToBeVisibleAsync();
        await Advance(WorkbenchStrings.HomeWhyHeadStartTitle);
        await Advance(WorkbenchStrings.HomeWhyDevicesTitle);
        await Expect(Views).ToHaveCountAsync(2);

        var view = await Views.Last.BoundingBoxAsync();
        var card = await Popover.BoundingBoxAsync();

        Assert.IsLessThanOrEqualTo(card!.X, view!.X + view.Width);
    }

    [TestMethod, EverySurface]
    public async Task TheGuides_Should_FollowTheAppsButtonsAndColors(WorkbenchSurface surface)
    {
        await OpenWorkbench("shifts");

        await OpenTools();
        await Press(Button(WorkbenchStrings.TourStart));
        await Expect(Next).ToHaveClassAsync(new Regex("bit-btn"));

        await Page.EvaluateAsync("document.documentElement.style.setProperty('--bit-clr-pri', 'rgb(200, 0, 120)')");

        await Expect(Next).ToHaveCSSAsync("background-color", "rgb(200, 0, 120)");
    }
}
