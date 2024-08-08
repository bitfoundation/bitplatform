using Boilerplate.Shared.Controllers.Dashboard;
using Boilerplate.Shared.Dtos.Dashboard;

namespace Boilerplate.Client.Core.Components.Pages.Dashboard;

public partial class OverallStatsWidget
{
    [AutoInject] IDashboardController dashboardController = default!;

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
            data = await dashboardController.GetOverallAnalyticsStatsData(CurrentCancellationToken);
        }
        finally
        {
            isLoading = false;
        }
    }
}
