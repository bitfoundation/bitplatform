using Microsoft.AspNetCore.WebUtilities;

namespace Boilerplate.Workbench.Components.Shell;

/// <summary>
/// The shell's own state: the open views (panes) and their scenarios, the selected view, the zoom, the mode, the tab
/// and the reviewer's settings. Saved in the browser so a reload comes back to the same views; the address mirrors it.
/// </summary>
[BlexStore(Name = "workbench", Persist = true)]
public partial class WorkbenchStore
{
    // The query keys that carry a scenario; an address without any of them leaves the saved views as they are.
    private static readonly string[] scenarioKeys = ["data", "theme", "culture", "chrome", "auth", "user", "device", "size", "rotated", "frame", "backdrop", "caption", "variant", "accent", "text", "motion", "vision", "panes"];

    [BlexState] private WorkbenchPane[] _panes = [new("1", new())];
    [BlexState] private string _selectedPaneId = "1";
    [BlexState] private string _zoom = "fit";
    [BlexState] private int _lastPaneId = 1;
    // A mode is a passing choice, so a reload starts in Browse.
    [BlexState(Persist = false)] private string _mode = WorkbenchModes.Browse;
    [BlexState] private string _column = WorkbenchModes.ViewColumn;
    [BlexState] private string? _reviewerName;
    [BlexState] private bool _replyNotifications;
    [BlexState] private bool _wasAskedToNotify;
    [BlexState] private bool _isReplySoundMuted;
    [BlexState] private bool _isNavigatorCollapsed;
    [BlexState] private bool _isInspectorCollapsed;
    [BlexState] private bool _isComponentNoteDismissed;

    /// <summary>The selected view, or the first one when no view has the selected id.</summary>
    public WorkbenchPane SelectedPane => Panes.FirstOrDefault(p => p.Id == SelectedPaneId) ?? Panes[0];

    /// <summary>
    /// The address query of the open views: one view's scenario keys, or every view packed into <c>panes</c>, plus the
    /// zoom when it isn't "fit".
    /// </summary>
    public string QueryString => BuildQueryString([.. Panes.Select(p => p.Scenario)]);

    /// <summary>
    /// The address query for a link to another page: the same views without the current page's design option and
    /// switches, which mean nothing there.
    /// </summary>
    public string OtherPageQueryString => BuildQueryString([.. Panes.Select(p => p.Scenario with { Variant = null, Switches = null })]);

    private string BuildQueryString(WorkbenchScenario[] scenarios)
    {
        var query = scenarios.Length == 1
            ? scenarios[0].ToQueryString()
            : QueryHelpers.AddQueryString(string.Empty, "panes", WorkbenchUrls.EncodePanes(scenarios));

        return Zoom == "fit" ? query : QueryHelpers.AddQueryString(query, "zoom", Zoom);
    }

    /// <summary>
    /// Applies a shell address, such as a shared link or a step back, to the views, unless it names no scenario or
    /// matches the views already open.
    /// </summary>
    [BlexAction]
    private void OnOpenUrl(string query)
    {
        var values = QueryHelpers.ParseQuery(query);

        if (values.TryGetValue("zoom", out var zoom) && string.IsNullOrWhiteSpace(zoom) is false)
        {
            Zoom = zoom.ToString();
        }

        if (scenarioKeys.Any(values.ContainsKey) is false && values.Keys.Any(key => key.StartsWith(WorkbenchScenario.SwitchQueryPrefix, StringComparison.Ordinal)) is false)
            return;

        var scenarios = values.TryGetValue("panes", out var panes) ? WorkbenchUrls.DecodePanes(panes) : [WorkbenchScenario.Parse(query)];

        if (scenarios.Length == 0 || scenarios.Select(s => s.ToQueryString()).SequenceEqual(Panes.Select(p => p.Scenario.ToQueryString())))
            return;

        Replace(scenarios);
    }

    /// <summary>Selects a view; an id no view has, such as a view just removed, is ignored.</summary>
    [BlexAction]
    private void OnSelectPane(string paneId)
    {
        if (Panes.Any(p => p.Id == paneId))
        {
            SelectedPaneId = paneId;
        }
    }

    [BlexAction] private void OnSetZoom(string zoom) => Zoom = zoom;

    /// <summary>Sets the mode; Edit text works in the side column, so it opens the column if it was folded away.</summary>
    [BlexAction]
    private void OnSetMode(string mode)
    {
        Mode = mode;

        if (mode == WorkbenchModes.Text)
        {
            IsInspectorCollapsed = false;
        }
    }

