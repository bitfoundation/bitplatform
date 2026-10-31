namespace Boilerplate.Workbench.Components.Pages.AppPages.Dashboard;

public partial class ProductsPercentageWidget
{
    private bool isLoading = true;
    private bool isFailed;
    private bool hasLoadedOnce;
    private readonly BitChartConfig config = new()
    {
        Type = BitChartType.Pie,
        Options = new BitChartOptions
        {
            AspectRatio = 1.1,
            Culture = CultureInfo.CurrentCulture,
            Plugins = new BitChartPluginOptions
            {
                Legend = new BitChartLegendOptions { Placement = BitPlacement.Right },
                Tooltip = new() { LabelFormatter = (_, value) => (value / 100).ToString("P1", CultureInfo.CurrentCulture) }
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
            var data = await ScenarioState.Load("dashboard.products-percentage", () => DashboardDemoData.Categories, () => [], CurrentCancellationToken);
            var total = Math.Max(1, data.Sum(d => d.ProductCount));

            config.Data = new()
            {
                Labels = [.. data.Select(d => d.Name)],
                Datasets =
                [
                    new()
                    {
                        Data = [.. data.Select(d => (double?)Math.Round(d.ProductCount * 100d / total, 1))],
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
