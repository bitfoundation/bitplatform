//+:cnd:noEmit
//#if (module == "Admin")
using Boilerplate.WorkbenchTests.Features.Chrome;
using Boilerplate.WorkbenchTests.Features.Showcases;

namespace Boilerplate.WorkbenchTests.Features.AppPages;

/// <summary>The app's dashboard in the Workbench: its figures and charts, editing its layout, who may open it, and its data states picked in the tools, as the app shows each.</summary>
[TestClass, TestCategory("UITest")]
public class DashboardUITests : AppViewPageTest
{
    private ILocator Widgets => View().GetByRole(AriaRole.List, new() { Name = AppStrings.Dashboard, Exact = true }).GetByRole(AriaRole.Listitem);

    private ILocator EditButton => View().GetByRole(AriaRole.Button, new() { Name = AppStrings.Edit, Exact = true });

    private ILocator Stats => Widgets.Filter(new() { HasText = AppStrings.TotalProducts });

    private ILocator CountChart => Widgets.Filter(new() { HasText = AppStrings.ProductsCountPerCategoryChart });

    private ILocator PercentageChart => Widgets.Filter(new() { HasText = AppStrings.ProductsPercentagePerCategory });

    /// <summary>What the app says when its server can't be reached, as a failed load in the Error state is.</summary>
    private ILocator Unreachable => View().GetByText(AppStrings.TransientException, new() { Exact = true }).First;

    /// <summary>A widget's place in the layout the page keeps on the device, as the view saves it: its id, span and whether it's hidden.</summary>
    private static string SavedLayout(string widget, int span, bool hidden) => $"{widget}:{span}:{hidden}";

    private static ILocator Figure(ILocator stats, string label) => stats.Locator(".bit-crd").Filter(new() { HasText = label }).GetByRole(AriaRole.Paragraph).First;

    private ILocator ResizeButton(ILocator widget) => widget.GetByRole(AriaRole.Button, new() { Name = AppStrings.Resize, Exact = true });

    private ILocator HideButton(ILocator widget) => widget.GetByRole(AriaRole.Button, new() { Name = AppStrings.Hide, Exact = true });

