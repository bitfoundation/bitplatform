using AdminPanel.Shared.Dtos.Dashboard;

namespace AdminPanel.Client.Shared.Pages;

public partial class OverallStatsWidget
{
    private bool _isLoading;
    private OverallAnalyticsStatsDataDto _data = default!;

    protected override async Task OnInitAsync()
    {
        await GetData();
    }

    private async Task GetData()
    {
        _isLoading = true;

        try
        {
            _data = await StateService.GetValue($"{nameof(HomePage)}-{nameof(OverallStatsWidget)}",
                            async () => await HttpClient.GetFromJsonAsync($"Dashboard/GetOverallAnalyticsStatsData",
                                AppJsonContext.Default.OverallAnalyticsStatsDataDto)) ?? new();
        }
        finally
        {
            _isLoading = false;
        }
    }
}
