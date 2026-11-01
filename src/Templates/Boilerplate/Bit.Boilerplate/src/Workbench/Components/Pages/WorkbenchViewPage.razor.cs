using Bit.Bonboarding;
using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Workbench.Components.Pages;

/// <summary>
/// /workbench/{key}: the shell around one page. On wider screens it shows the page's views as iframes of /preview side
/// by side with the inspector, review, checks and flows; on a phone it renders the page inline with the tools in a panel.
/// The views and their settings live in <see cref="WorkbenchStore"/> and are mirrored into the address.
/// </summary>
public partial class WorkbenchViewPage
{
    private WorkbenchPageInfo? pageInfo;
    // The address the store was last written to, to tell its own replace-navigations from the user's.
    private string? lastAppliedUri;
    // The store holds the views saved in the browser and the ones in the address; until then nothing is drawn.
    private bool isRestored;
    private bool isSmallScreen;
    private bool isToolsOpen;
    private bool startsTour;
    private bool isPickingInline;
    private bool isLinkCopied;
    private List<BitMenuButtonItem> compareItems = [];
    private List<BitMenuButtonItem> screenshotItems = [];
    private IReadOnlyList<WorkbenchBreakpoint> breakpoints = [];
    // A comment being written: the view and spot it was pinned to, and the view's state at that moment.
    private (string PaneId, WorkbenchReviewAnchor Anchor, string? State)? pendingComment;
    private string? selectedThreadId;
    // The pins last sent to the frames as JSON, so an unchanged set isn't sent again on every render.
    private string? shownPins;
    // A view to load with a snapshot once the current render is on screen, for showing a comment as it was left.
    // The saved state a comment's Show reloads a view with, while that view stays in the comment's scenario.
    private (string PaneId, string Query, string SnapshotId)? shownSnapshot;
    // The text clicked in Edit text mode and the resource entries it came from.
    private (string PaneId, string Text, WorkbenchTextMatch[] Matches, WorkbenchReviewAnchor Anchor)? pickedText;
    // The component clicked in Inspect mode, null when it isn't a bit BlazorUI one, the CSS variables its styles use and the
    // spot a comment on it is pinned to.
    private (string PaneId, WorkbenchComponentInfo? Info, WorkbenchCssVariable[] Variables, WorkbenchReviewAnchor Anchor)? pickedComponent;
    // The spot the Review tab last started a comment on, so going back to the tab doesn't start it again after a Cancel.
    private WorkbenchReviewAnchor? draftedAnchor;

    private static readonly (string Mode, string IconName, string TextKey)[] modes =
    [
        (WorkbenchModes.Browse, BitIconName.TouchPointer, nameof(WorkbenchStrings.ModeBrowse)),
        (WorkbenchModes.Comment, BitIconName.Comment, nameof(WorkbenchStrings.ModeComment)),
        (WorkbenchModes.Inspect, BitIconName.Search, nameof(WorkbenchStrings.ModeInspect)),
        (WorkbenchModes.Text, BitIconName.Edit, nameof(WorkbenchStrings.ModeText))
    ];

    // The tabs of the side column, in their order on screen.
    private static readonly (string Column, string TextKey)[] columns =
    [
        (WorkbenchModes.ViewColumn, nameof(WorkbenchStrings.ViewTab)),
        (WorkbenchModes.ReviewColumn, nameof(WorkbenchStrings.ReviewTab)),
        (WorkbenchModes.ChecksColumn, nameof(WorkbenchStrings.ChecksTab)),
        (WorkbenchModes.FlowsColumn, nameof(WorkbenchStrings.Flows))
    ];

    private readonly ElementReference[] columnTabs = new ElementReference[columns.Length];
    private DotNetObjectReference<WorkbenchViewPage>? reference;

    [AutoInject] private WorkbenchStore store = default!;
    [AutoInject] private BlexManager blexManager = default!;
    [AutoInject] private WorkbenchSnapshots snapshots = default!;
    [AutoInject] private BitThemeManager bitThemeManager = default!;
    [AutoInject] private WorkbenchReviewSession reviewSession = default!;
    [AutoInject] private IBonboardingService bonboardingService = default!;
    [AutoInject] private Clipboard clipboard = default!;
    [AutoInject] private WorkbenchReviewAttachments attachments = default!;
    [AutoInject] private WorkbenchTextEdits textEdits = default!;
    [AutoInject] private WorkbenchComponentEdits componentEdits = default!;
    [AutoInject] private ILogger<WorkbenchViewPage> logger = default!;

    private string ReviewerName => store.ReviewerName ?? Localizer[nameof(WorkbenchStrings.Reviewer)];

