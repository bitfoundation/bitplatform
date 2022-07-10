using AdminPanelTemplate.Shared.Dtos.Dashboard;
using Microsoft.AspNetCore.Components;

namespace AdminPanelTemplate.App.Pages.Dashboard;

public partial class OverallStatsWidget
{
    [AutoInject] private HttpClient httpClient = default!;

    [AutoInject] private IStateService stateService = default!;

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
            Data = await httpClient.GetFromJsonAsync($"Dashboard/GetOverallAnalyticsStatsData", AppJsonContext.Default.OverallAnalyticsStatsDataDto);
            
        }
        catch
        {
            

        }

    }
}
