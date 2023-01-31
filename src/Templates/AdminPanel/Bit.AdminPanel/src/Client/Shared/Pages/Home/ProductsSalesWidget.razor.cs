namespace AdminPanel.Client.Shared.Pages;

public partial class ProductsSalesWidget
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

            var data = await StateService.GetValue($"{nameof(HomePage)}-{nameof(ProductsSalesWidget)}", 
                                async () => await HttpClient.GetFromJsonAsync($"Dashboard/GetProductsSalesStats", 
                                    AppJsonContext.Default.ListProductSaleStatDto)) ?? new();

            BitChartBarDataset<decimal> chartDataSet = new BitChartBarDataset<decimal>();
            chartDataSet.AddRange(data.Select(d => d.SaleAmount));
            chartDataSet.BackgroundColor = data.Select(d => d.CategoryColor ?? string.Empty).ToArray();
            _config.Data.Datasets.Add(chartDataSet);
            _config.Data.Labels.AddRange(data.Select(d => d.ProductName ?? string.Empty));
        }
        finally
        {
            _isLoading = false;
        }
    }
}
