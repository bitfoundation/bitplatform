using BlazorWeb.Shared.Dtos.Dashboard;

namespace BlazorWeb.Client.Pages.Dashboard;

public partial class OverallStatsWidget
{
    private bool isLoading;
    private OverallAnalyticsStatsDataDto data = new();

    protected override async Task OnInitAsync()
    {
        await GetData();
    }

    private async Task GetData()
    {
        isLoading = true;

        try
        {
            data = await PrerenderStateService.GetValue($"{nameof(DashboardPage)}-{nameof(OverallStatsWidget)}",
                            async () => await HttpClient.GetFromJsonAsync($"Dashboard/GetOverallAnalyticsStatsData",
                                AppJsonContext.Default.OverallAnalyticsStatsDataDto)) ?? new();
        }
        finally
        {
            isLoading = false;
        }
    }
}
