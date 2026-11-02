namespace Boilerplate.Workbench.Components.Preview;

/// <summary>
/// Lays out a store screenshot at the device's own size: the caption above (or beside, in landscape) a scaled device
/// whose screen is an inner /preview of the same page without the backdrop.
/// </summary>
public partial class StoreComposition
{
    private bool isLandscape;
    private string? caption;
    private string? canvasStyle;
    private string? boxStyle;
    private string? deviceStyle;
    private string? screenUrl;
    private DevicePreset frameDevice = DevicePresets.Default;

    [Parameter] public WorkbenchPageInfo Page { get; set; } = default!;

    protected override void OnParametersSet()
    {
        caption = Scenario.Caption ?? Localizer[Page.CaptionKey ?? Page.TitleKey];
        frameDevice = GetFrameDevice(Scenario.DevicePreset);
        // The same pane and snapshot, so the inner screen shows the state the view has.
        screenUrl = WorkbenchUrls.Preview(Page.Key, Scenario with { Backdrop = null, Caption = null }, PaneId, WorkbenchUrls.ReadQuery(NavigationManager.Uri, WorkbenchUrls.SnapshotQueryKey));

        var (canvasWidth, canvasHeight) = Scenario.DevicePreset.GetViewport(Scenario.Rotated);
        var (frameWidth, frameHeight) = DeviceFrame.GetFrameSize(frameDevice, Scenario.Rotated, Scenario.Frame);
        var visualWidth = frameDevice.Kind is DeviceKind.Laptop ? frameWidth * 1.1 : frameWidth;
        // A long caption gets a smaller font, down to 60%.
        var fit = Math.Clamp(Math.Sqrt(32.0 / Math.Max(caption.Length, 1)), 0.6, 1);
        var isPhone = frameDevice.Kind is DeviceKind.Phone;

        isLandscape = canvasWidth > canvasHeight;

        // Sizes are fractions of the screenshot's own width and height, so every store size keeps the same proportions.
        var scale = isLandscape
            ? Math.Min(canvasWidth * 0.56 / visualWidth, canvasHeight * 0.8 / frameHeight)
            : canvasWidth * (isPhone ? 0.86 : 0.8) / visualWidth;
        var captionSize = isLandscape
            ? Math.Min(canvasHeight * 0.085, canvasWidth * 0.046) * fit
            : canvasWidth * (isPhone ? 0.088 : 0.068) * fit;
        var eyebrowSize = isLandscape
            ? Math.Min(canvasHeight * 0.03, canvasWidth * 0.016)
            : canvasWidth * (isPhone ? 0.034 : 0.024);
        var glowX = isLandscape ? (Scenario.IsRtl ? 30 : 70) : 50;

        canvasStyle = FormattableString.Invariant($"--wb-caption-size:{captionSize:0.##}px;--wb-eyebrow-size:{eyebrowSize:0.##}px;--wb-glow-x:{glowX}%;--wb-glow-y:{(isLandscape ? 50 : 55)}%;--wb-pattern-x:{(Scenario.IsRtl ? 100 : 0)}%;");
        boxStyle = FormattableString.Invariant($"width:{frameWidth * scale:0.##}px;height:{frameHeight * scale:0.##}px;");
        deviceStyle = FormattableString.Invariant($"transform:scale({scale:0.####});");
    }

    /// <summary>
    /// The device drawn around the screen: a desktop becomes a laptop, and flat-cornered phones and tablets get the
    /// rounded corners and camera a store image expects.
    /// </summary>
    private static DevicePreset GetFrameDevice(DevicePreset device)
    {
        return device.Kind switch
        {
            DeviceKind.Desktop => device with { Kind = DeviceKind.Laptop },
            DeviceKind.Phone when device.Cutout is DeviceCutout.None => device with { CornerRadius = Math.Max(device.CornerRadius, 28), Cutout = DeviceCutout.PunchHole },
            DeviceKind.Tablet or DeviceKind.Foldable => device with { CornerRadius = Math.Max(device.CornerRadius, 16) },
            _ => device
        };
    }
}
