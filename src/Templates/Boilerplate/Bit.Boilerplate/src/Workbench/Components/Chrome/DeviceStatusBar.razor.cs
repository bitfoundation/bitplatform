namespace Boilerplate.Workbench.Components.Chrome;

public partial class DeviceStatusBar
{
    [Parameter] public DevicePreset Device { get; set; } = DevicePresets.Default;
}
