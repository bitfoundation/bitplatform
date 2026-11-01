namespace Boilerplate.Workbench.Components.Shell;

/// <summary>
/// One view on the canvas: a header naming the device and the settings that differ from the defaults, the app's
/// breakpoints, and the page's preview in a device frame that Scripts/index.ts scales to fit and resizes.
/// </summary>
public partial class PreviewPane
{
    private DevicePreset device = DevicePresets.Default;
    // The iframe's src is only the first URL; later ones go through workbench.showInFrame, which replaces the frame's
    // location without adding to its history. shownUrl is the URL the frame was last sent to.
    private string? frameUrl;
    private string previewUrl = string.Empty;
    private string? shownUrl;
    private int frameWidth;
    private int frameHeight;
    private int viewportWidth;
    private int viewportHeight;
    private string deviceName = string.Empty;

    // The narrowest width a breakpoint snaps a view to, so the smallest breakpoint, which starts at 0, still shows a phone.
    private const int MinBreakpointWidth = 375;

    // The first words of the presets' generic names, such as "Desktop 1920×1080", and the text that translates them.
    private static readonly (string Prefix, string TextKey)[] genericDeviceNames =
    [
        ("Laptop ", nameof(WorkbenchStrings.DeviceLaptop)),
        ("Desktop ", nameof(WorkbenchStrings.DeviceDesktop)),
        ("Phone ", nameof(WorkbenchStrings.DevicePhone)),
        ("Tablet ", nameof(WorkbenchStrings.DeviceTablet))
    ];

    private const string OpenSuffix = " (open)";

    [Parameter] public WorkbenchPane Pane { get; set; } = default!;

    [Parameter] public WorkbenchPageInfo Page { get; set; } = default!;

    [Parameter] public bool IsSelected { get; set; }

    [Parameter] public bool CanRemove { get; set; }

    [Parameter] public bool FitWidth { get; set; }

    [Parameter] public string Zoom { get; set; } = "fit";

    [Parameter] public EventCallback<WorkbenchPane> OnSelect { get; set; }

    [Parameter] public EventCallback<WorkbenchPane> OnRemove { get; set; }

    [Parameter] public EventCallback<WorkbenchPane> OnRotate { get; set; }

    [Parameter] public EventCallback<WorkbenchPane> OnScreenshot { get; set; }

    [Parameter] public EventCallback<WorkbenchPaneSize> OnResize { get; set; }

    [Parameter] public IReadOnlyList<WorkbenchBreakpoint> Breakpoints { get; set; } = [];

    /// <summary>A saved state to load the page with, such as the one a comment was left on.</summary>
    [Parameter] public string? SnapshotId { get; set; }

    private bool ShowFrame => Pane.Scenario.Frame && Pane.Scenario.Backdrop is null;

    private string SizeText
    {
        get
        {
            var (width, height) = device.GetViewport(Pane.Scenario.Rotated);
            return FormattableString.Invariant($"{width}×{height} · {device.PixelRatio:0.###}x");
        }
    }

    /// <summary>The made-up address the desktop frame's browser bar shows.</summary>
    private string AddressText => $"your-app.com/{Page.Key}";

    /// <summary>The frame's unscaled size as CSS variables, which the scaler multiplies by the scale index.ts sets.</summary>
    private string StageStyle => FormattableString.Invariant($"--wb-frame-w:{frameWidth}px;--wb-frame-h:{frameHeight}px;");

    protected override void OnParametersSet()
    {
        device = Pane.Scenario.DevicePreset;
        deviceName = GetDeviceName(device, Localizer);
        previewUrl = WorkbenchUrls.Preview(Page.Key, Pane.Scenario, Pane.Id, SnapshotId);
        frameUrl ??= previewUrl;
        (frameWidth, frameHeight) = DeviceFrame.GetFrameSize(device, Pane.Scenario.Rotated, ShowFrame);
        (viewportWidth, viewportHeight) = device.GetViewport(Pane.Scenario.Rotated);
    }

