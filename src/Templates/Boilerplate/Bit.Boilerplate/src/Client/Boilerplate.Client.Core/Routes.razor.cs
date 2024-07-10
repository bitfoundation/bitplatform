﻿namespace Boilerplate.Client.Core;

public partial class Routes
{
    [AutoInject] IJSRuntime jsRuntime = default!;
    [AutoInject] IBitDeviceCoordinator bitDeviceCoordinator = default!;
    [AutoInject] IStorageService storageService = default!;
    [AutoInject] CultureInfoManager cultureInfoManager = default!;

    protected override async Task OnInitializedAsync()
    {
        if (AppOperatingSystem.IsBlazorHybrid)
        {
            if (CultureInfoManager.MultilingualEnabled)
            {
                cultureInfoManager.SetCurrentCulture(await storageService.GetItem("Culture") ?? // 1- User settings
                                                     CultureInfo.CurrentUICulture.Name); // 2- OS settings
            }

            await SetupBodyClasses();
        }

        await base.OnInitializedAsync();
    }

    private async Task SetupBodyClasses()
    {
        var cssClasses = new List<string> { };

        if (AppOperatingSystem.IsRunningOnWindows)
        {
            cssClasses.Add("bit-windows");
        }
        else if (AppOperatingSystem.IsRunningOnMacOS)
        {
            cssClasses.Add("bit-macos");
        }
        else if (AppOperatingSystem.IsRunningOnIOS)
        {
            cssClasses.Add("bit-ios");
        }
        else if (AppOperatingSystem.IsRunningOnAndroid)
        {
            cssClasses.Add("bit-android");
        }

        var cssVariables = new Dictionary<string, string>();
        var statusBarHeight = bitDeviceCoordinator.GetStatusBarHeight();

        if (AppOperatingSystem.IsRunningOnMacOS is false)
        {
            //For iOS this is handled in css using safe-area env() variables
            //For Android there's an issue with keyboard in fullscreen mode. more info: https://github.com/bitfoundation/bitplatform/issues/5626
            //For Windows there's an issue with TitleBar. more info: https://github.com/bitfoundation/bitplatform/issues/5695
            statusBarHeight = 0;
        }

        cssVariables.Add("--bit-status-bar-height", $"{statusBarHeight.ToString("F3", CultureInfo.InvariantCulture)}px");
        await jsRuntime.ApplyBodyElementClasses(cssClasses, cssVariables);
    }

    [AutoInject] NavigationManager? navigationManager { set => universalLinksNavigationManager = value; get => universalLinksNavigationManager; }
    public static NavigationManager? universalLinksNavigationManager;

    public static async Task OpenUniversalLink(string url, bool forceLoad = false, bool replace = false)
    {
        await Task.Run(async () =>
        {
            while (universalLinksNavigationManager is null)
            {
                await Task.Yield();
            }
        });

        universalLinksNavigationManager!.NavigateTo(url, forceLoad, replace);
    }
}
