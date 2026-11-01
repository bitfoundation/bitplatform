using System.Text.Json;
using System.Text.Json.Nodes;

namespace Boilerplate.Workbench.Components.Pages;

/// <summary>
/// /preview/{key}: one page alone with the scenario in its query string. Each view of the shell, each flow screen
/// and each screenshot loads one; it tells its host when it has settled, and lets the host read or reset its state.
/// </summary>
public partial class PreviewPage
{
    /// <summary>
    /// A snapshot id that starts with this names a state saved with the review (a flow screen's), read from disk
    /// instead of the snapshots in memory, so the link keeps working after they expire.
    /// </summary>
    public const string SavedStatePrefix = "saved-";

    private bool isReadyReported;
    // The start-up (the saved stores and the accent color) is over, failed or not, so the page may count as settled.
    private bool isStarted;
    private WorkbenchPageInfo? pageInfo;
    private DotNetObjectReference<PreviewPage>? reference;

    [AutoInject] private BlexManager blexManager = default!;
    [AutoInject] private WorkbenchSnapshots snapshots = default!;
    [AutoInject] private BitAccentColorService accentColorService = default!;
    [AutoInject] private WorkbenchTextRecorder textRecorder = default!;
    [AutoInject] private WorkbenchReviewAttachments attachments = default!;
    [AutoInject] private WorkbenchComponentInstances componentInstances = default!;

    [Parameter] public string? PageKey { get; set; }

    protected override async Task OnInitializedAsync()
    {
        ScenarioState.Set(WorkbenchScenario.Parse(new Uri(NavigationManager.Uri).Query));

        var snapshotId = WorkbenchUrls.ReadQuery(NavigationManager.Uri, WorkbenchUrls.SnapshotQueryKey);

        // A snapshot carries the stores of a view as it was when a screenshot, comment or flow screen was taken.
        if (snapshots.Find(snapshotId) is { } state)
        {
            blexManager.RestoreGlobalState(state);
        }
        else if (snapshotId?.StartsWith(SavedStatePrefix, StringComparison.Ordinal) is true && await ReadSavedState(snapshotId[SavedStatePrefix.Length..]) is { } saved)
        {
            blexManager.RestoreGlobalState(saved);
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        var previous = pageInfo;

        pageInfo = WorkbenchPageCatalog.Find(PageKey);

        // The app's navigation moved this preview to another page: the shell follows it.
        if (previous is not null && pageInfo is not null && previous != pageInfo)
        {
            await JSRuntime.InvokeVoidAsync("workbench.reportPage", pageInfo.Key);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender is false)
            return;

        // Started before anything that can fail or hang, so the host always hears from this preview within 20 seconds.
        _ = WaitUntilReady();

        reference = DotNetObjectReference.Create(this);

        try
        {
            await JSRuntime.InvokeVoidAsync("workbench.registerPreview", reference);

            // The page counts as settled only once the saved stores and the view's accent color are in place.
            await WhenStateRestored();
            await accentColorService.InitializeAsync();
        }
        finally
        {
            isStarted = true;
            ScenarioState.Touch();
        }
    }

    /// <summary>Every Blex store of this preview as JSON, for a screenshot, a comment or a flow screen to restore.</summary>
    [JSInvokable]
    public string CaptureState() => blexManager.CaptureGlobalState().ToJsonString();

    [JSInvokable]
    public void ResetState() => blexManager.ResetAll();

    /// <summary>The resource entries this preview rendered as the given text, for Edit text mode.</summary>
    [JSInvokable]
    public WorkbenchTextMatch[] FindTexts(string text) => textRecorder.Find(text, CultureInfo.CurrentUICulture.Name);

    /// <summary>
    /// The bit BlazorUI component of this preview whose root element has the id, with its parameters and its tag in the code,
    /// for Inspect mode; null when it's gone. It runs here, on the preview's circuit, as it reads that circuit's render tree.
    /// </summary>
    [JSInvokable]
    public WorkbenchComponentInfo? DescribeComponent(string elementId)
    {
        return componentInstances.Find(elementId) is { } component && WorkbenchComponentSources.RendererOf(component) == WorkbenchComponentSources.RendererOf(this)
            ? WorkbenchComponentParameters.Describe(component, elementId)
            : null;
    }

    /// <summary>
    /// Waits until the start-up is over, no load is pending and nothing happened for 450 ms (a Loading scenario never
    /// settles, so it only waits for quiet), or 20 seconds at most, then reports the preview ready.
    /// </summary>
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

                if ((isStarted && isQuiet && isSettled) || DateTimeOffset.UtcNow - startedAt > TimeSpan.FromSeconds(20))
                    break;
            }

            await InvokeAsync(ReportReady);
        }
        catch (Exception exception) when (exception is OperationCanceledException or JSDisconnectedException)
        {
            // The preview went away first: its frame was reloaded or closed.
        }
    }

    /// <summary>
    /// Tells the host the preview settled: the shell hides the view's loading ring, and screenshots wait for it.
    /// </summary>
    private async Task ReportReady()
    {
        if (isReadyReported)
            return;

        isReadyReported = true;

        await JSRuntime.InvokeVoidAsync("workbench.markReady");
    }

    /// <summary>A state saved with the review, by its attachment id; null when there is none or it doesn't read as JSON.</summary>
    private async Task<JsonObject?> ReadSavedState(string attachmentId)
    {
        if (await attachments.ReadState(attachmentId) is not { } json)
            return null;

        try
        {
            return JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public override async ValueTask DisposeAsync()
    {
        reference?.Dispose();

        await base.DisposeAsync();
    }
}