    /// <summary>Shows a tab of the side column, opening the column if it was folded away.</summary>
    [BlexAction]
    private void OnShowColumn(string column)
    {
        Column = column;
        IsInspectorCollapsed = false;
    }

    [BlexAction] private void OnToggleNavigator() => IsNavigatorCollapsed = IsNavigatorCollapsed is false;

    [BlexAction] private void OnToggleInspector() => IsInspectorCollapsed = IsInspectorCollapsed is false;

    [BlexAction] private void OnDismissComponentNote() => IsComponentNoteDismissed = true;

    [BlexAction] private void OnNameReviewer(string? name) => ReviewerName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();

    [BlexAction] private void OnSetReplyNotifications(bool on) => ReplyNotifications = on;

    [BlexAction] private void OnMarkAskedToNotify() => WasAskedToNotify = true;

    [BlexAction] private void OnToggleReplySound() => IsReplySoundMuted = IsReplySoundMuted is false;

    /// <summary>Sets the selected view's scenario and copies each setting that changed to the other views.</summary>
    [BlexAction]
    private void OnChangeScenario(WorkbenchScenario scenario)
    {
        var selected = SelectedPane;
        var previous = selected.Scenario;

        Panes = [.. Panes.Select(pane => pane.Id == selected.Id
            ? pane with { Scenario = scenario }
            : pane with { Scenario = Propagate(previous, scenario, pane.Scenario) })];
    }

    /// <summary>Takes a scenario that a view's page changed itself, such as through its own theme switch, for that view only.</summary>
    [BlexAction]
    private void OnReportScenario(string paneId, WorkbenchScenario scenario)
    {
        Panes = [.. Panes.Select(pane => pane.Id == paneId ? pane with { Scenario = scenario } : pane)];
    }

    /// <summary>Adds a copy of the selected view on another kind of device: a phone gets a tablet, a tablet a desktop, others a phone.</summary>
    [BlexAction]
    private void OnAddPane()
    {
        var source = SelectedPane.Scenario;
        var scenario = source.DevicePreset.Kind switch
        {
            DeviceKind.Phone or DeviceKind.Foldable => source with { Device = "ipad-pro-11", Rotated = false },
            DeviceKind.Tablet => source with { Device = "desktop-1536", Rotated = false },
            _ => source with { Device = DevicePresets.DefaultKey, Rotated = false }
        };

        var pane = new WorkbenchPane(NextPaneId(), scenario);

        Panes = [.. Panes, pane];
        SelectedPaneId = pane.Id;
    }

    [BlexAction]
    private void OnRemovePane(string paneId)
    {
        if (Panes.Length == 1)
            return;

        Panes = [.. Panes.Where(p => p.Id != paneId)];

        if (SelectedPaneId == paneId)
        {
            SelectedPaneId = Panes[0].Id;
        }
    }

    [BlexAction]
    private void OnResizePane(string paneId, int width, int height)
    {
        Panes = [.. Panes.Select(p => p.Id == paneId ? p with { Scenario = p.Scenario.WithSize(width, height) } : p)];
    }

    /// <summary>Replaces the views with one per design option of the page, the first showing the default.</summary>
    [BlexAction]
    private void OnCompareVariants(string[] variants)
    {
        var source = SelectedPane.Scenario;

        Replace([.. variants.Select((variant, index) => source with { Variant = index == 0 ? null : variant })]);
    }

    /// <summary>Replaces the views with one per value of a page switch, the first showing its default.</summary>
    [BlexAction]
    private void OnCompareSwitch(string key, string[] values)
    {
        var source = SelectedPane.Scenario;

        Replace([.. values.Select((value, index) => source.WithSwitch(key, index == 0 ? null : value))]);
    }

    [BlexAction]
    private void OnRotatePane(string paneId)
    {
        Panes = [.. Panes.Select(p => p.Id == paneId ? p with { Scenario = p.Scenario with { Rotated = p.Scenario.Rotated is false } } : p)];
    }

