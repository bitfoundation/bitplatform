namespace Boilerplate.Workbench.Components.Pages;

public partial class PreviewPage
{
    private bool isReadyReported;
    private WorkbenchPageInfo? pageInfo;
    private DotNetObjectReference<PreviewPage>? reference;

    [AutoInject] private BlexManager blexManager = default!;
    [AutoInject] private WorkbenchSnapshots snapshots = default!;
    [AutoInject] private BitAccentColorService accentColorService = default!;

    [Parameter] public string? PageKey { get; set; }

    protected override void OnInitialized()
    {
        ScenarioState.Set(WorkbenchScenario.Parse(new Uri(NavigationManager.Uri).Query));

        if (snapshots.Find(WorkbenchUrls.ReadQuery(NavigationManager.Uri, WorkbenchUrls.SnapshotQueryKey)) is { } state)
        {
            blexManager.RestoreGlobalState(state);
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        var previous = pageInfo;

        pageInfo = WorkbenchPageCatalog.Find(PageKey);

        if (previous is not null && pageInfo is not null && previous != pageInfo)
        {
            await JSRuntime.InvokeVoidAsync("workbench.reportPage", pageInfo.Key);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender is false)
            return;

        reference = DotNetObjectReference.Create(this);
        await JSRuntime.InvokeVoidAsync("workbench.registerPreview", reference);

        await WhenStateRestored();
        await accentColorService.InitializeAsync();

        ScenarioState.Touch();
        _ = WaitUntilReady();
    }

    [JSInvokable]
    public string CaptureState() => blexManager.CaptureGlobalState().ToJsonString();

    [JSInvokable]
    public void ResetState() => blexManager.ResetAll();

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

    public override async ValueTask DisposeAsync()
    {
        reference?.Dispose();

        await base.DisposeAsync();
    }
}
