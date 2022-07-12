using AdminPanel.Shared.Dtos.Dashboard;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace AdminPanel.App.Pages.Dashboard;

public partial class ProductsCountPerCategotyWidget
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
            var Data = await stateService.GetValue($"{nameof(AnalyticsPage)}-{nameof(ProductsCountPerCategotyWidget)}", async () => await httpClient.GetFromJsonAsync($"Dashboard/GetProductsCountPerCategotyStats", AppJsonContext.Default.ListProductsCountPerCategoryDto));
            Series = new ISeries[] {
                new ColumnSeries<int>()
                {
                    Name = "",
                    Values = Data.Select(d => d.ProductCount).ToArray()
                }
            };

            XAxis = new Axis[]{
                new Axis
                {
                    Labels = Data.Select(d=>d.CategoryName).ToArray(),
                }
            };


        }
        finally
        {
            IsLoading = false;
        }

    }

}
