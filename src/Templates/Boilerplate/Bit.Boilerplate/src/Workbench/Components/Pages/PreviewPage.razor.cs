namespace Boilerplate.Workbench.Components.Pages;

public partial class PreviewPage
{
    private bool isReadyReported;
    private WorkbenchPageInfo? pageInfo;

    [Parameter] public string? PageKey { get; set; }

    protected override void OnInitialized()
    {
        ScenarioState.Set(WorkbenchScenario.Parse(new Uri(NavigationManager.Uri).Query));
        ScenarioState.Settled += OnSettled;
    }

    protected override void OnParametersSet()
    {
        pageInfo = WorkbenchPageCatalog.Find(PageKey);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && (ScenarioState.IsSettled || Scenario.Data is WorkbenchDataState.Loading))
        {
            await ReportReady();
        }
    }

    private void OnSettled()
    {
        _ = InvokeAsync(async () =>
        {
            StateHasChanged();
            await Task.Delay(150);
            await ReportReady();
        });
    }

    private async Task ReportReady()
    {
        if (isReadyReported)
            return;

        isReadyReported = true;

        await JSRuntime.InvokeVoidAsync("workbench.markReady");
    }

    public override async ValueTask DisposeAsync()
    {
        ScenarioState.Settled -= OnSettled;

        await base.DisposeAsync();
    }
}
