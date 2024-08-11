//-:cnd:noEmit
namespace Boilerplate.Client.Maui.Services;

/// <summary>
/// More info at <see cref="IBitDeviceCoordinator"/>
/// </summary>
public class MauiDeviceCoordinator : IBitDeviceCoordinator
{
    public double GetStatusBarHeight()
    {
#if Android
        var resourceId = MauiApplication.Current.Resources!.GetIdentifier("status_bar_height", "dimen", "android");
        var dimensionPixelSize = MauiApplication.Current.Resources.GetDimensionPixelSize(resourceId);
        var density = (double)DeviceDisplay.Current.MainDisplayInfo.Density;
        return dimensionPixelSize / density;
#elif iOS
        var window = UIKit.UIApplication.SharedApplication.Windows.First().WindowScene;
        return window!.StatusBarManager!.StatusBarFrame.Height;
#elif Windows
        return 30;
#elif Mac
        return 25;
#else
        return 0;
#endif
    }

    public static readonly string BackgroundColorPrimaryDark = "#000000";
    public static readonly string BackgroundColorPrimaryLight = "#FFFFFF";
    // In case you need to change the background color, make sure to also update app.scss's --bit-clr-bg-pri accordingly.

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

        window.SetStatusBarColor(Android.Graphics.Color.ParseColor(isDark ? BackgroundColorPrimaryDark : BackgroundColorPrimaryLight));
#elif IOS
        var statusBarStyle = isDark ? UIKit.UIStatusBarStyle.LightContent : UIKit.UIStatusBarStyle.DarkContent;
        await Device.InvokeOnMainThreadAsync(() =>
        {
            UIKit.UIApplication.SharedApplication.SetStatusBarStyle(statusBarStyle, false);
            Platform.GetCurrentUIViewController().SetNeedsStatusBarAppearanceUpdate();
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
