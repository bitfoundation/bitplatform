using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

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

            var data = await stateService.GetValue($"{nameof(AnalyticsPage)}-{nameof(ProductsCountPerCategotyWidget)}", async () => await httpClient.GetFromJsonAsync($"Dashboard/GetProductsCountPerCategotyStats", AppJsonContext.Default.ListProductsCountPerCategoryDto));

            Series = new[] {
                new ColumnSeries<int>()
                {
                    Name = "",
                    Values = data.Select(d => d.ProductCount).ToArray()
                }
            };

            XAxis = new[]{
                new Axis
                {
                    Labels = data.Select(d => d.CategoryName).ToArray(),
                }
            };
        }
        finally
        {
            IsLoading = false;
        }
    }

}
