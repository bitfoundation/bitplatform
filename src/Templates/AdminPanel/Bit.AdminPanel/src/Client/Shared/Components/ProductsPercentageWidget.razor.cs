using AdminPanel.Client.Shared.Pages;

namespace AdminPanel.Client.Shared.Components;

public partial class ProductsPercentageWidget
{
    private BitChartPieConfig _config = default!;
    private bool _isLoading;

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
        try
        {
            _isLoading = true;

            var Data = await StateService.GetValue($"{nameof(HomePage)}-{nameof(ProductsPercentageWidget)}",
                async () => await HttpClient.GetFromJsonAsync($"Dashboard/GetProductsPercentagePerCategoryStats",
                                                                AppJsonContext.Default.ListProductPercentagePerCategoryDto));

            BitChartPieDataset<float> chartDataSet = new BitChartPieDataset<float>();
            chartDataSet.AddRange(Data!.Select(d => d.ProductPercentage));
            chartDataSet.BackgroundColor = Data.Select(d => d.CategoryColor).ToArray();
            _config.Data.Datasets.Add(chartDataSet);
            _config.Data.Labels.AddRange(Data.Select(d => d.CategoryName));
        }
        finally
        {
            _isLoading = false;
        }
    }
}
