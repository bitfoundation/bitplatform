using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace AdminPanel.App.Pages.Dashboard;

public partial class ProductsSalesWidget
{
    [AutoInject] private HttpClient httpClient = default!;

    [AutoInject] private IStateService stateService = default!;

    public bool IsLoading { get; set; }
    public ISeries[] Series { get; set; } = { };
    public Axis[] XAxis { get; set; } = { };

    protected override async Task OnInitAsync()
    {
        await GetData();
        await base.OnInitAsync();
    }

    private async Task GetData()
    {
        try
        {
            IsLoading = true;

            var Data = await stateService.GetValue($"{nameof(AnalyticsPage)}-{nameof(ProductsSalesWidget)}", async () => await httpClient.GetFromJsonAsync($"Dashboard/GetProductsSalesStats", AppJsonContext.Default.ListProductSaleStatDto));

            Series = new[]
            {
                new ColumnSeries<decimal>()
                {
                    Name = "",
                    DataLabelsSize=20,
                    Values = Data.Select(d => d.SaleAmount).ToArray()
                }
            };

            XAxis = new[]
            {
                new Axis
                {
                    Labels = Data.Select(d=>d.ProductName).ToArray(),
                    Position=AxisPosition.Start,
                    MaxLimit=20,
                    MinLimit=10,
                    MinStep = 0,
                    LabelsPaint = new SolidColorPaint(SKColors.Black),
                    TextSize = 10,
                }
            };
        }
        finally
        {
            IsLoading = false;
        }
    }
}
