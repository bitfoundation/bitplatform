namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard.Widgets;

/// <summary>A KPI card: the value, its change from the previous period and a sparkline, colored by whether the change is good.</summary>
public partial class PosKpiWidget
{
    private BitChartData trendData = new();

    private static readonly BitChartOptions trendOptions = new()
    {
        Sparkline = true,
        MaintainAspectRatio = false
    };

    [Parameter] public PosKpiMetric Metric { get; set; }

    /// <summary>Whether the change is good news: growth, or a fall for refunds.</summary>
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
        var color = $"var({(IsGoodChange ? BitCss.Var.Color.Success.Main : BitCss.Var.Color.Error.Main)})";

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
                    FillColor = BitChartColorUtil.Translucent(color, 0.13),
                    Fill = BitChartFillMode.Origin,
                    Tension = 0.4,
                    BorderWidth = 2,
                    PointRadius = 0
                }
            }
        };
    }

    /// <summary>Transactions are a count; the other metrics are money.</summary>
    private string FormatValue(decimal value) => Metric switch
    {
        PosKpiMetric.Transactions => PosFormat.Number(value),
        _ => PosFormat.Money(value)
    };
}
