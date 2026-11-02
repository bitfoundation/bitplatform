namespace Boilerplate.WorkbenchTests.Features.Flows;

/// <summary>What anyone can do with flows: the list at /flows and in the tools, the samples, a board with its live screens and labelled arrows, and presenting it.</summary>
[TestClass, TestCategory("UITest")]
public class FlowsUITests : FlowPageTest
{
    [TestMethod, EverySurface]
    public async Task TheFlowsPage_Should_ListTheFlowsAndOpenABoard(WorkbenchSurface surface)
    {
        var flow = await DrawThemeFlow();

        await OpenFlowsPage();

        await Expect(Page.GetByText(WorkbenchStrings.FlowsHint)).ToBeVisibleAsync();

        var card = Page.GetByRole(AriaRole.Link, new() { Name = flow.Name });

        await Expect(card).ToContainTextAsync(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ScreenCount, 2));
        await Expect(card).ToContainTextAsync(WorkbenchStrings.ThemePageTitle);
        await Expect(card).Not.ToContainTextAsync(WorkbenchStrings.FlowSample);

        await Press(card);

        await Expect(Page.Locator("main.board")).ToBeVisibleAsync();
        await Expect(Cards).ToHaveCountAsync(2);
        await Expect(Page.Locator(".topbar")).ToContainTextAsync(flow.Name);
        await Expect(Page).ToHaveTitleAsync(new Regex(Regex.Escape(flow.Name)));
    }

    [TestMethod, EverySurface]
    public async Task TheFlowsPage_Should_ListTheSamples(WorkbenchSurface surface)
    {
        await OpenFlowsPage();

        var buildings = Page.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.SampleFlowBuildings });
        var pos = Page.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.SampleFlowPos });

        await Expect(buildings).ToContainTextAsync(WorkbenchStrings.FlowSample);
        await Expect(pos).ToContainTextAsync(WorkbenchStrings.FlowSample);
        await Expect(pos).ToContainTextAsync(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ScreenCount, 4));

        await Press(pos);

        await Expect(Cards).ToHaveCountAsync(4);
        await Expect(Page.Locator("main.board")).ToHaveClassAsync(new Regex("read-only"));
    }

    [TestMethod, EverySurface]
    public async Task ANewFlow_Should_ReachAnOpenListLive(WorkbenchSurface surface)
    {
        await OpenFlowsPage();
        await MarkThePage();

        var flow = await DrawThemeFlow();

        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = flow.Name })).ToBeVisibleAsync();
        await ExpectNoReload();
    }

    [TestMethod, EverySurface]
    public async Task ABoard_Should_ShowItsScreensLiveInDeviceFrames(WorkbenchSurface surface)
    {
        var flow = await DrawThemeFlow();

        await OpenBoard(flow.Id, 2);

        await Expect(Page.Locator(".screen-note")).ToHaveTextAsync(["Light", "Dark"]);
        await Expect(Page.Locator(".screen-number")).ToHaveTextAsync(["1", "2"]);
        await Expect(Page.Locator(".screen-title")).ToHaveTextAsync([WorkbenchStrings.ThemePageTitle, WorkbenchStrings.ThemePageTitle]);
        await Expect(Page.Locator(".screen-card .device")).ToHaveCountAsync(2);

        var light = Screen(flow.Screens[0].Id);
        var dark = Screen(flow.Screens[1].Id);

        await Expect(light.GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ThemeHeading }).First).ToBeAttachedAsync();
        await Expect(light.Locator("html")).Not.ToHaveAttributeAsync("bit-theme", new Regex("dark"));
        await Expect(dark.Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));

        var width = await light.Locator("html").EvaluateAsync<int>("e => e.ownerDocument.defaultView.innerWidth");

        Assert.AreEqual(DevicePresets.Find("iphone-se").Width, width, "The screen shows the page at its device's real size.");
    }

    [TestMethod, EverySurface]
    public async Task ABoard_Should_DrawItsArrowsWithTheirLabels(WorkbenchSurface surface)
    {
        var flow = await DrawThemeFlow();

        await OpenBoard(flow.Id, 2);

        await Expect(Arrows).ToHaveCountAsync(1);
        await Expect(ArrowLabels).ToHaveTextAsync([TheArrow]);
        await Expect(ArrowLabels).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task ABoard_Should_FollowTheFlowLive(WorkbenchSurface surface)
    {
        var flow = await DrawThemeFlow();

        await OpenBoard(flow.Id, 2);
        await MarkThePage();

        const string newArrow = "On to the settings";

        await DrawFlow(flow.Name, [new("light", "theme", "?device=iphone-se", "Light", null), new("settings", "settings", "?device=iphone-se", "The settings", null), new("dark", "theme", "?device=iphone-se&theme=dark", "Dark", null)], [new("light", "settings", newArrow, null)], flow.Id);

        await Expect(Cards).ToHaveCountAsync(3);
        await Expect(ArrowLabels).ToHaveTextAsync([newArrow]);
        await ExpectNoReload();
    }

    [TestMethod, EverySurface]
    public async Task ABoardWhoseFlowIsDeleted_Should_SaySoLive(WorkbenchSurface surface)
    {
        var flow = await DrawThemeFlow();

        await OpenBoard(flow.Id, 2);

        await Server.ReviewHost.EditAsync(WorkbenchReviewDocument.Id, document => document.GetMap("flows").Delete(flow.Id));

        await Expect(Page.GetByText(WorkbenchStrings.FlowNotFound)).ToBeVisibleAsync();
        await Expect(Page.Locator("main.board")).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task AFlowThatDoesNotExist_Should_SaySoAndListTheFlows(WorkbenchSurface surface)
    {
        await Page.GotoAsync(new Uri(Server.Address, WorkbenchUrls.Flow("no-such-flow")).ToString());

        await Expect(Page.GetByText(WorkbenchStrings.FlowNotFound)).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.SampleFlowPos })).ToBeVisibleAsync();
        await Expect(Page.Locator("main.board")).ToHaveCountAsync(0);
        await Expect(PresentButton).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task AFlowWithNoScreens_Should_SaySo(WorkbenchSurface surface)
    {
        var id = Guid.NewGuid().ToString("N")[..16];
        var name = Unique("Nothing here yet");

        await Server.ReviewHost.EditAsync(WorkbenchReviewDocument.Id, document => WorkbenchReviewDocument.SaveFlow(document.GetMap("flows"), id, name, [], []));

        await OpenBoard(id, 0);

        await Expect(Page.GetByText(WorkbenchStrings.FlowHasNoScreens)).ToBeVisibleAsync();
        await Expect(PresentButton).ToHaveCountAsync(0);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ShowAllScreens })).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task AFlowsScreensDrawnWithTaps_Should_OpenInTheirStateWithTheirButtonMarked(WorkbenchSurface surface)
    {
        var flow = await DrawFlow(Unique("Adding a place"),
                                  [new("list", "buildings", "?device=iphone-se", "The list", null), new("form", "buildings", "?device=iphone-se", "The form", [WorkbenchStrings.BuildingsAdd])],
                                  [new("list", "form", "Tap Add", WorkbenchStrings.BuildingsAdd)]);

        Assert.IsTrue(flow.Screens[1].HasState, "The form's state, reached by its tap, was saved.");
        Assert.IsNotNull(flow.Links.Single().Spot, "The button the arrow starts from was found.");

        await OpenBoard(flow.Id, 2);

        await Expect(Screen(flow.Screens[1].Id).GetByText(WorkbenchStrings.BuildingsAddTitle)).ToBeVisibleAsync();
        await Expect(Screen(flow.Screens[0].Id).GetByText(WorkbenchStrings.BuildingsAddTitle)).ToHaveCountAsync(0);
        await Expect(CardOf(flow.Screens[0].Id).Locator(".spot")).ToHaveCountAsync(1);
        await Expect(CardOf(flow.Screens[1].Id).Locator(".spot")).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task TheBuildingSample_Should_ShowItsScreensInTheirStates(WorkbenchSurface surface)
    {
        await OpenBoard(WorkbenchSampleFlows.BuildingsId, 3);

        await Expect(Screen("form").GetByText(WorkbenchStrings.BuildingsAddTitle)).ToBeVisibleAsync();
        await Expect(Screen("map").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsMapView, Exact = true })).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod, EverySurface]
    public async Task ASample_Should_BeReadOnlyEvenInDevMode(WorkbenchSurface surface)
    {
        await OpenBoard(WorkbenchSampleFlows.BuildingsId, 3);

        await Expect(Page.Locator("main.board")).ToHaveClassAsync(new Regex("read-only"));
        await Expect(Page.Locator(".screen-remove")).ToHaveCountAsync(0);
        await Expect(DeleteFlowButton).ToHaveCountAsync(0);
        await Expect(Page.GetByText(WorkbenchStrings.BoardHelp)).ToHaveCountAsync(0);
        await Expect(Page.Locator(".bit-blow-handle").First).ToBeHiddenAsync();
        await Expect(PresentButton).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task Present_Should_StepThroughTheScreensAndStop(WorkbenchSurface surface)
    {
        var flow = await DrawFlow(Unique("Three steps"),
                                  [new("one", "theme", "?device=iphone-se", "The first", null), new("two", "theme", "?device=iphone-se&theme=dark", null, null), new("three", "settings", "?device=iphone-se", "The last", null)],
                                  [new("one", "two", null, null), new("two", "three", null, null)]);

        await OpenBoard(flow.Id, 3);

        await Expect(PresentButton).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Presenter).ToHaveCountAsync(0);

        await Press(PresentButton);

        var previous = Presenter.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PreviousScreen });
        var next = Presenter.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.NextScreen });

        await Expect(PresentButton).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Presenter).ToContainTextAsync(ScreenOf(1, 3));
        await Expect(Presenter).ToContainTextAsync("The first");
        await Expect(previous).ToBeDisabledAsync();
        await Expect(CardOf(flow.Screens[0].Id)).ToHaveClassAsync(new Regex(@"\bcurrent\b"));
        await Expect(CardOf(flow.Screens[0].Id)).ToBeInViewportAsync(new() { Ratio = 0.8f });

        await Press(next);

        // A screen with no note goes by its page's title.
        await Expect(Presenter).ToContainTextAsync(ScreenOf(2, 3));
        await Expect(Presenter).ToContainTextAsync(WorkbenchStrings.ThemePageTitle);
        await Expect(previous).ToBeEnabledAsync();
        await Expect(CardOf(flow.Screens[1].Id)).ToHaveClassAsync(new Regex(@"\bcurrent\b"));
        await Expect(CardOf(flow.Screens[0].Id)).Not.ToHaveClassAsync(new Regex(@"\bcurrent\b"));
        await Expect(CardOf(flow.Screens[1].Id)).ToBeInViewportAsync(new() { Ratio = 0.8f });

        await Press(next);

        await Expect(Presenter).ToContainTextAsync(ScreenOf(3, 3));
        await Expect(Presenter).ToContainTextAsync("The last");
        await Expect(next).ToBeDisabledAsync();

        await Press(previous);

        await Expect(Presenter).ToContainTextAsync(ScreenOf(2, 3));

        await Press(Presenter.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.StopPresenting }));

        await Expect(Presenter).ToHaveCountAsync(0);
        await Expect(PresentButton).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Page.Locator(".screen-card.current")).ToHaveCountAsync(0);

        // The Present button itself also starts and stops it.
        await Press(PresentButton);
        await Expect(Presenter).ToContainTextAsync(ScreenOf(1, 3));

        await Press(PresentButton);
        await Expect(Presenter).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task ThePresentedScreen_Should_BeUsableAndTheOthersNot(WorkbenchSurface surface)
    {
        var flow = await DrawFlow(Unique("Try it"), [new("list", "buildings", "?device=iphone-se", null, null), new("other", "theme", "?device=iphone-se", null, null)]);

        await OpenBoard(flow.Id, 2);

        var frame = Page.Locator($"iframe[data-screen-id='{flow.Screens[0].Id}']");

        await Expect(frame).ToHaveCSSAsync("pointer-events", "none");

        await Press(PresentButton);

        await Expect(frame).ToHaveCSSAsync("pointer-events", "auto");
        await Expect(Page.Locator($"iframe[data-screen-id='{flow.Screens[1].Id}']")).ToHaveCSSAsync("pointer-events", "none");

        await Press(Screen(flow.Screens[0].Id).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsAdd, Exact = true }));

        await Expect(Screen(flow.Screens[0].Id).GetByText(WorkbenchStrings.BuildingsAddTitle)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task ShowAll_Should_BringEveryScreenBackIntoSight(WorkbenchSurface surface)
    {
        var flow = await DrawThemeFlow();

        await OpenBoard(flow.Id, 2);

        await Press(PresentButton);

        await Expect(CardOf(flow.Screens[1].Id)).Not.ToBeInViewportAsync(new() { Ratio = 1 });

        await Press(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ShowAllScreens }));

        await Expect(CardOf(flow.Screens[0].Id)).ToBeInViewportAsync(new() { Ratio = 1 });
        await Expect(CardOf(flow.Screens[1].Id)).ToBeInViewportAsync(new() { Ratio = 1 });
    }

    [TestMethod, OnDesktops]
    public async Task ADoubleClickOnAScreen_Should_PresentIt(WorkbenchSurface surface)
    {
        var flow = await DrawThemeFlow();

        await OpenBoard(flow.Id, 2);

        await CardOf(flow.Screens[1].Id).Locator(".screen-title").DblClickAsync();

        await Expect(Presenter).ToContainTextAsync(ScreenOf(2, 2));
        await Expect(CardOf(flow.Screens[1].Id)).ToHaveClassAsync(new Regex(@"\bcurrent\b"));
    }

    [TestMethod, OnPhones]
    public async Task AFinger_Should_PanTheBoard(WorkbenchSurface surface)
    {
        var flow = await DrawThemeFlow();

        await OpenBoard(flow.Id, 2);

        var fitted = await BoardViewport();

        await SwipeUp(fromY: 700, toY: 500);

        await WorkbenchTestReviewer.WaitFor(BoardViewport, viewport => viewport != fitted);
    }

    [TestMethod, EverySurface]
    public async Task AScreensLink_Should_OpenItsViewInTheWorkbench(WorkbenchSurface surface)
    {
        var flow = await DrawThemeFlow();

        await OpenBoard(flow.Id, 2);

        await Press(CardOf(flow.Screens[1].Id).GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.OpenInWorkbench }));

        await Expect(Page).ToHaveURLAsync(new Regex(@"/workbench/theme\?"));
        await WaitForViews();
        await Expect(View().Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
    }

    [TestMethod, EverySurface]
    public async Task TheCrumbs_Should_LeadBackToTheListAndHome(WorkbenchSurface surface)
    {
        var flow = await DrawThemeFlow();

        await OpenBoard(flow.Id, 2);

        await Press(Page.Locator(".topbar a.crumb"));

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.Flows, Level = 1 })).ToBeVisibleAsync();
        await Expect(Page.Locator("main.board")).ToHaveCountAsync(0);

        await Press(Page.Locator(".topbar a.brand"));

        await Expect(Page).ToHaveURLAsync(new Uri(Server.Address, WorkbenchUrls.Home).ToString());
        await Expect(Page.Locator(".flows-page")).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task TheThemeSwitch_Should_TurnTheBoardDarkAndLeaveTheScreensAlone(WorkbenchSurface surface)
    {
        var flow = await DrawThemeFlow();

        await OpenBoard(flow.Id, 2);

        await Press(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ToggleWorkbenchTheme, Exact = true }));

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
        await Expect(Screen(flow.Screens[0].Id).Locator("html")).Not.ToHaveAttributeAsync("bit-theme", new Regex("dark"));
    }

    [TestMethod, EverySurface]
    public async Task TheTools_Should_ListTheFlowsAndOpenABoard(WorkbenchSurface surface)
    {
        var flow = await DrawThemeFlow();

        await OpenWorkbench("shifts");
        await OpenTab(WorkbenchModes.FlowsColumn);

        await Expect(Tools.GetByText(WorkbenchStrings.FlowsHint)).ToBeVisibleAsync();
        await Expect(Tools.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.SampleFlowBuildings })).ToBeVisibleAsync();

        var link = Tools.GetByRole(AriaRole.Link, new() { Name = flow.Name });

        await Expect(link).ToContainTextAsync(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ScreenCount, 2));

        await Press(link);

        await Expect(Page.Locator("main.board")).ToBeVisibleAsync();
        await Expect(Cards).ToHaveCountAsync(2);
    }

    [TestMethod, WithoutAnAgent]
    public async Task TheTools_Should_NotOfferToAddAViewWithoutAnAgent(WorkbenchSurface surface)
    {
        await OpenWorkbench("shifts");
        await OpenTab(WorkbenchModes.FlowsColumn);

        await Expect(Tools.GetByText(WorkbenchStrings.FlowsHint)).ToBeVisibleAsync();
        await Expect(Tools.GetByText(WorkbenchStrings.FlowsEmpty)).ToHaveCountAsync(0);
        await Expect(Tools.GetByText(WorkbenchStrings.AddToFlow)).ToHaveCountAsync(0);
        await Expect(ToolsButton(WorkbenchStrings.AddThisView)).ToHaveCountAsync(0);
    }

    [TestMethod, WithoutAnAgent]
    public async Task ABoardWithoutAnAgent_Should_BeReadOnly(WorkbenchSurface surface)
    {
        var flow = await DrawThemeFlow();

        await OpenBoard(flow.Id, 2);

        await Expect(Page.Locator("main.board")).ToHaveClassAsync(new Regex("read-only"));
        await Expect(Page.Locator(".screen-remove")).ToHaveCountAsync(0);
        await Expect(DeleteFlowButton).ToHaveCountAsync(0);
        await Expect(Page.GetByText(WorkbenchStrings.BoardHelp)).ToHaveCountAsync(0);
        await Expect(Page.Locator(".bit-blow-handle").First).ToBeHiddenAsync();
        await Expect(Arrows).ToHaveCountAsync(1);
        await Expect(ArrowLabels).ToHaveTextAsync([TheArrow]);

        await Press(PresentButton);

        await Expect(Presenter).ToContainTextAsync(ScreenOf(1, 2));
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithoutAgent)]
    public async Task ABoardWithoutAnAgent_Should_LetNothingBeMovedOrDeleted(WorkbenchSurface surface)
    {
        var flow = await DrawThemeFlow();

        await OpenBoard(flow.Id, 2);

        var screen = flow.Screens[0];
        var node = Page.Locator($".bit-blow-node[data-id='{screen.Id}']");
        var before = await node.GetAttributeAsync("style");

        await Drag(CardOf(screen.Id).Locator(".screen-title"), 120, 80);

        await Expect(node).ToHaveAttributeAsync("style", before!);

        await CardOf(screen.Id).Locator(".screen-title").ClickAsync();
        await Page.Keyboard.PressAsync("Delete");

        await Expect(Cards).ToHaveCountAsync(2);
        Assert.HasCount(2, (await FlowById(flow.Id)).Screens);

        await ArrowLabels.DblClickAsync(new() { Force = true });

        await Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = WorkbenchStrings.ArrowLabel })).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task NoFlowsOfItsOwn_Should_LeaveTheSamplesAndAskForANewOneOnlyWhereItCanBeMade(WorkbenchSurface surface)
    {
        await Server.ReviewHost.EditAsync(WorkbenchReviewDocument.Id, document =>
        {
            var flows = document.GetMap("flows");

            foreach (var key in flows.Keys.ToArray())
            {
                flows.Delete(key);
            }
        });

        await OpenFlowsPage();

        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.SampleFlowPos })).ToBeVisibleAsync();

        if (HasAgent)
        {
            await Expect(Page.GetByText(WorkbenchStrings.FlowsEmpty)).ToBeVisibleAsync();
        }
        else
        {
            await Expect(Page.GetByText(WorkbenchStrings.FlowsEmpty)).ToHaveCountAsync(0);
        }

        await Expect(Page.GetByText(WorkbenchStrings.NoFlowsYet)).ToHaveCountAsync(0);

        await OpenWorkbench("shifts");
        await OpenTab(WorkbenchModes.FlowsColumn);

        await Expect(Tools.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.SampleFlowPos })).ToBeVisibleAsync();

        if (HasAgent)
        {
            await Expect(Tools.GetByText(WorkbenchStrings.FlowsEmpty)).ToBeVisibleAsync();
            await Expect(Tools.GetByRole(AriaRole.Textbox, new() { Name = WorkbenchStrings.NewFlowName, Exact = true })).ToBeVisibleAsync();
        }
        else
        {
            await Expect(Tools.GetByText(WorkbenchStrings.FlowsEmpty)).ToHaveCountAsync(0);
        }
    }

    [TestMethod, WithAnAgent]
    public async Task AVisitorThroughATunnel_Should_SeeABoardReadOnly(WorkbenchSurface surface)
    {
        var flow = await DrawThemeFlow();

        await using var visitor = await Browser.NewContextAsync(new BrowserNewContextOptions(ContextOptions()) { ExtraHTTPHeaders = new Dictionary<string, string> { ["X-Forwarded-For"] = "203.0.113.7" } });
        var page = await visitor.NewPageAsync();

        await page.GotoAsync(new Uri(Server.Address, WorkbenchUrls.Flow(flow.Id)).ToString());

        await Expect(page.Locator("main.board")).ToHaveClassAsync(new Regex("read-only"));
        await Expect(page.Locator(".screen-card")).ToHaveCountAsync(2);
        await Expect(page.Locator(".screen-remove")).ToHaveCountAsync(0);
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.DeleteFlow })).ToHaveCountAsync(0);
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.MuteReplySound, Exact = true })).ToHaveCountAsync(0);
    }

    private async Task Drag(ILocator handle, float byX, float byY)
    {
        var box = await handle.BoundingBoxAsync();

        Assert.IsNotNull(box);

        var (x, y) = (box.X + box.Width / 2, box.Y + box.Height / 2);

        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + byX, y + byY, new() { Steps = 8 });
        await Page.Mouse.UpAsync();
    }
}
