using Boilerplate.Shared.Controllers.Dashboard;

namespace Boilerplate.Client.Core.Components.Pages.Dashboard;

public partial class ProductsSalesWidget
{
    [AutoInject] IDashboardController dashboardController = default!;

    private bool isLoading;
    private BitChart? chart;
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
        try
        {
            isLoading = true;

            var data = await dashboardController.GetProductsSalesStats(CurrentCancellationToken);

            BitChartBarDataset<decimal> chartDataSet = [.. data.Select(d => d.SaleAmount)];
            chartDataSet.BackgroundColor = data.Select(d => d.CategoryColor ?? string.Empty).ToArray();
            config.Data.Datasets.Add(chartDataSet);
            config.Data.Labels.AddRange(data.Select(d => d.ProductName ?? string.Empty));
        }
        finally
        {
            isLoading = false;
        }
    }
}
