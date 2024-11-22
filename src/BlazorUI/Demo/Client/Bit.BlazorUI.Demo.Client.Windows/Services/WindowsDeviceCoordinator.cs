using Bit.BlazorUI.Demo.Client.Core.Services.Contracts;

namespace Bit.BlazorUI.Demo.Client.Windows.Services;

public partial class WindowsDeviceCoordinator : IBitDeviceCoordinator
{
    public async Task ApplyTheme(bool isDark)
    {
        Application.SetColorMode(isDark ? SystemColorMode.Dark : SystemColorMode.Classic);
    }
}
