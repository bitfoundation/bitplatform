namespace Boilerplate.Workbench.Components.Pages.Showcase.PosDashboard.Widgets;

public partial class PosKpiWidget
{
    private BitChartData trendData = new();

    private static readonly BitChartOptions trendOptions = new()
    {
        Sparkline = true,
        MaintainAspectRatio = false
    };

    [Parameter] public PosKpiMetric Metric { get; set; }

    private bool IsGoodChange => Metric is PosKpiMetric.Refunds ? Data!.Change <= 0 : Data!.Change >= 0;

    protected override Task<PosKpi> Fetch(PosDashboardFilter filter, CancellationToken cancellationToken) => Metric switch
    {
        PosKpiMetric.Transactions => DataService.GetTransactions(filter, cancellationToken),
        PosKpiMetric.AverageSale => DataService.GetAverageSale(filter, cancellationToken),
        PosKpiMetric.Refunds => DataService.GetRefunds(filter, cancellationToken),
        _ => DataService.GetNetSales(filter, cancellationToken)
    };

    protected override bool IsEmpty(PosKpi data) => data.Value == 0 && data.Trend.Length == 0;

    protected override void OnDataLoaded()
    {
        var color = IsGoodChange ? "#22a06b" : "#e5484d";

        trendData = new()
        {
            Labels = [.. Data!.Trend.Select((_, i) => i.ToString(CultureInfo.InvariantCulture))],
            Datasets =
            {
                new BitChartDataset
                {
                    Data = [.. Data!.Trend.Select(v => (double?)v)],
                    BorderColor = color,
                    BackgroundColor = color,
                    FillColor = $"{color}22",
                    Fill = BitChartFillMode.Origin,
                    Tension = 0.4,
                    BorderWidth = 2,
                    PointRadius = 0
                }
            }
        };
    }

    private string FormatValue(decimal value) => Metric switch
    {
        PosKpiMetric.Transactions => PosFormat.Number(value),
        _ => PosFormat.Money(value)
    };
}
