using AdminPanel.App.Pages.Dashboard;
using AdminPanel.Shared.Dtos.Dashboard;

namespace AdminPanel.App.Components;

public partial class OverallStatsWidget
{
    [AutoInject] private HttpClient httpClient = default!;

    [AutoInject] private IStateService stateService = default!;

    public bool IsLoading { get; set; }
    public OverallAnalyticsStatsDataDto Data { get; set; } = new OverallAnalyticsStatsDataDto();

    protected override async Task OnInitAsync()
    {
        await GetData();
        await base.OnInitAsync();
    }

    private async Task GetData()
    {
        try
        {
            IsLoading = true;
            Data = await stateService.GetValue($"{nameof(HomePage)}-{nameof(OverallStatsWidget)}", async () => await httpClient.GetFromJsonAsync($"Dashboard/GetOverallAnalyticsStatsData", AppJsonContext.Default.OverallAnalyticsStatsDataDto));
        }
        finally
        {
            IsLoading = false;
        }
    }
}
