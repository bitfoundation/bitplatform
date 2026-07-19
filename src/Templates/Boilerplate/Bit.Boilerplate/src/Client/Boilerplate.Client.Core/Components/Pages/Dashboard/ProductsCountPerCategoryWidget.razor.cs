//+:cnd:noEmit
using Boilerplate.Shared.Features.Dashboard;

namespace Boilerplate.Client.Core.Components.Pages.Dashboard;

public partial class ProductsCountPerCategoryWidget
{
    [AutoInject] IDashboardController dashboardController = default!;

    private bool isLoading;
    private BitChartConfig config = default!;
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
            // A fresh config is built on each load so re-fetches replace the previous data instead of appending to it.
            config = new BitChartConfig
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

            var data = await dashboardController.GetProductsCountPerCategoryStats(CurrentCancellationToken);

            config.Data.Labels.AddRange(data.Select(d => d.CategoryName ?? string.Empty));
            config.Data.Datasets.Add(new BitChartDataset
            {
                Data = [.. data.Select(d => (double?)d.ProductCount)],
                BackgroundColors = [.. data.Select(d => d.CategoryColor ?? string.Empty)]
            });
        }
        finally
        {
            isLoading = false;
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