    /// <summary>The page list's toggle: a pane on the start side, with an arrow that points the way it will move.</summary>
    private string NavigatorToggleIcon => store.IsNavigatorCollapsed
        ? (CurrentDir is BitDir.Rtl ? BitIconName.OpenPane : BitIconName.OpenPaneMirrored)
        : (CurrentDir is BitDir.Rtl ? BitIconName.ClosePane : BitIconName.ClosePaneMirrored);

    /// <summary>The side column's toggle: the same, on the end side.</summary>
    private string InspectorToggleIcon => store.IsInspectorCollapsed
        ? (CurrentDir is BitDir.Rtl ? BitIconName.OpenPaneMirrored : BitIconName.OpenPane)
        : (CurrentDir is BitDir.Rtl ? BitIconName.ClosePaneMirrored : BitIconName.ClosePane);

    private int OpenThreadCount => pageInfo is null ? 0 : reviewSession.ThreadsOf(pageInfo.Key).Count(t => t.Status is not WorkbenchReviewStatus.Done);

    // The page was left while an await ran: what follows the await belongs to a view that is gone.
    private bool IsDisposed => CurrentCancellationToken.IsCancellationRequested;

    [Parameter] public string? PageKey { get; set; }

    protected override void OnInitialized()
    {
        startsTour = WorkbenchUrls.ReadQuery(NavigationManager.Uri, WorkbenchUrls.TourQueryKey) is not null;

        screenshotItems =
        [
            new() { Key = "view", Text = Localizer[nameof(WorkbenchStrings.ScreenshotThisView)], IconName = BitIconName.Camera, OnClick = item => _ = DownloadScreenshot(store.SelectedPane) },
            new() { Key = "all", Text = Localizer[nameof(WorkbenchStrings.ScreenshotAllViews)], IconName = BitIconName.PhotoCollection, OnClick = item => _ = DownloadAll() },
            new() { Key = "sheet-html", Text = Localizer[nameof(WorkbenchStrings.ReviewSheetHtml)], IconName = BitIconName.FileHTML, OnClick = item => _ = DownloadSheet("html") },
            new() { Key = "sheet-pdf", Text = Localizer[nameof(WorkbenchStrings.ReviewSheetPdf)], IconName = BitIconName.PDF, OnClick = item => _ = DownloadSheet("pdf") },
        ];

        // Every change to the views re-renders the shell and is written into the address.
        Subscribe(store);
        OwnsSubscription(blexManager.SubscribeTo<WorkbenchStore>(_ => Apply()));

        reviewSession.Changed += OnReviewChanged;
        WorkbenchComponentSources.Updated += OnCodeUpdated;
    }

    protected override void OnParametersSet()
    {
        var previous = pageInfo;

        pageInfo = WorkbenchPageCatalog.Find(PageKey);

        if (pageInfo != previous || compareItems.Count == 0)
        {
            BuildCompareItems();
        }

        // An address the shell didn't write itself, like Back or a link to another page: its views win.
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

        // A step that fails is logged and skipped, so the views still appear when one of them breaks.
        await RunStartStep(() => JSRuntime.InvokeVoidAsync("workbench.registerShell", reference).AsTask());
        if (IsDisposed)
            return;

        await RunStartStep(async () => { breakpoints = await JSRuntime.InvokeAsync<WorkbenchBreakpoint[]>("workbench.readBreakpoints"); });
        if (IsDisposed)
            return;

        // The views saved in the browser first, then the ones a shared link names on top of them.
        await RunStartStep(WhenStateRestored);
        if (IsDisposed)
            return;

        store.OpenUrl(new Uri(NavigationManager.Uri).Query);
        isRestored = true;

        await RunStartStep(() => JSRuntime.InvokeVoidAsync("workbench.setComponentNames", WorkbenchComponentNames.RootClasses, Localizer[nameof(WorkbenchStrings.NotFromTheme)].Value).AsTask());
        if (IsDisposed)
            return;

        await RunStartStep(() => reviewSession.Open(ReviewerName));
        if (IsDisposed)
            return;

        Apply();
        StateHasChanged();

        // The tour points at the shell's columns, which a phone doesn't have.
        if (startsTour && pageInfo is not null && isSmallScreen is false)
        {
            await RunStartStep(StartTour);
        }
    }

