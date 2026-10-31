namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard;

public static class PosWidgetCatalog
{
    public static IReadOnlyList<PosWidgetDefinition> All { get; } =
    [
        new("net-sales", nameof(WorkbenchStrings.PosNetSales), BitIconName.Money, typeof(PosKpiWidget), PosWidgetSize.Small, new Dictionary<string, object?> { [nameof(PosKpiWidget.Metric)] = PosKpiMetric.NetSales }),
        new("transactions", nameof(WorkbenchStrings.PosTransactions), BitIconName.ReceiptCheck, typeof(PosKpiWidget), PosWidgetSize.Small, new Dictionary<string, object?> { [nameof(PosKpiWidget.Metric)] = PosKpiMetric.Transactions }),
        new("average-sale", nameof(WorkbenchStrings.PosAverageSale), BitIconName.ShoppingCart, typeof(PosKpiWidget), PosWidgetSize.Small, new Dictionary<string, object?> { [nameof(PosKpiWidget.Metric)] = PosKpiMetric.AverageSale }),
        new("refunds", nameof(WorkbenchStrings.PosRefunds), BitIconName.Undo, typeof(PosKpiWidget), PosWidgetSize.Small, new Dictionary<string, object?> { [nameof(PosKpiWidget.Metric)] = PosKpiMetric.Refunds }),
        new("sales-by-hour", nameof(WorkbenchStrings.PosSalesByHour), BitIconName.BarChart4, typeof(PosSalesByHourWidget), PosWidgetSize.Medium),
        new("payment-methods", nameof(WorkbenchStrings.PosPaymentMethods), BitIconName.PaymentCard, typeof(PosPaymentMethodsWidget), PosWidgetSize.Medium),
        new("top-products", nameof(WorkbenchStrings.PosTopProducts), BitIconName.Trophy2, typeof(PosTopProductsWidget), PosWidgetSize.Medium),
        new("daily-goal", nameof(WorkbenchStrings.PosDailyGoal), BitIconName.Bullseye, typeof(PosDailyGoalWidget), PosWidgetSize.Medium),
        new("recent-orders", nameof(WorkbenchStrings.PosRecentOrders), BitIconName.Clock, typeof(PosRecentOrdersWidget), PosWidgetSize.Large),
        new("low-stock", nameof(WorkbenchStrings.PosLowStock), BitIconName.Package, typeof(PosLowStockWidget), PosWidgetSize.Medium),
        new("team", nameof(WorkbenchStrings.PosTeam), BitIconName.People, typeof(PosStaffWidget), PosWidgetSize.Medium),
        new("peak-hours", nameof(WorkbenchStrings.PosPeakHours), BitIconName.CalendarDay, typeof(PosPeakHoursWidget), PosWidgetSize.Large),
    ];

    public static PosWidgetDefinition? Find(string key) => All.FirstOrDefault(d => d.Key == key);

    public static List<PosWidgetLayout> CreateDefaultLayout() => [.. All.Select(d => new PosWidgetLayout { Key = d.Key, Size = d.DefaultSize })];
}
