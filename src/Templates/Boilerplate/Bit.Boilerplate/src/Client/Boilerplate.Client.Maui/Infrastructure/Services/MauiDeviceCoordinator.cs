//-:cnd:noEmit
// [mirror] IBitDeviceCoordinator - applying the theme to native chrome - keep in sync with:
// - src/Client/Boilerplate.Client.Windows/Infrastructure/Services/WindowsDeviceCoordinator.cs

using Bit.BlazorUI;

namespace Boilerplate.Client.Maui.Infrastructure.Services;

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

        // The theme's own page background, so the status bar and the WebView below it meet without a seam.
        window.SetStatusBarColor(Android.Graphics.Color.ParseColor(
            BitExtraThemeSurfaces.BackgroundPrimary[isDark ? BitExtraThemePresets.Fluent2Dark : BitExtraThemePresets.Fluent2Light]));
#elif IOS
        var statusBarStyle = isDark ? UIKit.UIStatusBarStyle.LightContent : UIKit.UIStatusBarStyle.DarkContent;
        await Device.InvokeOnMainThreadAsync(() =>
        {
            UIKit.UIApplication.SharedApplication.SetStatusBarStyle(statusBarStyle, false);
            Platform.GetCurrentUIViewController()!.SetNeedsStatusBarAppearanceUpdate();
        });
#elif MACCATALYST
        var window = UIKit.UIApplication.SharedApplication.Windows[0].WindowScene;
        window?.Titlebar?.TitleVisibility = UIKit.UITitlebarTitleVisibility.Hidden;
#endif
    }
}
