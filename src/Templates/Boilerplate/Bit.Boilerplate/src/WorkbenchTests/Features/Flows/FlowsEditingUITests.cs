namespace Boilerplate.WorkbenchTests.Features.Flows;

/// <summary>Changing flows in dev mode where a finger reaches too: adding the selected view from the tools, removing a screen and deleting a flow.</summary>
[TestClass, TestCategory("UITest")]
public class FlowsEditingUITests : FlowPageTest
{
    private ILocator FlowPicker => Tools.GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.Flow });

    private ILocator AddThisView => ToolsButton(WorkbenchStrings.AddThisView);

    private ILocator Field(string label) => Tools.GetByRole(AriaRole.Textbox, new() { Name = label, Exact = true });

    private async Task PickFlow(string name)
    {
        await Press(FlowPicker);
        await Press(Page.GetByRole(AriaRole.Option, new() { Name = name, Exact = true }));
    }

    [TestMethod, WithAnAgent]
    public async Task TheFlowsTab_Should_AddTheSelectedViewToANewFlow(WorkbenchSurface surface)
    {
        var name = Unique("Checking out");

        await OpenWorkbench("buildings", "?device=iphone-se&theme=dark");
        await OpenTab(WorkbenchModes.FlowsColumn);

        await PickFlow(WorkbenchStrings.NewFlow);

        await Expect(AddThisView).ToBeDisabledAsync();

        await Field(WorkbenchStrings.NewFlowName).FillAsync(name);
        await Field(WorkbenchStrings.ScreenNote).FillAsync("The list in dark");

        await Expect(AddThisView).ToBeEnabledAsync();

        await Press(AddThisView);

        var board = Tools.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.OpenBoard });

        await Expect(board).ToBeVisibleAsync();

        var flow = (await WorkbenchTestReviewer.WaitFor(() => Server.Agent.GetFlows(CancellationToken.None), flows => flows.Any(f => f.Name == name))).Single(f => f.Name == name);
        var screen = flow.Screens.Single();

        Assert.AreEqual("buildings", screen.PageKey);
        Assert.AreEqual("?theme=dark&device=iphone-se", screen.Query);
        Assert.AreEqual("The list in dark", screen.Note);
        Assert.IsTrue(screen.HasState, "The view's page state goes with the screen.");

        await Expect(Tools.GetByRole(AriaRole.Link, new() { Name = name })).ToBeVisibleAsync();
        await Expect(Field(WorkbenchStrings.ScreenNote)).ToHaveValueAsync(string.Empty);

        await Press(board);

        await Expect(Cards).ToHaveCountAsync(1);
        await Expect(Page.Locator(".screen-note")).ToHaveTextAsync("The list in dark");
        await Expect(Screen(screen.Id).Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
    }

    [TestMethod, WithAnAgent]
    public async Task TheFlowsTab_Should_AddTheViewToAnExistingFlowWithAnArrow(WorkbenchSurface surface)
    {
        var flow = await DrawFlow(Unique("Growing"), [new("first", "theme", "?device=iphone-se", "First", null)]);

        await OpenWorkbench("settings", "?device=iphone-se");
        await OpenTab(WorkbenchModes.FlowsColumn);

        // The flow drawn last is the one offered.
        await Expect(FlowPicker).ToContainTextAsync(flow.Name);
        await Expect(Field(WorkbenchStrings.NewFlowName)).ToHaveCountAsync(0);

        await Field(WorkbenchStrings.ScreenNote).FillAsync("Second");
        await Field(WorkbenchStrings.ArrowFromLast).FillAsync("Open the settings");

        await Press(AddThisView);

        var saved = await WaitForFlow(flow.Id, f => f.Screens.Length == 2);

        Assert.AreEqual("settings", saved.Screens[1].PageKey);
        Assert.AreEqual("Second", saved.Screens[1].Note);
        Assert.AreEqual((saved.Screens[0].Id, saved.Screens[1].Id, "Open the settings"), (saved.Links.Single().From, saved.Links.Single().To, saved.Links.Single().Label));

        await Press(Tools.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.OpenBoard }));

        await Expect(Cards).ToHaveCountAsync(2);
        await Expect(ArrowLabels).ToHaveTextAsync(["Open the settings"]);
    }

    [TestMethod, WithAnAgent]
    public async Task TheFlowsTab_Should_LetAnotherFlowBeChosen(WorkbenchSurface surface)
    {
        var chosen = await DrawFlow(Unique("Chosen"), [new("first", "theme", "", null, null)]);
        var last = await DrawFlow(Unique("Last"), [new("first", "theme", "", null, null)]);

        await OpenWorkbench("settings");
        await OpenTab(WorkbenchModes.FlowsColumn);

        await Expect(FlowPicker).ToContainTextAsync(last.Name);

        await PickFlow(chosen.Name);

        await Expect(FlowPicker).ToContainTextAsync(chosen.Name);

        await Press(AddThisView);

        await WaitForFlow(chosen.Id, f => f.Screens.Length == 2);

        Assert.HasCount(1, (await FlowById(last.Id)).Screens);
    }

    [TestMethod, WithAnAgent]
    public async Task TheFlowsTab_Should_ShowAFlowTheAgentDrawsLive(WorkbenchSurface surface)
    {
        await OpenWorkbench("settings");
        await OpenTab(WorkbenchModes.FlowsColumn);
        await MarkThePage();

        var flow = await DrawThemeFlow();

        await Expect(Tools.GetByRole(AriaRole.Link, new() { Name = flow.Name })).ToBeVisibleAsync();
        await Expect(FlowPicker).ToContainTextAsync(flow.Name);
        await ExpectNoReload();
    }

    [TestMethod, WithAnAgent]
    public async Task ABoardInDevMode_Should_OfferItsEditing(WorkbenchSurface surface)
    {
        var flow = await DrawThemeFlow();

        await OpenBoard(flow.Id, 2);

        await Expect(Page.Locator("main.board")).Not.ToHaveClassAsync(new Regex("read-only"));
        await Expect(Page.Locator(".screen-remove")).ToHaveCountAsync(2);
        await Expect(DeleteFlowButton).ToBeVisibleAsync();

        // The hint about dragging is for a pointer.
        if (IsPhone)
        {
            await Expect(Page.GetByText(WorkbenchStrings.BoardHelp)).ToBeHiddenAsync();
        }
        else
        {
            await Expect(Page.GetByText(WorkbenchStrings.BoardHelp)).ToBeVisibleAsync();

            await Press(PresentButton);

            await Expect(Page.GetByText(WorkbenchStrings.BoardHelp)).ToHaveCountAsync(0);
        }
    }

    [TestMethod, WithAnAgent]
    public async Task RemovingAScreen_Should_TakeItsArrowsWithIt(WorkbenchSurface surface)
    {
        var flow = await DrawThemeFlow();

        await OpenBoard(flow.Id, 2);

        await Expect(Arrows).ToHaveCountAsync(1);

        await Press(CardOf(flow.Screens[1].Id).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.RemoveScreen }));

        await Expect(Cards).ToHaveCountAsync(1);
        await Expect(Arrows).ToHaveCountAsync(0);
        await Expect(Page.Locator(".screen-note")).ToHaveTextAsync("Light");

        var saved = await WaitForFlow(flow.Id, f => f.Screens.Length == 1);

        Assert.IsEmpty(saved.Links);
    }

    [TestMethod, WithAnAgent]
    public async Task RemovingTheLastScreen_Should_LeaveAnEmptyBoardThatSaysSo(WorkbenchSurface surface)
    {
        var flow = await DrawFlow(Unique("Short"), [new("only", "theme", "", null, null)]);

        await OpenBoard(flow.Id, 1);

        await Press(CardOf(flow.Screens[0].Id).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.RemoveScreen }));

        await Expect(Cards).ToHaveCountAsync(0);
        await Expect(Page.GetByText(WorkbenchStrings.FlowHasNoScreens)).ToBeVisibleAsync();
        await Expect(PresentButton).ToHaveCountAsync(0);
        await Expect(DeleteFlowButton).ToBeVisibleAsync();
    }

    [TestMethod, WithAnAgent]
    public async Task DeletingAFlow_Should_AskOnceMoreAndGoBackToTheList(WorkbenchSurface surface)
    {
        var flow = await DrawThemeFlow();

        await OpenBoard(flow.Id, 2);

        await Press(DeleteFlowButton);

        await Expect(DeleteFlowButton).ToContainTextAsync(WorkbenchStrings.DeleteFlowConfirm);
        await Expect(Cards).ToHaveCountAsync(2);

        Assert.Contains(f => f.Id == flow.Id, await Server.Agent.GetFlows(CancellationToken.None));

        await Press(DeleteFlowButton);

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.Flows, Level = 1 })).ToBeVisibleAsync();
        await Expect(Page.GetByText(WorkbenchStrings.FlowNotFound)).ToHaveCountAsync(0);
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = flow.Name })).ToHaveCountAsync(0);

        await WorkbenchTestReviewer.WaitFor(() => Server.Agent.GetFlows(CancellationToken.None), flows => flows.Any(f => f.Id == flow.Id) is false);
    }
}
