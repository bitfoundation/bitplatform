namespace Boilerplate.Workbench.Components.Pages;

public partial class WorkbenchViewPage
{
    private WorkbenchPageInfo? pageInfo;
    private string? lastAppliedUri;
    private bool isRestored;
    private bool isSmallScreen;
    private bool isToolsOpen;
    private bool isPickingInline;
    private bool isLinkCopied;
    private List<BitMenuButtonItem> compareItems = [];
    private List<BitMenuButtonItem> screenshotItems = [];
    private IReadOnlyList<WorkbenchBreakpoint> breakpoints = [];
    private (string PaneId, WorkbenchReviewAnchor Anchor, string? State)? pendingComment;
    private string? selectedThreadId;
    private string? shownPins;
    private (string PaneId, string Url)? pendingFrameUrl;
    private (string PaneId, string Text, WorkbenchTextMatch[] Matches, WorkbenchReviewAnchor Anchor)? pickedText;

    private static readonly (string Mode, string IconName, string TextKey)[] modes =
    [
        (WorkbenchModes.Browse, BitIconName.TouchPointer, nameof(WorkbenchStrings.ModeBrowse)),
        (WorkbenchModes.Comment, BitIconName.Comment, nameof(WorkbenchStrings.ModeComment)),
        (WorkbenchModes.Inspect, BitIconName.Search, nameof(WorkbenchStrings.ModeInspect)),
        (WorkbenchModes.Text, BitIconName.Edit, nameof(WorkbenchStrings.ModeText))
    ];
    private DotNetObjectReference<WorkbenchViewPage>? reference;

    [AutoInject] private WorkbenchStore store = default!;
    [AutoInject] private BlexManager blexManager = default!;
    [AutoInject] private WorkbenchSnapshots snapshots = default!;
    [AutoInject] private BitThemeManager bitThemeManager = default!;
    [AutoInject] private WorkbenchReviewSession reviewSession = default!;
    [AutoInject] private WorkbenchReviewAttachments attachments = default!;
    [AutoInject] private WorkbenchTextEdits textEdits = default!;

    private string ReviewerName => store.ReviewerName ?? Localizer[nameof(WorkbenchStrings.Reviewer)];

    private int OpenThreadCount => pageInfo is null ? 0 : reviewSession.ThreadsOf(pageInfo.Key).Count(t => t.Status is not WorkbenchReviewStatus.Done);

    [Parameter] public string? PageKey { get; set; }

    protected override void OnInitialized()
    {
        screenshotItems =
        [
            new() { Key = "view", Text = Localizer[nameof(WorkbenchStrings.ScreenshotThisView)], IconName = BitIconName.Camera, OnClick = item => _ = DownloadScreenshot(store.SelectedPane) },
            new() { Key = "all", Text = Localizer[nameof(WorkbenchStrings.ScreenshotAllViews)], IconName = BitIconName.PhotoCollection, OnClick = item => _ = DownloadAll() },
            new() { Key = "sheet-html", Text = Localizer[nameof(WorkbenchStrings.ReviewSheetHtml)], IconName = BitIconName.FileHTML, OnClick = item => _ = DownloadSheet("html") },
            new() { Key = "sheet-pdf", Text = Localizer[nameof(WorkbenchStrings.ReviewSheetPdf)], IconName = BitIconName.PDF, OnClick = item => _ = DownloadSheet("pdf") },
        ];

        Subscribe(store);
        OwnsSubscription(blexManager.SubscribeTo<WorkbenchStore>(_ => Apply()));

        reviewSession.Changed += OnReviewChanged;
    }

    protected override void OnParametersSet()
    {
        var previous = pageInfo;

        pageInfo = WorkbenchPageCatalog.Find(PageKey);

        if (pageInfo != previous || compareItems.Count == 0)
        {
            BuildCompareItems();
        }

        if (isRestored && NavigationManager.Uri != lastAppliedUri)
        {
            store.OpenUrl(new Uri(NavigationManager.Uri).Query);
        }

        Apply();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender is false)
        {
            await SyncFrames();
            return;
        }

        reference = DotNetObjectReference.Create(this);
        await JSRuntime.InvokeVoidAsync("workbench.registerShell", reference);

