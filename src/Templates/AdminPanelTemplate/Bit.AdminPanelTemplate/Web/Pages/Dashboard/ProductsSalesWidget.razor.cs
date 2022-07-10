using AdminPanelTemplate.Shared.Dtos.Dashboard;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace AdminPanelTemplate.App.Pages.Dashboard;

public partial class ProductsSalesWidget
{
    [AutoInject] private HttpClient httpClient = default!;

    [AutoInject] private IStateService stateService = default!;

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
            var Data = await httpClient.GetFromJsonAsync($"Dashboard/GetProductsSalesStats", AppJsonContext.Default.ListProductSaleStatDto);


            Series = new ISeries[] {
                new ColumnSeries<decimal>()
                {
                    Name = "",
                    Values = Data.Select(d => d.SaleAmount).ToArray()
                }
            };

            XAxis = new Axis[]{
                new Axis
                {
                    Labels = Data.Select(d=>d.ProductName).ToArray(),
                    Labeler = Labelers.Currency
                }
            };


        }
        catch
        {


        }

    }

}
