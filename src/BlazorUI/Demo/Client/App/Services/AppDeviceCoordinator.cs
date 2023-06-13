namespace Bit.BlazorUI.Demo.Client.App.Services;

public class AppDeviceCoordinator : IBitDeviceCoordinator
{
    public double GetStatusBarHeight()
    {
#if iOS
        //This is handled in css using env() variables
        return 0;
#elif ANDROID
        var resourceId = MauiApplication.Current.Resources!.GetIdentifier("status_bar_height", "dimen", "android");
        var dimensionPixelSize = MauiApplication.Current.Resources.GetDimensionPixelSize(resourceId);
        var density = (double)DeviceDisplay.Current.MainDisplayInfo.Density;
        return dimensionPixelSize / density;
#elif WINDOWS
        return 30;
#elif MACCATALYST
        return 25;
#else 
        return 0;
#endif
    }

    public void SetUserAppTheme(bool isDark)
    {
        Application.Current.UserAppTheme = isDark ? AppTheme.Dark : AppTheme.Light;
#if ANDROID
        var window = Platform.CurrentActivity?.Window;
        if (isDark)
        {
            window.DecorView.SystemUiVisibility &= ~(Android.Views.StatusBarVisibility)Android.Views.SystemUiFlags.LightStatusBar;
        }
        else
        {
            window.DecorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)(Android.Views.SystemUiFlags.LayoutFullscreen | Android.Views.SystemUiFlags.LayoutStable | Android.Views.SystemUiFlags.LightStatusBar);
        }
#endif
    }
}
