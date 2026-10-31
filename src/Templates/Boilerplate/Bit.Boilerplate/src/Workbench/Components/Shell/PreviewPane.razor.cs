namespace Boilerplate.Workbench.Components.Shell;

public partial class PreviewPane
{
    private DevicePreset device = DevicePresets.Default;
    private string? frameUrl;
    private string previewUrl = string.Empty;
    private string? shownUrl;
    private int frameWidth;
    private int frameHeight;
    private int viewportWidth;
    private int viewportHeight;

    private const int MinBreakpointWidth = 375;

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

    private bool ShowFrame => Pane.Scenario.Frame && Pane.Scenario.Backdrop is null;

    private string SizeText
    {
        get
        {
            var (width, height) = device.GetViewport(Pane.Scenario.Rotated);
            return FormattableString.Invariant($"{width}×{height} · {device.PixelRatio:0.###}x");
        }
    }

    private string AddressText => $"your-app.com/{Page.Key}";

    private string StageStyle => FormattableString.Invariant($"--wb-frame-w:{frameWidth}px;--wb-frame-h:{frameHeight}px;");

    protected override void OnParametersSet()
    {
        device = Pane.Scenario.DevicePreset;
        previewUrl = WorkbenchUrls.Preview(Page.Key, Pane.Scenario, Pane.Id);
        frameUrl ??= previewUrl;
        (frameWidth, frameHeight) = DeviceFrame.GetFrameSize(device, Pane.Scenario.Rotated, ShowFrame);
        (viewportWidth, viewportHeight) = device.GetViewport(Pane.Scenario.Rotated);
    }

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
        await JSRuntime.InvokeVoidAsync("workbench.showInFrame", Pane.Id, previewUrl);
    }

    public static string GetDeviceIconName(DevicePreset device) => device.Kind switch
    {
        DeviceKind.Phone or DeviceKind.Foldable => BitIconName.CellPhone,
        DeviceKind.Tablet => BitIconName.Tablet,
        _ => BitIconName.TVMonitor
    };

    private IEnumerable<string> GetChips()
    {
        var scenario = Pane.Scenario;

        if (scenario.Theme is not WorkbenchThemeMode.Light)
            yield return Localizer[$"Theme{scenario.Theme}"];

        if (scenario.Culture is not null)
            yield return CultureInfoManager.SupportedCultures.FirstOrDefault(sc => sc.Culture.Name == scenario.Culture).DisplayName ?? scenario.Culture;

        if (scenario.Data is not WorkbenchDataState.Live)
            yield return Localizer[$"Data{scenario.Data}"];

        if (scenario.Auth is not WorkbenchAuthState.SignedIn)
            yield return Localizer[scenario.Auth.ToString()];

        if (scenario.Auth is not WorkbenchAuthState.SignedOut && scenario.User != 0)
            yield return Localizer[WorkbenchDemoUsers.Find(scenario.User).RoleKey];

        if (scenario.Chrome is false)
            yield return Localizer[nameof(WorkbenchStrings.NoChrome)];

        if (scenario.Backdrop is { } backdrop)
            yield return Localizer[$"Backdrop{backdrop}"];

        if (scenario.Variant is not null && Page.ResolveVariant(scenario) is { } variant)
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
