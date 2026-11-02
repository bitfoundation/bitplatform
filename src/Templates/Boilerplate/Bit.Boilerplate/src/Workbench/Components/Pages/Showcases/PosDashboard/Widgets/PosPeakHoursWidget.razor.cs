namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard.Widgets;

/// <summary>A heat map of orders per weekday and hour; the week starts on the culture's first day.</summary>
public partial class PosPeakHoursWidget
{
    // The busiest cell's orders, which every cell's shade is relative to.
    private int maxOrders = 1;
    private int[] hours = [];
    private DayOfWeek[] days = [];
    private Dictionary<(DayOfWeek, int), int> cells = [];

    protected override Task<PosHeatCell[]> Fetch(PosDashboardFilter filter, CancellationToken cancellationToken) => DataService.GetPeakHours(filter, cancellationToken);

    protected override bool IsEmpty(PosHeatCell[] data) => data.Length == 0;

    protected override void OnDataLoaded()
    {
        cells = Data!.ToDictionary(c => (c.Day, c.Hour), c => c.Orders);
        hours = [.. Data!.Select(c => c.Hour).Distinct().Order()];

        var firstDay = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
        days = [.. Enumerable.Range(0, 7).Select(i => (DayOfWeek)(((int)firstDay + i) % 7))];

        maxOrders = Math.Max(1, cells.Values.Max());
    }

    private int Lookup(DayOfWeek day, int hour) => cells.GetValueOrDefault((day, hour));

    /// <summary>A cell's shade from 0 to 1, written for CSS whatever the culture.</summary>
    private string Level(int orders) => (orders / (double)maxOrders).ToString("0.00", CultureInfo.InvariantCulture);

    /// <summary>An hour as the culture writes it, in 24-hour form.</summary>
    private static string FormatHour(int hour) => new DateTime(1, 1, 1, hour, 0, 0).ToString("HH", CultureInfo.CurrentCulture);
}
