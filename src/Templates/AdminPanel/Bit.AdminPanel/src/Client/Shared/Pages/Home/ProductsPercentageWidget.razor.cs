namespace AdminPanel.Client.Shared.Pages;

public partial class ProductsPercentageWidget
{
    private bool _isLoading;
    private BitChartPieConfig _config = default!;

    protected override async Task OnInitAsync()
    {
        _config = new BitChartPieConfig
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
        _isLoading = true;

        try
        {
            var data = await StateService.GetValue($"{nameof(HomePage)}-{nameof(ProductsPercentageWidget)}",
                                async () => await HttpClient.GetFromJsonAsync($"Dashboard/GetProductsPercentagePerCategoryStats",
                                    AppJsonContext.Default.ListProductPercentagePerCategoryDto)) ?? new();

            BitChartPieDataset<float> chartDataSet = new BitChartPieDataset<float>();
            chartDataSet.AddRange(data!.Select(d => d.ProductPercentage));
            chartDataSet.BackgroundColor = data.Select(d => d.CategoryColor ?? string.Empty).ToArray();
            _config.Data.Datasets.Add(chartDataSet);
            _config.Data.Labels.AddRange(data.Select(d => d.CategoryName ?? string.Empty));
        }
        finally
        {
            _isLoading = false;
        }
    }
}
