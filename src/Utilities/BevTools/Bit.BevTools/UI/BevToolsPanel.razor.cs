using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.BevTools;

/// <summary>
/// The BevTools overlay. Place it once inside an interactive component (normally the layout). It renders nothing
/// during prerendering or when BevTools is disabled, and it re-renders only when captured data changed.
/// </summary>
public partial class BevToolsPanel : ComponentBase, IBevToolsCommandContext, IAsyncDisposable
{
    private static readonly JsonSerializerOptions PrefsJson = new(JsonSerializerDefaults.Web);
    private readonly CancellationTokenSource _cts = new();
    private IJSObjectReference? _module;
    private DotNetObjectReference<BevJsBridge>? _bridgeRef;
    private long _lastVersion = -1;
    private long _lastUiVersion = -1;
    private long _viewRevision;
    private bool _interactive;
    private bool _prefsApplied;
    private string _searchText = "";
    private Task? _loop;
    private IDisposable? _prerenderHandoff;

    /// <summary>The scope's session: the one registered with <c>AddBitBevToolsServices</c>, or the one attached since.</summary>
    public BevToolsSession Session { get; private set; } = default!;

    public BitBevToolsOptions Options { get; private set; } = default!;

    /// <summary>Part of <see cref="IBevToolsCommandContext"/>: commands contributed by extensions resolve services through it.</summary>
    [Inject] public IServiceProvider Services { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    public BevToolsUiState Ui => Session.Ui;

    public BevServiceGraph? Graph => Session.Registry.ServiceGraph;

    private IReadOnlyList<BevToolsPanelDescriptor>? _extensionPanels;
    private string? _extensionPanelsPlatform;

    /// <summary>Extension panels applicable to this session's platform. Cached: the header reads it several times per render.</summary>
    public IReadOnlyList<BevToolsPanelDescriptor> ExtensionPanels
    {
        get
        {
            if (_extensionPanels is null || !string.Equals(_extensionPanelsPlatform, Session.Platform, StringComparison.Ordinal))
            {
                _extensionPanelsPlatform = Session.Platform;
                _extensionPanels = Session.Registry.Panels.Where(p => p.RequiredPlatform is null || string.Equals(p.RequiredPlatform, Session.Platform, StringComparison.OrdinalIgnoreCase)).ToArray();
            }

            return _extensionPanels;
        }
    }

    private static readonly (string Id, string Title)[] BuiltInPanels =
    [
        ("components", "Components"),
        ("profiler", "Profiler"),
        ("timeline", "Timeline"),
        ("state", "State"),
        ("forms", "Forms"),
        ("routing", "Routing"),
        ("network", "Network"),
        ("interop", "JS Interop"),
        ("errors", "Errors"),
        ("services", "Services"),
        ("diagnostics", "Diagnostics"),
        ("runtime", "Runtime"),
        ("browser", "Browser"),
        ("about", "About"),
    ];

    protected override void OnInitialized()
    {
        Session = RuntimeAttachment.Resolve(Services)
            ?? throw new InvalidOperationException("BevTools is not available in this scope: register it with AddBitBevToolsServices, call BitBevTools.Attach before rendering BevToolsPanel, or render BevToolsLoaderPanel (Bit.BevTools.Loader) instead.");
        Options = Session.Options;
        if (!Session.IsEnabled)
        {
            return;
        }

        var (platform, interactive) = ResolveRenderer();
        _interactive = interactive;
        Session.Platform ??= platform;
        Session.IsInteractive = interactive;
#if NET9_0_OR_GREATER
        Session.RenderModeName = AssignedRenderMode?.GetType().Name.Replace("RenderMode", "");
#endif
        if (!_interactive)
        {
            // Prerendering: this session ends with the response, so what it saw is handed to the interactive one.
            _prerenderHandoff = BevPrerenderHandoff.RegisterWhilePrerendering(Session, Services, this, Navigation);
            return;
        }

        Session.Prerender ??= BevPrerenderHandoff.TakeWhenInteractive(Services);
        Session.PanelComponent = this;
        RuntimeAttachment.AttachToRenderer(Session, this);
        Session.EnsureActivated();
        Navigation.LocationChanged += OnLocationChanged;
        Ui.OpenedFromCode += OnOpenedFromCode;
        Session.SymbolsWanted += OnSymbolsWanted;
    }

    private void OnSymbolsWanted(System.Reflection.Assembly assembly) => _ = InvokeAsync(() => LoadSymbolsAsync(assembly));

    /// <summary>
    /// A closed panel polls once a second, so without this an app's own "Open dev tools" button would seem to do
    /// nothing for up to that long.
    /// </summary>
    private void OnOpenedFromCode() => _ = InvokeAsync(() =>
    {
        // Saving before the saved preferences are read would overwrite them with the defaults; the first render saves
        // the open state once it has read them.
        if (_prefsApplied)
        {
            Ui.OpenRequested = false;
            _ = SavePrefsAsync();
        }

        RefreshCapturedData();
        _viewRevision++;
        StateHasChanged();
    });

    private (string? Platform, bool Interactive) ResolveRenderer()
    {
#if NET9_0_OR_GREATER
        return (RendererInfo.Name, RendererInfo.IsInteractive);
#else
        // .NET 8 has no RendererInfo, but the JS runtime each host hands its renderer tells the same story: prerendering
        // and static SSR get one that refuses every call, and each interactive host has its own.
        return JS.GetType().Name switch
        {
            "UnsupportedJavaScriptRuntime" => ("Static", false),
            "RemoteJSRuntime" => ("Server", true),
            "WebViewJSRuntime" => ("WebView", true),
            _ => (OperatingSystem.IsBrowser() ? "WebAssembly" : null, true),
        };
#endif
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            await FocusAfterRenderAsync();
            return;
        }

        if (!_interactive || !Session.IsEnabled)
        {
            return;
        }

        try
        {
            _module = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Bit.BevTools/bit-bevtools.js");
            if (Options.EnableBrowserBridge)
            {
                _bridgeRef = DotNetObjectReference.Create(new BevJsBridge(
                    Session,
                    () => InvokeAsync(() => { Toggle(); StateHasChanged(); }),
                    () => InvokeAsync(() => { OpenPalette(); StateHasChanged(); }),
                    show => InvokeAsync(() => { SetOpen(show); StateHasChanged(); }))
                {
                    Picked = instanceId => InvokeAsync(() =>
                    {
                        Ui.Picking = false;
                        SelectComponent(instanceId);
                    }),
                    PickEnded = error => InvokeAsync(() =>
                    {
                        Ui.Picking = false;
                        Ui.Touch();
                        if (error is not null)
                        {
                            Notify(error);
                        }
                        else
                        {
                            StateHasChanged();
                        }
                    }),
                });
                var snapshot = await _module.InvokeAsync<BevBrowserSnapshot>("attach", _bridgeRef, new
                {
                    shortcut = Options.Ui.ToggleShortcut,
                    trackJsToDotNet = Options.TrackJsInterop,
                    trackTraffic = Options.TrackWebSockets,
                    // The hub redacts URLs it shows itself (sockets, failed resources) with the same patterns.
                    sensitivePatterns = Options.SensitiveNamePatterns,
                    sessionId = Session.Id,
                    // Calls into this module name it by this id over a circuit: the hub counts them as BevTools' own.
                    moduleObjectId = BlazorReflection.JSObjectId(_module),
                    platform = Session.Platform,
                    renderMode = Session.RenderModeName,
                    label = RuntimeLabel(),
                });
                ApplySnapshot(snapshot);
                Session.Browser.TargetId = snapshot?.TargetId;
                Session.Browser.BridgeAttached = true;
            }

            var prefs = await _module.InvokeAsync<string?>("loadPrefs");
            ApplyPrefs(prefs);
        }
        catch (Exception ex)
        {
            // Whatever attaching fails with - a JS error, a disconnect, an interop timeout while the tab is throttled or
            // paused in the browser's debugger, a result that does not deserialize - must not escape a lifecycle method:
            // on Blazor Server that terminates the circuit, and the diagnostics tool would take the app down with it.
            Session.Browser.BridgeError = ex.Message;
        }

        // Settled whether or not preferences were saved or could be read: an open request left pending would
        // overrule the preference of a panel rendered later, after the user closed this one.
        _prefsApplied = true;
        var openRequested = Ui.OpenRequested;
        Ui.OpenRequested = false;

        if (_cts.IsCancellationRequested)
        {
            // Disposed while attaching; the token source is gone.
            return;
        }

        if (openRequested)
        {
            // Opened from code before the preferences were read: the open state is saved now, over what was read.
            _ = SavePrefsAsync();
        }

        _loop = RefreshLoopAsync(_cts.Token);
        StateHasChanged();
    }

