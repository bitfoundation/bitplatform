//-:cnd:noEmit
using Microsoft.JSInterop;

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

    [JSInvokable(nameof(ApplyTheme))]
    public static async Task ApplyTheme(string backgroundColor, bool isDark)
    {
        Application.Current!.UserAppTheme = isDark ? AppTheme.Dark : AppTheme.Light;
#if Android
        var window = Platform.CurrentActivity?.Window;
        window!.DecorView!.SystemUiFlags = Android.Views.SystemUiFlags.LightStatusBar;
        if (isDark)
        {
            window!.DecorView!.SystemUiFlags &= ~Android.Views.SystemUiFlags.LightStatusBar;
        }

        window.SetStatusBarColor(Android.Graphics.Color.ParseColor(backgroundColor));
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
