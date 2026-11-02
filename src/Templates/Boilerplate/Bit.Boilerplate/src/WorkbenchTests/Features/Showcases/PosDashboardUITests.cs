namespace Boilerplate.WorkbenchTests.Features.Showcases;

/// <summary>The POS dashboard in a view: every widget and its data in each data state, the store and period filter, the reorder button, and the layout the user customizes.</summary>
[TestClass, TestCategory("UITest")]
public class PosDashboardUITests : ShowcaseUITest
{
    private const string PageKey = "pos-dashboard";

    private static readonly int widgetCount = PosWidgetCatalog.All.Count;

    private static readonly string[] titles = [.. PosWidgetCatalog.All.Select(widget => WorkbenchStrings.ResourceManager.GetString(widget.TitleKey)!)];

    private ILocator Widgets => View().Locator("section.widget[aria-label]");

    private ILocator Loading => View().Locator(".widget-loading");

    private ILocator Failed => View().Locator(".widget-state.failed");

    private ILocator Empty => View().Locator(".widget-state.empty");

    private ILocator EditHint => View().GetByText(WorkbenchStrings.PosEditHint);

    private ILocator Gallery => View().Locator(".gallery");

    private ILocator StorePicker => View().GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.PosStore });

    private ILocator Widget(string title) => View().GetByRole(AriaRole.Region, new() { Name = title, Exact = true });

    private ILocator Period(string name) => View().GetByRole(AriaRole.Radio, new() { Name = name, Exact = true });

    private ILocator AddWidget(int hidden) => ViewButton(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.PosAddWidget, hidden));

    /// <summary>The cell of the grid that holds a widget at a size.</summary>
    private ILocator WidgetAtSize(string title, PosWidgetSize size) => View().Locator($".widget-{size.ToString().ToLowerInvariant()}").Filter(new() { Has = Widget(title) });

    private Task<string[]> WidgetOrder() => Widgets.EvaluateAllAsync<string[]>("widgets => widgets.map(widget => widget.getAttribute('aria-label'))");

    private async Task StartCustomizing()
    {
        await Press(ViewButton(WorkbenchStrings.PosCustomize));
        await Expect(EditHint).ToBeVisibleAsync();
    }

    private async Task FinishCustomizing()
    {
        await Press(ViewButton(WorkbenchStrings.PosDone));
        await Expect(EditHint).ToHaveCountAsync(0);
    }

    private ILocator OpenMenu => View().GetByRole(AriaRole.Menu).Locator("visible=true");

    /// <summary>Picks an item of a widget's menu, which opens in a layer of its own.</summary>
    private async Task UseTheMenu(string title, string item)
    {
        var options = Widget(title).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosWidgetOptions });

        await ScrollToAndWaitUntilStill(options);
        await Press(options);
        await Press(OpenMenu.GetByText(item, new() { Exact = true }));
    }

    private async Task Hide(string title)
    {
        await UseTheMenu(title, WorkbenchStrings.PosHideWidget);
        await Expect(Widget(title)).ToHaveCountAsync(0);
    }

    /// <summary>What a widget's body says, its cells' and charts' labels too; empty while it loads.</summary>
    private Task<string> DataOf(string title) => Widget(title).Locator(".widget-body").EvaluateAsync<string>("body => body.querySelector('.widget-loading') ? '' : body.textContent + [...body.querySelectorAll('[aria-label]')].map(e => e.getAttribute('aria-label')).join()");

    private async Task ExpectItsDataToChange(string title, string before)
    {
        await WorkbenchTestReviewer.WaitFor(() => DataOf(title), now => now.Length > 0 && now != before);
    }

    [TestMethod, EverySurface]
    public async Task EveryWidget_Should_ShowItsData(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Expect(Widgets).ToHaveCountAsync(widgetCount);
        await Expect(Loading).ToHaveCountAsync(0);
        await Expect(View().Locator(".widget-state")).ToHaveCountAsync(0);

        foreach (var title in titles)
        {
            await Expect(Widget(title)).ToContainTextAsync(title);
        }

        await Expect(Widget(WorkbenchStrings.PosSalesByHour).Locator(".bit-cht-svg")).ToBeVisibleAsync();
        await Expect(Widget(WorkbenchStrings.PosPaymentMethods).Locator(".bit-cht-svg")).ToBeVisibleAsync();

        await ExpectNoSidewaysScroll();
    }

    [TestMethod, EverySurface]
    public async Task TheGreeting_Should_NameWhoeverIsSignedIn(WorkbenchSurface surface)
    {
        var greeting = View().GetByRole(AriaRole.Heading, new() { Level = 2 }).First;

        await OpenWorkbench(PageKey);

        await Expect(greeting).ToContainTextAsync(WorkbenchStrings.DemoUserYas);

        await OpenWorkbench(PageKey, "?user=2");

        await Expect(greeting).ToContainTextAsync(WorkbenchStrings.DemoUserZahra);
        await Expect(greeting).Not.ToContainTextAsync(WorkbenchStrings.DemoUserYas);
    }

    [TestMethod, EverySurface]
    public async Task TheChartsLegend_Should_TurnASeriesOffAndOn(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        var lastWeek = Widget(WorkbenchStrings.PosSalesByHour).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosLastWeek, Exact = true });

        await Expect(lastWeek).ToHaveAttributeAsync("aria-pressed", "true");

        await Press(lastWeek);

        await Expect(lastWeek).ToHaveAttributeAsync("aria-pressed", "false");

        await Press(lastWeek);

        await Expect(lastWeek).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod, EverySurface]
    public async Task TheKpis_Should_ShowTheirValueChangeAndTrend(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        var netSales = Widget(WorkbenchStrings.PosNetSales);

        await Expect(netSales.Locator(".kpi-value")).ToHaveTextAsync(PosFormat.Money(5_077.60m));
        await Expect(netSales.Locator(".kpi-change.good")).ToContainTextAsync(PosFormat.SignedPercent(0.12m));
        await Expect(netSales.Locator(".kpi-trend .bit-cht-svg")).ToBeVisibleAsync();

        await Expect(Widget(WorkbenchStrings.PosTransactions).Locator(".kpi-value")).ToHaveTextAsync(PosFormat.Number(723));
        await Expect(Widget(WorkbenchStrings.PosAverageSale).Locator(".kpi-value")).ToHaveTextAsync(new Regex(@"^€\d"));

        var refunds = Widget(WorkbenchStrings.PosRefunds);

        await Expect(refunds.Locator(".kpi-value")).ToHaveTextAsync(PosFormat.Money(94.05m));
        await Expect(refunds.Locator(".kpi-change.good")).ToContainTextAsync(PosFormat.SignedPercent(-0.21m));
    }

    [TestMethod, EverySurface]
    public async Task TheLists_Should_ShowTheirItems(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        var payments = Widget(WorkbenchStrings.PosPaymentMethods);

        await Expect(payments.Locator(".legend li")).ToHaveCountAsync(4);
        await Expect(payments.Locator(".legend li").First).ToContainTextAsync(WorkbenchStrings.PosPaymentCard);
        await Expect(payments.Locator(".legend li").First).ToContainTextAsync(PosFormat.Percent(0.57m));
        await Expect(payments.Locator(".donut-total")).ToHaveTextAsync(PosFormat.CompactMoney(5_077.60m));

        var products = Widget(WorkbenchStrings.PosTopProducts);

        await Expect(products.Locator(".products li")).ToHaveCountAsync(5);
        await Expect(products.Locator(".rank")).ToHaveTextAsync(["1", "2", "3", "4", "5"]);

        var goal = Widget(WorkbenchStrings.PosDailyGoal);

        await Expect(goal.Locator(".ring-percent")).ToHaveTextAsync((1_846.40m / 2_500m).ToString("P0", CultureInfo.CurrentCulture));

        var orders = Widget(WorkbenchStrings.PosRecentOrders);

        await Expect(orders.Locator("tbody tr")).ToHaveCountAsync(8);
        await Expect(orders.Locator(".status-pending")).ToHaveTextAsync(WorkbenchStrings.PosOrderStatusPending);
        await Expect(orders.Locator(".status-refunded")).ToHaveTextAsync(WorkbenchStrings.PosOrderStatusRefunded);
        await Expect(orders.Locator(".status-paid")).ToHaveCountAsync(6);

        var stock = Widget(WorkbenchStrings.PosLowStock);

        await Expect(stock.Locator(".stock li")).ToHaveCountAsync(4);
        await Expect(stock.Locator(".badge.out")).ToHaveTextAsync(WorkbenchStrings.PosOutOfStock);
        await Expect(stock.Locator(".badge.low")).ToHaveCountAsync(3);

        var team = Widget(WorkbenchStrings.PosTeam);

        await Expect(team.Locator(".team li")).ToHaveCountAsync(4);
        await Expect(team.Locator(".medal")).ToHaveCountAsync(1);

        var peakHours = Widget(WorkbenchStrings.PosPeakHours);

        await Expect(peakHours.Locator(".cell")).ToHaveCountAsync(7 * 13);
        await Expect(peakHours.Locator(".scale")).ToContainTextAsync(WorkbenchStrings.PosQuiet);
    }

    [TestMethod, EverySurface]
    public async Task Reorder_Should_MarkTheItemOrderedAndStayAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        var oatMilk = Widget(WorkbenchStrings.PosLowStock).Locator(".stock li").Filter(new() { HasText = WorkbenchStrings.PosStockOatMilk });

        await Press(oatMilk.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosReorder, Exact = true }));

        var ordered = oatMilk.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosOrdered, Exact = true });

        await Expect(ordered).ToBeDisabledAsync();
        await Expect(Widget(WorkbenchStrings.PosLowStock).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosReorder, Exact = true })).ToHaveCountAsync(3);

        await ReloadTheView();

        await Expect(ordered).ToBeDisabledAsync();
    }

    [TestMethod, EverySurface]
    public async Task Loading_Should_KeepEveryWidgetsPlaceholder(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?data=loading");

        await Expect(Loading).ToHaveCountAsync(widgetCount);
        await Expect(View().Locator(".widget-state")).ToHaveCountAsync(0);
        await Expect(Loading.First).ToHaveAttributeAsync("aria-label", WorkbenchStrings.PosLoadingWidget);
    }

    [TestMethod, EverySurface]
    public async Task Slow_Should_ShowPlaceholdersFirstAndTheDataAfterAWhile(WorkbenchSurface surface)
    {
        await OpenWithoutWaiting(PageKey, "?data=slow");

        await Expect(Loading).ToHaveCountAsync(widgetCount);
        await Expect(Loading).ToHaveCountAsync(0);

        await Expect(Widgets).ToHaveCountAsync(widgetCount);
        await Expect(View().Locator(".widget-state")).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task Empty_Should_SayWhatEachWidgetIsWaitingFor(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?data=empty");

        await Expect(Empty).ToHaveCountAsync(widgetCount);

        (string Widget, string Message)[] messages =
        [
            (WorkbenchStrings.PosNetSales, WorkbenchStrings.PosNoSalesYet),
            (WorkbenchStrings.PosTransactions, WorkbenchStrings.PosNoSalesYet),
            (WorkbenchStrings.PosAverageSale, WorkbenchStrings.PosNoSalesYet),
            (WorkbenchStrings.PosRefunds, WorkbenchStrings.PosNoSalesYet),
            (WorkbenchStrings.PosSalesByHour, WorkbenchStrings.PosNoSalesYet),
            (WorkbenchStrings.PosPaymentMethods, WorkbenchStrings.PosNoPaymentsYet),
            (WorkbenchStrings.PosTopProducts, WorkbenchStrings.PosNoSalesYet),
            (WorkbenchStrings.PosDailyGoal, WorkbenchStrings.PosNoGoalSet),
            (WorkbenchStrings.PosRecentOrders, WorkbenchStrings.PosNoOrdersYet),
            (WorkbenchStrings.PosLowStock, WorkbenchStrings.PosStockAllGood),
            (WorkbenchStrings.PosTeam, WorkbenchStrings.PosNoShiftsYet),
            (WorkbenchStrings.PosPeakHours, WorkbenchStrings.PosNoOrdersYet)
        ];

        Assert.HasCount(widgetCount, messages);

        foreach (var (widget, message) in messages)
        {
            await Expect(Widget(widget).Locator(".widget-state")).ToContainTextAsync(message);
        }
    }

    [TestMethod, EverySurface]
    public async Task Error_Should_FailEveryWidgetOnItsOwnAndARetryShouldLoadIt(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?data=error");

        await Expect(Failed).ToHaveCountAsync(widgetCount);
        await Expect(Widget(WorkbenchStrings.PosTopProducts).GetByRole(AriaRole.Alert)).ToContainTextAsync(WorkbenchStrings.PosWidgetFailedTitle);

        await Press(Widget(WorkbenchStrings.PosTopProducts).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosRetry }));

        await Expect(Widget(WorkbenchStrings.PosTopProducts).Locator(".widget-state")).ToHaveCountAsync(0);
        await Expect(Widget(WorkbenchStrings.PosTopProducts).Locator(".products li")).ToHaveCountAsync(5);
        await Expect(Failed).ToHaveCountAsync(widgetCount - 1);
    }

    [TestMethod, EverySurface]
    public async Task Mixed_Should_GiveEachWidgetAStateOfItsOwn(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?data=mixed");

        await Expect(Failed).ToHaveCountAsync(widgetCount / 3);
        await Expect(Empty).ToHaveCountAsync(widgetCount / 3);
        await Expect(Loading).ToHaveCountAsync(0);

        await Press(Failed.First.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosRetry }));

        await Expect(Failed).ToHaveCountAsync(widgetCount / 3 - 1);
        await Expect(Empty).ToHaveCountAsync(widgetCount / 3);
    }

    [TestMethod, EverySurface]
    public async Task TheDataSwitchInTheTools_Should_ReachTheWidgetsAndComeBack(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Expect(Failed).ToHaveCountAsync(0);

        await UseTheTools(WorkbenchStrings.DataError);

        await Expect(Failed).ToHaveCountAsync(widgetCount);

        await UseTheTools(WorkbenchStrings.DataEmpty);

        await Expect(Empty).ToHaveCountAsync(widgetCount);

        await UseTheTools(WorkbenchStrings.DataLive);

        await Expect(Widgets).ToHaveCountAsync(widgetCount);
        await Expect(View().Locator(".widget-state")).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task ThePeriod_Should_ReachEveryWidget(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        var before = new Dictionary<string, string>();

        foreach (var title in titles)
        {
            before[title] = await DataOf(title);
        }

        await Expect(Period(WorkbenchStrings.PosToday)).ToBeCheckedAsync();

        await Press(Period(WorkbenchStrings.Pos30Days));

        await Expect(Period(WorkbenchStrings.Pos30Days)).ToBeCheckedAsync();

        // Low stock is about the shelves, so it reads the same for any period.
        foreach (var title in titles.Where(title => title != WorkbenchStrings.PosLowStock))
        {
            await ExpectItsDataToChange(title, before[title]);
        }

        foreach (var title in new[] { WorkbenchStrings.PosNetSales, WorkbenchStrings.PosTransactions, WorkbenchStrings.PosAverageSale, WorkbenchStrings.PosRefunds,
                                      WorkbenchStrings.PosPaymentMethods, WorkbenchStrings.PosTopProducts, WorkbenchStrings.PosDailyGoal, WorkbenchStrings.PosTeam })
        {
            await Expect(Widget(title).Locator(".widget-header")).ToContainTextAsync(WorkbenchStrings.PosLast30Days);
        }
    }

    [TestMethod, EverySurface]
    public async Task EachPeriod_Should_NameItsOwnComparison(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        var compare = Widget(WorkbenchStrings.PosNetSales).Locator(".kpi-compare");

        await Expect(compare).ToHaveTextAsync(WorkbenchStrings.PosVsYesterday);

        await Press(Period(WorkbenchStrings.Pos7Days));

        await Expect(compare).ToHaveTextAsync(WorkbenchStrings.PosVsPrevious7Days);
        await Expect(Widget(WorkbenchStrings.PosNetSales).Locator(".widget-header")).ToContainTextAsync(WorkbenchStrings.PosLast7Days);

        await Press(Period(WorkbenchStrings.Pos30Days));

        await Expect(compare).ToHaveTextAsync(WorkbenchStrings.PosVsPrevious30Days);

        await Press(Period(WorkbenchStrings.PosToday));

        await Expect(compare).ToHaveTextAsync(WorkbenchStrings.PosVsYesterday);
    }

    [TestMethod, EverySurface]
    public async Task TheStore_Should_ReachEveryWidget(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        var before = new Dictionary<string, string>();

        foreach (var title in titles)
        {
            before[title] = await DataOf(title);
        }

        await Expect(StorePicker).ToContainTextAsync(WorkbenchStrings.PosStoreAll);

        await Pick(StorePicker, WorkbenchStrings.PosStoreAmsterdam);

        await Expect(StorePicker).ToContainTextAsync(WorkbenchStrings.PosStoreAmsterdam);
        await Expect(Widget(WorkbenchStrings.PosNetSales).Locator(".kpi-value")).ToHaveTextAsync(PosFormat.Money(Math.Round(1_846.40m * 1.18m, 2)));

        foreach (var title in titles.Where(title => title != WorkbenchStrings.PosLowStock))
        {
            await ExpectItsDataToChange(title, before[title]);
        }
    }

    [TestMethod, EverySurface]
    public async Task TheFilter_Should_StayAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Pick(StorePicker, WorkbenchStrings.PosStoreUtrecht);
        await Press(Period(WorkbenchStrings.Pos7Days));

        await Expect(Widget(WorkbenchStrings.PosNetSales).Locator(".kpi-compare")).ToHaveTextAsync(WorkbenchStrings.PosVsPrevious7Days);

        await ReloadTheView();

        await Expect(StorePicker).ToContainTextAsync(WorkbenchStrings.PosStoreUtrecht);
        await Expect(Period(WorkbenchStrings.Pos7Days)).ToBeCheckedAsync();
        await Expect(Widget(WorkbenchStrings.PosNetSales).Locator(".kpi-compare")).ToHaveTextAsync(WorkbenchStrings.PosVsPrevious7Days);
    }

    [TestMethod, EverySurface]
    public async Task Customize_Should_OfferAHandleAndAMenuOnEveryWidgetUntilDone(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Expect(View().Locator(".widget-handle")).ToHaveCountAsync(0);
        await Expect(EditHint).ToHaveCountAsync(0);

        await StartCustomizing();

        await Expect(View().Locator(".widget-handle")).ToHaveCountAsync(widgetCount);
        await Expect(View().GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosWidgetOptions })).ToHaveCountAsync(widgetCount);
        await Expect(AddWidget(0)).ToBeDisabledAsync();

        await FinishCustomizing();

        await Expect(View().Locator(".widget-handle")).ToHaveCountAsync(0);
        await Expect(View().GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosWidgetOptions })).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task EditMode_Should_StayAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await StartCustomizing();

        await ReloadTheView();

        await Expect(EditHint).ToBeVisibleAsync();
        await Expect(View().Locator(".widget-handle")).ToHaveCountAsync(widgetCount);
    }

    [TestMethod, EverySurface]
    public async Task AHiddenWidget_Should_StayHiddenAfterAReloadAndComeBackFromTheGallery(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await StartCustomizing();
        await Hide(WorkbenchStrings.PosTeam);

        await Expect(Widgets).ToHaveCountAsync(widgetCount - 1);

        await FinishCustomizing();
        await ReloadTheView();

        await Expect(Widgets).ToHaveCountAsync(widgetCount - 1);
        await Expect(Widget(WorkbenchStrings.PosTeam)).ToHaveCountAsync(0);

        await StartCustomizing();
        await Press(AddWidget(1));

        await Expect(Gallery).ToContainTextAsync(WorkbenchStrings.PosTeam);

        await Press(Gallery.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosAdd, Exact = true }));

        await Expect(Widgets).ToHaveCountAsync(widgetCount);
        await Expect(Widget(WorkbenchStrings.PosTeam).Locator(".team li")).ToHaveCountAsync(4);
        await Expect(AddWidget(0)).ToBeDisabledAsync();

        await ReloadTheView();

        await Expect(Widgets).ToHaveCountAsync(widgetCount);
    }

    [TestMethod, EverySurface]
    public async Task TheGallery_Should_ListEveryHiddenWidgetAndCloseWithTheLastOne(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await StartCustomizing();
        await Hide(WorkbenchStrings.PosRefunds);
        await Hide(WorkbenchStrings.PosNetSales);

        await Press(AddWidget(2));

        var items = Gallery.Locator(".gallery-item");

        await Expect(items).ToHaveCountAsync(2);
        await Expect(items).ToContainTextAsync([WorkbenchStrings.PosRefunds, WorkbenchStrings.PosNetSales]);

        await Press(items.Filter(new() { HasText = WorkbenchStrings.PosNetSales }).GetByRole(AriaRole.Button));

        await Expect(items).ToHaveCountAsync(1);
        await Expect(Widget(WorkbenchStrings.PosNetSales)).ToBeVisibleAsync();

        await Press(items.GetByRole(AriaRole.Button));

        await Expect(Gallery).Not.ToBeInViewportAsync();
        await Expect(Widgets).ToHaveCountAsync(widgetCount);
    }

    [TestMethod, EverySurface]
    public async Task TheGallery_Should_StayOpenAfterAReloadAndCloseWithItsButton(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await StartCustomizing();
        await Hide(WorkbenchStrings.PosRefunds);
        await Press(AddWidget(1));

        await Expect(Gallery).ToBeInViewportAsync();

        await ReloadTheView();

        await Expect(Gallery).ToBeInViewportAsync();
        await Expect(Gallery).ToContainTextAsync(WorkbenchStrings.PosRefunds);

        await Press(View().Locator(".bit-pnl").Filter(new() { Has = Gallery }).GetByRole(AriaRole.Button).Locator("visible=true").First);

        await Expect(Gallery).Not.ToBeInViewportAsync();
        await Expect(Widget(WorkbenchStrings.PosRefunds)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task TheMenu_Should_MoveAWidgetLaterAndEarlierAndKeepItAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await StartCustomizing();

        Assert.AreEqual(WorkbenchStrings.PosNetSales, (await WidgetOrder())[0]);

        await UseTheMenu(WorkbenchStrings.PosNetSales, WorkbenchStrings.PosMoveLater);

        await WorkbenchTestReviewer.WaitFor(WidgetOrder, order => order[0] == WorkbenchStrings.PosTransactions && order[1] == WorkbenchStrings.PosNetSales);

        await ReloadTheView();

        await WorkbenchTestReviewer.WaitFor(WidgetOrder, order => order[0] == WorkbenchStrings.PosTransactions && order[1] == WorkbenchStrings.PosNetSales);

        await UseTheMenu(WorkbenchStrings.PosNetSales, WorkbenchStrings.PosMoveEarlier);

        await WorkbenchTestReviewer.WaitFor(WidgetOrder, order => order[0] == WorkbenchStrings.PosNetSales && order[1] == WorkbenchStrings.PosTransactions);
    }

    [TestMethod, EverySurface]
    public async Task TheMenu_Should_LeaveTheEndsOfTheGridWhereTheyAre(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await StartCustomizing();

        var original = await WidgetOrder();

        await UseTheMenu(titles[0], WorkbenchStrings.PosMoveEarlier);
        await UseTheMenu(titles[^1], WorkbenchStrings.PosMoveLater);

        Assert.AreSequenceEqual(original, await WidgetOrder());
    }

    [TestMethod, EverySurface]
    public async Task TheMenu_Should_ResizeAWidgetAndKeepItAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        var title = WorkbenchStrings.PosNetSales;

        await StartCustomizing();

        await Expect(WidgetAtSize(title, PosWidgetSize.Small)).ToBeVisibleAsync();

        var small = (await Widget(title).BoundingBoxAsync())!.Width;

        await UseTheMenu(title, WorkbenchStrings.PosSizeLarge);

        await Expect(WidgetAtSize(title, PosWidgetSize.Large)).ToBeVisibleAsync();

        await WorkbenchTestReviewer.WaitFor(async () => (await Widget(title).BoundingBoxAsync())!.Width, width => width > small * 1.5);

        await FinishCustomizing();
        await ReloadTheView();

        await Expect(WidgetAtSize(title, PosWidgetSize.Large)).ToBeVisibleAsync();

        await StartCustomizing();
        await Press(Widget(title).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosWidgetOptions }));

        await Expect(OpenMenu.GetByRole(AriaRole.Menuitemcheckbox, new() { Name = WorkbenchStrings.PosSizeLarge, Exact = true })).ToBeCheckedAsync();
        await Expect(OpenMenu.GetByRole(AriaRole.Menuitemcheckbox, new() { Name = WorkbenchStrings.PosSizeSmall, Exact = true })).Not.ToBeCheckedAsync();
    }

    [TestMethod, EverySurface]
    public async Task ResetLayout_Should_BringTheDefaultLayoutBackForGood(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        var original = await WidgetOrder();

        await StartCustomizing();

        await Hide(WorkbenchStrings.PosRefunds);
        await UseTheMenu(WorkbenchStrings.PosNetSales, WorkbenchStrings.PosMoveLater);
        await UseTheMenu(WorkbenchStrings.PosTeam, WorkbenchStrings.PosSizeLarge);

        await Expect(Widgets).ToHaveCountAsync(widgetCount - 1);
        await Expect(WidgetAtSize(WorkbenchStrings.PosTeam, PosWidgetSize.Large)).ToBeVisibleAsync();

        await Press(ViewButton(WorkbenchStrings.PosResetLayout));

        await Expect(Widgets).ToHaveCountAsync(widgetCount);
        await Expect(WidgetAtSize(WorkbenchStrings.PosTeam, PosWidgetSize.Medium)).ToBeVisibleAsync();
        await Expect(AddWidget(0)).ToBeDisabledAsync();

        Assert.AreSequenceEqual(original, await WidgetOrder());

        await ReloadTheView();

        await Expect(Widgets).ToHaveCountAsync(widgetCount);

        Assert.AreSequenceEqual(original, await WidgetOrder());
    }

    [TestMethod, EverySurface]
    public async Task DraggingAHandle_Should_ReorderTheWidgetsAndKeepItAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await StartCustomizing();

        var handle = ViewButton(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.PosDragWidget, WorkbenchStrings.PosNetSales));

        await Drag(handle, Widget(WorkbenchStrings.PosAverageSale), toX: 0.7, toY: 0.7);

        var moved = await WorkbenchTestReviewer.WaitFor(WidgetOrder, order => order[0] != WorkbenchStrings.PosNetSales);

        await FinishCustomizing();
        await ReloadTheView();

        await WorkbenchTestReviewer.WaitFor(WidgetOrder, order => order.SequenceEqual(moved));
    }

    [TestMethod, OnDesktops]
    public async Task TheKeyboard_Should_MoveAWidget(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await StartCustomizing();

        await View().Locator("[data-bortable-item]").First.FocusAsync();

        await Page.Keyboard.PressAsync("Space");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("Space");

        await WorkbenchTestReviewer.WaitFor(WidgetOrder, order => order[0] == WorkbenchStrings.PosTransactions && order[1] == WorkbenchStrings.PosNetSales);
    }

    [TestMethod, OnDesktops]
    public async Task AWideView_Should_PutFourSmallWidgetsInARow(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?device=desktop-1536");

        var first = (await Widget(WorkbenchStrings.PosNetSales).BoundingBoxAsync())!;
        var fourth = (await Widget(WorkbenchStrings.PosRefunds).BoundingBoxAsync())!;
        var medium = (await Widget(WorkbenchStrings.PosSalesByHour).BoundingBoxAsync())!;

        Assert.AreEqual(first.Y, fourth.Y, 2);
        Assert.IsGreaterThan(first.X + first.Width * 2, fourth.X);
        Assert.IsGreaterThan(first.Width * 1.5, medium.Width);

        await ExpectNoSidewaysScroll();
    }

    [TestMethod, EverySurface]
    public async Task ANarrowView_Should_PutTwoSmallWidgetsInARowAndTheOthersAlone(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        var first = (await Widget(WorkbenchStrings.PosNetSales).BoundingBoxAsync())!;
        var second = (await Widget(WorkbenchStrings.PosTransactions).BoundingBoxAsync())!;
        var third = (await Widget(WorkbenchStrings.PosAverageSale).BoundingBoxAsync())!;
        var medium = (await Widget(WorkbenchStrings.PosSalesByHour).BoundingBoxAsync())!;

        Assert.AreEqual(first.Y, second.Y, 2);
        Assert.IsGreaterThan(first.Y, third.Y);
        Assert.IsGreaterThan(first.Width * 1.5, medium.Width);

        await ExpectNoSidewaysScroll();
    }
}
