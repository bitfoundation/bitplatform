using Boilerplate.Shared.Features.Dashboard;

namespace Boilerplate.Client.Core.Components.Pages.Dashboard;

public partial class ProductsCountPerCategoryWidget
{
    [AutoInject] IDashboardController dashboardController = default!;

    private bool isLoading;
    private BitChartConfig config = default!;

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        config = new BitChartConfig
        {
            Type = BitChartType.Bar,
            Options = new BitChartOptions
            {
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

        await GetData();
    }

    private async Task GetData()
    {
        isLoading = true;

        try
        {
            var data = await dashboardController.GetProductsCountPerCategoryStats(CurrentCancellationToken);

            config.Data.Labels.AddRange(data.Select(d => d.CategoryName ?? string.Empty));
            config.Data.Datasets.Add(new BitChartDataset
            {
                Data = [.. data.Select(d => (double?)d.ProductCount)],
                BackgroundColors = [.. data.Select(d => d.CategoryColor ?? string.Empty)]
            });
        }
        finally
        {
            isLoading = false;
        }
    }
}
