//-:cnd:noEmit
using Microsoft.AspNetCore.Components.Routing;

namespace Boilerplate.Client.Core;

public partial class App
{
#if BlazorWebAssembly
    private List<System.Reflection.Assembly> lazyLoadedAssemblies = new();
    [AutoInject] private Microsoft.AspNetCore.Components.WebAssembly.Services.LazyAssemblyLoader assemblyLoader = default!;
    [AutoInject] private AuthenticationStateProvider authenticationStateProvider = default!;
#endif

    [AutoInject] private IStorageService storageService = default!;
    [AutoInject] private IJSRuntime jsRuntime = default!;
    [AutoInject] private IBitDeviceCoordinator bitDeviceCoordinator = default!;

#if MultilingualEnabled
    private bool cultureHasNotBeenSet = true;
#endif

    protected override async Task OnInitializedAsync()
    {
        if(BlazorModeDetector.Current.IsBlazorHybrid())
        {
            await SetupBodyClasses();
            await base.OnInitializedAsync();
        }
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && BlazorModeDetector.Current.IsBlazorHybrid() is false)
        {
            await SetupBodyClasses();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task SetupBodyClasses()
    {
        var cssClasses = new List<string>();

        if (BlazorModeDetector.Current.IsBlazorWebAssembly())
        {
            cssClasses.Add("bit-blazor-wasm");
        }
        else if (BlazorModeDetector.Current.IsBlazorServer())
        {
            cssClasses.Add("bit-blazor-server");
        }
        else if (BlazorModeDetector.Current.IsBlazorHybrid())
        {
            cssClasses.Add("bit-blazor-hybrid");

            if (OperatingSystem.IsWindows())
            {
                cssClasses.Add("bit-windows");
            }
            else if (OperatingSystem.IsLinux())
            {
                cssClasses.Add("bit-linux");
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

    private async Task OnNavigateAsync(NavigationContext args)
    {
        // Blazor Server & Pre Rendering use created cultures in UseRequestLocalization middleware
        // Android, windows and iOS have to set culture programmatically.
        // Browser's culture is handled in the Web project's Program.BlazorWebAssembly.cs
#if MultilingualEnabled
        if (cultureHasNotBeenSet && BlazorModeDetector.Current.IsBlazorHybrid())
        {
            cultureHasNotBeenSet = false;
            var preferredCulture = await storageService.GetItem("Culture");
            CultureInfoManager.SetCurrentCulture(preferredCulture);
        }
#endif

#if BlazorWebAssembly
        if ((args.Path is "dashboard") && lazyLoadedAssemblies.Any(asm => asm.GetName().Name == "Newtonsoft.Json") is false)
        {
            var isAuthenticated = (await authenticationStateProvider.GetAuthenticationStateAsync()).User?.Identity?.IsAuthenticated is true;
            if (isAuthenticated)
            {
                var assemblies = await assemblyLoader.LoadAssembliesAsync(["Newtonsoft.Json.wasm", "System.Private.Xml.wasm", "System.Data.Common.wasm"]);
                lazyLoadedAssemblies.AddRange(assemblies);
            }
        }
#endif
    }
}
