namespace Boilerplate.Workbench.Components.Shell;

/// <summary>
/// Draws a phone, tablet, laptop or desktop around its content at the device's CSS pixel size, or a bare screen.
/// The canvas fits and resizes views by the sizes computed here, so they must match the frame's styles.
/// </summary>
public partial class DeviceFrame
{
    [Parameter] public DevicePreset Device { get; set; } = DevicePresets.Default;

    [Parameter] public bool Rotated { get; set; }

    [Parameter] public bool ShowFrame { get; set; } = true;

    /// <summary>Whether the screen shows dark content, which turns the home indicator light; null follows the Workbench's own theme.</summary>
    [Parameter] public bool? OnDarkContent { get; set; }

    [Parameter] public string? Address { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Adds the edge and corner handles that index.ts turns into a drag to a custom size.</summary>
    [Parameter] public bool Resizable { get; set; }

    /// <summary>The frame's thickness on each side, or zero without a frame; a desktop's top is its browser bar.</summary>
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

    /// <summary>The device's outer size: its viewport plus the bezel.</summary>
    public static (int Width, int Height) GetFrameSize(DevicePreset device, bool rotated, bool showFrame)
    {
        var (width, height) = device.GetViewport(rotated);
        var (left, top, right, bottom) = GetBezel(device, showFrame);

        return (width + left + right, height + top + bottom);
    }

    private string GetClass()
    {
        return $"device {Device.Kind.ToString().ToLowerInvariant()} {(ShowFrame ? "framed" : "bare")}{(Device.IsLandscape(Rotated) ? " landscape" : null)}";
    }

    /// <summary>Sizes the device and hands the screen size, its corner radius and the bezel to the styles as CSS variables.</summary>
    private string GetStyle()
    {
        var (width, height) = Device.GetViewport(Rotated);
        var (left, top, right, bottom) = GetBezel(Device, ShowFrame);
        var (frameWidth, frameHeight) = GetFrameSize(Device, Rotated, ShowFrame);
        var radius = ShowFrame ? Device.CornerRadius : 0;

        return FormattableString.Invariant($"width:{frameWidth}px;height:{frameHeight}px;--wb-screen-w:{width}px;--wb-screen-h:{height}px;--wb-screen-r:{radius}px;--wb-bezel-l:{left}px;--wb-bezel-t:{top}px;--wb-bezel-r:{right}px;--wb-bezel-b:{bottom}px;");
    }
}
