namespace Boilerplate.Workbench.Components.Pages.AppPages.Dashboard;

/// <summary>The Workbench copy of the app's products per category chart, on <c>DashboardDemoData</c> through the scenario state.</summary>
public partial class ProductsCountPerCategoryWidget
{
    private bool isLoading = true;
    private bool isFailed;
    private bool hasLoadedOnce;
    private readonly BitChartConfig config = new()
    {
        Type = BitChartType.Bar,
        Options = new BitChartOptions
        {
            AspectRatio = 1.4,
            Culture = CultureInfo.CurrentCulture,
            Plugins = new BitChartPluginOptions
            {
                Legend = new BitChartLegendOptions { Display = false }
            },
            Scales =
            {
                ["y"] = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Linear, BeginAtZero = true }
            }
        }
    };

    protected override async Task OnInitializedAsync()
    {
        if (IsPrerendering)
            return;

        await GetData();
    }

    private async Task GetData()
    {
        isLoading = true;
        isFailed = false;
        StateHasChanged();

        try
        {
            var data = await ScenarioState.Load("dashboard.products-per-category", () => DashboardDemoData.Categories, () => [], CurrentCancellationToken);

            config.Data = new()
            {
                Labels = [.. data.Select(d => d.Name)],
                Datasets =
                [
                    new()
                    {
                        Data = [.. data.Select(d => (double?)d.ProductCount)],
                        BackgroundColors = [.. data.Select(d => d.Color)]
                    }
                ]
            };

            hasLoadedOnce = true;
        }
        catch (WorkbenchSimulatedException)
        {
            isFailed = true;
        }
        catch (OperationCanceledException)
        {
            return;
        }

        isLoading = false;
        StateHasChanged();
    }
}
