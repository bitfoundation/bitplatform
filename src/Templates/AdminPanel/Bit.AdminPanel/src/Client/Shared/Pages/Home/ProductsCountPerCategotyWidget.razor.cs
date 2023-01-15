namespace AdminPanel.Client.Shared.Pages;

public partial class ProductsCountPerCategotyWidget
{

    private bool _isLoading;
    private BitChart? _chart;
    private BitChartBarConfig _config = default!;

    protected override async Task OnInitAsync()
    {
        _config = new BitChartBarConfig
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
            _isLoading = true;

            var data = await StateService.GetValue($"{nameof(HomePage)}-{nameof(ProductsCountPerCategotyWidget)}",
                                async () => await HttpClient.GetFromJsonAsync($"Dashboard/GetProductsCountPerCategotyStats",
                                    AppJsonContext.Default.ListProductsCountPerCategoryDto)) ?? new();

            BitChartBarDataset<int> chartDataSet = new BitChartBarDataset<int>();
            chartDataSet.AddRange(data.Select(d => d.ProductCount));
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
