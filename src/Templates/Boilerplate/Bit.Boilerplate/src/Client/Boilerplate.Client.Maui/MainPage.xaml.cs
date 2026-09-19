//+:cnd:noEmit
using Bit.BlazorUI;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Client.Maui;

public partial class MainPage
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HeadOutlet))]
    public MainPage()
    {
        InitializeComponent();

        // What shows before the WebView has painted anything, so it is the theme's own page background - the same
        // color MauiDeviceCoordinator gives the status bar.
        var light = Color.FromArgb(BitExtraThemeSurfaces.BackgroundPrimary[BitExtraThemePresets.Fluent2Light]);
        var dark = Color.FromArgb(BitExtraThemeSurfaces.BackgroundPrimary[BitExtraThemePresets.Fluent2Dark]);
        this.SetAppThemeColor(BackgroundColorProperty, light, dark);
        AppWebView.SetAppThemeColor(BackgroundColorProperty, light, dark);
    }
}
