namespace AdminPanel.App.Pages.Dashboard;

public partial class ProductsSalesWidget
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

            var Data = await stateService.GetValue($"{nameof(AnalyticsPage)}-{nameof(ProductsSalesWidget)}", async () => await httpClient.GetFromJsonAsync($"Dashboard/GetProductsSalesStats", AppJsonContext.Default.ListProductSaleStatDto));

            BitChartBarDataset<decimal> chartDataSet = new BitChartBarDataset<decimal>();
            chartDataSet.AddRange(Data!.Select(d => d.SaleAmount));
            //chartDataSet.BackgroundColor = Data.Select(d => d.CategoryColor).ToArray();
            _config!.Data.Datasets.Add(chartDataSet);
            _config.Data.Labels.AddRange(Data.Select(d => d.ProductName));

            //Series = new[]
            //{
            //    new ColumnSeries<decimal>()
            //    {
            //        Name = "",
            //        DataLabelsSize=20,
            //        Values = Data.Select(d => d.SaleAmount).ToArray()
            //    }
            //};

            //XAxis = new[]
            //{
            //    new Axis
            //    {
            //        Labels = Data.Select(d=>d.ProductName).ToArray(),
            //        Position=AxisPosition.Start,
            //        MaxLimit=20,
            //        MinLimit=10,
            //        MinStep = 0,
            //        LabelsPaint = new SolidColorPaint(SKColors.Black),
            //        TextSize = 10,
            //    }
            //};
        }
        finally
        {
            IsLoading = false;
        }
    }
}
