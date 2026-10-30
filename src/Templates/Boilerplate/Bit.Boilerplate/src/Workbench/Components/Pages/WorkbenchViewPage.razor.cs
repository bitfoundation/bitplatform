namespace Boilerplate.Workbench.Components.Pages;

public partial class WorkbenchViewPage
{
    private WorkbenchPageInfo? pageInfo;
    private string? lastAppliedUri;
    private bool isRestored;
    private bool isSmallScreen;
    private bool isToolsOpen;
    private bool isLinkCopied;
    private List<BitMenuButtonItem> compareItems = [];
    private List<BitMenuButtonItem> screenshotItems = [];
    private DotNetObjectReference<WorkbenchViewPage>? reference;

    [AutoInject] private WorkbenchStore store = default!;
    [AutoInject] private BlexManager blexManager = default!;
    [AutoInject] private WorkbenchSnapshots snapshots = default!;
    [AutoInject] private BitThemeManager bitThemeManager = default!;

    [Parameter] public string? PageKey { get; set; }

    protected override void OnInitialized()
    {
        compareItems =
        [
            new() { Key = "single", Text = Localizer[nameof(WorkbenchStrings.CompareSingle)], IconName = BitIconName.CellPhone, OnClick = _ => store.Compare("single") },
            new() { Key = "devices", Text = Localizer[nameof(WorkbenchStrings.CompareDevices)], IconName = BitIconName.Devices2, OnClick = _ => store.Compare("devices") },
            new() { Key = "themes", Text = Localizer[nameof(WorkbenchStrings.CompareThemes)], IconName = BitIconName.ClearNight, OnClick = _ => store.Compare("themes") },
            new() { Key = "languages", Text = Localizer[nameof(WorkbenchStrings.CompareLanguages)], IconName = BitIconName.Globe, OnClick = _ => store.Compare("languages") },
            new() { Key = "states", Text = Localizer[nameof(WorkbenchStrings.CompareStates)], IconName = BitIconName.Split, OnClick = _ => store.Compare("states") },
            new() { Key = "accounts", Text = Localizer[nameof(WorkbenchStrings.CompareSignIn)], IconName = BitIconName.People, OnClick = _ => store.Compare("accounts") },
        ];

        screenshotItems =
        [
            new() { Key = "view", Text = Localizer[nameof(WorkbenchStrings.ScreenshotThisView)], IconName = BitIconName.Camera, OnClick = item => _ = DownloadScreenshot(store.SelectedPane) },
            new() { Key = "all", Text = Localizer[nameof(WorkbenchStrings.ScreenshotAllViews)], IconName = BitIconName.PhotoCollection, OnClick = item => _ = DownloadAll() },
        ];

        Subscribe(store);
        OwnsSubscription(blexManager.SubscribeTo<WorkbenchStore>(_ => Apply()));
    }

    protected override void OnParametersSet()
    {
        pageInfo = WorkbenchPageCatalog.Find(PageKey);

        if (isRestored && NavigationManager.Uri != lastAppliedUri)
        {
            store.OpenUrl(new Uri(NavigationManager.Uri).Query);
        }

        Apply();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender is false)
            return;

        reference = DotNetObjectReference.Create(this);
        await JSRuntime.InvokeVoidAsync("workbench.registerShell", reference);

        await WhenStateRestored();

        store.OpenUrl(new Uri(NavigationManager.Uri).Query);
        isRestored = true;

        Apply();
        StateHasChanged();
    }

    [JSInvokable]
    public void ReportScenario(string paneId, string query)
    {
        store.ReportScenario(paneId, WorkbenchScenario.Parse(query));
    }

    [JSInvokable]
    public void ReportPage(string pageKey)
    {
        if (WorkbenchPageCatalog.Find(pageKey) is not { } page || page == pageInfo)
            return;

        NavigationManager.NavigateTo($"{WorkbenchUrls.Workbench(page.Key)}{store.QueryString}");
    }

    private void Apply()
    {
        ScenarioState.Set(store.SelectedPane.Scenario);

        if (isRestored is false || pageInfo is null)
        {
            lastAppliedUri = NavigationManager.Uri;
            return;
        }

        var url = NavigationManager.ToAbsoluteUri($"{WorkbenchUrls.Workbench(pageInfo.Key)}{store.QueryString}").ToString();

        if (url != NavigationManager.Uri)
        {
            lastAppliedUri = url;
            NavigationManager.NavigateTo(url, replace: true);
        }
        else
        {
            lastAppliedUri = url;
        }
    }

    private void OnSmallScreenChanged(bool matched)
    {
        isSmallScreen = matched;
    }

    private async Task Reset()
    {
        var pane = store.SelectedPane;

        if (isSmallScreen)
        {
            foreach (var pageStore in blexManager.Stores.OfType<BlexStoreBase>().Where(s => s != store))
            {
                pageStore.ResetState();
            }
        }
        else
        {
            await JSRuntime.InvokeVoidAsync("workbench.resetFrame", pane.Id);
        }

        store.ChangeScenario(new WorkbenchScenario { Device = pane.Scenario.Device, Rotated = pane.Scenario.Rotated });
    }

    private async Task<string?> CaptureState(WorkbenchPane pane)
    {
        var state = isSmallScreen
            ? blexManager.CaptureGlobalState().ToJsonString()
            : await JSRuntime.InvokeAsync<string?>("workbench.captureFrame", pane.Id);

        return snapshots.Add(state);
    }

    private async Task DownloadScreenshot(WorkbenchPane pane)
    {
        if (pageInfo is null)
            return;

        var snapshotId = await CaptureState(pane);

        await JSRuntime.InvokeVoidAsync("workbench.download", WorkbenchUrls.Screenshot(pageInfo.Key, pane.Scenario, snapshotId));
    }

    private async Task DownloadAll()
    {
        if (pageInfo is null)
            return;

        List<(string, WorkbenchScenario, string?)> shots = [];

        foreach (var pane in store.Panes)
        {
            shots.Add((pageInfo.Key, pane.Scenario, await CaptureState(pane)));
        }

        await JSRuntime.InvokeVoidAsync("workbench.download", WorkbenchUrls.ScreenshotArchive(shots));
    }

    private async Task CopyLink()
    {
        await JSRuntime.InvokeVoidAsync("workbench.copy", NavigationManager.Uri);

        isLinkCopied = true;
        StateHasChanged();

        await Task.Delay(1500);

        isLinkCopied = false;
        StateHasChanged();
    }

    private async Task ToggleShellTheme()
    {
        await bitThemeManager.ToggleDarkLightAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        reference?.Dispose();

        await base.DisposeAsync();
    }
}
