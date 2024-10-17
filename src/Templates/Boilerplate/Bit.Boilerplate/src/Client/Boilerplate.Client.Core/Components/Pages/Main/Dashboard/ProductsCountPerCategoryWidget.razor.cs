using Boilerplate.Shared.Controllers.Dashboard;

namespace Boilerplate.Client.Core.Components.Pages.Main.Dashboard;

public partial class ProductsCountPerCategoryWidget
{
    [AutoInject] IDashboardController dashboardController = default!;

    private bool isLoading;
    private BitChartBarConfig config = default!;

    protected override async Task OnInitAsync()
    {
        config = new BitChartBarConfig
        {
            Options = new BitChartBarOptions
            {
                Responsive = true,
                Legend = new BitChartLegend()
                {
                    Display = false,
                },
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

            BitChartBarDataset<int> chartDataSet = [.. data.Select(d => d.ProductCount)];
            chartDataSet.BackgroundColor = data.Select(d => d.CategoryColor ?? string.Empty).ToArray();
            config.Data.Datasets.Add(chartDataSet);
            config.Data.Labels.AddRange(data.Select(d => d.CategoryName ?? string.Empty));
        }
        finally
        {
            isLoading = false;
        }
    }

}