        breakpoints = await JSRuntime.InvokeAsync<WorkbenchBreakpoint[]>("workbench.readBreakpoints");

        await WhenStateRestored();

        store.OpenUrl(new Uri(NavigationManager.Uri).Query);
        isRestored = true;

        await JSRuntime.InvokeVoidAsync("workbench.setComponentNames", WorkbenchComponentNames.RootClasses, Localizer[nameof(WorkbenchStrings.NotFromTheme)].Value);
        await reviewSession.Open(ReviewerName);

        Apply();
        StateHasChanged();
    }

    private async Task SyncFrames()
    {
        if (pendingFrameUrl is { } frame)
        {
            pendingFrameUrl = null;
            await JSRuntime.InvokeVoidAsync("workbench.showInFrame", frame.PaneId, frame.Url);
        }

        await JSRuntime.InvokeVoidAsync("workbench.setMode", store.Mode);

        if (pageInfo is null)
            return;

        var pins = store.Panes.ToDictionary(pane => pane.Id, pane => reviewSession.ThreadsOf(pageInfo.Key)
            .Where(t => t.Status is not WorkbenchReviewStatus.Done || t.Id == selectedThreadId)
            .Select(t => new { t.Id, Number = t.Number.ToString(CultureInfo.InvariantCulture), t.Anchor.Selector, t.Anchor.Text, t.Anchor.X, t.Anchor.Y, Status = t.Status.ToString().ToLowerInvariant(), Selected = t.Id == selectedThreadId })
            .Concat(pendingComment is { } pending && pending.PaneId == pane.Id
                ? [new { Id = "draft", Number = "+", pending.Anchor.Selector, pending.Anchor.Text, pending.Anchor.X, pending.Anchor.Y, Status = "draft", Selected = true }]
                : [])
            .ToArray());

        var signature = System.Text.Json.JsonSerializer.Serialize(pins);

        if (signature == shownPins)
            return;

        shownPins = signature;
        await JSRuntime.InvokeVoidAsync("workbench.showPins", pins);
    }

    private void SetMode(string mode)
    {
        if (mode is WorkbenchModes.Comment)
        {
            store.ShowColumn(WorkbenchModes.ReviewColumn);

            if (reviewSession.CanWrite is false)
                return;
        }

        store.SetMode(mode);
    }

    [JSInvokable]
    public async Task BeginComment(string paneId, WorkbenchReviewAnchor anchor)
    {
        if (reviewSession.CanWrite is false)
            return;

        var state = await JSRuntime.InvokeAsync<string?>("workbench.captureFrame", paneId);

        pendingComment = (paneId, anchor, state);
        selectedThreadId = null;

        store.SelectPane(paneId);
        store.ShowColumn(WorkbenchModes.ReviewColumn);

        StateHasChanged();
    }

    private async Task PinInline()
    {
        isToolsOpen = false;
        isPickingInline = true;

        await JSRuntime.InvokeVoidAsync("workbench.pickInline");
    }

    [JSInvokable]
    public void BeginInlineComment(WorkbenchReviewAnchor anchor)
    {
        isPickingInline = false;

        if (reviewSession.CanWrite is false)
            return;

        pendingComment = (store.SelectedPane.Id, anchor, blexManager.CaptureGlobalState().ToJsonString());
        selectedThreadId = null;
        isToolsOpen = true;

        StateHasChanged();
    }

    private async Task CancelInlinePick()
    {
        isPickingInline = false;

        await JSRuntime.InvokeVoidAsync("workbench.cancelInlinePick");
    }

    [JSInvokable]
    public void BeginTextEdit(string paneId, string text, WorkbenchTextMatch[] matches, WorkbenchReviewAnchor anchor)
    {
        pickedText = string.IsNullOrWhiteSpace(text) ? null : (paneId, text, matches, anchor);

        store.SelectPane(paneId);

        StateHasChanged();
    }

    private EventCallback<string> AskAgentCallback => reviewSession.CanWrite ? EventCallback.Factory.Create<string>(this, AskAgentAboutText) : default;

    private async Task OnTextSaved()
    {
        await JSRuntime.InvokeVoidAsync("workbench.reloadFrames");
    }

    private async Task AskAgentAboutText(string request)
    {
        if (pageInfo is null || pickedText is not { } picked || store.Panes.FirstOrDefault(p => p.Id == picked.PaneId) is not { } pane)
            return;

        var state = await JSRuntime.InvokeAsync<string?>("workbench.captureFrame", pane.Id);

        selectedThreadId = await reviewSession.AddThread(pageInfo.Key, pane.Scenario.ToQueryString(), picked.Anchor, ReviewerName, request, state);
        pickedText = null;

        store.SetMode(WorkbenchModes.Comment);
        store.ShowColumn(WorkbenchModes.ReviewColumn);
    }

    [JSInvokable]
    public void OpenThread(string threadId)
    {
        selectedThreadId = threadId;
        pendingComment = null;

        store.ShowColumn(WorkbenchModes.ReviewColumn);

        StateHasChanged();
    }

    private async Task SendComment(string text)
    {
        if (pageInfo is null || pendingComment is not { } pending || store.Panes.FirstOrDefault(p => p.Id == pending.PaneId) is not { } pane)
            return;

        pendingComment = null;
        selectedThreadId = await reviewSession.AddThread(pageInfo.Key, pane.Scenario.ToQueryString(), pending.Anchor, ReviewerName, text, pending.State);
    }

    private void CancelComment()
    {
        pendingComment = null;
    }

    private void SelectThread(WorkbenchReviewThread thread)
    {
        selectedThreadId = selectedThreadId == thread.Id ? null : thread.Id;
    }

    private async Task ShowThread(WorkbenchReviewThread thread)
    {
        if (pageInfo is null)
            return;

        selectedThreadId = thread.Id;

        var pane = store.SelectedPane;
        var snapshotId = thread.HasState ? snapshots.Add(await attachments.ReadState(thread.Id)) : null;

        store.ChangeScenario(thread.Scenario);

        if (snapshotId is not null)
        {
            pendingFrameUrl = (pane.Id, WorkbenchUrls.Preview(pageInfo.Key, thread.Scenario, pane.Id, snapshotId));
        }

        isToolsOpen = false;
    }

    private async Task<string?> CaptureSelectedState()
    {
        return isSmallScreen
            ? blexManager.CaptureGlobalState().ToJsonString()
            : await JSRuntime.InvokeAsync<string?>("workbench.captureFrame", store.SelectedPane.Id);
    }

    private void OnReviewChanged() => InvokeAsync(StateHasChanged);

    [JSInvokable]
    public void ReportScenario(string paneId, string query)
    {
        store.ReportScenario(paneId, WorkbenchScenario.Parse(query));
    }

    [JSInvokable]
    public void ResizePane(string paneId, int width, int height)
    {
        store.ResizePane(paneId, width, height);
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

        lastAppliedUri = url;

        if (url != NavigationManager.Uri)
        {
            NavigationManager.NavigateTo(url, replace: true);
        }
    }

    private void BuildCompareItems()
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

        if (pageInfo is null)
            return;

        if (pageInfo.Variants.Length > 1)
        {
            var variants = pageInfo.Variants.Select(v => v.Key).ToArray();
            compareItems.Add(new() { Key = "variants", Text = Localizer[nameof(WorkbenchStrings.CompareVariants)], IconName = BitIconName.ViewAll, OnClick = _ => store.CompareVariants(variants) });
        }

        foreach (var pageSwitch in pageInfo.Switches)
        {
            var values = pageSwitch.Options.Select(o => o.Value).ToArray();
            compareItems.Add(new() { Key = $"switch-{pageSwitch.Key}", Text = Localizer[pageSwitch.NameKey], IconName = BitIconName.ToggleRight, OnClick = _ => store.CompareSwitch(pageSwitch.Key, values) });
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

    private async Task DownloadSheet(string format)
    {
        if (pageInfo is null)
            return;

        List<(string, WorkbenchScenario, string?)> shots = [];

        foreach (var pane in store.Panes)
        {
            shots.Add((pageInfo.Key, pane.Scenario, await CaptureState(pane)));
        }

        await JSRuntime.InvokeVoidAsync("workbench.download", WorkbenchUrls.ReviewSheet(pageInfo.Key, shots, format));
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
        reviewSession.Changed -= OnReviewChanged;
        reference?.Dispose();

        await base.DisposeAsync();
    }
}
