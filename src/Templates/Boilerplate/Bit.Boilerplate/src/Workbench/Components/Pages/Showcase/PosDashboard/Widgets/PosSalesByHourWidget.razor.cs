using Boilerplate.Workbench.Components.Pages.Showcase.PosDashboard.Data;

namespace Boilerplate.Workbench.Components.Pages.Showcase.PosDashboard.Widgets;

public partial class PosSalesByHourWidget
{
    private static readonly int[] skeletonHeights = [35, 70, 62, 44, 52, 80, 66, 40, 48, 58, 42, 30];

    private BitChartData chartData = new();

    private readonly BitChartOptions chartOptions = new()
    {
        MaintainAspectRatio = false,
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Placement = BitPlacement.Bottom } },
        Scales =
        {
            ["x"] = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Category, Grid = new BitChartGridOptions { Display = false }, Ticks = new BitChartTickOptions { AutoSkip = true, MaxRotation = 0 } },
            ["y"] = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Linear, BeginAtZero = true, Ticks = new BitChartTickOptions { Prefix = "€", MaxTicksLimit = 5 } }
        }
    };

    protected override Task<PosHourlySales[]> Fetch(PosDashboardFilter filter, CancellationToken cancellationToken) => DataService.GetSalesByHour(filter, cancellationToken);

    protected override bool IsEmpty(PosHourlySales[] data) => data.Length == 0;

    protected override void OnDataLoaded()
    {
        chartData = new()
        {
            Labels = [.. Data!.Select(h => new DateTime(1, 1, 1, h.Hour, 0, 0).ToString("HH:mm", CultureInfo.CurrentCulture))],
            Datasets =
            {
                new BitChartDataset
                {
                    Label = Localizer[Dashboard.Filter.Days == 1 ? nameof(WorkbenchStrings.PosToday) : nameof(WorkbenchStrings.PosThisPeriod)],
                    Data = [.. Data!.Select(h => (double?)h.Today)],
                    BackgroundColor = "#4f6bed",
                    BorderRadius = 6,
                    MaxBarThickness = 22,
                    Order = 1
                },
                new BitChartDataset
                {
                    Type = BitChartType.Line,
                    Label = Localizer[nameof(WorkbenchStrings.PosLastWeek)],
                    Data = [.. Data!.Select(h => (double?)h.LastWeek)],
                    BorderColor = "#f5a524",
                    BackgroundColor = "#f5a524",
                    BorderDash = [5, 4],
                    BorderWidth = 2,
                    Tension = 0.4,
                    PointRadius = 0,
                    Order = 0
                }
            }
        };
    }
}
