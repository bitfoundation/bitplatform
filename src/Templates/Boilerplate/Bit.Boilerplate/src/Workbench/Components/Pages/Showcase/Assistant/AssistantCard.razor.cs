namespace Boilerplate.Workbench.Components.Pages.Showcase.Assistant;

public partial class AssistantCard
{
    private const decimal weekTotal = 34_820m;
    private const decimal weekChange = 0.084m;

    private static readonly double[] dailySales = [4380, 4710, 4520, 4960, 5340, 6120, 4790];

    private static readonly (string NameKey, int Left)[] stockItems =
    [
        (nameof(WorkbenchStrings.AssistantStockOatMilk), 4),
        (nameof(WorkbenchStrings.AssistantStockCroissants), 6),
        (nameof(WorkbenchStrings.AssistantStockPaperCups), 1)
    ];

    private static readonly BitChartOptions salesOptions = new()
    {
        MaintainAspectRatio = false,
        Plugins = new() { Legend = new() { Display = false } },
        Scales = new() { ["y"] = new() { BeginAtZero = true } }
    };

    private readonly HashSet<string> orderedItems = [];
    private BitChartData salesData = new();
    private bool isCopied;

    [Parameter] public AssistantCardKind Kind { get; set; }

    protected override void OnInitialized()
    {
        var today = DateTime.Today;

        salesData = new()
        {
            Labels = [.. Enumerable.Range(0, dailySales.Length).Select(i => today.AddDays(i - dailySales.Length + 1).ToString("ddd", CultureInfo.CurrentCulture))],
            Datasets =
            {
                new BitChartDataset
                {
                    Data = [.. dailySales.Select(value => (double?)value)],
                    BackgroundColor = $"var({BitCss.Var.Color.Primary.Main})",
                    BorderRadius = 6,
                    MaxBarThickness = 28
                }
            }
        };
    }

    private async Task CopyPromo()
    {
        await JSRuntime.InvokeVoidAsync("workbench.copy", Localizer[nameof(WorkbenchStrings.AssistantPromoDraft)].Value);

        isCopied = true;
    }
}
