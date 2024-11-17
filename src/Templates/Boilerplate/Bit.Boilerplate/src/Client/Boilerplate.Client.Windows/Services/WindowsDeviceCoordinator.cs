//-:cnd:noEmit
namespace Boilerplate.Client.Windows.Services;

public partial class WindowsDeviceCoordinator : IBitDeviceCoordinator
{
    //#if (framework == 'net9.0')
    public async Task ApplyTheme(bool isDark)
    {
        App.Current.ThemeMode = isDark ? System.Windows.ThemeMode.Dark : System.Windows.ThemeMode.Light;
    }
    //#endif
}
