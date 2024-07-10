namespace Boilerplate.Client.Core;

public partial class Routes
{
    [AutoInject] IJSRuntime jsRuntime = default!;
    [AutoInject] IBitDeviceCoordinator bitDeviceCoordinator = default!;
    [AutoInject] IStorageService storageService = default!;
    [AutoInject] CultureInfoManager cultureInfoManager = default!;

    protected override async Task OnInitializedAsync()
    {
        if (PlatformInfo.IsBlazorHybrid)
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

        if (PlatformInfo.IsWindows)
        {
            cssClasses.Add("bit-windows");
        }
        else if (PlatformInfo.IsMacOS)
        {
            cssClasses.Add("bit-macos");
        }
        else if (PlatformInfo.IsIOS)
        {
            cssClasses.Add("bit-ios");
        }
        else if (PlatformInfo.IsAndroid)
        {
            cssClasses.Add("bit-android");
        }

        var cssVariables = new Dictionary<string, string>();
        var statusBarHeight = bitDeviceCoordinator.GetStatusBarHeight();

        if (PlatformInfo.IsMacOS is false)
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
