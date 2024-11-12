using Boilerplate.Shared.Controllers.Dashboard;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Dashboard;

public partial class ProductsPercentageWidget
{
    [AutoInject] IDashboardController dashboardController = default!;

    private bool isLoading;
    private BitChartPieConfig config = default!;

    protected override async Task OnInitAsync()
    {
        config = new BitChartPieConfig
        {
            Options = new BitChartPieOptions
            {
                Responsive = true,
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

            BitChartPieDataset<float> chartDataSet = [.. data!.Select(d => d.ProductPercentage)];
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
