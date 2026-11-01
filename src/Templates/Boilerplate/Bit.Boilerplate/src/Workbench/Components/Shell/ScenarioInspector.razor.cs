namespace Boilerplate.Workbench.Components.Shell;

/// <summary>
/// The View tab: every setting of the selected view's scenario, from the device and the data state to the theme
/// tweaks. It holds no scenario of its own; each change goes out through <see cref="ValueChanged"/> as a new one.
/// </summary>
public partial class ScenarioInspector
{
    // "fit" scales each view to the canvas; the numbers are fixed scales.
    private static readonly (string Key, string? TextKey)[] zoomLevels = [("fit", nameof(WorkbenchStrings.ZoomFit)), ("0.5", null), ("0.75", null), ("1", null)];

    private List<BitDropdownItem<string>> deviceItems = [];
    private List<BitDropdownItem<string>> cultureItems = [];
    private List<BitDropdownItem<int>> userItems = [];
    private List<BitDropdownItem<string>> visionItems = [];
    // The backdrop that turning Background back on brings back.
    private WorkbenchBackdrop lastBackdrop = WorkbenchBackdrop.Paper;

    [Parameter] public WorkbenchScenario Value { get; set; } = new();

    [Parameter] public EventCallback<WorkbenchScenario> ValueChanged { get; set; }

    [Parameter] public string Zoom { get; set; } = "fit";

    [Parameter] public EventCallback<string> ZoomChanged { get; set; }

    /// <summary>Shows the device settings; the phone layout hides them, since the page fills the phone itself.</summary>
    [Parameter] public bool ShowDevice { get; set; } = true;

    [Parameter] public bool ShowZoom { get; set; } = true;

    [Parameter] public WorkbenchPageInfo? Page { get; set; }

    [Parameter] public EventCallback OnReset { get; set; }

    /// <summary>The text with the store's screenshot rules, when a store size is picked.</summary>
    private string? StoreRuleKey => Value.DevicePreset.Store switch
    {
        "App Store" => nameof(WorkbenchStrings.StoreRuleAppStore),
        "Google Play" => nameof(WorkbenchStrings.StoreRuleGooglePlay),
        "Microsoft Store" => nameof(WorkbenchStrings.StoreRuleMicrosoftStore),
        _ => null
    };

    /// <summary>The caption store screenshots carry when none is typed: the page's caption, or else its title.</summary>
    private string? CaptionPlaceholder => Page is null ? null : Localizer[Page.CaptionKey ?? Page.TitleKey].Value;

    private int ViewportWidth => Value.DevicePreset.GetViewport(Value.Rotated).Width;

    private int ViewportHeight => Value.DevicePreset.GetViewport(Value.Rotated).Height;

    protected override void OnInitialized()
    {
        deviceItems = [];

        // The presets grouped by kind, the store sizes in a group of their own (with their pixel size), and Custom last.
        foreach (var group in DevicePresets.All.GroupBy(d => d.IsStorePreset ? "Store" : d.Kind.ToString()))
        {
            deviceItems.Add(new() { ItemType = BitDropdownItemType.Header, Text = Localizer[$"DeviceGroup{group.Key}"] });

            deviceItems.AddRange(group.Select(device =>
            {
                var (width, height) = device.GetPixels(rotated: false);
                var name = PreviewPane.GetDeviceName(device, Localizer);
                return new BitDropdownItem<string>
                {
                    Value = device.Key,
                    Text = device.IsStorePreset
                        ? FormattableString.Invariant($"{device.Store} · {name} · {width}×{height}")
                        : FormattableString.Invariant($"{name} · {device.Width}×{device.Height}")
                };
            }));
        }

        deviceItems.Add(new() { ItemType = BitDropdownItemType.Header, Text = Localizer[nameof(WorkbenchStrings.DeviceGroupCustom)] });
        deviceItems.Add(new() { Value = DevicePresets.CustomKey, Text = Localizer[nameof(WorkbenchStrings.CustomSize)] });

        visionItems = [new() { Value = string.Empty, Text = Localizer[nameof(WorkbenchStrings.VisionTypical)] }, .. Enum.GetValues<WorkbenchVision>().Select(vision => new BitDropdownItem<string> { Value = vision.ToString(), Text = Localizer[$"Vision{vision}"] })];

        cultureItems = [.. CultureInfoManager.SupportedCultures.Select(sc => new BitDropdownItem<string> { Value = sc.Culture.Name, Text = sc.DisplayName })];
        userItems = [.. WorkbenchDemoUsers.All.Select((user, index) => new BitDropdownItem<int> { Value = index, Text = $"{Localizer[user.NameKey]} · {Localizer[user.RoleKey]}" })];
    }

    private static BitVariant VariantOf(bool isActive) => isActive ? BitVariant.Fill : BitVariant.Outline;

    /// <summary>The aria-pressed value of a segment, so screen readers hear which one is on.</summary>
    private static string PressedOf(bool isActive) => isActive ? "true" : "false";

    /// <summary>An accent's name in the Workbench's language, or its own name when none is translated for it.</summary>
    private string GetAccentName(BitAccentColorItem accent)
    {
        return Localizer[$"Accent{accent.Name}"] is { ResourceNotFound: false } text ? text.Value : accent.Name ?? accent.Color;
    }

    private static string SegmentsOf(int count) => count switch { 2 => "two", 4 => "four", _ => "three" };

    /// <summary>Picks a device in portrait; Custom starts from the current viewport size.</summary>
    private Task SelectDevice(string? device)
    {
        if (device == DevicePresets.CustomKey)
            return Update(Value.WithSize(ViewportWidth, ViewportHeight));

        return Update(Value with { Device = device ?? DevicePresets.DefaultKey, Rotated = false });
    }

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
