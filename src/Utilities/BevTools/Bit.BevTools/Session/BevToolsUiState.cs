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
    internal BevProfilerView Profiler { get; } = new();

    /// <summary>The Network panel's view (requests or endpoints), endpoint filter and sorts.</summary>
    internal BevNetworkView Network { get; } = new();

    /// <summary>The Errors panel's view (errors or groups), group filter and sorts.</summary>
    internal BevErrorsView Errors { get; } = new();

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

/// <summary>A table's sort: the column and its direction. Selecting the sorted column again reverses it.</summary>
internal sealed class BevSortState(string key, bool ascending)
{
    public string Key { get; private set; } = key;

    public bool Ascending { get; private set; } = ascending;

    public void Set(string key, bool ascending)
    {
        Key = key;
        Ascending = ascending;
    }

    /// <param name="key">The column selected.</param>
    /// <param name="ascendingFirst">The direction a column starts in: text A to Z, figures largest first.</param>
    public void Select(string key, bool ascendingFirst)
    {
        if (Key == key)
        {
            Ascending = !Ascending;
        }
        else
        {
            Key = key;
            Ascending = ascendingFirst;
        }
    }
}

internal sealed class BevNetworkView
{
    /// <summary>One row per endpoint instead of one per request.</summary>
    public bool ByEndpoint { get; set; }

    /// <summary>Only the requests to this <see cref="BevHttpRecord.EndpointTemplate"/>.</summary>
    public string? Endpoint { get; set; }

    public BevSortState Requests { get; } = new("time", ascending: false);

    public BevSortState Endpoints { get; } = new("requests", ascending: false);
}

internal sealed class BevErrorsView
{
    /// <summary>One row per group of errors that are one problem (<see cref="BevErrorGroups"/>).</summary>
    public bool ByGroup { get; set; }

    /// <summary>Only the errors of this <see cref="BevErrorRecord.Group"/>, shown by its <see cref="GroupTitle"/>.</summary>
    public string? Group { get; set; }

    public string? GroupTitle { get; set; }

    public BevSortState Errors { get; } = new("last", ascending: false);

    public BevSortState Groups { get; } = new("occurrences", ascending: false);
}

internal sealed class BevProfilerView
{
    public BevSortState Sorting { get; } = new("total", ascending: false);

    public bool ByType { get; set; }

    public bool SinceBaseline { get; set; }

    public bool IncludeDisposed { get; set; }

    /// <summary>The render batch whose cost tree is shown, and its timeline event (gone once the timeline is cleared).</summary>
    public long? BatchId { get; set; }

    public long? BatchEventId { get; set; }

    /// <summary>The selected batch as a tree (self and subtree time) or on a time axis.</summary>
    public bool BatchOnTimeAxis { get; set; }

    /// <summary>Whether the renders-per-page section is open; it reads every retained render, so only while it is.</summary>
    public bool ShowRoutes { get; set; }

    /// <summary>The uploaded export's profile, so the comparison can be run again after another interaction.</summary>
    public BevRenderProfile? ComparedProfile { get; set; }

    public string? ComparedFile { get; set; }

    public BevProfileComparisonResult? Comparison { get; set; }

    public string? ComparisonError { get; set; }
}
