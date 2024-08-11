namespace Bit.BlazorUI.Demo.Client.Maui.Services;

/// <summary>
/// More info at <see cref="IBitDeviceCoordinator"/>
/// </summary>
public class MauiDeviceCoordinator : IBitDeviceCoordinator
{
    public double GetStatusBarHeight()
    {
#if ANDROID
        var resourceId = MauiApplication.Current.Resources!.GetIdentifier("status_bar_height", "dimen", "android");
        var dimensionPixelSize = MauiApplication.Current.Resources.GetDimensionPixelSize(resourceId);
        var density = (double)DeviceDisplay.Current.MainDisplayInfo.Density;
        return dimensionPixelSize / density;
#elif IOS
        var window = UIKit.UIApplication.SharedApplication.Windows.First().WindowScene;
        return window!.StatusBarManager!.StatusBarFrame.Height;
#elif WINDOWS
        return 30;
#elif MACCATALYST
        return 25;
#else
        return 0;
#endif
    }

    public async Task ApplyTheme(bool isDark)
    {
        Application.Current!.UserAppTheme = isDark ? AppTheme.Dark : AppTheme.Light;
#if ANDROID
        var window = Platform.CurrentActivity?.Window;
        window!.DecorView!.SystemUiFlags = Android.Views.SystemUiFlags.LightStatusBar;
        if (isDark)
        {
            window!.DecorView!.SystemUiFlags &= ~Android.Views.SystemUiFlags.LightStatusBar;
        }

        window.SetStatusBarColor(isDark ? Android.Graphics.Color.ParseColor("#0D1117") : Android.Graphics.Color.White);
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
