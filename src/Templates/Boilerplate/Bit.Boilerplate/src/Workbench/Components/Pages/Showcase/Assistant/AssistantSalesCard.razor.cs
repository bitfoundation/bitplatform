namespace Boilerplate.Workbench.Components.Pages.Showcase.Assistant;

public partial class AssistantSalesCard
{
    private static readonly BitChartOptions options = new()
    {
        MaintainAspectRatio = false,
        Plugins = new() { Legend = new() { Display = false } },
        Scales = new() { ["y"] = new() { BeginAtZero = true } }
    };

    private BitChartData data = new();

    [Parameter] public decimal Total { get; set; }

    [Parameter] public decimal Change { get; set; }

    [Parameter] public double[] Days { get; set; } = [];

    protected override void OnParametersSet()
    {
        var today = DateTime.Today;

        data = new()
        {
            Labels = [.. Enumerable.Range(0, Days.Length).Select(i => today.AddDays(i - Days.Length + 1).ToString("ddd", CultureInfo.CurrentCulture))],
            Datasets =
            {
                new BitChartDataset
                {
                    Data = [.. Days.Select(value => (double?)value)],
                    BackgroundColor = $"var({BitCss.Var.Color.Primary.Main})",
                    BorderRadius = 6,
                    MaxBarThickness = 28
                }
            }
        };
    }
}
