using AdminPanel.Client.Shared.Pages;

namespace AdminPanel.Client.Shared.Components;

public partial class ProductsSalesWidget
{
    public bool IsLoading { get; set; }
    private BitChartBarConfig _config = default!;
    private BitChart? _chart;

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
        await base.OnInitAsync();
    }

    private async Task GetData()
    {
        try
        {
            IsLoading = true;

            var Data = await StateService.GetValue($"{nameof(HomePage)}-{nameof(ProductsSalesWidget)}", async () => await HttpClient.GetFromJsonAsync($"Dashboard/GetProductsSalesStats", AppJsonContext.Default.ListProductSaleStatDto));

            BitChartBarDataset<decimal> chartDataSet = new BitChartBarDataset<decimal>();
            chartDataSet.AddRange(Data.Select(d => d.SaleAmount));
            chartDataSet.BackgroundColor = Data.Select(d => d.CategoryColor).ToArray();
            _config.Data.Datasets.Add(chartDataSet);
            _config.Data.Labels.AddRange(Data.Select(d => d.ProductName));
        }
        finally
        {
            IsLoading = false;
        }
    }
}
