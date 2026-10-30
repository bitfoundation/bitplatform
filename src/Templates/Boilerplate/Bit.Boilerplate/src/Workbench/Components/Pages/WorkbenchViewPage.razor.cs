using Microsoft.AspNetCore.WebUtilities;

namespace Boilerplate.Workbench.Components.Pages;

public partial class WorkbenchViewPage
{
    private WorkbenchPageInfo? pageInfo;
    private List<WorkbenchPane> panes = [new("1", new())];
    private string selectedPaneId = "1";
    private string zoom = "fit";
    private string currentQuery = string.Empty;
    private string? lastAppliedUri;
    private int lastPaneId = 1;
    private bool isSmallScreen;
    private bool isToolsOpen;
    private bool isLinkCopied;
    private List<BitMenuButtonItem> compareItems = [];
    private List<BitMenuButtonItem> screenshotItems = [];

    [AutoInject] private BitThemeManager bitThemeManager = default!;

    [Parameter] public string? PageKey { get; set; }

    private WorkbenchPane SelectedPane => panes.FirstOrDefault(p => p.Id == selectedPaneId) ?? panes[0];

    protected override void OnInitialized()
    {
        compareItems =
        [
            new() { Key = "single", Text = Localizer[nameof(WorkbenchStrings.CompareSingle)], IconName = BitIconName.CellPhone, OnClick = _ => Compare("single") },
            new() { Key = "devices", Text = Localizer[nameof(WorkbenchStrings.CompareDevices)], IconName = BitIconName.Devices2, OnClick = _ => Compare("devices") },
            new() { Key = "themes", Text = Localizer[nameof(WorkbenchStrings.CompareThemes)], IconName = BitIconName.ClearNight, OnClick = _ => Compare("themes") },
            new() { Key = "languages", Text = Localizer[nameof(WorkbenchStrings.CompareLanguages)], IconName = BitIconName.Globe, OnClick = _ => Compare("languages") },
            new() { Key = "states", Text = Localizer[nameof(WorkbenchStrings.CompareStates)], IconName = BitIconName.Split, OnClick = _ => Compare("states") },
            new() { Key = "accounts", Text = Localizer[nameof(WorkbenchStrings.CompareSignIn)], IconName = BitIconName.People, OnClick = _ => Compare("accounts") },
        ];

        screenshotItems =
        [
            new() { Key = "view", Text = Localizer[nameof(WorkbenchStrings.ScreenshotThisView)], IconName = BitIconName.Camera, OnClick = item => _ = DownloadSelected() },
            new() { Key = "all", Text = Localizer[nameof(WorkbenchStrings.ScreenshotAllViews)], IconName = BitIconName.PhotoCollection, OnClick = item => _ = DownloadAll() },
            new() { Key = "stores", Text = Localizer[nameof(WorkbenchStrings.StoreScreenshots)], IconName = BitIconName.Picture, OnClick = _ => NavigationManager.NavigateTo($"{WorkbenchUrls.Screenshots}?page={pageInfo?.Key}") },
        ];
    }

    protected override void OnParametersSet()
    {
        pageInfo = WorkbenchPageCatalog.Find(PageKey);

        if (NavigationManager.Uri == lastAppliedUri)
            return;

        var uri = new Uri(NavigationManager.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);

        zoom = query.TryGetValue("zoom", out var zoomValue) && string.IsNullOrWhiteSpace(zoomValue) is false ? zoomValue.ToString() : "fit";

        var scenarios = query.TryGetValue("panes", out var panesValue) ? WorkbenchUrls.DecodePanes(panesValue) : [];

        if (scenarios.Length == 0)
        {
            scenarios = [WorkbenchScenario.Parse(uri.Query)];
        }

        panes = [.. scenarios.Select(s => new WorkbenchPane(NextPaneId(), s))];
        selectedPaneId = panes[0].Id;

        Apply(updateUrl: false);
    }

    private string NextPaneId() => (++lastPaneId).ToString(CultureInfo.InvariantCulture);

    private void OnSmallScreenChanged(bool matched)
    {
        isSmallScreen = matched;
    }

    private void SelectPane(WorkbenchPane pane)
    {
        selectedPaneId = pane.Id;
        Apply(updateUrl: false);
    }

