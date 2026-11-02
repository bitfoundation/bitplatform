namespace Boilerplate.WorkbenchTests.Features.Flows;

/// <summary>The flows: the boards, the list of them, the samples that come with the Workbench and the Flows tab that adds a view to one.</summary>
[TestClass, TestCategory("UITest")]
public class FlowsUITests : WorkbenchPageTest
{
    [TestMethod]
    public async Task AFlowsBoard_Should_ShowItsScreensAndStepThroughThem()
    {
        var (id, problem) = await Server.Agent.SaveFlow("Test Agent", new(
            $"Browsing the theme {Guid.NewGuid():N}",
            [new("light", "theme", "?device=iphone-se", "Light", null), new("dark", "theme", "?device=iphone-se&theme=dark", "Dark", null)],
            [new("light", "dark", "Switch the theme", null)],
            null,
            null), CancellationToken.None);

        Assert.IsNull(problem);

        await Page.GotoAsync(new Uri(Server.Address, $"flows/{id}").ToString());

        await Expect(Page.Locator(".screen-card")).ToHaveCountAsync(2);
        await Expect(Page.Locator(".screen-note")).ToHaveTextAsync(["Light", "Dark"]);

        await Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Present }).ClickAsync();

        var presenter = Page.GetByRole(AriaRole.Toolbar, new() { Name = WorkbenchStrings.Present });

        await Expect(presenter).ToContainTextAsync(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ScreenOf, 1, 2));

        await presenter.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.NextScreen }).ClickAsync();

        await Expect(presenter).ToContainTextAsync(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ScreenOf, 2, 2));
    }

    [TestMethod]
    public async Task TheFlowsPage_Should_ListTheFlows()
    {
        var name = $"Listed flow {Guid.NewGuid():N}";

        await Server.Agent.SaveFlow("Test Agent", new(name, [new("only", "settings", "", null, null)], null, null, null), CancellationToken.None);

        await Page.GotoAsync(new Uri(Server.Address, "flows").ToString());

        await Expect(Page.GetByText(name)).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task TheBuildingSample_Should_ShowItsScreensInTheirStates()
    {
        await Page.GotoAsync(new Uri(Server.Address, WorkbenchUrls.Flow(WorkbenchSampleFlows.BuildingsId)).ToString());

        await Expect(Page.Locator(".screen-card")).ToHaveCountAsync(3);
        await Expect(Page.FrameLocator("iframe[data-screen-id='form']").GetByText(WorkbenchStrings.BuildingsAddTitle)).ToBeVisibleAsync();
        await Expect(Page.FrameLocator("iframe[data-screen-id='map']").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsMapView, Exact = true })).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod]
    public async Task ASample_Should_BeReadOnlyEvenInDevMode()
    {
        await Page.GotoAsync(new Uri(Server.Address, WorkbenchUrls.Flow(WorkbenchSampleFlows.BuildingsId)).ToString());

        await Expect(Page.Locator("main.board.read-only")).ToBeVisibleAsync();
        await Expect(Page.Locator(".screen-remove")).ToHaveCountAsync(0);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.DeleteFlow })).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task TheFlowsPage_Should_ListTheSamples()
    {
        await Page.GotoAsync(new Uri(Server.Address, WorkbenchUrls.Flows).ToString());

        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.SampleFlowBuildings })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.SampleFlowPos })).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task TheFlowsTab_Should_AddTheSelectedViewToANewFlow()
    {
        var name = $"Checking out {Guid.NewGuid():N}";

        await OpenWorkbench("buildings", "?device=iphone-se&theme=dark");

        await Page.GetByRole(AriaRole.Tab, new() { Name = WorkbenchStrings.Flows }).ClickAsync();

        await Page.GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.Flow }).ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = WorkbenchStrings.NewFlow, Exact = true }).ClickAsync();

        await Page.GetByLabel(WorkbenchStrings.NewFlowName, new() { Exact = true }).FillAsync(name);
        await Page.GetByLabel(WorkbenchStrings.ScreenNote, new() { Exact = true }).FillAsync("The list in dark");
        await Button(WorkbenchStrings.AddThisView).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.OpenBoard })).ToBeVisibleAsync();

        var flow = (await WorkbenchTestReviewer.WaitFor(() => Server.Agent.GetFlows(CancellationToken.None), flows => flows.Any(f => f.Name == name))).Single(f => f.Name == name);
        var screen = flow.Screens.Single();

        Assert.AreEqual("buildings", screen.PageKey);
        Assert.AreEqual("?theme=dark&device=iphone-se", screen.Query);
        Assert.AreEqual("The list in dark", screen.Note);
        Assert.IsTrue(screen.HasState, "The view's page state goes with the screen.");
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithoutAgent)]
    public async Task TheFlowsTab_Should_NotAskForAnAgent(WorkbenchSurface surface)
    {
        await OpenWorkbench("shifts");

        await Page.Locator("#wb-tab-flows").ClickAsync();

        await Expect(Page.GetByText(WorkbenchStrings.FlowsHint)).ToBeVisibleAsync();
        await Expect(Page.GetByText(WorkbenchStrings.FlowsEmpty)).ToHaveCountAsync(0);
        await Expect(Page.GetByText(WorkbenchStrings.AddToFlow)).ToHaveCountAsync(0);
    }
}
