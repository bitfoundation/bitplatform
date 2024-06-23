namespace Bit.BlazorUI.Demo.Client.Core;

public partial class Routes
{
    [AutoInject] IJSRuntime jsRuntime = default!;
    [AutoInject] IBitDeviceCoordinator bitDeviceCoordinator = default!;

    protected override async Task OnInitializedAsync()
    {
        if (AppRenderMode.IsBlazorHybrid)
        {
            await SetupBodyClasses();
        }

        await base.OnInitializedAsync();
    }

    private async Task SetupBodyClasses()
    {
        var cssClasses = new List<string> { };

        if (OperatingSystem.IsWindows())
        {
            cssClasses.Add("bit-windows");
        }
        else if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
        {
            cssClasses.Add("bit-macos");
        }
        else if (OperatingSystem.IsIOS() && OperatingSystem.IsMacCatalyst() is false)
        {
            cssClasses.Add("bit-ios");
        }
        else if (OperatingSystem.IsAndroid())
        {
            cssClasses.Add("bit-android");
        }

        var cssVariables = new Dictionary<string, string>();
        var statusBarHeight = bitDeviceCoordinator.GetStatusBarHeight();

        if (OperatingSystem.IsMacCatalyst() is false)
        {
            //For iOS this is handled in css using safe-area env() variables
            //For Android there's an issue with keyboard in fullscreen mode. more info: https://github.com/bitfoundation/bitplatform/issues/5626
            //For Windows there's an issue with TitleBar. more info: https://github.com/bitfoundation/bitplatform/issues/5695
            statusBarHeight = 0;
        }

        cssVariables.Add("--bit-status-bar-height", $"{statusBarHeight.ToString("F3", CultureInfo.InvariantCulture)}px");
        await jsRuntime.ApplyBodyElementClasses(cssClasses, cssVariables);
    }
}