    private void OnScenarioChanged(WorkbenchScenario scenario)
    {
        var previous = SelectedPane.Scenario;

        panes = [.. panes.Select(pane => pane.Id == selectedPaneId
            ? pane with { Scenario = scenario }
            : pane with { Scenario = Propagate(previous, scenario, pane.Scenario) })];

        Apply();
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
            Frame = previous.Frame != current.Frame ? current.Frame : target.Frame
        };
    }

    private void OnZoomChanged(string value)
    {
        zoom = value;
        Apply();
    }

    private void AddPane()
    {
        var source = SelectedPane.Scenario;
        var next = source.DevicePreset.Kind switch
        {
            DeviceKind.Phone or DeviceKind.Foldable => source with { Device = "ipad-pro-11", Landscape = false },
            DeviceKind.Tablet => source with { Device = "desktop-1536", Landscape = false },
            _ => source with { Device = DevicePresets.DefaultKey, Landscape = false }
        };

        var pane = new WorkbenchPane(NextPaneId(), next);
        panes.Add(pane);
        selectedPaneId = pane.Id;
        Apply();
    }

    private void RemovePane(WorkbenchPane pane)
    {
        if (panes.Count == 1)
            return;

        panes.Remove(pane);

        if (selectedPaneId == pane.Id)
        {
            selectedPaneId = panes[0].Id;
        }

        Apply();
    }

    private void RotatePane(WorkbenchPane pane)
    {
        panes = [.. panes.Select(p => p.Id == pane.Id ? p with { Scenario = p.Scenario with { Landscape = p.Scenario.Landscape is false } } : p)];
        Apply();
    }

    private void Compare(string mode)
    {
        var source = SelectedPane.Scenario with { Landscape = false };

        WorkbenchScenario[] scenarios = mode switch
        {
            "devices" => [source with { Device = DevicePresets.DefaultKey }, source with { Device = "ipad-pro-11" }, source with { Device = "desktop-1536" }],
            "themes" => [source with { Theme = WorkbenchThemeMode.Light }, source with { Theme = WorkbenchThemeMode.Dark }],
            "languages" => [source with { Culture = null }, source with { Culture = "fa-IR" }, source with { Culture = "nl-NL" }],
            "states" => [source with { Data = WorkbenchDataState.Live }, source with { Data = WorkbenchDataState.Loading }, source with { Data = WorkbenchDataState.Empty }, source with { Data = WorkbenchDataState.Error }],
            "accounts" => [source with { Auth = WorkbenchAuthState.SignedOut }, source with { Auth = WorkbenchAuthState.SignedIn }, source with { Auth = WorkbenchAuthState.MultiAccount }],
            _ => [SelectedPane.Scenario]
        };

        panes = [.. scenarios.DistinctBy(s => s.ToQueryString()).Select(s => new WorkbenchPane(NextPaneId(), s))];
        selectedPaneId = panes[0].Id;
        Apply();
    }

    private void Apply(bool updateUrl = true)
    {
        currentQuery = BuildQuery();
        ScenarioState.Set(SelectedPane.Scenario);

        if (updateUrl && pageInfo is not null)
        {
            var url = $"{WorkbenchUrls.Workbench(pageInfo.Key)}{currentQuery}";
            lastAppliedUri = NavigationManager.ToAbsoluteUri(url).ToString();
            NavigationManager.NavigateTo(url, replace: true);
        }
        else
        {
            lastAppliedUri = NavigationManager.Uri;
        }

        StateHasChanged();
    }

    private string BuildQuery()
    {
        var query = panes.Count == 1
            ? panes[0].Scenario.ToQueryString()
            : QueryHelpers.AddQueryString(string.Empty, "panes", WorkbenchUrls.EncodePanes(panes.Select(p => p.Scenario)));

        return zoom == "fit" ? query : QueryHelpers.AddQueryString(query, "zoom", zoom);
    }

    private async Task DownloadSelected()
    {
        if (pageInfo is null)
            return;

        await JSRuntime.InvokeVoidAsync("workbench.download", WorkbenchUrls.Screenshot(pageInfo.Key, SelectedPane.Scenario));
    }

    private async Task DownloadAll()
    {
        if (pageInfo is null)
            return;

        var url = QueryHelpers.AddQueryString($"/api/screenshots/{pageInfo.Key}/zip", "panes", WorkbenchUrls.EncodePanes(panes.Select(p => p.Scenario)));

        await JSRuntime.InvokeVoidAsync("workbench.download", url);
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
}
