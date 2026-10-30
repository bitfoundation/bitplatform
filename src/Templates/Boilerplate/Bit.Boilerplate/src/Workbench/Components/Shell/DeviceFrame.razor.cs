namespace Boilerplate.Workbench.Components.Shell;

public partial class DeviceFrame
{
    [Parameter] public DevicePreset Device { get; set; } = DevicePresets.Default;

    [Parameter] public bool Landscape { get; set; }

    [Parameter] public bool ShowFrame { get; set; } = true;

    [Parameter] public bool OnDarkContent { get; set; }

    [Parameter] public string? Address { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    public static (int Left, int Top, int Right, int Bottom) GetBezel(DevicePreset device, bool showFrame)
    {
        if (showFrame is false)
            return (0, 0, 0, 0);

        return device.Kind switch
        {
            DeviceKind.Phone or DeviceKind.Foldable => (14, 14, 14, 14),
            DeviceKind.Tablet => (20, 20, 20, 20),
            DeviceKind.Laptop => (16, 16, 16, 38),
            _ => (1, 42, 1, 1)
        };
    }

    public static (int Width, int Height) GetFrameSize(DevicePreset device, bool landscape, bool showFrame)
    {
        var (width, height) = device.GetViewport(landscape);
        var (left, top, right, bottom) = GetBezel(device, showFrame);

        return (width + left + right, height + top + bottom);
    }

    private string GetClass()
    {
        return $"device {Device.Kind.ToString().ToLowerInvariant()} {(ShowFrame ? "framed" : "bare")}{(Landscape ? " landscape" : null)}";
    }

    private string GetStyle()
    {
        var (width, height) = Device.GetViewport(Landscape);
        var (left, top, right, bottom) = GetBezel(Device, ShowFrame);
        var (frameWidth, frameHeight) = GetFrameSize(Device, Landscape, ShowFrame);
        var radius = ShowFrame ? Device.CornerRadius : 0;

        return FormattableString.Invariant($"width:{frameWidth}px;height:{frameHeight}px;--wb-screen-w:{width}px;--wb-screen-h:{height}px;--wb-screen-r:{radius}px;--wb-bezel-l:{left}px;--wb-bezel-t:{top}px;--wb-bezel-r:{right}px;--wb-bezel-b:{bottom}px;");
    }
}
