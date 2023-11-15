using BlazorWeb.Shared.Dtos.Dashboard;

namespace BlazorWeb.Client.Pages.Dashboard;

public partial class OverallStatsWidget
{
    private bool _isLoading;
    private OverallAnalyticsStatsDataDto _data = new();

    protected override async Task OnInitAsync()
    {
        await GetData();
    }

    private async Task GetData()
    {
        _isLoading = true;

        try
        {
            _data = await PrerenderStateService.GetValue($"{nameof(DashboardPage)}-{nameof(OverallStatsWidget)}",
                            async () => await HttpClient.GetFromJsonAsync($"Dashboard/GetOverallAnalyticsStatsData",
                                AppJsonContext.Default.OverallAnalyticsStatsDataDto)) ?? new();
        }
        finally
        {
            _isLoading = false;
        }
    }
}
