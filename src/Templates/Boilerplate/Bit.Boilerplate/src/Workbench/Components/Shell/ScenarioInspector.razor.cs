namespace Boilerplate.Workbench.Components.Shell;

public partial class ScenarioInspector
{
    private static readonly (string Key, string? TextKey)[] zoomLevels = [("fit", nameof(WorkbenchStrings.ZoomFit)), ("0.5", null), ("0.75", null), ("1", null)];

    private List<BitDropdownItem<string>> deviceItems = [];
    private List<BitDropdownItem<string>> cultureItems = [];
    private WorkbenchBackdrop lastBackdrop = WorkbenchBackdrop.Brand;

    [Parameter] public WorkbenchScenario Value { get; set; } = new();

    [Parameter] public EventCallback<WorkbenchScenario> ValueChanged { get; set; }

    [Parameter] public string Zoom { get; set; } = "fit";

    [Parameter] public EventCallback<string> ZoomChanged { get; set; }

    [Parameter] public bool ShowDevice { get; set; } = true;

    [Parameter] public bool ShowZoom { get; set; } = true;

    [Parameter] public WorkbenchPageInfo? Page { get; set; }

    [Parameter] public EventCallback OnReset { get; set; }

    private string? StoreRuleKey => Value.DevicePreset.Store switch
    {
        "App Store" => nameof(WorkbenchStrings.StoreRuleAppStore),
        "Google Play" => nameof(WorkbenchStrings.StoreRuleGooglePlay),
        "Microsoft Store" => nameof(WorkbenchStrings.StoreRuleMicrosoftStore),
        _ => null
    };

    private string? CaptionPlaceholder => Page is null ? null : Localizer[Page.CaptionKey ?? Page.TitleKey].Value;

    protected override void OnInitialized()
    {
        deviceItems = [];

        foreach (var group in DevicePresets.All.GroupBy(d => d.IsStorePreset ? "Store" : d.Kind.ToString()))
        {
            deviceItems.Add(new() { ItemType = BitDropdownItemType.Header, Text = Localizer[$"DeviceGroup{group.Key}"] });

            deviceItems.AddRange(group.Select(device =>
            {
                var (width, height) = device.GetPixels(rotated: false);
                return new BitDropdownItem<string>
                {
                    Value = device.Key,
                    Text = device.IsStorePreset
                        ? FormattableString.Invariant($"{device.Store} · {device.Name} · {width}×{height}")
                        : FormattableString.Invariant($"{device.Name} · {device.Width}×{device.Height}")
                };
            }));
        }

        cultureItems = [.. CultureInfoManager.SupportedCultures.Select(sc => new BitDropdownItem<string> { Value = sc.Culture.Name, Text = sc.DisplayName })];
    }

    private static BitVariant VariantOf(bool isActive) => isActive ? BitVariant.Fill : BitVariant.Outline;

    protected override void OnParametersSet()
    {
        if (Value.Backdrop is { } backdrop)
        {
            lastBackdrop = backdrop;
        }
    }

    private Task SelectBackdrop(WorkbenchBackdrop backdrop) => Update(Value with { Backdrop = backdrop });

    private Task Update(WorkbenchScenario scenario) => ValueChanged.InvokeAsync(scenario);

    public static string GetDataStateIconName(WorkbenchDataState state) => state switch
    {
        WorkbenchDataState.Live => BitIconName.CheckMark,
        WorkbenchDataState.Loading => BitIconName.Sync,
        WorkbenchDataState.Empty => BitIconName.Inbox,
        WorkbenchDataState.Error => BitIconName.ErrorBadge,
        WorkbenchDataState.Mixed => BitIconName.Split,
        _ => BitIconName.Stopwatch
    };

    public static string GetThemeIconName(WorkbenchThemeMode theme) => theme switch
    {
        WorkbenchThemeMode.Light => BitIconName.Sunny,
        WorkbenchThemeMode.Dark => BitIconName.ClearNight,
        _ => BitIconName.Devices2
    };
}
