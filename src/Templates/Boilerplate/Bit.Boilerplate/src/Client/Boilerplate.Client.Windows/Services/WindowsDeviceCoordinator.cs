//+:cnd:noEmit
using Boilerplate.Client.Core.Styles;

namespace Boilerplate.Client.Windows.Services;

public partial class WindowsDeviceCoordinator : IBitDeviceCoordinator
{
    public async Task ApplyTheme(bool isDark)
    {
        //#if (framework == 'net9.0')
        Application.SetColorMode(isDark ? SystemColorMode.Dark : SystemColorMode.Classic);
        Application.OpenForms[0]!.FormCaptionBackColor = ColorTranslator.FromHtml(isDark ? ThemeColors.PrimaryDarkBgColor : ThemeColors.PrimaryLightBgColor);
        //#endif
    }
}
