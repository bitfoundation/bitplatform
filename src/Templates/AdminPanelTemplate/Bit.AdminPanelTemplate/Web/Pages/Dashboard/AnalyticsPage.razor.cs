using AdminPanelTemplate.Shared.Dtos.Dashboard;
using Microsoft.AspNetCore.Components;

namespace AdminPanelTemplate.App.Pages.Dashboard;

public partial class AnalyticsPage
{
    [AutoInject] private HttpClient httpClient = default!;

    [AutoInject] private IStateService stateService = default!;

    public bool IsLoading { get; set; }

   

}