    /// <summary>Runs one start-up step; a failure is logged and the other steps still run, so the canvas never waits for ever.</summary>
    private async Task RunStartStep(Func<Task> step)
    {
        try
        {
            await step();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "A start-up step of the workbench page failed.");
        }
    }

    /// <summary>
    /// After each render, hands the frames what lives outside Blazor: a view to reload with a snapshot, the mode, and
    /// the comment pins of every view.
    /// </summary>
    private async Task SyncFrames()
    {
        await JSRuntime.InvokeVoidAsync("workbench.setMode", store.Mode);

        if (pageInfo is null)
            return;

        // Open comments, the selected one even when done, and the draft being written, per view.
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

    /// <summary>
    /// Comment mode opens the Review tab; a read-only viewer gets the tab but not the mode, with the page back to use. Inspect
    /// mode opens the first tab, where the picked component's panel shows.
    /// </summary>
    private void SetMode(string mode)
    {
        if (mode is WorkbenchModes.Comment)
        {
            store.ShowColumn(WorkbenchModes.ReviewColumn);

            if (reviewSession.CanWrite is false)
            {
                mode = WorkbenchModes.Browse;
            }
        }

        if (mode is WorkbenchModes.Inspect)
        {
            store.ShowColumn(WorkbenchModes.ViewColumn);
        }
        else
        {
            pickedComponent = null;
        }

        store.SetMode(mode);
    }

    /// <summary>A click in a view in Comment mode: opens a draft pinned there, with the view's state captured for its screenshot.</summary>
    [JSInvokable]
    public async Task BeginComment(string paneId, WorkbenchReviewAnchor anchor)
    {
        if (reviewSession.CanWrite is false)
            return;

        var state = await JSRuntime.InvokeAsync<string?>("workbench.captureFrame", paneId);

        if (IsDisposed)
            return;

        pendingComment = (paneId, anchor, state);
        selectedThreadId = null;

        store.SelectPane(paneId);
        store.ShowColumn(WorkbenchModes.ReviewColumn);

        StateHasChanged();
    }

    /// <summary>Phone layout: closes the tools so the next tap on the page pins a comment there.</summary>
    private async Task PinInline()
    {
        isToolsOpen = false;
        isPickingInline = true;

        await JSRuntime.InvokeVoidAsync("workbench.pickInline");
    }

    /// <summary>The tap that pins a comment on the inline page; its state is this circuit's own, as the page runs here.</summary>
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

    /// <summary>A text clicked in Edit text mode, with the resource entries that render it.</summary>
    [JSInvokable]
    public void BeginTextEdit(string paneId, string text, WorkbenchTextMatch[] matches, WorkbenchReviewAnchor anchor)
    {
        pickedText = string.IsNullOrWhiteSpace(text) ? null : (paneId, text, matches, anchor);

        store.SelectPane(paneId);

        StateHasChanged();
    }

    /// <summary>A component clicked in Inspect mode, as its view describes it, with the CSS variables its styles use.</summary>
    [JSInvokable]
    public void BeginComponentEdit(string paneId, WorkbenchComponentInfo? info, WorkbenchCssVariable[] variables, WorkbenchReviewAnchor anchor)
    {
        pickedComponent = (paneId, info, variables, anchor);

        store.SelectPane(paneId);

        StateHasChanged();
    }

    /// <summary>
    /// Opens a tab of the side column. The Review tab, opened while Inspect mode has a component picked, starts a comment
    /// pinned to that component, ready to be typed, as the component is already known.
    /// </summary>
    private async Task ShowColumn(string column)
    {
        store.ShowColumn(column);

        if (column is not WorkbenchModes.ReviewColumn || store.Mode != WorkbenchModes.Inspect || reviewSession.CanWrite is false
            || pickedComponent is not { } picked || picked.Anchor == draftedAnchor)
            return;

        draftedAnchor = picked.Anchor;

        var state = await JSRuntime.InvokeAsync<string?>("workbench.captureFrame", picked.PaneId);

        if (IsDisposed)
            return;

        pendingComment = (picked.PaneId, picked.Anchor, state);
        selectedThreadId = null;
    }

    /// <summary>Hot reload applied a change: the picked component is read again once the views have drawn it.</summary>
    private void OnCodeUpdated() => _ = InvokeAsync(async () =>
    {
        if (pickedComponent is null || store.Mode != WorkbenchModes.Inspect)
            return;

        try
        {
            await Task.Delay(400, CurrentCancellationToken);
            await JSRuntime.InvokeVoidAsync("workbench.inspectAgain");
        }
        catch (Exception exception) when (exception is OperationCanceledException or JSDisconnectedException)
        {
            // The shell went away while it waited.
        }
    });

    // No callback for a read-only viewer, so the text panel hides its button that asks the agent.
    private EventCallback<string> AskAgentCallback => reviewSession.CanWrite ? EventCallback.Factory.Create<string>(this, AskAgentAboutText) : default;

    /// <summary>Saved translations reach the views only through a reload, as the resources are read when a page renders.</summary>
    private async Task OnTextSaved()
    {
        await JSRuntime.InvokeVoidAsync("workbench.reloadFrames");
    }

    /// <summary>Turns a request about the picked text into a comment pinned on it, for the agent to answer.</summary>
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

    /// <summary>A click on a comment's pin in a view.</summary>
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

    /// <summary>Puts the selected view into the scenario a comment was left in and reloads it with the state it had then.</summary>
    private async Task ShowThread(WorkbenchReviewThread thread)
    {
        if (pageInfo is null)
            return;

        selectedThreadId = thread.Id;

        var pane = store.SelectedPane;
        var snapshotId = thread.HasState ? snapshots.Add(await attachments.ReadState(thread.Id)) : null;

        store.ChangeScenario(thread.Scenario);

        shownSnapshot = snapshotId is null ? null : (pane.Id, thread.Scenario.ToQueryString(), snapshotId);

        isToolsOpen = false;
    }

    /// <summary>The snapshot a view loads with: a shown comment's state, until the view leaves that comment's scenario.</summary>
    private string? SnapshotOf(WorkbenchPane pane)
    {
        return shownSnapshot is { } shown && shown.PaneId == pane.Id && shown.Query == pane.Scenario.ToQueryString() ? shown.SnapshotId : null;
    }

    /// <summary>The selected view's stores: read from its iframe, or from this circuit when the page runs inline on a phone.</summary>
    private async Task<string?> CaptureSelectedState()
    {
        return isSmallScreen
            ? blexManager.CaptureGlobalState().ToJsonString()
            : await JSRuntime.InvokeAsync<string?>("workbench.captureFrame", store.SelectedPane.Id);
    }

    private void OnReviewChanged() => InvokeAsync(StateHasChanged);

    /// <summary>
    /// A sign-in or sign-up button pressed in a view. Those pages aren't here, so this brings the user picker into sight, whatever
    /// mode, tab or collapsed column was hiding it, and points at it with the rest of the screen dimmed.
    /// </summary>
    [JSInvokable]
    public async Task ShowSignInGuide(string? paneId)
    {
        if (paneId is not null)
        {
            store.SelectPane(paneId);
        }

        SetMode(WorkbenchModes.Browse);

        if (isSmallScreen)
        {
            isToolsOpen = true;
        }
        else
        {
            store.ShowColumn(WorkbenchModes.ViewColumn);
        }

        StateHasChanged();

        await bonboardingService.StartAsync(new BonboardingTourDefinition
        {
            Id = "sign-in-guide",
            Persist = false,
            Steps =
            {
                new BonboardingStepDefinition
                {
                    Target = BonboardingStepTarget.Anchor("sign-in"),
                    Title = nameof(WorkbenchStrings.SignInNoteTitle),
                    Description = nameof(WorkbenchStrings.SignInGuide),
                    Placement = BonboardingPlacement.InlineStart,
                    Interaction = BonboardingInteractionMode.TargetOnly,
                    Progress = BonboardingProgressStyle.None,
                    ShowSkip = false,
                    DoneLabel = Localizer[nameof(WorkbenchStrings.GotIt)]
                }
            }
        });
    }

    /// <summary>A view whose page changed its own scenario, like a theme picked in the app's menu.</summary>
    [JSInvokable]
    public void ReportScenario(string paneId, string query)
    {
        store.ReportScenario(paneId, WorkbenchScenario.Parse(query));
    }

    /// <summary>The end of a drag on a view's edge or corner.</summary>
    [JSInvokable]
    public void ResizePane(string paneId, int width, int height)
    {
        store.ResizePane(paneId, width, height);
    }

    /// <summary>A view whose page navigated to another catalog page: the shell, and so every view, follows it.</summary>
    [JSInvokable]
    public void ReportPage(string pageKey)
    {
        if (WorkbenchPageCatalog.Find(pageKey) is not { } page || page == pageInfo)
            return;

        NavigationManager.NavigateTo($"{WorkbenchUrls.Workbench(page.Key)}{store.OtherPageQueryString}");
    }

    /// <summary>
    /// Gives the shell's own circuit the selected view's scenario (the phone layout renders the page with it) and writes
    /// the views into the address, so a reload or a copied link opens the same ones.
    /// </summary>
    private void Apply()
    {
        ScenarioState.Set(store.SelectedPane.Scenario);

        if (isRestored is false || pageInfo is null)
        {
            lastAppliedUri = NavigationManager.Uri;
            return;
        }

        // AbsoluteUri keeps the escaping the browser reports the address with, so equal addresses compare equal.
        var url = NavigationManager.ToAbsoluteUri($"{WorkbenchUrls.Workbench(pageInfo.Key)}{store.QueryString}").AbsoluteUri;

        lastAppliedUri = url;

        if (url != NavigationManager.Uri)
        {
            NavigationManager.NavigateTo(url, replace: true);
        }
    }

    /// <summary>The Compare menu: the built-in spreads, then the page's own design options and switches.</summary>
    private void BuildCompareItems()
    {
        compareItems =
        [
            new() { Key = "single", Text = Localizer[nameof(WorkbenchStrings.CompareSingle)], IconName = BitIconName.CellPhone, OnClick = _ => store.Compare("single") },
            new() { Key = "devices", Text = Localizer[nameof(WorkbenchStrings.CompareDevices)], IconName = BitIconName.Devices2, OnClick = _ => store.Compare("devices") },
            new() { Key = "themes", Text = Localizer[nameof(WorkbenchStrings.CompareThemes)], IconName = BitIconName.ClearNight, OnClick = _ => store.Compare("themes") },
            .. CultureInfoManager.InvariantGlobalization
                ? Array.Empty<BitMenuButtonItem>()
                : [new() { Key = "languages", Text = Localizer[nameof(WorkbenchStrings.CompareLanguages)], IconName = BitIconName.Globe, OnClick = _ => store.Compare("languages") }],
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

    /// <summary>Clears what was done on the selected view's page and puts its scenario back to the defaults, keeping its device.</summary>
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

    /// <summary>Saves a view's current stores as a snapshot for the screenshot service; null when there is none.</summary>
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

        await JSRuntime.InvokeVoidAsync("workbench.download", WorkbenchUrls.ScreenshotArchive(await CaptureAll(pageInfo.Key)));
    }

    /// <summary>Every view with its settings and comments in one HTML or PDF file.</summary>
    private async Task DownloadSheet(string format)
    {
        if (pageInfo is null)
            return;

        var pageKey = pageInfo.Key;

        await JSRuntime.InvokeVoidAsync("workbench.download", WorkbenchUrls.ReviewSheet(pageKey, await CaptureAll(pageKey), format));
    }

    /// <summary>Every view with a snapshot of its stores as they are now, for the screenshot archive and the review sheet.</summary>
    private async Task<List<(string PageKey, WorkbenchScenario Scenario, string? SnapshotId)>> CaptureAll(string pageKey)
    {
        List<(string PageKey, WorkbenchScenario Scenario, string? SnapshotId)> shots = [];

        foreach (var pane in store.Panes)
        {
            shots.Add((pageKey, pane.Scenario, await CaptureState(pane)));
        }

        return shots;
    }

    private async Task CopyLink()
    {
        // A browser can refuse the clipboard to a copy that arrives after a server round trip; Safari always does.
        try
        {
            await clipboard.WriteText(NavigationManager.Uri);
        }
        catch (JSException)
        {
            SnackBarService.Error(Localizer[nameof(WorkbenchStrings.CopyFailed)]);
            return;
        }

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

    /// <summary>Arrow keys, Home and End move between the side column's tabs, mirrored in right-to-left.</summary>
    private async Task OnColumnTabKeyDown(KeyboardEventArgs args)
    {
        var current = Math.Max(0, Array.FindIndex(columns, c => c.Column == store.Column));
        var forward = CurrentDir is BitDir.Rtl ? "ArrowLeft" : "ArrowRight";
        var backward = CurrentDir is BitDir.Rtl ? "ArrowRight" : "ArrowLeft";

        var next = args.Key switch
        {
            "Home" => 0,
            "End" => columns.Length - 1,
            _ when args.Key == forward => (current + 1) % columns.Length,
            _ when args.Key == backward => (current + columns.Length - 1) % columns.Length,
            _ => -1
        };

        if (next < 0)
            return;

        await ShowColumn(columns[next].Column);

        await columnTabs[next].FocusAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        reviewSession.Changed -= OnReviewChanged;
        WorkbenchComponentSources.Updated -= OnCodeUpdated;
        reference?.Dispose();

        // A guide left running would keep pointing at a shell that is gone; the tour goes on when this is its way to the Flows page.
        var isTourOnItsWay = bonboardingService.FindSession(TourId) is not null && IsOnFlowsPage;

        try
        {
            if (isTourOnItsWay is false)
            {
                await bonboardingService.StopAllAsync();
            }
        }
        catch (JSDisconnectedException)
        {
        }

        await base.DisposeAsync();
    }
}
