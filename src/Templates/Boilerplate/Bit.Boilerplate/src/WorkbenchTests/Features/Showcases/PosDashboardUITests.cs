namespace Boilerplate.WorkbenchTests.Features.Showcases;

/// <summary>The POS dashboard in a browser: its widgets in each data state, the period filter, and the layout the user customizes.</summary>
[TestClass, TestCategory("UITest")]
public class PosDashboardUITests : WorkbenchPageTest
{
    private ILocator Widgets => Page.Locator("section.widget");

    private ILocator Widget(string titleKey) => Page.Locator($"section.widget[aria-label='{WorkbenchStrings.ResourceManager.GetString(titleKey)}']");

    private ILocator Button(string name) => Page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    [TestMethod]
    public async Task EveryWidget_Should_ShowItsData()
    {
        await OpenPreview("pos-dashboard", "?device=desktop-1536");

        await Expect(Widgets).ToHaveCountAsync(PosWidgetCatalog.All.Count);
        await Expect(Page.Locator(".widget-loading")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".widget-state")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Error_Should_FailEveryWidgetOnItsOwnAndARetryShouldLoadIt()
    {
        await OpenPreview("pos-dashboard", "?device=desktop-1536&data=error");

        await Expect(Page.Locator(".widget-state.failed")).ToHaveCountAsync(PosWidgetCatalog.All.Count);

        await Widget(nameof(WorkbenchStrings.PosTopProducts)).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosRetry }).ClickAsync();

        await Expect(Widget(nameof(WorkbenchStrings.PosTopProducts)).Locator(".widget-state")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".widget-state.failed")).ToHaveCountAsync(PosWidgetCatalog.All.Count - 1);
    }

    [TestMethod]
    public async Task Empty_Should_SayWhatWillShowUp()
    {
        await OpenPreview("pos-dashboard", "?device=desktop-1536&data=empty");

        await Expect(Page.Locator(".widget-state.empty")).ToHaveCountAsync(PosWidgetCatalog.All.Count);
        await Expect(Widget(nameof(WorkbenchStrings.PosRecentOrders))).ToContainTextAsync(WorkbenchStrings.PosNoOrdersYet);
        await Expect(Widget(nameof(WorkbenchStrings.PosLowStock))).ToContainTextAsync(WorkbenchStrings.PosStockAllGood);
    }

    [TestMethod]
    public async Task Loading_Should_KeepEveryWidgetsPlaceholder()
    {
        await OpenPreview("pos-dashboard", "?device=desktop-1536&data=loading");

        await Expect(Page.Locator(".widget-loading")).ToHaveCountAsync(PosWidgetCatalog.All.Count);
    }

    [TestMethod]
    public async Task ThePeriod_Should_ReachEveryWidget()
    {
        await OpenPreview("pos-dashboard", "?device=desktop-1536");

        var netSales = Widget(nameof(WorkbenchStrings.PosNetSales));

        await Expect(netSales).ToContainTextAsync(WorkbenchStrings.PosToday);

        var today = await netSales.Locator(".widget-body").InnerTextAsync();

        await Page.GetByRole(AriaRole.Radio, new() { Name = WorkbenchStrings.Pos30Days, Exact = true }).ClickAsync();

        await Expect(netSales).ToContainTextAsync(WorkbenchStrings.PosLast30Days);
        await Expect(netSales.Locator(".widget-body")).Not.ToHaveTextAsync(today);
    }

    [TestMethod]
    public async Task AHiddenWidget_Should_StayHiddenAfterAReloadAndComeBackFromTheGallery()
    {
        await OpenPreview("pos-dashboard", "?device=desktop-1536");

        await Button(WorkbenchStrings.PosCustomize).ClickAsync();
        await Expect(Page.GetByText(WorkbenchStrings.PosEditHint)).ToBeVisibleAsync();

        var team = Widget(nameof(WorkbenchStrings.PosTeam));

        await team.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosWidgetOptions }).ClickAsync();
        await team.GetByText(WorkbenchStrings.PosHideWidget, new() { Exact = true }).ClickAsync();

        await Expect(team).ToHaveCountAsync(0);
        await Expect(Widgets).ToHaveCountAsync(PosWidgetCatalog.All.Count - 1);

        await Button(WorkbenchStrings.PosDone).ClickAsync();
        await Expect(Page.GetByText(WorkbenchStrings.PosEditHint)).ToHaveCountAsync(0);

        await Page.ReloadAsync();
        await Page.WaitForFunctionAsync("() => window.__workbenchReady === true");

        await Expect(Widgets).ToHaveCountAsync(PosWidgetCatalog.All.Count - 1);

        await Button(WorkbenchStrings.PosCustomize).ClickAsync();
        await Button(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.PosAddWidget, 1)).ClickAsync();

        var gallery = Page.Locator(".gallery");

        await Expect(gallery).ToContainTextAsync(WorkbenchStrings.PosTeam);

        await gallery.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosAdd }).ClickAsync();

        await Expect(Widgets).ToHaveCountAsync(PosWidgetCatalog.All.Count);
        await Expect(Button(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.PosAddWidget, 0))).ToBeDisabledAsync();
    }

    [TestMethod]
    public async Task ResetLayout_Should_BringTheDefaultLayoutBack()
    {
        await OpenPreview("pos-dashboard", "?device=desktop-1536");

        await Button(WorkbenchStrings.PosCustomize).ClickAsync();

        await Widget(nameof(WorkbenchStrings.PosRefunds)).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosWidgetOptions }).ClickAsync();
        await Widget(nameof(WorkbenchStrings.PosRefunds)).GetByText(WorkbenchStrings.PosHideWidget, new() { Exact = true }).ClickAsync();

        await Expect(Widgets).ToHaveCountAsync(PosWidgetCatalog.All.Count - 1);

        await Button(WorkbenchStrings.PosResetLayout).ClickAsync();

        await Expect(Widgets).ToHaveCountAsync(PosWidgetCatalog.All.Count);
    }
}
