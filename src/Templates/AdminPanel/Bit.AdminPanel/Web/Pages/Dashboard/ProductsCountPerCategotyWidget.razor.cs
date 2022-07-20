namespace AdminPanel.App.Pages.Dashboard;

public partial class ProductsCountPerCategotyWidget
{
    [AutoInject] private HttpClient httpClient = default!;

    [AutoInject] private IStateService stateService = default!;

    public bool IsLoading { get; set; }

    private BitChartBarConfig? _config;
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

            var data = await stateService.GetValue($"{nameof(AnalyticsPage)}-{nameof(ProductsCountPerCategotyWidget)}", async () => await httpClient.GetFromJsonAsync($"Dashboard/GetProductsCountPerCategotyStats", AppJsonContext.Default.ListProductsCountPerCategoryDto));

            BitChartBarDataset<int> chartDataSet = new BitChartBarDataset<int>();
            chartDataSet.AddRange(data!.Select(d => d.ProductCount));
            chartDataSet.BackgroundColor = data.Select(d => d.CategoryColor).ToArray();
            _config!.Data.Datasets.Add(chartDataSet);
            _config.Data.Labels.AddRange(data.Select(d => d.CategoryName));
        }
        finally
        {
            IsLoading = false;
        }
    }

}
