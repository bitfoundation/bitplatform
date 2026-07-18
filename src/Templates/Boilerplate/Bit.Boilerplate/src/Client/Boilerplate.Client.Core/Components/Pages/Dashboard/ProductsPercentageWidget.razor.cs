using Boilerplate.Shared.Features.Dashboard;

namespace Boilerplate.Client.Core.Components.Pages.Dashboard;

public partial class ProductsPercentageWidget
{
    [AutoInject] IDashboardController dashboardController = default!;

    private bool isLoading;
    private BitChartConfig config = default!;

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        config = new BitChartConfig
        {
            Type = BitChartType.Pie,
            Options = new BitChartOptions
            {
                Plugins = new BitChartPluginOptions
                {
                    Legend = new BitChartLegendOptions { Position = BitChartPosition.Right }
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
            var data = await dashboardController.GetProductsPercentagePerCategoryStats(CurrentCancellationToken);

            config.Data.Labels.AddRange(data.Select(d => d.CategoryName ?? string.Empty));
            config.Data.Datasets.Add(new BitChartDataset
            {
                Data = [.. data.Select(d => (double?)d.ProductPercentage)],
                BackgroundColors = [.. data.Select(d => d.CategoryColor ?? string.Empty)]
            });
        }
        finally
        {
            isLoading = false;
        }
    }
}