    /// <summary>
    /// Replaces the views with the selected scenario spread over devices, themes, languages, data states or sign-in
    /// states; "single" keeps only the selected view.
    /// </summary>
    [BlexAction]
    private void OnCompare(string mode)
    {
        var source = SelectedPane.Scenario with { Rotated = false };

        WorkbenchScenario[] scenarios = mode switch
        {
            "devices" => [source with { Device = DevicePresets.DefaultKey }, source with { Device = "ipad-pro-11" }, source with { Device = "desktop-1536" }],
            "themes" => [source with { Theme = WorkbenchThemeMode.Light }, source with { Theme = WorkbenchThemeMode.Dark }],
            "languages" => [.. GetCompareCultures().Select(culture => source with { Culture = culture })],
            "states" => [source with { Data = WorkbenchDataState.Live }, source with { Data = WorkbenchDataState.Loading }, source with { Data = WorkbenchDataState.Empty }, source with { Data = WorkbenchDataState.Error }],
            "accounts" => [source with { Auth = WorkbenchAuthState.SignedOut }, source with { Auth = WorkbenchAuthState.SignedIn }, source with { Auth = WorkbenchAuthState.MultiAccount }],
            _ => [SelectedPane.Scenario]
        };

        Replace([.. scenarios.DistinctBy(s => s.ToQueryString())]);
    }

    /// <summary>
    /// The languages Compare spreads a view over: the default one, the first right-to-left one and one that is neither,
    /// out of the languages the app speaks. Only the default one when the app speaks one language.
    /// </summary>
    private static string?[] GetCompareCultures()
    {
        if (CultureInfoManager.InvariantGlobalization)
            return [null];

        var defaultCulture = CultureInfoManager.DefaultCulture;
        var others = CultureInfoManager.SupportedCultures.Select(sc => sc.Culture).Where(c => c.Name != defaultCulture.Name).ToArray();
        var rightToLeft = others.FirstOrDefault(c => c.TextInfo.IsRightToLeft);
        var other = others.FirstOrDefault(c => c.TextInfo.IsRightToLeft is false && c.TwoLetterISOLanguageName != defaultCulture.TwoLetterISOLanguageName);

        return [null, .. new[] { rightToLeft, other }.OfType<CultureInfo>().Select(c => c.Name)];
    }

    /// <summary>Swaps in new views; the first keeps the selected view's id, so its frame is reused rather than created again.</summary>
    private void Replace(WorkbenchScenario[] scenarios)
    {
        var keptId = SelectedPane.Id;

        Panes = [.. scenarios.Select((scenario, index) => new WorkbenchPane(index == 0 ? keptId : NextPaneId(), scenario))];
        SelectedPaneId = keptId;
    }

    private string NextPaneId()
    {
        LastPaneId++;

        return LastPaneId.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Copies to <paramref name="target"/> each setting that differs between <paramref name="previous"/> and
    /// <paramref name="current"/>, page switches one by one. The device, its size and rotation are never copied.
    /// </summary>
    private static WorkbenchScenario Propagate(WorkbenchScenario previous, WorkbenchScenario current, WorkbenchScenario target)
    {
        var result = target with
        {
            Data = previous.Data != current.Data ? current.Data : target.Data,
            Theme = previous.Theme != current.Theme ? current.Theme : target.Theme,
            Culture = previous.Culture != current.Culture ? current.Culture : target.Culture,
            Chrome = previous.Chrome != current.Chrome ? current.Chrome : target.Chrome,
            Auth = previous.Auth != current.Auth ? current.Auth : target.Auth,
            User = previous.User != current.User ? current.User : target.User,
            Frame = previous.Frame != current.Frame ? current.Frame : target.Frame,
            Backdrop = previous.Backdrop != current.Backdrop ? current.Backdrop : target.Backdrop,
            Caption = previous.Caption != current.Caption ? current.Caption : target.Caption,
            Variant = previous.Variant != current.Variant ? current.Variant : target.Variant,
            Accent = previous.Accent != current.Accent ? current.Accent : target.Accent,
            TextScale = previous.TextScale != current.TextScale ? current.TextScale : target.TextScale,
            ReducedMotion = previous.ReducedMotion != current.ReducedMotion ? current.ReducedMotion : target.ReducedMotion,
            Vision = previous.Vision != current.Vision ? current.Vision : target.Vision
        };

        // Only the switches whose value changed, so a view compared on another switch keeps its own value of it.
        foreach (var key in GetSwitchKeys(previous).Union(GetSwitchKeys(current)))
        {
            if (current.GetSwitch(key) is var value && value != previous.GetSwitch(key))
            {
                result = result.WithSwitch(key, value);
            }
        }

        return result;
    }

    /// <summary>The keys of the switches a scenario sets, read from its <c>key=value;...</c> form.</summary>
    private static IEnumerable<string> GetSwitchKeys(WorkbenchScenario scenario)
    {
        return (scenario.Switches ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries).Select(pair => pair.Split('=', 2)[0]);
    }
}
