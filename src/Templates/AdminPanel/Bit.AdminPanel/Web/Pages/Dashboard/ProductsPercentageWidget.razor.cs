namespace AdminPanel.App.Pages.Dashboard;

public partial class ProductsPercentageWidget
{
    [AutoInject] private HttpClient httpClient = default!;

    [AutoInject] private IStateService stateService = default!;
    public bool IsLoading { get; set; }

    private BitChartPieConfig? _config;
    private BitChart? _chart;


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
        await base.OnInitAsync();
    }

    private async Task GetData()
    {
        try
        {
            IsLoading = true;

            var Data = await stateService.GetValue($"{nameof(AnalyticsPage)}-{nameof(ProductsPercentageWidget)}", async () => await httpClient.GetFromJsonAsync($"Dashboard/GetProductsPercentagePerCategoryStats", AppJsonContext.Default.ListProductPercentagePerCategoryDto));

            BitChartPieDataset<float> chartDataSet = new BitChartPieDataset<float>();
            chartDataSet.AddRange(Data!.Select(d => d.ProductPercentage));
            chartDataSet.BackgroundColor = Data.Select(d => d.CategoryColor).ToArray();
            _config!.Data.Datasets.Add(chartDataSet);
            _config.Data.Labels.AddRange(Data.Select(d => d.CategoryName));
        }
        finally
        {
            IsLoading = false;
        }
    }
}
