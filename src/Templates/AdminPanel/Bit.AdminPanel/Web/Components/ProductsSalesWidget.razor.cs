using AdminPanel.App.Pages.Dashboard;

namespace AdminPanel.App.Components;

public partial class ProductsSalesWidget
{
    [AutoInject] private HttpClient httpClient = default!;

    [AutoInject] private IStateService stateService = default!;

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

            var Data = await stateService.GetValue($"{nameof(HomePage)}-{nameof(ProductsSalesWidget)}", async () => await httpClient.GetFromJsonAsync($"Dashboard/GetProductsSalesStats", AppJsonContext.Default.ListProductSaleStatDto));

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