    private async Task RefreshLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(Ui.IsOpen || Ui.FlashRenders ? Math.Max(50, Options.Ui.RefreshIntervalMs) : 1000, token);
                try
                {
                    await InvokeAsync(ApplyPendingChanges);
                }
                catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException or JSDisconnectedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Session.Errors.Record(ex, "bevtools", "Refreshing the BevTools UI failed.");
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void ApplyPendingChanges()
    {
        // A notification leaves when its time is up, not when something else happens to re-render the panel.
        if (Ui.Notification is not null && DateTimeOffset.UtcNow - Ui.NotificationAt >= BevToolsUiState.NotificationLifetime)
        {
            Ui.Notification = null;
            Ui.Touch();
        }

        // Closed, the rules otherwise wait for the panel to open; a run checked in CI asks for them as data arrives.
        if (!Ui.IsOpen && Options.Diagnostics.EvaluateWhileClosed && Session.Diagnostics.IsStale)
        {
            Session.RenderTracker.CloseBatch();
            Session.Components.Refresh();
            Session.Diagnostics.Evaluate();
        }

        var dataChanged = Session.Version != _lastVersion;
        var uiChanged = Ui.Version != _lastUiVersion;
        if (!dataChanged && !uiChanged)
        {
            // A trailing evaluation: the last change may have come within the second the previous one is throttled
            // to (a redirect loop ends in milliseconds), and with nothing changing after it the findings would stay
            // behind. Nothing else re-renders the panel then either, so it does once the rules have run - unless live
            // updates are off, which freezes the open tab until Refresh.
            if (Ui.IsOpen && Ui.LiveUpdates && Session.Diagnostics.IsStale)
            {
                Session.Diagnostics.Evaluate();
                if (!Session.Diagnostics.IsStale)
                {
                    _viewRevision++;
                    StateHasChanged();
                }
            }

            return;
        }

        Session.RenderTracker.CloseBatch();
        _lastVersion = Session.Version;
        _lastUiVersion = Ui.Version;
        if (dataChanged)
        {
            FlashRenders();
        }

        var updateOpenPanel = Ui.IsOpen && (uiChanged || Ui.LiveUpdates);
        if (updateOpenPanel)
        {
            Session.Components.Refresh();
            Session.Diagnostics.Evaluate();
            _viewRevision++;
        }

        if (!Ui.IsOpen || updateOpenPanel)
        {
            StateHasChanged();
        }
    }

