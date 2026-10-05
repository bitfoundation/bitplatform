//+:cnd:noEmit
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Client.Maui;

public partial class MainPage
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HeadOutlet))]
    public MainPage()
    {
        InitializeComponent();

        // What shows before the WebView has painted anything, and on iOS through it, so it is the theme's background.
        var light = Color.FromArgb(AppThemePresets.LightBackground);
        var dark = Color.FromArgb(AppThemePresets.DarkBackground);
        this.SetAppThemeColor(BackgroundColorProperty, light, dark);
        AppWebView.SetAppThemeColor(BackgroundColorProperty, light, dark);
    }
}
