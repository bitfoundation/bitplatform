namespace Boilerplate.Workbench.Components.Pages.Showcase.PosDashboard.Data;

public partial class PosDashboardDataService
{
    [AutoInject] private WorkbenchScenarioState scenarioState = default!;

    public static IReadOnlyList<string> StoreKeys { get; } =
    [
        nameof(WorkbenchStrings.PosStoreAll),
        nameof(WorkbenchStrings.PosStoreAmsterdam),
        nameof(WorkbenchStrings.PosStoreRotterdam),
        nameof(WorkbenchStrings.PosStoreUtrecht)
    ];

    private static readonly (string Key, string Emoji, decimal Price, int Popularity)[] products =
    [
        (nameof(WorkbenchStrings.PosProductFlatWhite), "☕", 3.90m, 96),
        (nameof(WorkbenchStrings.PosProductOatLatte), "🥛", 4.40m, 88),
        (nameof(WorkbenchStrings.PosProductCroissant), "🥐", 2.95m, 84),
        (nameof(WorkbenchStrings.PosProductAppleTart), "🥧", 4.75m, 61),
        (nameof(WorkbenchStrings.PosProductCappuccino), "☕", 3.60m, 58),
        (nameof(WorkbenchStrings.PosProductStroopwafel), "🧇", 2.25m, 55),
        (nameof(WorkbenchStrings.PosProductSourdough), "🍞", 6.50m, 34),
        (nameof(WorkbenchStrings.PosProductColdBrew), "🧊", 4.20m, 31),
    ];

    private static readonly string[] staff =
    [
        nameof(WorkbenchStrings.PosStaffEmma),
        nameof(WorkbenchStrings.PosStaffLucas),
        nameof(WorkbenchStrings.PosStaffMila),
        nameof(WorkbenchStrings.PosStaffSem)
    ];

    public Task<PosKpi> GetNetSales(PosDashboardFilter filter, CancellationToken cancellationToken) =>
        scenarioState.Load("pos.net-sales", () => Kpi(filter, "net-sales", 1_846.40m, 0.12m), () => new PosKpi(0, 0, []), cancellationToken);

    public Task<PosKpi> GetTransactions(PosDashboardFilter filter, CancellationToken cancellationToken) =>
        scenarioState.Load("pos.transactions", () => Kpi(filter, "transactions", 263, 0.06m, round: true), () => new PosKpi(0, 0, []), cancellationToken);

    public Task<PosKpi> GetAverageSale(PosDashboardFilter filter, CancellationToken cancellationToken) =>
        scenarioState.Load("pos.average-sale", () => Kpi(filter, "average-sale", 7.02m, 0.04m, scaleByVolume: false), () => new PosKpi(0, 0, []), cancellationToken);

    public Task<PosKpi> GetRefunds(PosDashboardFilter filter, CancellationToken cancellationToken) =>
        scenarioState.Load("pos.refunds", () => Kpi(filter, "refunds", 34.20m, -0.21m), () => new PosKpi(0, 0, []), cancellationToken);

    public Task<PosHourlySales[]> GetSalesByHour(PosDashboardFilter filter, CancellationToken cancellationToken) =>
        scenarioState.Load<PosHourlySales[]>("pos.sales-by-hour", () =>
        {
            var random = CreateRandom(filter, "sales-by-hour");
            var factor = VolumeFactor(filter) / filter.Days;
            return [.. Enumerable.Range(7, 14).Select(hour =>
            {
                var today = (decimal)Curve(hour) * 196 * factor * (decimal)(0.9 + random.NextDouble() * 0.2);
                var lastWeek = (decimal)Curve(hour) * 182 * factor * (decimal)(0.85 + random.NextDouble() * 0.25);
                return new PosHourlySales(hour, Math.Round(today, 2), Math.Round(lastWeek, 2));
            })];
        }, () => [], cancellationToken);

    public Task<PosPaymentShare[]> GetPaymentMethods(PosDashboardFilter filter, CancellationToken cancellationToken) =>
        scenarioState.Load<PosPaymentShare[]>("pos.payment-methods", () =>
        {
            var total = 1_846.40m * VolumeFactor(filter);
            return
            [
                new(nameof(WorkbenchStrings.PosPaymentCard), Math.Round(total * 0.57m, 2), "#4f6bed"),
                new(nameof(WorkbenchStrings.PosPaymentWallet), Math.Round(total * 0.28m, 2), "#0fb5ae"),
                new(nameof(WorkbenchStrings.PosPaymentCash), Math.Round(total * 0.11m, 2), "#f5a524"),
                new(nameof(WorkbenchStrings.PosPaymentGiftCard), Math.Round(total * 0.04m, 2), "#e5487d")
            ];
        }, () => [], cancellationToken);

    public Task<PosTopProduct[]> GetTopProducts(PosDashboardFilter filter, CancellationToken cancellationToken) =>
        scenarioState.Load<PosTopProduct[]>("pos.top-products", () =>
        {
            var random = CreateRandom(filter, "top-products");
            var factor = VolumeFactor(filter);
            return [.. products.Take(5).Select(p =>
            {
                var units = (int)Math.Round(p.Popularity * factor * (decimal)(0.9 + random.NextDouble() * 0.2));
                return new PosTopProduct(p.Key, p.Emoji, units, units * p.Price);
            }).OrderByDescending(p => p.Revenue)];
        }, () => [], cancellationToken);

