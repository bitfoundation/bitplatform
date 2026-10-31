using Microsoft.AspNetCore.WebUtilities;

namespace Boilerplate.Workbench.Components.Shell;

[BlexStore(Name = "workbench", Persist = true)]
public partial class WorkbenchStore
{
    private static readonly string[] scenarioKeys = ["data", "theme", "culture", "chrome", "auth", "user", "device", "size", "rotated", "frame", "backdrop", "caption", "variant", "accent", "text", "motion", "vision", "panes"];

    [BlexState] private WorkbenchPane[] _panes = [new("1", new())];
    [BlexState] private string _selectedPaneId = "1";
    [BlexState] private string _zoom = "fit";
    [BlexState] private int _lastPaneId = 1;
    [BlexState(Persist = false)] private string _mode = WorkbenchModes.Browse;
    [BlexState] private string _column = WorkbenchModes.ViewColumn;
    [BlexState] private string? _reviewerName;

    public WorkbenchPane SelectedPane => Panes.FirstOrDefault(p => p.Id == SelectedPaneId) ?? Panes[0];

    public string QueryString
    {
        get
        {
            var query = Panes.Length == 1
                ? Panes[0].Scenario.ToQueryString()
                : QueryHelpers.AddQueryString(string.Empty, "panes", WorkbenchUrls.EncodePanes(Panes.Select(p => p.Scenario)));

            return Zoom == "fit" ? query : QueryHelpers.AddQueryString(query, "zoom", Zoom);
        }
    }

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

    [BlexAction] private void OnSelectPane(string paneId) => SelectedPaneId = paneId;

    [BlexAction] private void OnSetZoom(string zoom) => Zoom = zoom;

    [BlexAction] private void OnSetMode(string mode) => Mode = mode;

    [BlexAction] private void OnShowColumn(string column) => Column = column;

    [BlexAction] private void OnNameReviewer(string? name) => ReviewerName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();

    [BlexAction]
    private void OnChangeScenario(WorkbenchScenario scenario)
    {
        var previous = SelectedPane.Scenario;

        Panes = [.. Panes.Select(pane => pane.Id == SelectedPaneId
            ? pane with { Scenario = scenario }
            : pane with { Scenario = Propagate(previous, scenario, pane.Scenario) })];
    }

    [BlexAction]
    private void OnReportScenario(string paneId, WorkbenchScenario scenario)
    {
        Panes = [.. Panes.Select(pane => pane.Id == paneId ? pane with { Scenario = scenario } : pane)];
    }

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

    [BlexAction]
    private void OnCompareVariants(string[] variants)
    {
        var source = SelectedPane.Scenario;

        Replace([.. variants.Select((variant, index) => source with { Variant = index == 0 ? null : variant })]);
    }

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

    [BlexAction]
    private void OnCompare(string mode)
    {
        var source = SelectedPane.Scenario with { Rotated = false };

        WorkbenchScenario[] scenarios = mode switch
        {
            "devices" => [source with { Device = DevicePresets.DefaultKey }, source with { Device = "ipad-pro-11" }, source with { Device = "desktop-1536" }],
            "themes" => [source with { Theme = WorkbenchThemeMode.Light }, source with { Theme = WorkbenchThemeMode.Dark }],
            "languages" => [source with { Culture = null }, source with { Culture = "fa-IR" }, source with { Culture = "nl-NL" }],
            "states" => [source with { Data = WorkbenchDataState.Live }, source with { Data = WorkbenchDataState.Loading }, source with { Data = WorkbenchDataState.Empty }, source with { Data = WorkbenchDataState.Error }],
            "accounts" => [source with { Auth = WorkbenchAuthState.SignedOut }, source with { Auth = WorkbenchAuthState.SignedIn }, source with { Auth = WorkbenchAuthState.MultiAccount }],
            _ => [SelectedPane.Scenario]
        };

        Replace([.. scenarios.DistinctBy(s => s.ToQueryString())]);
    }

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

    private static WorkbenchScenario Propagate(WorkbenchScenario previous, WorkbenchScenario current, WorkbenchScenario target)
    {
        return target with
        {
            Data = previous.Data != current.Data ? current.Data : target.Data,
            Theme = previous.Theme != current.Theme ? current.Theme : target.Theme,
            Culture = previous.Culture != current.Culture ? current.Culture : target.Culture,
            Chrome = previous.Chrome != current.Chrome ? current.Chrome : target.Chrome,
            Auth = previous.Auth != current.Auth ? current.Auth : target.Auth,
            Frame = previous.Frame != current.Frame ? current.Frame : target.Frame,
            Backdrop = previous.Backdrop != current.Backdrop ? current.Backdrop : target.Backdrop,
            Caption = previous.Caption != current.Caption ? current.Caption : target.Caption,
            Variant = previous.Variant != current.Variant ? current.Variant : target.Variant,
            Switches = previous.Switches != current.Switches ? current.Switches : target.Switches,
            Accent = previous.Accent != current.Accent ? current.Accent : target.Accent,
            TextScale = previous.TextScale != current.TextScale ? current.TextScale : target.TextScale,
            ReducedMotion = previous.ReducedMotion != current.ReducedMotion ? current.ReducedMotion : target.ReducedMotion,
            Vision = previous.Vision != current.Vision ? current.Vision : target.Vision
        };
    }
}