    /// <summary>Sends the frame to the view's preview URL when its scenario or page has changed.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            shownUrl = frameUrl;
            return;
        }

        if (previewUrl == shownUrl)
            return;

        shownUrl = previewUrl;

        try
        {
            await JSRuntime.InvokeVoidAsync("workbench.showInFrame", Pane.Id, previewUrl);
        }
        catch (JSException)
        {
            // The frame's location can't be reached, such as on another site's page, but its src can still be set.
            frameUrl = previewUrl;
            StateHasChanged();
        }
    }

    public static string GetDeviceIconName(DevicePreset device) => device.Kind switch
    {
        DeviceKind.Phone or DeviceKind.Foldable => BitIconName.CellPhone,
        DeviceKind.Tablet => BitIconName.Tablet,
        _ => BitIconName.TVMonitor
    };

    /// <summary>The device's name in the Workbench's language: generic names are translated, product names stay as they are.</summary>
    public static string GetDeviceName(DevicePreset device, IStringLocalizer<WorkbenchStrings> localizer)
    {
        if (device.Key == DevicePresets.CustomKey)
            return localizer[nameof(WorkbenchStrings.CustomSize)];

        foreach (var (prefix, textKey) in genericDeviceNames)
        {
            if (device.Name.StartsWith(prefix, StringComparison.Ordinal))
                return localizer[textKey, device.Name[prefix.Length..]];
        }

        return device.Name.EndsWith(OpenSuffix, StringComparison.Ordinal)
            ? localizer[nameof(WorkbenchStrings.DeviceOpen), device.Name[..^OpenSuffix.Length]]
            : device.Name;
    }

    /// <summary>A short label for each setting of this view that differs from the default, shown in its header.</summary>
    private IEnumerable<string> GetChips()
    {
        var scenario = Pane.Scenario;

        if (scenario.Theme is not WorkbenchThemeMode.Light)
            yield return Localizer[$"Theme{scenario.Theme}"];

        if (scenario.Culture is not null)
            yield return CultureInfoManager.SupportedCultures.FirstOrDefault(sc => sc.Culture.Name == scenario.Culture).DisplayName ?? scenario.Culture;

        if (scenario.Data is not WorkbenchDataState.Live)
            yield return Localizer[$"Data{scenario.Data}"];

        if (scenario.Auth is not WorkbenchAuthState.MultiAccount)
            yield return Localizer[scenario.Auth.ToString()];

        if (scenario.Auth is not WorkbenchAuthState.SignedOut && scenario.User != 0)
            yield return Localizer[WorkbenchDemoUsers.Find(scenario.User).RoleKey];

        if (scenario.Chrome is false)
            yield return Localizer[nameof(WorkbenchStrings.NoChrome)];

        if (scenario.Backdrop is { } backdrop)
            yield return Localizer[$"Backdrop{backdrop}"];

        if (scenario.Variant is not null && Page.Variants.FirstOrDefault(v => v.Key == scenario.Variant) is { } variant)
            yield return Localizer[variant.NameKey];

        foreach (var pageSwitch in Page.Switches)
        {
            if (pageSwitch.Resolve(scenario) is var option && option != pageSwitch.Default)
                yield return Localizer[option.TextKey];
        }

        if (scenario.Accent is not null)
            yield return Localizer[nameof(WorkbenchStrings.AccentColor)];

        if (scenario.TextScale != 100)
            yield return FormattableString.Invariant($"{Localizer[nameof(WorkbenchStrings.TextSize)]} {scenario.TextScale}%");

        if (scenario.ReducedMotion)
            yield return Localizer[nameof(WorkbenchStrings.ReduceMotion)];

        if (scenario.Vision is { } vision)
            yield return Localizer[$"Vision{vision}"];
    }

    private Task Select() => OnSelect.InvokeAsync(Pane);
}
