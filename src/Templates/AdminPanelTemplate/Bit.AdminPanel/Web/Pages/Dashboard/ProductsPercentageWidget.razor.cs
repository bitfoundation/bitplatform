using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace AdminPanel.App.Pages.Dashboard;

public partial class ProductsPercentageWidget
{
    [AutoInject] private HttpClient httpClient = default!;

    [AutoInject] private IStateService stateService = default!;
    public bool IsLoading { get; set; }

    public ISeries[] Series { get; set; } = { };

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

            var Data = await stateService.GetValue($"{nameof(AnalyticsPage)}-{nameof(ProductsPercentageWidget)}", async () => await httpClient.GetFromJsonAsync($"Dashboard/GetProductsPercentagePerCategoryStats", AppJsonContext.Default.ListProductPercentagePerCategoryDto));

            Series = Data!.Select(d =>
                    new PieSeries<float>
                    {
                        Values = new float[] { d.ProductPercentage },
                        Name = d.CategoryName,
                        Fill = new SolidColorPaint(SKColor.Parse(d.CategoryColor)),
                        DataLabelsPosition = PolarLabelsPosition.Start,
                        DataLabelsFormatter = point => point.PrimaryValue.ToString("N1"),
                        DataLabelsPaint = new SolidColorPaint(SKColors.Black)
                    }).ToArray();
        }
        finally
        {
            IsLoading = false;
        }
    }
}
