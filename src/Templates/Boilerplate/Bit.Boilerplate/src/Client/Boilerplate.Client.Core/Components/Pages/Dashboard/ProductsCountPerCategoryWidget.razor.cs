//+:cnd:noEmit
using Boilerplate.Shared.Features.Dashboard;

namespace Boilerplate.Client.Core.Components.Pages.Dashboard;

public partial class ProductsCountPerCategoryWidget
{
    [AutoInject] IDashboardController dashboardController = default!;

    private bool isLoading;
    private bool hasLoadedOnce;
    private readonly BitChartConfig config = new()
    {
        Type = BitChartType.Bar,
        Options = new BitChartOptions
        {
            Plugins = new BitChartPluginOptions
            {
                Legend = new BitChartLegendOptions { Display = false }
            },
            Scales =
            {
                ["y"] = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Linear, BeginAtZero = true }
            }
        }
    };
    //#if (signalR == true)
    private Action? unsubscribe;
    //#endif

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        //#if (signalR == true)
        // Instead of reloading the whole app, refresh only this widget's data when the dashboard changes.
        unsubscribe = PubSubService.Subscribe(SharedAppMessages.DASHBOARD_DATA_CHANGED, async _ => await InvokeAsync(GetData));
        //#endif

        await GetData();
    }

    private async Task GetData()
    {
        isLoading = true;
        StateHasChanged();

        try
        {
            var data = await dashboardController.GetProductsCountPerCategoryStats(CurrentCancellationToken);

            config.Data = new()
            {
                Labels = [.. data.Select(d => d.CategoryName ?? string.Empty)],
                Datasets = [
                    new()
                    {
                        Data= [.. data.Select(d => (double?)d.ProductCount)],
                        BackgroundColors = data.All(d => string.IsNullOrWhiteSpace(d.CategoryColor) is false)
                                        ? [.. data.Select(d => d.CategoryColor!)]
                                        : null
                    }]
            };
        }
        finally
        {
            isLoading = false;
            hasLoadedOnce = true;
            StateHasChanged();
        }
    }


    //#if (signalR == true)
    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await base.DisposeAsync(disposing);

        unsubscribe?.Invoke();
    }
    //#endif
}
