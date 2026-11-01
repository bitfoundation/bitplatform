namespace Boilerplate.WorkbenchTests.Features.Shell;

[TestClass, TestCategory("UITest")]
public class ShellUITests : WorkbenchPageTest
{
    private ILocator Button(string name) => Page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    [TestMethod]
    public async Task APage_Should_OpenInAViewOfTheShell()
    {
        await OpenWorkbench("theme");

        await Expect(Page).ToHaveTitleAsync($"{WorkbenchStrings.ThemePageTitle} · {WorkbenchStrings.WorkbenchTitle}");
        await Expect(View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ThemeHeading, Level = 1 }).First).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = DevicePresets.Default.Name })).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task APageTheWorkbenchLacks_Should_SaySoWithAWayBack()
    {
        await Page.GotoAsync(new Uri(Server.Address, "workbench/missing").ToString());

        await Expect(Page.GetByText(WorkbenchStrings.PageNotFound)).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.BackToHome })).ToHaveAttributeAsync("href", "/");
    }

    [TestMethod]
    public async Task ThePageList_Should_OpenAnotherPageOnTheSameViews()
    {
        await OpenWorkbench("theme", "?theme=dark&device=ipad-mini");

        await Page.GetByRole(AriaRole.Navigation, new() { Name = WorkbenchStrings.Pages }).GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.BuildingsTitle }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(@"/workbench/buildings\?theme=dark&device=ipad-mini$"));
        await WaitForViews();
        await Expect(View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.BuildingsHeading })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "iPad mini" })).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ThePageListAndTheSideColumn_Should_FoldAwayAndStayFoldedAfterAReload()
    {
        await OpenWorkbench("theme");

        var pages = Button(WorkbenchStrings.Pages);
        var tools = Button(WorkbenchStrings.Tools);

        await Expect(pages).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Page.Locator("#wb-navigator")).ToBeVisibleAsync();

        await pages.ClickAsync();
        await tools.ClickAsync();

        await Expect(Page.Locator(".workbench.nav-collapsed.inspector-collapsed")).ToBeVisibleAsync();
        await Expect(pages).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(tools).ToHaveAttributeAsync("aria-expanded", "false");

        await Page.ReloadAsync();
        await WaitForViews();

        await Expect(Page.Locator(".workbench.nav-collapsed.inspector-collapsed")).ToBeVisibleAsync();
        await Expect(Button(WorkbenchStrings.Pages)).ToHaveAttributeAsync("aria-expanded", "false");

        // Edit text works in the side column, so it brings the column back.
        await Button(WorkbenchStrings.ModeText).ClickAsync();

        await Expect(Page.Locator(".workbench.nav-collapsed:not(.inspector-collapsed)")).ToBeVisibleAsync();
        await Expect(Page.GetByText(WorkbenchStrings.EditTextHint)).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task TheDataState_Should_ReachTheViewAndTheAddressAndSurviveAReload()
    {
        await OpenWorkbench("buildings");

        await Button(WorkbenchStrings.DataEmpty).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?data=empty$"));
        await Expect(View().GetByText(WorkbenchStrings.BuildingsEmptyTitle)).ToBeVisibleAsync();
        await Expect(Button(WorkbenchStrings.DataEmpty)).ToHaveAttributeAsync("aria-pressed", "true");

        await Page.ReloadAsync();
        await WaitForViews();

        await Expect(View().GetByText(WorkbenchStrings.BuildingsEmptyTitle)).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ADevice_Should_BePickedFromTheList()
    {
        await OpenWorkbench("theme");

        await Page.GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.Device }).ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = "Galaxy S26 · 360×780", Exact = true }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?device=galaxy-s26$"));
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "Galaxy S26", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.Locator(".pane-size")).ToHaveTextAsync("360×780 · 3x");
    }

    [TestMethod]
    public async Task RotatingAView_Should_TurnItsSize()
    {
        await OpenWorkbench("theme", "?device=iphone-se");

        await Expect(Page.Locator(".pane-size")).ToHaveTextAsync("375×667 · 2x");

        await Button(WorkbenchStrings.Rotate).ClickAsync();

        await Expect(Page.Locator(".pane-size")).ToHaveTextAsync("667×375 · 2x");
        await Expect(Page).ToHaveURLAsync(new Regex("rotated=true"));
        await Expect(Button(WorkbenchStrings.Landscape)).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod]
    public async Task AddingAndRemovingViews_Should_ChangeTheCanvas()
    {
        await OpenWorkbench("theme");

        await Button(WorkbenchStrings.AddView).ClickAsync();
        await WaitForViews();

        await Expect(Page.Locator(".pane")).ToHaveCountAsync(2);
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "iPad Pro 11\"" })).ToBeVisibleAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(@"\?panes="));

        await Page.GetByRole(AriaRole.Group, new() { Name = "iPad Pro 11\"" }).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.RemoveView }).ClickAsync();

        await Expect(Page.Locator(".pane")).ToHaveCountAsync(1);
        await Expect(Button(WorkbenchStrings.RemoveView)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task CompareDevices_Should_ShowAPhoneATabletAndADesktop()
    {
        await OpenWorkbench("theme");

        await Button(WorkbenchStrings.Compare).ClickAsync();
        await Page.GetByText(WorkbenchStrings.CompareDevices, new() { Exact = true }).ClickAsync();
        await WaitForViews();

        await Expect(Page.Locator(".pane")).ToHaveCountAsync(3);

        foreach (var device in new[] { DevicePresets.Default.Name, "iPad Pro 11\"", "Desktop 1536×864" })
        {
            await Expect(Page.GetByRole(AriaRole.Group, new() { Name = device, Exact = true })).ToBeVisibleAsync();
        }
    }

    [TestMethod]
    public async Task CompareStates_Should_ShowEveryDataStateSideBySide()
    {
        await OpenWorkbench("buildings");

        await Button(WorkbenchStrings.Compare).ClickAsync();
        await Page.GetByText(WorkbenchStrings.CompareStates, new() { Exact = true }).ClickAsync();
        await WaitForViews();

        await Expect(Page.Locator(".pane")).ToHaveCountAsync(4);
        await Expect(Page.Locator(".pane-chip")).ToHaveTextAsync([WorkbenchStrings.DataLoading, WorkbenchStrings.DataEmpty, WorkbenchStrings.DataError]);

        var panes = await Page.Locator("iframe[data-pane-id]").EvaluateAllAsync<string[]>("frames => frames.map(f => f.dataset.paneId)");

        await Expect(View(panes[2]).GetByText(WorkbenchStrings.BuildingsEmptyTitle)).ToBeVisibleAsync();
        await Expect(View(panes[3]).GetByText(WorkbenchStrings.BuildingsFailedTitle)).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ALinkWithSeveralViews_Should_OpenThoseViews()
    {
        var panes = WorkbenchUrls.EncodePanes([new() { Device = "iphone-se" }, new() { Device = "ipad-mini", Theme = WorkbenchThemeMode.Dark }]);

        await OpenWorkbench("theme", $"?panes={panes}");

        await Expect(Page.Locator(".pane")).ToHaveCountAsync(2);
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "iPhone SE" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "iPad mini" }).Locator(".pane-chip")).ToHaveTextAsync(WorkbenchStrings.ThemeDark);
    }

    [TestMethod]
    public async Task ABreakpoint_Should_SnapTheViewToItsWidth()
    {
        await OpenWorkbench("theme", "?device=desktop-1536");

        var breakpoints = Page.GetByRole(AriaRole.Group, new() { Name = WorkbenchStrings.Breakpoints }).GetByRole(AriaRole.Button);

        await Expect(breakpoints.First).ToBeVisibleAsync();
        await Expect(Page.Locator(".breakpoint[aria-pressed='true']")).ToHaveCountAsync(1);

        await breakpoints.Nth(2).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(@"device=custom&size=\d+x864"));
        await Expect(Page.Locator(".breakpoint").Nth(2)).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod]
    public async Task OnAPhone_Should_ShowOnlyThePageWithAButtonForTheTools()
    {
        await Page.SetViewportSizeAsync(390, 844);
        await Page.GotoAsync(new Uri(Server.Address, "workbench/buildings").ToString());

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.BuildingsHeading })).ToBeVisibleAsync();
        await Expect(Page.Locator("iframe[data-pane-id]")).ToHaveCountAsync(0);

        await Button(WorkbenchStrings.Tools).ClickAsync();

        await Expect(Page.GetByText(WorkbenchStrings.DataStateHint)).ToBeVisibleAsync();
        await Expect(Button(WorkbenchStrings.PinComment)).ToBeVisibleAsync();
    }
}
