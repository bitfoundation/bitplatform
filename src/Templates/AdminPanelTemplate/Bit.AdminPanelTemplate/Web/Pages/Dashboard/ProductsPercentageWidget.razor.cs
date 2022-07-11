using AdminPanelTemplate.Shared.Dtos.Dashboard;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace AdminPanelTemplate.App.Pages.Dashboard;

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
            Series = Data.Select(d => 
                    new PieSeries<double> { Values = new double[] { d.ProductPercentage }, Name = d.CategoryName }).ToArray();
        }
        finally
        {
            IsLoading = false;

        }

    }

}
