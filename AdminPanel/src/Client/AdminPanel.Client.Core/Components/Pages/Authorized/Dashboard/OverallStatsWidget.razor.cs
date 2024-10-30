using AdminPanel.Shared.Controllers.Dashboard;
using AdminPanel.Shared.Dtos.Dashboard;

namespace AdminPanel.Client.Core.Components.Pages.Authorized.Dashboard;

public partial class OverallStatsWidget
{
    [AutoInject] IDashboardController dashboardController = default!;

    private bool isLoading;
    private OverallAnalyticsStatsDataResponseDto dto = new();

    protected override async Task OnInitAsync()
    {
        await GetData();
    }

    private async Task GetData()
    {
        isLoading = true;

        try
        {
            dto = await dashboardController.GetOverallAnalyticsStatsData(CurrentCancellationToken);
        }
        finally
        {
            isLoading = false;
        }
    }
}
