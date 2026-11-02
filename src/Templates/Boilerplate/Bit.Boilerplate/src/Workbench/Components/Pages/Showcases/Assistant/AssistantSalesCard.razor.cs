namespace Boilerplate.Workbench.Components.Pages.Showcases.Assistant;

/// <summary>
/// The card the assistant draws for its <c>sales_trend</c> generative UI: the week's net sales, the change and a bar
/// per day. Its parameters are bound from the reply's JSON.
/// </summary>
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
        // One label per value, ending today, as short weekday names in the view's language.
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
