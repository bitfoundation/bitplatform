namespace Bit.BevTools;

public enum BevPanelSize
{
    Small,
    Medium,
    Large,
}

/// <summary>UI state that survives navigation within a session (circuit).</summary>
public sealed class BevToolsUiState
{
    private readonly Dictionary<string, string?> _queries = new(StringComparer.OrdinalIgnoreCase);

    public bool IsOpen { get; set; }

    public string ActivePanelId { get; set; } = "components";

    public BevToolsDock Dock { get; set; }

    public BevToolsTheme Theme { get; set; }

    public BevPanelSize Size { get; set; } = BevPanelSize.Medium;

    public long? SelectedComponentId { get; set; }

    public long? SelectedEventId { get; set; }

    public long? SelectedErrorId { get; set; }

    public long? SelectedHttpId { get; set; }

    public string? SelectedStateProvider { get; set; }

    public bool CommandPaletteOpen { get; set; }

    public bool ShowHiddenComponents { get; set; }

    /// <summary>When true, the open tab re-reads captured data on the refresh cadence. Off means Refresh is required.</summary>
    public bool LiveUpdates { get; set; } = true;

    public string? Notification { get; set; }

    public DateTimeOffset NotificationAt { get; set; }

    /// <summary>Which timeline kinds are visible.</summary>
    public HashSet<BevToolsEventKind> HiddenTimelineKinds { get; } = [];

    public HashSet<long> CollapsedTreeNodes { get; } = [];

    public string? GetQuery(string panelId) => _queries.TryGetValue(panelId, out var q) ? q : null;

    public void SetQuery(string panelId, string? query) => _queries[panelId] = query;

    /// <summary>Incremented whenever the UI itself changes state so the host re-renders even without new activity.</summary>
    public long Version { get; private set; }

    public void Touch() => Version++;

    /// <summary>
    /// Set by <see cref="Open"/> until a panel has reconciled it with the saved preferences, so a saved preference to
    /// keep the panel closed does not close it again right after the app opened it.
    /// </summary>
    internal bool OpenRequested { get; set; }

    /// <summary>Raised by <see cref="Open"/>, so a panel that is already rendered shows it at once instead of on its next poll.</summary>
    internal event Action? OpenedFromCode;

    /// <summary>
    /// Opens the panel from code, for example from the app's own "Open dev tools" button. A rendered panel opens at
    /// once; one rendered later opens even when the saved preferences say it was closed last time.
    /// </summary>
    public void Open()
    {
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
