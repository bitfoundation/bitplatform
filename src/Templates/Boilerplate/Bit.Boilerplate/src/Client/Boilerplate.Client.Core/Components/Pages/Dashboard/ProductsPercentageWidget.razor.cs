//+:cnd:noEmit
using Boilerplate.Shared.Features.Dashboard;

namespace Boilerplate.Client.Core.Components.Pages.Dashboard;

public partial class ProductsPercentageWidget
{
    [AutoInject] IDashboardController dashboardController = default!;

    private bool isLoading;
    private bool hasLoadedOnce;
    private readonly BitChartConfig config = new()
    {
        Type = BitChartType.Pie,
        Options = new BitChartOptions
        {
            Plugins = new BitChartPluginOptions
            {
                Legend = new BitChartLegendOptions { Position = BitSide.Right }
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
            var data = await dashboardController.GetProductsPercentagePerCategoryStats(CurrentCancellationToken);

            config.Data = new()
            {
                Labels = [.. data.Select(d => d.CategoryName ?? string.Empty)],
                Datasets = [
                    new()
                    {
                        Data= [.. data.Select(d => (double?)d.ProductPercentage)],
                        // Only supply the colours when every category has one: BitChart returns a BackgroundColors
                        // entry verbatim with no empty check, so a single blank would paint that slice with fill=""
                        // instead of falling back to the built-in palette.
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
