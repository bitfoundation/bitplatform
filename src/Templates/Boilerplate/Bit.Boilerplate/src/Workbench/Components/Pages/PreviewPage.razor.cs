namespace Boilerplate.Workbench.Components.Pages;

public partial class PreviewPage
{
    private bool isReadyReported;
    private WorkbenchPageInfo? pageInfo;

    [Parameter] public string? PageKey { get; set; }

    protected override void OnInitialized()
    {
        ScenarioState.Set(WorkbenchScenario.Parse(new Uri(NavigationManager.Uri).Query));
    }

    protected override void OnParametersSet()
    {
        pageInfo = WorkbenchPageCatalog.Find(PageKey);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            ScenarioState.Touch();
            _ = WaitUntilReady();
        }
    }

    private async Task WaitUntilReady()
    {
        var startedAt = DateTimeOffset.UtcNow;

        try
        {
            while (true)
            {
                await Task.Delay(100, CurrentCancellationToken);

                var isQuiet = DateTimeOffset.UtcNow - ScenarioState.LastActivity > TimeSpan.FromMilliseconds(450);
                var isSettled = Scenario.Data is WorkbenchDataState.Loading || ScenarioState.IsSettled;

                if ((isQuiet && isSettled) || DateTimeOffset.UtcNow - startedAt > TimeSpan.FromSeconds(20))
                    break;
            }

            await InvokeAsync(ReportReady);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task ReportReady()
    {
        if (isReadyReported)
            return;

        isReadyReported = true;

        await JSRuntime.InvokeVoidAsync("workbench.markReady");
    }

}
