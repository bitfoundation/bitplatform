//-:cnd:noEmit
using Boilerplate.Client.Core.Styles;

namespace Boilerplate.Client.Maui.Services;

/// <summary>
/// More info at <see cref="IBitDeviceCoordinator"/>
/// </summary>
public partial class MauiDeviceCoordinator : IBitDeviceCoordinator
{
    public async Task ApplyTheme(bool isDark)
    {
        Application.Current!.UserAppTheme = isDark ? AppTheme.Dark : AppTheme.Light;
#if Android
        var window = Platform.CurrentActivity?.Window;
        window!.DecorView!.SystemUiFlags = Android.Views.SystemUiFlags.LightStatusBar;
        if (isDark)
        {
            window!.DecorView!.SystemUiFlags &= ~Android.Views.SystemUiFlags.LightStatusBar;
        }

        window.SetStatusBarColor(Android.Graphics.Color.ParseColor(isDark ? ThemeColors.PrimaryDarkBgColor : ThemeColors.PrimaryLightBgColor));
#elif IOS
        var statusBarStyle = isDark ? UIKit.UIStatusBarStyle.LightContent : UIKit.UIStatusBarStyle.DarkContent;
        await Device.InvokeOnMainThreadAsync(() =>
        {
            UIKit.UIApplication.SharedApplication.SetStatusBarStyle(statusBarStyle, false);
            Platform.GetCurrentUIViewController()!.SetNeedsStatusBarAppearanceUpdate();
        });
#elif MACCATALYST
        var window = UIKit.UIApplication.SharedApplication.Windows[0].WindowScene;
        if (window != null)
        {
            window.Titlebar!.TitleVisibility = UIKit.UITitlebarTitleVisibility.Hidden;
        }
#endif
    }
}
