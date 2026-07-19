//+:cnd:noEmit
using Boilerplate.Shared.Features.Dashboard;

namespace Boilerplate.Client.Core.Components.Pages.Dashboard;

public partial class OverallStatsWidget
{
    [AutoInject] IDashboardController dashboardController = default!;

    private bool isLoading;
    private OverallAnalyticsStatsDataResponseDto dto = new();
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
            dto = await dashboardController.GetOverallAnalyticsStatsData(CurrentCancellationToken);
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
