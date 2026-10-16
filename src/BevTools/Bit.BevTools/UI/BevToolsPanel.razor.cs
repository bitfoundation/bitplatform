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
    private string _searchText = "";
    private Task? _loop;

    [Inject] public BevToolsSession Session { get; set; } = default!;

    [Inject] public BitBevToolsOptions Options { get; set; } = default!;

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
        ("network", "Network"),
        ("interop", "JS Interop"),
        ("errors", "Errors"),
        ("services", "Services"),
        ("diagnostics", "Diagnostics"),
        ("browser", "Browser"),
        ("about", "About"),
    ];

    protected override void OnInitialized()
    {
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
            return;
        }

        Session.EnsureActivated();
        Navigation.LocationChanged += OnLocationChanged;
    }

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
        if (!firstRender || !_interactive || !Session.IsEnabled)
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
                    () => InvokeAsync(() => { OpenPalette(); StateHasChanged(); })));
                var snapshot = await _module.InvokeAsync<BevBrowserSnapshot>("attach", _bridgeRef, new { shortcut = Options.Ui.ToggleShortcut, trackJsToDotNet = Options.TrackJsInterop });
                ApplySnapshot(snapshot);
                Session.Browser.BridgeAttached = true;
            }

            var prefs = await _module.InvokeAsync<string?>("loadPrefs");
            ApplyPrefs(prefs);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException)
        {
            Session.Browser.BridgeError = ex.Message;
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
                await Task.Delay(Ui.IsOpen ? Math.Max(50, Options.Ui.RefreshIntervalMs) : 1000, token);
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
        var dataChanged = Session.Version != _lastVersion;
        var uiChanged = Ui.Version != _lastUiVersion;
        if (!dataChanged && !uiChanged)
        {
            return;
        }

        Session.RenderTracker.CloseBatch();
        _lastVersion = Session.Version;
        _lastUiVersion = Ui.Version;

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

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        var last = Session.LastNavigationEventId is { } id ? Session.Timeline.Find(id) : null;
        if (last is not null && System.Diagnostics.Stopwatch.GetElapsedTime(last.StartTicks).TotalMilliseconds < 500 && last.Title.Contains(Navigation.ToBaseRelativePath(e.Location), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var recordedId = Session.Timeline.Record(new BevToolsEvent
        {
            Kind = BevToolsEventKind.Navigation,
            Category = "navigation",
            Title = "Navigate to /" + Navigation.ToBaseRelativePath(e.Location),
            Detail = e.IsNavigationIntercepted ? "intercepted link" : "programmatic",
            ParentEventId = Session.CurrentTriggerEventId,
        });

        // Not recorded while capture is paused: there is no event to point at.
        if (recordedId > 0)
        {
            Session.LastNavigationEventId = recordedId;
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

            if (prefs.Open is { } open)
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
            var prefs = new Prefs { Open = Ui.IsOpen, Dock = Ui.Dock.ToString(), Theme = Ui.Theme.ToString(), Size = Ui.Size.ToString(), Panel = Ui.ActivePanelId, Live = Ui.LiveUpdates };
            await _module.InvokeVoidAsync("savePrefs", JsonSerializer.Serialize(prefs, PrefsJson));
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException)
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
    }

    // ----- IBevToolsCommandContext -----

    public void ShowPanel(string panelId, string? query = null)
    {
        var wasClosed = !Ui.IsOpen;
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
        if (!Options.CaptureComponentStateOnRender && Ui.SelectedComponentId is { } previousId && previousId != instanceId)
        {
            var previous = Session.Components.Get(previousId);
            if (previous is not null)
            {
                previous.CaptureState = false;
            }
        }

        Ui.SelectedComponentId = instanceId;
        var record = Session.Components.Get(instanceId);
        if (record is not null)
        {
            record.CaptureState = true;
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
        catch (Exception ex) when (ex is JSException or JSDisconnectedException)
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
        catch (Exception ex) when (ex is JSException or JSDisconnectedException)
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
            Session.Browser.LocalStorage = await _module.InvokeAsync<BevStorageEntry[]>("getStorage", "local", patterns);
            Session.Browser.SessionStorage = await _module.InvokeAsync<BevStorageEntry[]>("getStorage", "session", patterns);
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
        if (e.Key == "Escape" && Ui.CommandPaletteOpen)
        {
            Ui.CommandPaletteOpen = false;
            Ui.Touch();
        }
        else if ((e.CtrlKey || e.MetaKey) && (e.Key == "k" || e.Key == "K"))
        {
            OpenPalette();
        }
    }

    private string ThemeAttribute => Ui.Theme.ToString().ToLowerInvariant();

    private string PanelClass => $"bev-panel bev-dock-{Ui.Dock.ToString().ToLowerInvariant()} bev-size-{Ui.Size.ToString().ToLowerInvariant()}";

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
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

        if (_interactive)
        {
            Navigation.LocationChanged -= OnLocationChanged;
        }

        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("detach");
                await _module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSException or JSDisconnectedException or ObjectDisposedException)
            {
            }
        }

        _bridgeRef?.Dispose();
        _cts.Dispose();
    }
}
