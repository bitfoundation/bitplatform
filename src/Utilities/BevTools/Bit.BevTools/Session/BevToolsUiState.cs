namespace Bit.BevTools;

/// <summary>UI state that survives navigation within a session (circuit).</summary>
public sealed class BevToolsUiState
{
    private readonly Dictionary<string, string?> _queries = new(StringComparer.OrdinalIgnoreCase);

    public bool IsOpen { get; set; }

    public string ActivePanelId { get; set; } = "components";

    public BevToolsDock Dock { get; set; }

    public BevToolsTheme Theme { get; set; }

    public BevToolsPanelSize Size { get; set; } = BevToolsPanelSize.Medium;

    public long? SelectedComponentId { get; set; }

    public long? SelectedEventId { get; set; }

    public long? SelectedErrorId { get; set; }

    public long? SelectedHttpId { get; set; }

    public string? SelectedStateProvider { get; set; }

    public bool CommandPaletteOpen { get; set; }

    public bool ShowHiddenComponents { get; set; }

    /// <summary>Outline components in the page as they render, coloured by how often (persisted with the other preferences).</summary>
    public bool FlashRenders { get; set; }

    /// <summary>True while the page is in pick mode, started from this panel.</summary>
    public bool Picking { get; set; }

    /// <summary>When true, the open tab re-reads captured data on the refresh cadence. Off means Refresh is required.</summary>
    public bool LiveUpdates { get; set; } = true;

    /// <summary>How long a notification shows before it goes away by itself.</summary>
    public static readonly TimeSpan NotificationLifetime = TimeSpan.FromSeconds(4);

    public string? Notification { get; set; }

    public DateTimeOffset NotificationAt { get; set; }

    /// <summary>Which timeline kinds are visible.</summary>
    public HashSet<BevToolsEventKind> HiddenTimelineKinds { get; } = [];

    public HashSet<long> CollapsedTreeNodes { get; } = [];

    /// <summary>The Profiler's sort, grouping, selected batch and comparison, kept while other tabs are open.</summary>
    internal BevToolsProfilerView Profiler { get; } = new();

    /// <summary>The Network panel's view (requests or endpoints), endpoint filter and sorts.</summary>
    internal BevToolsNetworkView Network { get; } = new();

    /// <summary>The Errors panel's view (errors or groups), group filter and sorts.</summary>
    internal BevToolsErrorsView Errors { get; } = new();

    /// <summary>The log entry selected in the Logs panel.</summary>
    public long? SelectedLogId { get; set; }

    /// <summary>The Logs panel's view (entries or categories).</summary>
    internal BevToolsLogsView Logs { get; } = new();

    public string? GetQuery(string panelId) => _queries.TryGetValue(panelId, out var q) ? q : null;

    public void SetQuery(string panelId, string? query) => _queries[panelId] = query;

    /// <summary>Incremented whenever the UI itself changes state so the host re-renders even without new activity.</summary>
    public long Version { get; private set; }

    public void Touch() => Version++;

    /// <summary>
    /// Set by <see cref="Open(string?, string?)"/> until a panel has reconciled it with the saved preferences, so a saved preference to
    /// keep the panel closed does not close it again right after the app opened it.
    /// </summary>
    internal bool OpenRequested { get; set; }

    /// <summary>Raised by <see cref="Open(string?, string?)"/>, so a panel that is already rendered shows it at once instead of on its next poll.</summary>
    internal event Action? OpenedFromCode;

    /// <summary>
    /// Set by <see cref="Open(string?, string?)"/> with a panel until a panel has read the saved preferences, so the
    /// panel used last time does not replace the one the app asked for.
    /// </summary>
    internal bool PanelRequested { get; set; }

    /// <summary>
    /// Opens the panel from code, for example from the app's own "Open dev tools" button. A rendered panel opens at
    /// once; one rendered later opens even when the saved preferences say it was closed last time.
    /// </summary>
    public void Open() => Open(panelId: null, query: null);

    /// <summary>
    /// Opens the panel from code on <paramref name="panelId"/> (a built-in panel's id, such as <c>"logs"</c>, or one an
    /// extension or <c>BevToolsOptions.AddPanel</c> added), with <paramref name="query"/> in its filter when given; a null
    /// <paramref name="panelId"/> keeps the panel that was shown. As <see cref="Open()"/>, it wins over the saved
    /// preferences of a panel rendered later.
    /// </summary>
    public void Open(string? panelId, string? query = null)
    {
        if (panelId is not null)
        {
            ActivePanelId = panelId;
            PanelRequested = true;
            if (query is not null)
            {
                SetQuery(panelId, query);
            }
        }

        IsOpen = true;
        OpenRequested = true;
        Touch();
        OpenedFromCode?.Invoke();
    }

    public void Notify(string message)
    {
        Notification = message;
        NotificationAt = DateTimeOffset.UtcNow;
        Touch();
    }
}
