//+:cnd:noEmit
// [mirror] IBitDeviceCoordinator - applying the theme to native chrome - keep in sync with:
// - src/Client/Boilerplate.Client.Maui/Infrastructure/Services/MauiDeviceCoordinator.cs

using Boilerplate.Client.Core.Styles;

namespace Boilerplate.Client.Windows.Infrastructure.Services;

public partial class WindowsDeviceCoordinator : IBitDeviceCoordinator
{
    public async Task ApplyTheme(bool isDark)
    {
        Application.SetColorMode(isDark ? SystemColorMode.Dark : SystemColorMode.Classic);
        Application.OpenForms[0]!.FormCaptionBackColor = ColorTranslator.FromHtml(isDark ? ThemeColors.PrimaryDarkBgColor : ThemeColors.PrimaryLightBgColor);
    }
}
