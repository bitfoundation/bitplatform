namespace AdminPanelTemplate.App.Pages.Dashboard;

public partial class AnalyticsPage
{
    [AutoInject] private HttpClient httpClient = default!;

    [AutoInject] private IStateService stateService = default!;

    public bool IsLoading { get; set; }


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();
    }




}
