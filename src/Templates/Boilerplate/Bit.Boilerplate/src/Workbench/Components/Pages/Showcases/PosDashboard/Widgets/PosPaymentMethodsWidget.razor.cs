namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard.Widgets;

public partial class PosPaymentMethodsWidget
{
    private decimal total;
    private BitChartData chartData = new();

    private static readonly BitChartOptions chartOptions = new()
    {
        MaintainAspectRatio = false,
        CutoutPercentage = 72,
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } }
    };

    protected override Task<PosPaymentShare[]> Fetch(PosDashboardFilter filter, CancellationToken cancellationToken) => DataService.GetPaymentMethods(filter, cancellationToken);

    protected override bool IsEmpty(PosPaymentShare[] data) => data.Length == 0;

    protected override void OnDataLoaded()
    {
        total = Data!.Sum(s => s.Amount);

        chartData = new()
        {
            Labels = [.. Data!.Select(s => Localizer[s.MethodKey].Value)],
            Datasets =
            {
                new BitChartDataset
                {
                    Label = Localizer[Definition.TitleKey],
                    Data = [.. Data!.Select(s => (double?)s.Amount)],
                    BackgroundColors = [.. Data!.Select(s => s.Color)],
                    BorderWidth = 0,
                    SpacingArc = 2
                }
            }
        };
    }
}