    public Task<PosStockAlert[]> GetLowStock(PosDashboardFilter filter, CancellationToken cancellationToken) =>
        scenarioState.Load<PosStockAlert[]>("pos.low-stock", () =>
        [
            new(nameof(WorkbenchStrings.PosStockOatMilk), "🥛", 3, 24),
            new(nameof(WorkbenchStrings.PosStockCroissantDough), "🥐", 0, 40),
            new(nameof(WorkbenchStrings.PosStockCoffeeBeans), "🫘", 4, 12),
            new(nameof(WorkbenchStrings.PosStockCups), "🥤", 120, 500)
        ], () => [], cancellationToken);

    public Task<PosOrder[]> GetRecentOrders(PosDashboardFilter filter, CancellationToken cancellationToken) =>
        scenarioState.Load<PosOrder[]>("pos.recent-orders", () =>
        {
            var random = CreateRandom(filter, "recent-orders");
            var now = DateTimeOffset.Now;
            var payments = new[] { nameof(WorkbenchStrings.PosPaymentCard), nameof(WorkbenchStrings.PosPaymentWallet), nameof(WorkbenchStrings.PosPaymentCash) };
            return [.. Enumerable.Range(0, 8).Select(i =>
            {
                var items = random.Next(1, 6);
                var total = Math.Round(items * (decimal)(2.4 + random.NextDouble() * 3.1), 2);
                var status = i == 2 ? PosOrderStatus.Refunded : i == 0 ? PosOrderStatus.Pending : PosOrderStatus.Paid;
                return new PosOrder($"#{4817 - i * 3}", now.AddMinutes(-(i * 7 + random.Next(0, 5))), staff[i % staff.Length], items, total, payments[random.Next(payments.Length)], status);
            })];
        }, () => [], cancellationToken);

    public Task<PosStaffMember[]> GetStaff(PosDashboardFilter filter, CancellationToken cancellationToken) =>
        scenarioState.Load<PosStaffMember[]>("pos.staff", () =>
        {
            var random = CreateRandom(filter, "staff");
            var factor = VolumeFactor(filter);
            return [.. staff.Select((name, i) =>
            {
                var orders = (int)Math.Round((92 - i * 17) * factor * (decimal)(0.9 + random.NextDouble() * 0.2));
                return new PosStaffMember(name, Math.Round(orders * 7.02m, 2), orders);
            }).OrderByDescending(s => s.Sales)];
        }, () => [], cancellationToken);

    public Task<PosGoal> GetDailyGoal(PosDashboardFilter filter, CancellationToken cancellationToken) =>
        scenarioState.Load("pos.daily-goal", () =>
        {
            var factor = VolumeFactor(filter);
            return new PosGoal(Math.Round(2_500m * factor, 0), Math.Round(1_846.40m * factor, 2));
        }, () => new PosGoal(0, 0), cancellationToken);

    public Task<PosHeatCell[]> GetPeakHours(PosDashboardFilter filter, CancellationToken cancellationToken) =>
        scenarioState.Load<PosHeatCell[]>("pos.peak-hours", () =>
        {
            var random = CreateRandom(filter, "peak-hours");
            var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
            return [.. days.SelectMany(day => Enumerable.Range(7, 13).Select(hour =>
            {
                var weekend = day is DayOfWeek.Saturday or DayOfWeek.Sunday ? 1.35 : 1;
                return new PosHeatCell(day, hour, (int)Math.Round(Curve(hour) * 18 * weekend * (0.75 + random.NextDouble() * 0.5)));
            }))];
        }, () => [], cancellationToken);

    private static PosKpi Kpi(PosDashboardFilter filter, string source, decimal baseValue, decimal change, bool round = false, bool scaleByVolume = true)
    {
        var random = CreateRandom(filter, source);
        var value = scaleByVolume ? baseValue * VolumeFactor(filter) : baseValue * (decimal)(0.96 + random.NextDouble() * 0.08);
        var previous = value / (1 + change);

        if (round)
        {
            value = Math.Round(value);
            previous = Math.Round(previous);
        }

        var trend = Enumerable.Range(0, 12).Select(i => Math.Round(previous + (value - previous) * i / 11 + value * (decimal)((random.NextDouble() - 0.5) * 0.18), 2)).ToArray();

        return new PosKpi(Math.Round(value, 2), Math.Round(previous, 2), trend);
    }

    private static decimal VolumeFactor(PosDashboardFilter filter)
    {
        var store = filter.StoreKey switch
        {
            nameof(WorkbenchStrings.PosStoreAmsterdam) => 1.18m,
            nameof(WorkbenchStrings.PosStoreRotterdam) => 0.86m,
            nameof(WorkbenchStrings.PosStoreUtrecht) => 0.71m,
            _ => 2.75m
        };

        return store * filter.Days * (filter.Days == 1 ? 1 : 0.94m);
    }

    private static double Curve(int hour) => hour switch
    {
        7 => 0.55,
        8 => 1.0,
        9 => 0.92,
        10 => 0.64,
        11 => 0.7,
        12 => 0.98,
        13 => 0.88,
        14 => 0.58,
        15 => 0.66,
        16 => 0.74,
        17 => 0.6,
        18 => 0.42,
        19 => 0.3,
        _ => 0.2
    };

    private static Random CreateRandom(PosDashboardFilter filter, string source)
    {
        var seed = 17;

        foreach (var c in $"{source}|{filter.StoreKey}|{filter.Days}")
        {
            seed = unchecked(seed * 31 + c);
        }

        return new Random(seed);
    }
}
