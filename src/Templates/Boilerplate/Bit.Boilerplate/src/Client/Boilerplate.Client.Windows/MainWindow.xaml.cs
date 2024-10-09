﻿using Microsoft.Web.WebView2.Core;
using Microsoft.AspNetCore.Components.WebView;

namespace Boilerplate.Client.Windows;

public partial class MainWindow
{
    public MainWindow()
    {
        AppPlatform.IsBlazorHybrid = true;
        var services = new ServiceCollection();
        services.AddClientWindowsProjectServices();
        InitializeComponent();
        AppWebView.Services = services.BuildServiceProvider();
        if (CultureInfoManager.MultilingualEnabled)
        {
            AppWebView.Services.GetRequiredService<CultureInfoManager>().SetCurrentCulture(
                App.Current.Properties["Culture"]?.ToString() ?? // 1- User settings
                CultureInfo.CurrentUICulture.Name); // 2- OS Settings
        }
        AppWebView.Services.GetRequiredService<IPubSubService>().Subscribe(PubSubMessages.CULTURE_CHANGED, async culture =>
        {
            App.Current.Shutdown();
            Application.Restart();
        });
        AppWebView.Loaded += async delegate
        {
            AppWebView.WebView.DefaultBackgroundColor = ColorTranslator.FromHtml(App.Current.Resources["PrimaryBgColor"].ToString()!);
            await AppWebView.WebView.EnsureCoreWebView2Async();
            AppWebView.WebView.CoreWebView2.PermissionRequested += async (sender, args) =>
            {
                args.Handled = true;
                args.State = CoreWebView2PermissionState.Allow;
            };
            var settings = AppWebView.WebView.CoreWebView2.Settings;
            if (AppEnvironment.IsDev() is false)
            {
                settings.IsZoomControlEnabled = false;
                settings.AreBrowserAcceleratorKeysEnabled = false;
            }
            AppWebView.WebView.NavigationCompleted += async delegate
            {
                await AppWebView.WebView.ExecuteScriptAsync("Blazor.start()");
            };
        };
    }

    void BlazorWebViewInitializing(object sender, BlazorWebViewInitializingEventArgs e)
    {
        e.EnvironmentOptions = new() { AdditionalBrowserArguments = "--unsafely-treat-insecure-origin-as-secure=https://0.0.0.0 --enable-notifications" };
    }
}