    [TestMethod, EverySurface]
    public async Task TheDashboard_Should_ShowItsFiguresAndCharts(WorkbenchSurface surface)
    {
        await OpenWorkbench("dashboard");

        await Expect(PageTitle).ToHaveTextAsync(WorkbenchStrings.AppDashboardTitle);
        await Expect(Widgets).ToHaveCountAsync(3);

        foreach (var (label, value) in new[]
        {
            (AppStrings.TotalCategories, "5"),
            (AppStrings.CategoriesWithProductCount, "5"),
            (AppStrings.TotalProducts, "50"),
            (AppStrings.Last30DaysProductCount, "12")
        })
        {
            await Expect(Figure(Stats, label)).ToHaveTextAsync(value);
        }

        await Expect(CountChart.GetByRole(AriaRole.Application, new() { Name = AppStrings.ProductsCountPerCategoryChart, Exact = true })).ToBeVisibleAsync();
        await Expect(PercentageChart.GetByRole(AriaRole.Application, new() { Name = AppStrings.ProductsPercentagePerCategory, Exact = true })).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task ASignedOutVisitor_Should_SeeTheNotAuthorizedPage(WorkbenchSurface surface)
    {
        await OpenWorkbench("dashboard", "?auth=signedout");

        await Expect(View().GetByText(AppStrings.YouAreNotAuthorized)).ToBeVisibleAsync();
        await Expect(Widgets).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task EveryDemoUser_Should_OpenTheDashboard(WorkbenchSurface surface)
    {
        foreach (var user in new[] { 1, 2 })
        {
            await OpenWorkbench("dashboard", $"?user={user}");

            await Expect(Widgets).ToHaveCountAsync(3);
            await Expect(View().GetByText(AppStrings.YouAreNotAuthorized)).ToHaveCountAsync(0);
        }
    }

    [TestMethod, EverySurface]
    public async Task EditMode_Should_OfferResizingAndHidingAndEndWithAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench("dashboard");

        await Expect(ResizeButton(Stats)).ToHaveCountAsync(0);

        await Press(EditButton);

        await Expect(EditButton).ToHaveAttributeAsync("aria-pressed", "true");

        foreach (var widget in new[] { Stats, CountChart, PercentageChart })
        {
            await Expect(ResizeButton(widget)).ToBeVisibleAsync();
            await Expect(HideButton(widget)).ToBeVisibleAsync();
        }

        // As in the app, editing lasts while the page is open.
        await ReloadTheView();

        await Expect(Stats).ToBeVisibleAsync();
        await Expect(ResizeButton(Stats)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task AHiddenWidget_Should_GoWhenEditingEndsStayHiddenAndComeBack(WorkbenchSurface surface)
    {
        await OpenWorkbench("dashboard");

        await Press(EditButton);
        await Press(HideButton(CountChart));

        await Expect(HideButton(CountChart)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(CountChart.GetByRole(AriaRole.Application)).ToBeVisibleAsync();

        await Press(EditButton);

        await Expect(CountChart).ToHaveCountAsync(0);
        await Expect(Stats).ToBeVisibleAsync();

        await ReloadTheViewOnceSaved(SavedLayout("ProductsCountPerCategoryWidget", 3, hidden: true));

        await Expect(CountChart).ToHaveCountAsync(0);

        await Press(EditButton);
        await Press(HideButton(CountChart));

        await Expect(HideButton(CountChart)).ToHaveAttributeAsync("aria-pressed", "false");

        await Press(EditButton);

        await Expect(CountChart.GetByRole(AriaRole.Application)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task Resizing_Should_GoThroughTheSpansAndStayAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench("dashboard");

        await Press(EditButton);

        await Expect(Stats).ToHaveClassAsync(new Regex(@"\bspan-6\b"));

        await Press(ResizeButton(Stats));
        await Expect(Stats).ToHaveClassAsync(new Regex(@"\bspan-2\b"));

        await Press(ResizeButton(Stats));
        await Expect(Stats).ToHaveClassAsync(new Regex(@"\bspan-3\b"));

        await Press(ResizeButton(Stats));
        await Expect(Stats).ToHaveClassAsync(new Regex(@"\bspan-4\b"));

        await ReloadTheViewOnceSaved(SavedLayout("OverallStatsWidget", 4, hidden: false));

        await Expect(Stats).ToHaveClassAsync(new Regex(@"\bspan-4\b"));

        await Press(EditButton);
        await Press(ResizeButton(Stats));
        await Expect(Stats).ToHaveClassAsync(new Regex(@"\bspan-6\b"));
    }

    // A wide view has the columns a span takes up, which a phone's screen is too narrow for.
    [TestMethod, OnDesktops]
    public async Task AWideWidget_Should_TakeUpTheWidthItsSpanSays(WorkbenchSurface surface)
    {
        await OpenWorkbench("dashboard", "?device=desktop-1536");

        await Press(EditButton);

        await Press(ResizeButton(Stats));
        await Expect(Stats).ToHaveClassAsync(new Regex(@"\bspan-2\b"));

        var narrow = (await Stats.BoundingBoxAsync())!.Width;

        await Press(ResizeButton(Stats));
        await Press(ResizeButton(Stats));
        await Press(ResizeButton(Stats));
        await Expect(Stats).ToHaveClassAsync(new Regex(@"\bspan-6\b"));

        var wide = (await Stats.BoundingBoxAsync())!.Width;

        Assert.IsGreaterThan(narrow * 2, wide);
    }

    [TestMethod, EverySurface]
    public async Task AWidget_Should_BeDraggedToAnotherPlaceOnlyWhileEditingAndStayThereAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench("dashboard");

        await Press(EditButton);

        await Expect(Widgets.First).ToContainTextAsync(AppStrings.TotalProducts);
        await Expect(CountChart.GetByRole(AriaRole.Application)).ToBeVisibleAsync();

        await Drag(CountChart.GetByRole(AriaRole.Heading), Stats);

        await Expect(Widgets.First).ToContainTextAsync(AppStrings.ProductsCountPerCategoryChart);
        await Expect(Widgets.Nth(1)).ToContainTextAsync(AppStrings.TotalProducts);

        await ReloadTheViewOnceSaved("\"dashboard-widgets\":\"ProductsCountPerCategoryWidget");

        await Expect(Widgets.First).ToContainTextAsync(AppStrings.ProductsCountPerCategoryChart);
    }

    [TestMethod, EverySurface]
    public async Task AWidget_Should_StayInPlaceWhenNotEditing(WorkbenchSurface surface)
    {
        await OpenWorkbench("dashboard");

        await Expect(CountChart.GetByRole(AriaRole.Application)).ToBeVisibleAsync();

        await Drag(CountChart.GetByRole(AriaRole.Heading), Stats);

        await Expect(Widgets.First).ToContainTextAsync(AppStrings.TotalProducts);
    }

    /// <summary>Drags with the mouse, or a finger held long enough for a drag on a phone.</summary>
    private async Task Drag(ILocator from, ILocator to)
    {
        if (IsPhone is false)
        {
            await from.DragToAsync(to);
            return;
        }

        await from.ScrollIntoViewIfNeededAsync();

        var start = (await from.BoundingBoxAsync())!;
        var end = (await to.BoundingBoxAsync())!;

        await SwipeWithAFinger((start.X + start.Width / 2, start.Y + start.Height / 2), (end.X + end.Width / 2, Math.Max(end.Y + end.Height / 4, 1)), holdMilliseconds: 300);
    }

    [TestMethod, EverySurface]
    public async Task Loading_Should_KeepThePlaceholdersOfEveryWidget(WorkbenchSurface surface)
    {
        await OpenWorkbench("dashboard");

        await Expect(Figure(Stats, AppStrings.TotalProducts)).ToHaveTextAsync("50");

        await PressInTheTools(WorkbenchStrings.DataLoading);

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?data=loading$"));
        await Expect(Stats.Locator(".bit-ldn-elp")).ToHaveCountAsync(4);
        await Expect(Unreachable).ToHaveCountAsync(0);
        await Expect(View().GetByRole(AriaRole.Application)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task Empty_Should_CountNothing(WorkbenchSurface surface)
    {
        await OpenWorkbench("dashboard");

        await PressInTheTools(WorkbenchStrings.DataEmpty);

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?data=empty$"));

        foreach (var label in new[] { AppStrings.TotalCategories, AppStrings.CategoriesWithProductCount, AppStrings.TotalProducts, AppStrings.Last30DaysProductCount })
        {
            await Expect(Figure(Stats, label)).ToHaveTextAsync("0");
        }

        await Expect(Unreachable).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task Error_Should_SayTheServerCantBeReachedAndShowNoFigures(WorkbenchSurface surface)
    {
        await OpenWorkbench("dashboard");

        await PressInTheTools(WorkbenchStrings.DataError);

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?data=error$"));
        await Expect(Unreachable).ToBeVisibleAsync();
        await Expect(Figure(Stats, AppStrings.TotalProducts)).ToHaveTextAsync("0");
        await Expect(View().GetByRole(AriaRole.Application)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task Mixed_Should_GiveEachWidgetAStateOfItsOwn(WorkbenchSurface surface)
    {
        await OpenWorkbench("dashboard", "?data=mixed");

        // In the order the widgets ask: the figures fail, the count chart is empty and the percentage chart has its data.
        await Expect(Unreachable).ToBeVisibleAsync();
        await Expect(Figure(Stats, AppStrings.TotalProducts)).ToHaveTextAsync("0");
        await Expect(PercentageChart.GetByRole(AriaRole.Application)).ToBeVisibleAsync();
        await Expect(View().GetByRole(AriaRole.Application)).ToHaveCountAsync(1);
    }

    [TestMethod, EverySurface]
    public async Task Slow_Should_ShowPlaceholdersFirstAndEndWithTheData(WorkbenchSurface surface)
    {
        await PlaceholderTimer.Watch(Page, ".bit-ldn-elp", ".bit-cht-svg");

        await OpenWorkbench("dashboard", "?data=slow");

        await Expect(Figure(Stats, AppStrings.TotalProducts)).ToHaveTextAsync("50");
        await Expect(View().GetByRole(AriaRole.Application)).ToHaveCountAsync(2);
        await Expect(Stats.Locator(".bit-ldn-elp")).ToHaveCountAsync(0);

        Assert.IsGreaterThanOrEqualTo(PlaceholderTimer.SlowLoad, await PlaceholderTimer.Read(Page), "The charts came as fast as without the slow state.");
    }
}
//#endif
