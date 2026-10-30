namespace Boilerplate.Workbench.Components.Preview;

public partial class StoreComposition
{
    private bool isLandscape;
    private string? caption;
    private string? canvasStyle;
    private string? deviceStyle;
    private DevicePreset frameDevice = DevicePresets.Default;

    [Parameter] public WorkbenchPageInfo Page { get; set; } = default!;

    [Parameter] public WorkbenchComposition Composition { get; set; } = default!;

    protected override void OnParametersSet()
    {
        caption = Composition.Caption ?? Localizer[Page.CaptionKey ?? Page.TitleKey];
        frameDevice = GetFrameDevice(Scenario.DevicePreset);

        var (canvasWidth, canvasHeight) = Scenario.DevicePreset.GetViewport(Scenario.Rotated);
        var (frameWidth, frameHeight) = DeviceFrame.GetFrameSize(frameDevice, Scenario.Rotated, showFrame: true);
        var visualWidth = frameDevice.Kind is DeviceKind.Laptop ? frameWidth * 1.1 : frameWidth;

        isLandscape = canvasWidth > canvasHeight;

        var captionHeight = canvasHeight * (isLandscape ? 0.22 : 0.2);
        var bottomMargin = canvasHeight * (isLandscape ? 0.07 : 0.035);
        var availableHeight = canvasHeight - captionHeight - bottomMargin;
        var scale = Math.Min(canvasWidth * (isLandscape ? 0.84 : 0.86) / visualWidth, availableHeight / frameHeight);
        var left = (canvasWidth - frameWidth * scale) / 2;
        var top = captionHeight + (availableHeight - frameHeight * scale) / 2;
        var captionSize = isLandscape ? canvasHeight * 0.06 : canvasWidth * 0.072;

        canvasStyle = FormattableString.Invariant($"--wb-caption-size:{captionSize:0.##}px;--wb-caption-top:{captionHeight * 0.32:0.##}px;--wb-caption-width:{canvasWidth * (isLandscape ? 0.7 : 0.88):0.##}px;--wb-glow-top:{top + frameHeight * scale / 2:0.##}px;");
        deviceStyle = FormattableString.Invariant($"left:{left:0.##}px;top:{top:0.##}px;transform:scale({scale:0.####});");
    }

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
