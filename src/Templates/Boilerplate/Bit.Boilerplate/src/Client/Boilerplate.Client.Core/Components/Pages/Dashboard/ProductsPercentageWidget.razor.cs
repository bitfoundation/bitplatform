namespace Boilerplate.Client.Core.Components.Pages.Dashboard;

public partial class ProductsPercentageWidget
{
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
            var data = await PrerenderStateService.GetValue($"{nameof(DashboardPage)}-{nameof(ProductsPercentageWidget)}",
                                async () => await HttpClient.GetFromJsonAsync($"Dashboard/GetProductsPercentagePerCategoryStats",
                                    AppJsonContext.Default.ListProductPercentagePerCategoryResponseDto, CurrentCancellationToken)) ?? [];

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