    /// <summary>"Server circuit", "WebAssembly": how the runtime switcher and the companion window name this runtime.</summary>
    private string RuntimeLabel() => Session.Platform switch
    {
        "Server" => "Server circuit",
        "WebAssembly" => "WebAssembly",
        "WebView" => "Blazor Hybrid",
        { } other => other,
        null => "Blazor",
    } + " · session #" + Session.Id;

    /// <summary>The other runtimes on the page this panel's header can switch to.</summary>
    internal IReadOnlyList<BevTargetInfo> OtherTargets => Session.Browser.Targets.Where(t => t.Id != Session.Browser.TargetId).ToArray();

    /// <summary>Opens this runtime's panel and asks the hub to close the others', so one panel shows at a time.</summary>
    private async Task SwitchToAsync(string targetId)
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.InvokeVoidAsync("show", targetId);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException)
        {
        }
    }

    /// <summary>Opens the companion window, which shows every BevTools runtime of the page outside the page itself.</summary>
    public async Task OpenCompanionAsync()
    {
        var opened = false;
        if (_module is not null)
        {
            try
            {
                opened = await _module.InvokeAsync<bool>("openCompanion", (string?)null);
            }
            catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException)
            {
            }
        }

        Notify(opened ? "Companion window opened" : "The companion window could not be opened (blocked pop-up?)");
    }

    /// <summary>Starts picking a component in the page, or stops it when it is running.</summary>
    public async Task TogglePickAsync()
    {
        if (_module is null || Session.Browser.TargetId is not { } target)
        {
            Notify("Picking needs the browser bridge, which is not attached in this panel.");
            return;
        }

        try
        {
            if (Ui.Picking)
            {
                Ui.Picking = false;
                await _module.InvokeVoidAsync("stopPick");
            }
            else
            {
                Ui.Picking = await _module.InvokeAsync<bool>("startPick", target, new { includeHidden = Ui.ShowHiddenComponents, tellPanel = true });
                Ui.Notify(Ui.Picking ? "Click a component in the page · Esc cancels" : "Components cannot be mapped to this page: its Blazor renderer exposes no logical element tree.");
            }
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException)
        {
            Ui.Picking = false;
        }

        Ui.Touch();
        StateHasChanged();
    }

    /// <summary>
    /// Outlines a component in the page. Moving through the tree by keyboard calls it with
    /// <paramref name="explicitRequest"/> false and stays quiet when the component cannot be shown; *Show in page* scrolls
    /// to it and says why when it cannot. (Hovering a row is handled by the page hub, without a round trip through here.)
    /// </summary>
    internal async Task HighlightAsync(long instanceId, bool explicitRequest = false)
    {
        if (_module is null || Session.Browser.TargetId is not { } target || Session.Components.Get(instanceId) is not { IsBevTools: false } record)
        {
            return;
        }

        string? error = null;
        try
        {
            var location = BevDomMap.Locate(Session, record, out error);
            if (location is null)
            {
                await _module.InvokeVoidAsync("clearHighlight");
            }
            else
            {
                var result = await _module.InvokeAsync<BevHighlightResult>("highlight", target, location, new { scroll = explicitRequest, durationMs = explicitRequest ? 2500 : 0 });
                error = result is { Ok: false } ? result.Error : result is { Visible: false } ? record.DisplayName + " renders nothing visible right now." : null;
            }
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException or ObjectDisposedException)
        {
            error = ex.Message;
        }
        catch (Exception ex)
        {
            // A diagnostics tool must never end a circuit over an outline.
            Session.Errors.Record(ex, "bevtools", "Showing a component in the page failed.");
            error = "Showing it in the page failed: " + ex.GetType().Name;
        }

        if (explicitRequest && error is not null)
        {
            Notify(error);
        }
    }

    private readonly HashSet<string> _symbolsRequested = new(StringComparer.Ordinal);

    /// <summary>
    /// Downloads the symbols (PDB) of an assembly that has none on disk - every assembly on WebAssembly - once: the
    /// page hub fetches the file the runtime's own resource list names for this build. The open tab re-reads when they arrive.
    /// </summary>
    internal async Task LoadSymbolsAsync(System.Reflection.Assembly assembly)
    {
        if (_module is null || !BevSourceLocator.NeedsDownload(assembly) || !_symbolsRequested.Add(assembly.FullName ?? ""))
        {
            return;
        }

        byte[]? pdb;
        try
        {
            // Null: the app lists no PDB for it. A failed download throws, and is not remembered as "no symbols".
            pdb = await _module.InvokeAsync<byte[]?>("fetchPdb", assembly.GetName().Name);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException)
        {
            _symbolsRequested.Remove(assembly.FullName ?? "");
            return;
        }

        BevSourceLocator.Provide(assembly, pdb);
        Refresh();
    }

    internal async Task ClearHighlightAsync()
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.InvokeVoidAsync("clearHighlight");
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException)
        {
        }
    }

    public void ToggleFlashRenders()
    {
        Ui.FlashRenders = !Ui.FlashRenders;
        _flashCounts.Clear();
        _lastFlashTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        Ui.Touch();
        _ = SavePrefsAsync();
        Notify(Ui.FlashRenders ? "Components are outlined in the page as they render" : "Render highlighting off");
    }

    private const int MaxFlashesPerTick = 60;
    private readonly Dictionary<long, int> _flashCounts = [];
    private long _lastFlashTicks;

    /// <summary>
    /// Outlines what rendered since the last tick. Runs on the refresh loop, after the renders, so the browser has the
    /// batch before the outline request: over a circuit both travel on one ordered connection.
    /// </summary>
    private void FlashRenders()
    {
        if (!Ui.FlashRenders || _module is null || Session.Browser.TargetId is not { } target)
        {
            return;
        }

        var since = _lastFlashTicks;
        _lastFlashTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        var rendered = Session.Components.All
            .Where(r => !r.IsBevTools && !r.IsDisposed && (Ui.ShowHiddenComponents || !r.IsHidden) && r.LastRenderTicks > since)
            .Select(r =>
            {
                var count = r.RenderCount - _flashCounts.GetValueOrDefault(r.InstanceId, r.RenderCount - 1);
                _flashCounts[r.InstanceId] = r.RenderCount;
                return (Record: r, Count: Math.Max(1, count));
            })
            .OrderByDescending(r => r.Count)
            .Take(MaxFlashesPerTick)
            .ToList();
        if (rendered.Count == 0)
        {
            return;
        }

        // One pass, one batch: the rows of a list share their parent's frames, walked once.
        var batch = new BevDomMapBatch();
        var items = new List<object>(rendered.Count);
        foreach (var (record, count) in rendered)
        {
            if (BevDomMap.Locate(Session, record, out _, batch) is { } location)
            {
                items.Add(new { location, count });
            }
        }

        if (items.Count > 0)
        {
            _ = FlashAsync(target, items);
        }

        // Counts of components that are gone; bounded by the registry, which forgets them too.
        if (_flashCounts.Count > 2 * Session.Components.TrackedCount + 256)
        {
            foreach (var id in _flashCounts.Keys.Where(id => Session.Components.Get(id) is null or { IsDisposed: true }).ToList())
            {
                _flashCounts.Remove(id);
            }
        }
    }

    private async Task FlashAsync(string target, List<object> items)
    {
        try
        {
            await _module!.InvokeVoidAsync("flash", target, items);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException or ObjectDisposedException)
        {
        }
    }

    private void SetOpen(bool open)
    {
        if (Ui.IsOpen == open)
        {
            return;
        }

        Ui.IsOpen = open;
        if (open)
        {
            RefreshCapturedData();
            _viewRevision++;
        }

        Ui.Touch();
        _ = SavePrefsAsync();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        // Redacted at capture time like HTTP URLs: reset links and sign-in callbacks carry tokens in the query or the
        // fragment, and the title is shown, searched and exported.
        var path = Session.Redactor.RedactUrl(Navigation.ToBaseRelativePath(e.Location));
        // On .NET 10 the router's Navigate activity records the navigation first (the router handles LocationChanged
        // before this panel does), so the event is already there: once, for this one LocationChanged. Matching by title
        // would drop real navigations - /products right after /products/1 - and the activity names a route template, not
        // the path, anyway.
        var cause = e.IsNavigationIntercepted ? "intercepted link" : "programmatic";

        // The last accessibility check was of the page that was: its findings would be reported against this one.
        if (Session.Browser.Accessibility is not null)
        {
            Session.Browser.Accessibility = null;
            Session.Browser.AccessibilityCheckedAt = null;
        }
        var last = Session.LastNavigationEventId is { } id ? Session.Timeline.Find(id) : null;
        if (last is not null && last.Id != Session.LastLocationChangedEventId && last.Id != Session.MatchedNavigationEventId
            && System.Diagnostics.Stopwatch.GetElapsedTime(last.StartTicks).TotalMilliseconds < 500)
        {
            Session.MatchedNavigationEventId = last.Id;
            Session.NoteNavigationCause(last.Id, cause);
            return;
        }

        // The router handles LocationChanged before this panel and renders the new page right there, so on this path (no
        // Navigate activity: .NET 8 and 9, or framework instrumentation off) the page's first renders have happened, in
        // the batch still open now. Dated from that batch the navigation comes before them, as it did; dated now, each
        // page's first render would be put on the page it left (Profiler → Renders per page).
        var startTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        if (Session.RenderTracker.CurrentBatch is { Closed: false, IsTracked: true } batch
            && (last is null || batch.StartTicks > last.StartTicks)
            && System.Diagnostics.Stopwatch.GetElapsedTime(batch.StartTicks).TotalSeconds < 1)
        {
            startTicks = batch.StartTicks;
        }

        var recordedId = Session.Timeline.Record(new BevToolsEvent
        {
            Kind = BevToolsEventKind.Navigation,
            Category = "navigation",
            Title = "Navigate to /" + path,
            Detail = cause,
            ParentEventId = Session.CurrentTriggerEventId,
            StartTicks = startTicks,
            Timestamp = DateTimeOffset.UtcNow - System.Diagnostics.Stopwatch.GetElapsedTime(startTicks),
        });
        if (recordedId > 0)
        {
            Session.NoteNavigationCause(recordedId, cause);
        }

        // Not recorded while capture is paused: there is no event to point at.
        if (recordedId > 0)
        {
            Session.LastNavigationEventId = recordedId;
            Session.LastLocationChangedEventId = recordedId;
        }
    }

    private void ApplySnapshot(BevBrowserSnapshot? s)
    {
        if (s is null)
        {
            return;
        }

        var b = Session.Browser;
        b.Online = s.Online;
        b.Visibility = s.Visibility;
        b.UserAgent = s.UserAgent;
        b.JsHeapUsedMb = s.JsHeapUsedMb;
        b.JsHeapLimitMb = s.JsHeapLimitMb;
        b.NavigationLoadMs = s.NavigationLoadMs;
        b.DomContentLoadedMs = s.DomContentLoadedMs;
        b.ResourceCount = s.ResourceCount;
    }

    private void ApplyPrefs(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        try
        {
            var prefs = JsonSerializer.Deserialize<Prefs>(json, PrefsJson);
            if (prefs is null)
            {
                return;
            }

            if (prefs.Open is { } open && !Ui.OpenRequested)
            {
                Ui.IsOpen = open;
            }

            if (Enum.TryParse<BevToolsDock>(prefs.Dock, true, out var dock))
            {
                Ui.Dock = dock;
            }

            if (Enum.TryParse<BevToolsTheme>(prefs.Theme, true, out var theme))
            {
                Ui.Theme = theme;
            }

            if (Enum.TryParse<BevPanelSize>(prefs.Size, true, out var size))
            {
                Ui.Size = size;
            }

            if (!string.IsNullOrEmpty(prefs.Panel) && IsPanelAvailable(prefs.Panel))
            {
                Ui.ActivePanelId = prefs.Panel;
            }

            if (prefs.Live is { } live)
            {
                Ui.LiveUpdates = live;
            }

            if (prefs.Flash is { } flash)
            {
                Ui.FlashRenders = flash;
                _lastFlashTicks = System.Diagnostics.Stopwatch.GetTimestamp();
            }
        }
        catch (JsonException)
        {
        }
    }

    private bool IsPanelAvailable(string panelId) =>
        BuiltInPanels.Any(p => string.Equals(p.Id, panelId, StringComparison.Ordinal)) ||
        ExtensionPanels.Any(p => string.Equals(p.Id, panelId, StringComparison.Ordinal));

    private async Task SavePrefsAsync()
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            var prefs = new Prefs { Open = Ui.IsOpen, Dock = Ui.Dock.ToString(), Theme = Ui.Theme.ToString(), Size = Ui.Size.ToString(), Panel = Ui.ActivePanelId, Live = Ui.LiveUpdates, Flash = Ui.FlashRenders };
            await _module.InvokeVoidAsync("savePrefs", JsonSerializer.Serialize(prefs, PrefsJson));
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException)
        {
        }
    }

    private sealed class Prefs
    {
        public bool? Open { get; set; }

        public string? Dock { get; set; }

        public string? Theme { get; set; }

        public string? Size { get; set; }

        public string? Panel { get; set; }

        public bool? Live { get; set; }

        public bool? Flash { get; set; }
    }

    // ----- IBevToolsCommandContext -----

    public void ShowPanel(string panelId, string? query = null)
    {
        var wasClosed = !Ui.IsOpen;
        if (!wasClosed && Ui.ActivePanelId != panelId)
        {
            // A link in one tab that shows another (a form's owner, an event, a request) removes the element that had
            // focus: focus goes to the new tab, as in any tab list - but only from inside the panel, never out of the app.
            _focusAfterRender ??= (".bev-tab.bev-active", true);
        }

        Ui.ActivePanelId = panelId;
        Ui.IsOpen = true;
        if (wasClosed)
        {
            RefreshCapturedData();
        }

        if (query is not null)
        {
            Ui.SetQuery(panelId, query);
        }

        Ui.CommandPaletteOpen = false;
        Ui.Touch();
        _viewRevision++;
        _ = SavePrefsAsync();
        StateHasChanged();
    }

    public void SelectComponent(long instanceId)
    {
        InspectComponent(instanceId);
        var record = Session.Components.Get(instanceId);
        if (record is not null)
        {
            // expand ancestors so the node is visible
            var parent = record.Parent;
            while (parent is not null)
            {
                Ui.CollapsedTreeNodes.Remove(parent.InstanceId);
                parent = parent.Parent;
            }
        }

        ShowPanel("components");
    }

    /// <summary>
    /// Makes a component the inspected one: its state is captured on every render from now on, and the previously
    /// inspected one stops being captured (unless every component is). The one implementation behind selecting from
    /// the tree and from anywhere else.
    /// </summary>
    internal void InspectComponent(long instanceId)
    {
        if (!Options.CaptureComponentStateOnRender && Ui.SelectedComponentId is { } previousId && previousId != instanceId
            && Session.Components.Get(previousId) is { } previous)
        {
            previous.CaptureState = false;
        }

        Ui.SelectedComponentId = instanceId;
        if (Session.Components.Get(instanceId) is { } record)
        {
            record.CaptureState = true;
        }
    }

    public void SelectEvent(long eventId)
    {
        Ui.SelectedEventId = eventId;
        ShowPanel("timeline");
    }

    public void SelectError(long errorId)
    {
        Ui.SelectedErrorId = errorId;
        ShowPanel("errors");
    }

    public void SelectHttp(long requestId)
    {
        Ui.SelectedHttpId = requestId;
        ShowPanel("network");
    }

    public void Notify(string message)
    {
        Ui.Notify(message);
        StateHasChanged();
    }

    public void PersistPreferences() => _ = SavePrefsAsync();

    public void RefreshView() => Refresh();

    public void Refresh()
    {
        RefreshCapturedData();
        _viewRevision++;
        StateHasChanged();
    }

    private void OnLiveChanged(ChangeEventArgs e)
    {
        Ui.LiveUpdates = e.Value is true;
        Ui.Touch();
        _ = SavePrefsAsync();
        if (Ui.LiveUpdates)
        {
            Refresh();
        }
    }

    public void Toggle()
    {
        Ui.IsOpen = !Ui.IsOpen;
        // Opened: focus goes to the panel's active tab. Closed: to the badge, but only if focus was in the panel - a
        // shortcut pressed in the app must not take focus away from it.
        _focusAfterRender = (Ui.IsOpen ? ".bev-tab.bev-active" : ".bev-badge", false);
        if (Ui.IsOpen)
        {
            RefreshCapturedData();
        }

        Ui.Touch();
        _ = SavePrefsAsync();
    }

    /// <summary>Pauses or resumes capture and re-reads the open tab so the marker event shows up immediately.</summary>
    public void ToggleCapture()
    {
        if (Session.IsCapturing)
        {
            Session.PauseCapture();
        }
        else
        {
            Session.ResumeCapture();
        }

        Ui.Touch();
        Refresh();
    }

    /// <summary>Part of <see cref="IBevToolsCommandContext"/>: copies text through the browser bridge.</summary>
    public Task<bool> CopyTextAsync(string text) => CopyAsync(text);

    /// <summary>Part of <see cref="IBevToolsCommandContext"/>: offers a text file for download through the browser bridge.</summary>
    public async Task<bool> DownloadTextAsync(string fileName, string content, string contentType = "text/plain")
    {
        if (_module is null)
        {
            return false;
        }

        try
        {
            return await _module.InvokeAsync<bool>("downloadText", fileName, content, contentType);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>
    /// Brings the tree and the findings up to date immediately. Without this the panel opens on whatever the polling
    /// loop last produced, so the component tree can be blank for the first moments after opening — which reads as
    /// "BevTools sees nothing" exactly when the developer is forming their first impression of it.
    /// </summary>
    private void RefreshCapturedData()
    {
        try
        {
            Session.RenderTracker.CloseBatch();
            Session.Components.Refresh(force: true);
            Session.Diagnostics.Evaluate(force: true);
            _lastVersion = Session.Version;
            _lastUiVersion = Ui.Version;
        }
        catch (Exception ex)
        {
            Session.Errors.Record(ex, "bevtools", "Refreshing captured data failed.");
        }
    }

    public async Task<bool> CopyAsync(string text)
    {
        if (_module is null)
        {
            return false;
        }

        try
        {
            return await _module.InvokeAsync<bool>("copyText", text);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException)
        {
            return false;
        }
    }

    public async Task LoadStorageAsync()
    {
        if (_module is null)
        {
            Session.Browser.StorageError = "The browser bridge module is not loaded in this BevTools panel.";
            Ui.Touch();
            return;
        }

        Session.Browser.StorageReading = true;
        Session.Browser.StorageError = null;
        Ui.Touch();
        try
        {
            var patterns = Options.SensitiveNamePatterns.ToArray();
            // Under ValueAccess None no value leaves the browser: keys and sizes only.
            var previews = Session.ValueAccess != BevValueAccess.None;
            Session.Browser.LocalStorage = await _module.InvokeAsync<BevStorageEntry[]>("getStorage", "local", patterns, previews);
            Session.Browser.SessionStorage = await _module.InvokeAsync<BevStorageEntry[]>("getStorage", "session", patterns, previews);
            Session.Browser.StorageReadAt = DateTimeOffset.UtcNow;
            Session.Browser.StorageError = null;
            ApplySnapshot(await _module.InvokeAsync<BevBrowserSnapshot>("refresh"));
        }
        catch (Exception ex)
        {
            Session.Browser.StorageError = ex.GetType().Name + ": " + ex.Message;
            Session.Errors.Record(ex, "bevtools", "Reading browser storage failed.");
        }
        finally
        {
            Session.Browser.StorageReading = false;
        }

        Ui.Touch();
    }

    /// <summary>Runs the page hub's accessibility checks and keeps the findings for the Browser panel, the rule and exports.</summary>
    public async Task CheckAccessibilityAsync(bool includeBevTools = false)
    {
        if (_module is null)
        {
            Session.Browser.AccessibilityError = "The browser bridge module is not loaded in this BevTools panel.";
            Ui.Touch();
            return;
        }

        try
        {
            Session.Browser.Accessibility = await _module.InvokeAsync<BevA11yFinding[]>("auditPage", new { includeBevTools });
            Session.Browser.AccessibilityCheckedAt = DateTimeOffset.UtcNow;
            Session.Browser.AccessibilityError = null;
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException)
        {
            Session.Browser.AccessibilityError = ex.GetType().Name + ": " + ex.Message;
        }

        Session.Diagnostics.Evaluate(force: true);
        Session.Touch();
        Refresh();
    }

    internal async Task HighlightFindingAsync(int index, string label)
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.InvokeAsync<bool>("highlightFinding", index, label);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException)
        {
        }
    }

    private void SetTheme()
    {
        Ui.Theme = Ui.Theme switch { BevToolsTheme.Auto => BevToolsTheme.Dark, BevToolsTheme.Dark => BevToolsTheme.Light, _ => BevToolsTheme.Auto };
        Ui.Touch();
        _ = SavePrefsAsync();
    }

    private void SetDock()
    {
        Ui.Dock = Ui.Dock == BevToolsDock.Bottom ? BevToolsDock.Right : BevToolsDock.Bottom;
        Ui.Touch();
        _ = SavePrefsAsync();
    }

    private void SetSize()
    {
        Ui.Size = Ui.Size switch { BevPanelSize.Small => BevPanelSize.Medium, BevPanelSize.Medium => BevPanelSize.Large, _ => BevPanelSize.Small };
        Ui.Touch();
        _ = SavePrefsAsync();
    }

    private void OpenPalette(string? initial = null)
    {
        _searchText = initial ?? "";
        Ui.CommandPaletteOpen = true;
        Ui.Touch();
    }

    private void OnRootKeyDown(KeyboardEventArgs e)
    {
        // The palette handles its own keys and stops them; this is Escape with focus elsewhere while it is open.
        if (e.Key == "Escape" && Ui.CommandPaletteOpen)
        {
            Ui.CommandPaletteOpen = false;
            Ui.Touch();
        }
        else if (e.Key == "Escape" && Ui.IsOpen && !Ui.Picking)
        {
            // Escape anywhere in the panel closes it, and focus goes back to the badge that reopens it.
            Toggle();
        }
        else if ((e.CtrlKey || e.MetaKey) && (e.Key == "k" || e.Key == "K"))
        {
            OpenPalette();
        }
    }

    /// <summary>Where focus goes after the next render, and whether only when it was in the panel (or lost) before.</summary>
    private (string Selector, bool OnlyFromPanel)? _focusAfterRender;

    /// <summary>Moves focus after the next render (the palette asks for it when it closes).</summary>
    internal void FocusAfterRender(string selector, bool onlyFromPanel = true)
    {
        _focusAfterRender = (selector, onlyFromPanel);
        StateHasChanged();
    }

    /// <summary>The tab list's keyboard model: arrows move to the previous or next tab and show it, Home and End to the ends.</summary>
    private void OnTabKeyDown(KeyboardEventArgs e)
    {
        var ids = BuiltInPanels.Select(p => p.Id).Concat(ExtensionPanels.Select(p => p.Id)).ToList();
        var index = ids.IndexOf(Ui.ActivePanelId);
        var next = e.Key switch
        {
            "ArrowRight" or "ArrowDown" => (index + 1) % ids.Count,
            "ArrowLeft" or "ArrowUp" => (index - 1 + ids.Count) % ids.Count,
            "Home" => 0,
            "End" => ids.Count - 1,
            _ => -1,
        };
        if (next < 0)
        {
            return;
        }

        ShowPanel(ids[next]);
        _focusAfterRender = (".bev-tab.bev-active", false);
    }

    /// <summary>Moves focus within this panel once the render that shows the element has happened.</summary>
    private async Task FocusAfterRenderAsync()
    {
        if (_focusAfterRender is not { } focus || _module is null || Session.Browser.TargetId is not { } target)
        {
            return;
        }

        _focusAfterRender = null;
        try
        {
            await _module.InvokeVoidAsync("focusIn", target, focus.Selector, focus.OnlyFromPanel);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException)
        {
        }
    }

    private string ThemeAttribute => Ui.Theme.ToString().ToLowerInvariant();

    // While picking, the panel shrinks to its header: what it covers of the page has to be reachable.
    private string PanelClass => $"bev-panel bev-dock-{Ui.Dock.ToString().ToLowerInvariant()} bev-size-{Ui.Size.ToString().ToLowerInvariant()}" + (Ui.Picking ? " bev-picking" : "");

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        if (Session is not null)
        {
            // Picking is the page's: a panel rendered later starts expanded, not waiting for a pick nobody will finish.
            Session.Ui.Picking = false;
        }

        _prerenderHandoff?.Dispose();
        if (Session is not null)
        {
            RuntimeAttachment.OnPanelDisposed(Session);
        }

        if (_loop is not null)
        {
            try
            {
                await _loop;
            }
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
            {
            }
        }

        if (_interactive && Session is not null)
        {
            Navigation.LocationChanged -= OnLocationChanged;
            Ui.OpenedFromCode -= OnOpenedFromCode;
            Session.SymbolsWanted -= OnSymbolsWanted;
        }

        if (_module is not null)
        {
            try
            {
                // Only this runtime: the page can hold another one (a Web App's circuit next to its WebAssembly runtime).
                await _module.InvokeVoidAsync("detach", Session?.Browser.TargetId);
                await _module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSException or JSDisconnectedException or ObjectDisposedException or OperationCanceledException)
            {
            }
        }

        _bridgeRef?.Dispose();
        _cts.Dispose();
    }
}
