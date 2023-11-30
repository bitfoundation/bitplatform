using Boilerplate.Shared.Dtos.Dashboard;

namespace Boilerplate.Client.Core.Components.Pages.Dashboard;

public partial class OverallStatsWidget
{
    private bool isLoading;
    private OverallAnalyticsStatsDataResponseDto data = new();

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
                                AppJsonContext.Default.OverallAnalyticsStatsDataResponseDto)) ?? new();
        }
        finally
        {
            isLoading = false;
        }
    }
}
