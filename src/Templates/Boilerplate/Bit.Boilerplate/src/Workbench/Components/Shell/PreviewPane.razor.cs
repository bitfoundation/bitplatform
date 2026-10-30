namespace Boilerplate.Workbench.Components.Shell;

public partial class PreviewPane
{
    private DevicePreset device = DevicePresets.Default;
    private string previewUrl = string.Empty;
    private int frameWidth;
    private int frameHeight;

    [Parameter] public WorkbenchPane Pane { get; set; } = default!;

    [Parameter] public WorkbenchPageInfo Page { get; set; } = default!;

    [Parameter] public bool IsSelected { get; set; }

    [Parameter] public bool CanRemove { get; set; }

    [Parameter] public bool FitWidth { get; set; }

    [Parameter] public string Zoom { get; set; } = "fit";

    [Parameter] public EventCallback<WorkbenchPane> OnSelect { get; set; }

    [Parameter] public EventCallback<WorkbenchPane> OnRemove { get; set; }

    [Parameter] public EventCallback<WorkbenchPane> OnRotate { get; set; }

    private string SizeText
    {
        get
        {
            var (width, height) = device.GetViewport(Pane.Scenario.Landscape);
            return FormattableString.Invariant($"{width}×{height} · {device.PixelRatio:0.###}x");
        }
    }

    private string AddressText => $"your-app.com/{Page.Key}";

    private string StageStyle => FormattableString.Invariant($"--wb-frame-w:{frameWidth}px;--wb-frame-h:{frameHeight}px;");

    protected override void OnParametersSet()
    {
        device = Pane.Scenario.DevicePreset;
        previewUrl = WorkbenchUrls.Preview(Page.Key, Pane.Scenario);
        (frameWidth, frameHeight) = DeviceFrame.GetFrameSize(device, Pane.Scenario.Landscape, Pane.Scenario.Frame);
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

        if (scenario.Chrome is false)
            yield return Localizer[nameof(WorkbenchStrings.NoChrome)];
    }

    private Task Select() => OnSelect.InvokeAsync(Pane);

    private async Task DownloadScreenshot()
    {
        await JSRuntime.InvokeVoidAsync("workbench.download", WorkbenchUrls.Screenshot(Page.Key, Pane.Scenario));
    }
}
