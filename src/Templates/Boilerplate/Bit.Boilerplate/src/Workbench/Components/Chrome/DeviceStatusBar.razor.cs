namespace Boilerplate.Workbench.Components.Chrome;

/// <summary>A phone or tablet status bar, drawn in the Android or iOS style over the top safe area and hidden from screen readers.</summary>
public partial class DeviceStatusBar
{
    [Parameter] public DevicePreset Device { get; set; } = DevicePresets.Default;
}
