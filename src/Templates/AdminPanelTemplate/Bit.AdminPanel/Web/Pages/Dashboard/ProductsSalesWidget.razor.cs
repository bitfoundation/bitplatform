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
            Series = new ISeries[] {
                new ColumnSeries<decimal>()
                {
                    Name = "",
                    DataLabelsSize=20,
                    Values = Data.Select(d => d.SaleAmount).ToArray()
                }
            };

            XAxis = new Axis[]{
                new Axis
                {
                    Labels = Data.Select(d=>d.ProductName).ToArray(),
                    //Labels = Data.Select(d=>d.ProductName[0..Math.Min(d.ProductName.Length, 5)]).ToArray(),
                    //Labeler = Labelers.Currency,
                    Position=AxisPosition.Start,
                    MaxLimit=20,
                    MinLimit=10,
                    MinStep = 0,
                    //LabelsRotation=-90,
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
