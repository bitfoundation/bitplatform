namespace Boilerplate.WorkbenchTests.Features.Flows;

/// <summary>Changing a board with a pointer, which a desktop in dev mode offers: dragging a screen, drawing and naming an arrow, marking where it starts and deleting it.</summary>
[TestClass, TestCategory("UITest")]
public class FlowBoardEditingUITests : FlowPageTest
{
    private const string NewSampleLabel = "Tap Add building";

    private ILocator SpotOf(string screenId) => CardOf(screenId).Locator(".spot");

    private ILocator PickButton => Page.Locator(".pick-banner").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Cancel, Exact = true });

    private static (float X, float Y) Middle(LocatorBoundingBoxResult box) => (box.X + box.Width / 2, box.Y + box.Height / 2);

    private async Task Drag((float X, float Y) from, (float X, float Y) to)
    {
        await Page.Mouse.MoveAsync(from.X, from.Y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync((from.X + to.X) / 2, (from.Y + to.Y) / 2, new() { Steps = 6 });
        await Page.Mouse.MoveAsync(to.X, to.Y, new() { Steps = 6 });
        await Page.Mouse.UpAsync();
    }

    /// <summary>Draws an arrow by dragging from a screen's dot on its right edge to the dot on another's left edge.</summary>
    private async Task Connect(string fromScreenId, string toScreenId)
    {
        var from = await Page.Locator($".bit-blow-node[data-id='{fromScreenId}'] .bit-blow-handle.source[data-handle-id='out']").BoundingBoxAsync();
        var to = await Page.Locator($".bit-blow-node[data-id='{toScreenId}'] .bit-blow-handle.target").BoundingBoxAsync();

        Assert.IsNotNull(from);
        Assert.IsNotNull(to);

        await Drag(Middle(from), Middle(to));
    }

    /// <summary>A flow of two screens with no arrow yet, side by side.</summary>
    private Task<WorkbenchFlow> DrawTwoScreens() => DrawFlow(Unique("Two screens"), [new("one", "buildings", "?device=iphone-se", "One", null), new("two", "theme", "?device=iphone-se", "Two", null)]);

    private async Task<(WorkbenchFlow Flow, WorkbenchFlowLink Link)> OpenABoardWithAnArrow(string? label = TheArrow)
    {
        var flow = await DrawFlow(Unique("With an arrow"), [new("one", "buildings", "?device=iphone-se", "One", null), new("two", "theme", "?device=iphone-se", "Two", null)], [new("one", "two", label, null)]);

        await OpenBoard(flow.Id, 2);
        await Expect(Arrows).ToHaveCountAsync(1);

        return (flow, flow.Links.Single());
    }

    private async Task OpenTheLabelEditorOf(string linkId)
    {
        await DoublePressArrow(linkId);

        await Expect(LabelEditor).ToBeVisibleAsync();
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task DraggingAScreen_Should_MoveItAndStayAfterAReload(WorkbenchSurface surface)
    {
        var flow = await DrawTwoScreens();

        await OpenBoard(flow.Id, 2);

        var screen = flow.Screens[0];
        var (x, y) = Middle((await CardOf(screen.Id).Locator(".screen-title").BoundingBoxAsync())!);

        await Drag((x, y), (x + 120, y + 80));

        var saved = await WaitForFlow(flow.Id, f => f.Screens[0].X != screen.X || f.Screens[0].Y != screen.Y);

        Assert.IsGreaterThan(screen.X, saved.Screens[0].X);
        Assert.IsGreaterThan(screen.Y, saved.Screens[0].Y);

        await Page.ReloadAsync();
        await Expect(Cards).ToHaveCountAsync(2);

        await Expect(Page.Locator($".bit-blow-node[data-id='{screen.Id}']")).ToHaveAttributeAsync("style", new Regex($@"translate\({saved.Screens[0].X}px,\s*{saved.Screens[0].Y}px\)"));
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task ConnectingTwoScreens_Should_DrawAnArrowAndAskItsName(WorkbenchSurface surface)
    {
        var flow = await DrawTwoScreens();

        await OpenBoard(flow.Id, 2);

        await Expect(Arrows).ToHaveCountAsync(0);

        await Connect(flow.Screens[0].Id, flow.Screens[1].Id);

        await Expect(Arrows).ToHaveCountAsync(1);
        await Expect(LabelEditor).ToBeVisibleAsync();
        await Expect(LabelField).ToBeFocusedAsync();
        await Expect(LabelField).ToHaveValueAsync(string.Empty);

        var saved = await WaitForFlow(flow.Id, f => f.Links.Length == 1);

        Assert.AreEqual((flow.Screens[0].Id, flow.Screens[1].Id), (saved.Links[0].From, saved.Links[0].To));

        await LabelField.FillAsync("Tap Next");
        await Press(EditorButton(WorkbenchStrings.Save));

        await Expect(LabelEditor).ToHaveCountAsync(0);
        await Expect(ArrowLabels).ToHaveTextAsync(["Tap Next"]);

        await WaitForFlow(flow.Id, f => f.Links[0].Label == "Tap Next");

        await Page.ReloadAsync();

        await Expect(ArrowLabels).ToHaveTextAsync(["Tap Next"]);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task AnArrowsLabel_Should_ChangeOnlyWhenSaved(WorkbenchSurface surface)
    {
        var (flow, link) = await OpenABoardWithAnArrow();

        await OpenTheLabelEditorOf(link.Id);

        await Expect(LabelField).ToHaveValueAsync(TheArrow);

        await LabelField.FillAsync("Thrown away");
        await Press(EditorButton(WorkbenchStrings.Cancel));

        await Expect(LabelEditor).ToHaveCountAsync(0);
        await Expect(ArrowLabels).ToHaveTextAsync([TheArrow]);

        await OpenTheLabelEditorOf(link.Id);

        await LabelField.FillAsync("Thrown away with Escape");
        await LabelField.PressAsync("Escape");

        await Expect(LabelEditor).ToHaveCountAsync(0);
        await Expect(ArrowLabels).ToHaveTextAsync([TheArrow]);

        await OpenTheLabelEditorOf(link.Id);

        await Expect(LabelField).ToHaveValueAsync(TheArrow);

        await LabelField.FillAsync("A better name");
        await Press(EditorButton(WorkbenchStrings.Save));

        await Expect(ArrowLabels).ToHaveTextAsync(["A better name"]);

        await WaitForFlow(flow.Id, f => f.Links[0].Label == "A better name");
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task AnArrowsLabel_Should_OpenItsEditorOnADoublePress(WorkbenchSurface surface)
    {
        await OpenABoardWithAnArrow();

        await DoublePress(ArrowLabels.First);

        await Expect(LabelEditor).ToBeVisibleAsync();
        await Expect(LabelField).ToHaveValueAsync(TheArrow);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task ChangingASamplesArrow_Should_OpenACopyWithTheChangeAndLeaveTheSample(WorkbenchSurface surface)
    {
        await OpenBoard(WorkbenchSampleFlows.BuildingsId, 3);

        await DoublePress(SampleAddArrowLabel);

        await Expect(LabelEditor).ToBeVisibleAsync();
        await Expect(LabelField).ToHaveValueAsync(WorkbenchStrings.BuildingsAdd);
        await Expect(LabelEditor.GetByRole(AriaRole.Note)).ToBeVisibleAsync();

        // Saved as it was, the label changes nothing, and so makes no copy.
        await Press(EditorButton(WorkbenchStrings.Save));

        await Expect(LabelEditor).ToHaveCountAsync(0);
        await Expect(Page).ToHaveURLAsync(new Regex($"/flows/{WorkbenchSampleFlows.BuildingsId}$"));

        await DoublePress(SampleAddArrowLabel);
        await LabelField.FillAsync(NewSampleLabel);
        await Press(EditorButton(WorkbenchStrings.Save));

        await Expect(Page).Not.ToHaveURLAsync(new Regex($"/flows/{WorkbenchSampleFlows.BuildingsId}"));
        await Expect(Page.Locator("main.board")).Not.ToHaveClassAsync(new Regex("read-only"));
        await Expect(SampleAddArrowLabel).ToHaveTextAsync(NewSampleLabel);
        await Expect(Screen("form").GetByText(WorkbenchStrings.BuildingsAddTitle)).ToBeVisibleAsync();
        await Expect(Screen("map").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsMapView, Exact = true })).ToHaveAttributeAsync("aria-pressed", "true");

        var copy = await WaitForFlow(Page.Url.Split('/')[^1], f => f.Links.Any(l => l.Label == NewSampleLabel));

        Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.FlowCopyName, WorkbenchStrings.SampleFlowBuildings), copy.Name);
        Assert.AreSequenceEqual(["list", "form", "map"], copy.Screens.Select(s => s.Id));

        await OpenBoard(WorkbenchSampleFlows.BuildingsId, 3);

        await Expect(SampleAddArrowLabel).ToHaveTextAsync(WorkbenchStrings.BuildingsAdd);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task MarkingWhereASamplesArrowStarts_Should_OpenACopyWithItsEditorStillOpen(WorkbenchSurface surface)
    {
        await OpenBoard(WorkbenchSampleFlows.BuildingsId, 3);

        await DoublePress(SampleAddArrowLabel);
        await LabelField.FillAsync(NewSampleLabel);
        await Press(EditorButton(WorkbenchStrings.MarkArrowStart));

        // The pick is ready once the screen's page shows its crosshair.
        await Expect(Page.Locator(".pick-banner")).ToBeVisibleAsync();
        await Expect(Screen("list").Locator("html")).ToHaveCSSAsync("cursor", "crosshair");

        await Screen("list").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsAdd, Exact = true }).ClickAsync();

        await Expect(Page).Not.ToHaveURLAsync(new Regex($"/flows/{WorkbenchSampleFlows.BuildingsId}"));
        await Expect(LabelEditor).ToBeVisibleAsync();
        await Expect(LabelEditor).ToContainTextAsync(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ArrowStartMarked, 1));
        await Expect(LabelField).ToHaveValueAsync(NewSampleLabel);
        await Expect(SpotOf("list")).ToHaveCountAsync(1);

        await WaitForFlow(Page.Url.Split('/')[^1], f => f.Links.Single(l => l.Id == WorkbenchSampleFlows.BuildingsAddArrowId).Spot is not null);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task EmptyingAnArrowsLabel_Should_LeaveItUnnamed(WorkbenchSurface surface)
    {
        var (flow, link) = await OpenABoardWithAnArrow();

        await OpenTheLabelEditorOf(link.Id);

        await LabelField.FillAsync(string.Empty);
        await Press(EditorButton(WorkbenchStrings.Save));

        await Expect(ArrowLabels).ToHaveCountAsync(0);
        await Expect(Arrows).ToHaveCountAsync(1);

        await WaitForFlow(flow.Id, f => f.Links[0].Label is null);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task TheArrowEditor_Should_DeleteTheArrow(WorkbenchSurface surface)
    {
        var (flow, link) = await OpenABoardWithAnArrow();

        await OpenTheLabelEditorOf(link.Id);
        await Press(EditorButton(WorkbenchStrings.DeleteArrow));

        await Expect(LabelEditor).ToHaveCountAsync(0);
        await Expect(Arrows).ToHaveCountAsync(0);

        await WaitForFlow(flow.Id, f => f.Links.Length == 0);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task TheDeleteKey_Should_RemoveASelectedArrowAndScreen(WorkbenchSurface surface)
    {
        var (flow, link) = await OpenABoardWithAnArrow();

        await Page.Locator($".bit-blow-edge[data-id='{link.Id}'] .bit-blow-edge-label").ClickAsync(new() { Force = true });
        await Expect(Page.Locator($".bit-blow-edge[data-id='{link.Id}']")).ToHaveClassAsync(new Regex(@"\bselected\b"));

        await Page.Keyboard.PressAsync("Delete");

        await Expect(Arrows).ToHaveCountAsync(0);
        await WaitForFlow(flow.Id, f => f.Links.Length == 0);

        await CardOf(flow.Screens[1].Id).Locator(".screen-title").ClickAsync();
        await Page.Keyboard.PressAsync("Delete");

        await Expect(Cards).ToHaveCountAsync(1);
        await WaitForFlow(flow.Id, f => f.Screens.Length == 1);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task MarkingTheButton_Should_StartTheArrowFromItWithoutPressingIt(WorkbenchSurface surface)
    {
        var (flow, link) = await OpenABoardWithAnArrow();
        var from = flow.Screens[0].Id;

        await OpenTheLabelEditorOf(link.Id);

        await Expect(LabelEditor).ToContainTextAsync(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ArrowStartHint, 1));

        await Press(EditorButton(WorkbenchStrings.MarkArrowStart));

        var banner = Page.Locator(".pick-banner");

        await Expect(banner).ToContainTextAsync(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.PickArrowStart, 1, 2));
        await Expect(LabelEditor).ToHaveCountAsync(0);
        await Expect(CardOf(from)).ToHaveClassAsync(new Regex(@"\bpicking\b"));
        await Expect(CardOf(from)).ToBeInViewportAsync(new() { Ratio = 0.8f });

        await Expect(Screen(from).Locator("html")).ToHaveCSSAsync("cursor", "crosshair");

        await Screen(from).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsAdd, Exact = true }).ClickAsync();

        await Expect(banner).ToHaveCountAsync(0);
        await Expect(LabelEditor).ToBeVisibleAsync();
        await Expect(LabelEditor).ToContainTextAsync(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ArrowStartMarked, 1));
        await Expect(SpotOf(from)).ToHaveCountAsync(1);
        await Expect(Screen(from).GetByText(WorkbenchStrings.BuildingsAddTitle)).ToHaveCountAsync(0);

        var saved = await WaitForFlow(flow.Id, f => f.Links[0].Spot is not null);
        var spot = saved.Links[0].Spot!;

        Assert.IsTrue(spot.Width is > 0 and < 1 && spot.Height is > 0 and < 1, "The spot is the button's box, not the whole screen.");

        await Press(EditorButton(WorkbenchStrings.Cancel));

        await Page.ReloadAsync();

        await Expect(SpotOf(from)).ToHaveCountAsync(1);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task AMarkedArrowStart_Should_BeRemovable(WorkbenchSurface surface)
    {
        var flow = await DrawFlow(Unique("Marked"),
                                  [new("one", "buildings", "?device=iphone-se", "One", null), new("two", "theme", "?device=iphone-se", "Two", null)],
                                  [new("one", "two", TheArrow, WorkbenchStrings.BuildingsAdd)]);
        var from = flow.Screens[0].Id;

        await OpenBoard(flow.Id, 2);

        await Expect(SpotOf(from)).ToHaveCountAsync(1);

        await OpenTheLabelEditorOf(flow.Links[0].Id);

        await Expect(EditorButton(WorkbenchStrings.MarkArrowStartAgain)).ToBeVisibleAsync();

        await Press(EditorButton(WorkbenchStrings.RemoveArrowStart));

        await Expect(EditorButton(WorkbenchStrings.MarkArrowStart)).ToBeVisibleAsync();
        await Expect(EditorButton(WorkbenchStrings.RemoveArrowStart)).ToHaveCountAsync(0);
        await Expect(SpotOf(from)).ToHaveCountAsync(0);

        await WaitForFlow(flow.Id, f => f.Links[0].Spot is null);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task MarkingAgain_Should_MoveTheArrowsStart(WorkbenchSurface surface)
    {
        var flow = await DrawFlow(Unique("Marked twice"),
                                  [new("one", "buildings", "?device=iphone-se", "One", null), new("two", "theme", "?device=iphone-se", "Two", null)],
                                  [new("one", "two", TheArrow, WorkbenchStrings.BuildingsAdd)]);
        var from = flow.Screens[0].Id;

        await OpenBoard(flow.Id, 2);
        await OpenTheLabelEditorOf(flow.Links[0].Id);

        await Press(EditorButton(WorkbenchStrings.MarkArrowStartAgain));

        await Expect(Screen(from).Locator("html")).ToHaveCSSAsync("cursor", "crosshair");

        await Screen(from).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsMapView, Exact = true }).ClickAsync();

        await Expect(LabelEditor).ToBeVisibleAsync();

        var saved = await WaitForFlow(flow.Id, f => f.Links[0].Spot != flow.Links[0].Spot);

        Assert.IsNotNull(saved.Links[0].Spot);

        await Expect(Screen(from).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsMapView, Exact = true })).ToHaveAttributeAsync("aria-pressed", "false");
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task MarkingOnAScreenThatHasNotLoaded_Should_SaySo(WorkbenchSurface surface)
    {
        var (flow, link) = await OpenABoardWithAnArrow();

        await Page.Locator($"iframe[data-screen-id='{flow.Screens[0].Id}']").EvaluateAsync("frame => frame.remove()");

        await OpenTheLabelEditorOf(link.Id);

        await Press(EditorButton(WorkbenchStrings.MarkArrowStart));

        await Expect(LabelEditor.GetByText(WorkbenchStrings.ScreenNotLoaded)).ToBeVisibleAsync();
        await Expect(Page.Locator(".pick-banner")).ToHaveCountAsync(0);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public Task TheBannersCancel_Should_BringBackTheEditorAndChangeNothing(WorkbenchSurface surface) => CancelTheMark(withEscape: false);

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public Task Escape_Should_BringBackTheEditorAndChangeNothing(WorkbenchSurface surface) => CancelTheMark(withEscape: true);

    private async Task CancelTheMark(bool withEscape)
    {
        var (flow, link) = await OpenABoardWithAnArrow();
        var from = flow.Screens[0].Id;

        await OpenTheLabelEditorOf(link.Id);
        await Press(EditorButton(WorkbenchStrings.MarkArrowStart));

        // The screen's page takes the pick once the board has zoomed to it, which its cursor tells.
        await Expect(Screen(from).Locator("html")).ToHaveCSSAsync("cursor", "crosshair");
        await Expect(Page.Locator(".pick-banner")).ToBeVisibleAsync();

        if (withEscape)
        {
            await Page.Keyboard.PressAsync("Escape");
        }
        else
        {
            await Press(PickButton);
        }

        await Expect(Page.Locator(".pick-banner")).ToHaveCountAsync(0);
        await Expect(LabelEditor).ToBeVisibleAsync();
        await Expect(SpotOf(from)).ToHaveCountAsync(0);

        Assert.IsNull((await FlowById(flow.Id)).Links[0].Spot);

        // The screen goes back to behaving as it did: nothing waits for a click on it any more.
        await Press(EditorButton(WorkbenchStrings.Cancel));
        await Press(PresentButton);

        // A press on a screen the board is still zooming to lands beside its button.
        await Expect(CardOf(from)).ToHaveClassAsync(new Regex(@"\bcurrent\b"));
        await SettledBoardViewport();

        await Screen(from).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsAdd, Exact = true }).ClickAsync();

        await Expect(Screen(from).GetByText(WorkbenchStrings.BuildingsAddTitle)).ToBeVisibleAsync();
    }
}
