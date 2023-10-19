//-:cnd:noEmit
using Microsoft.AspNetCore.Components.Routing;

namespace AdminPanel.Client.Core;

public partial class App
{
#if BlazorWebAssembly && !BlazorHybrid
    private List<System.Reflection.Assembly> _lazyLoadedAssemblies = new();
    [AutoInject] private Microsoft.AspNetCore.Components.WebAssembly.Services.LazyAssemblyLoader _assemblyLoader = default!;
    [AutoInject] private AuthenticationStateProvider _authenticationStateProvider = default!;
#endif

    [AutoInject] private IJSRuntime _jsRuntime = default!;
    [AutoInject] private IBitDeviceCoordinator _bitDeviceCoordinator { get; set; } = default!;

#if BlazorHybrid && MultilingualEnabled
    private bool _cultureHasNotBeenSet = true;
#endif

#if BlazorHybrid
    protected override async Task OnInitializedAsync()
    {
        await SetupBodyClasses();
        await base.OnInitializedAsync();
    }
#else
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await SetupBodyClasses();
        }

        await base.OnAfterRenderAsync(firstRender);
    }
#endif

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
        else if (BlazorModeDetector.Current.IsBlazorElectron())
        {
            cssClasses.Add("bit-blazor-electron");
        }

        var cssVariables = new Dictionary<string, string>();
        var statusBarHeight = _bitDeviceCoordinator.GetStatusBarHeight();

        if (OperatingSystem.IsMacCatalyst() is false)
        {
            //For iOS this is handled in css using safe-area env() variables
            //For Android there's an issue with keyboard in fullscreen mode. more info: https://github.com/bitfoundation/bitplatform/issues/5626
            //For Windows there's an issue with TitleBar. more info: https://github.com/bitfoundation/bitplatform/issues/5695
            statusBarHeight = 0;
        }

        cssVariables.Add("--bit-status-bar-height", $"{statusBarHeight.ToString("F3", CultureInfo.InvariantCulture)}px");
        await _jsRuntime.ApplyBodyElementClasses(cssClasses, cssVariables);
    }

    private async Task OnNavigateAsync(NavigationContext args)
    {
        // Blazor Server & Pre Rendering use created cultures in UseRequestLocalization middleware
        // Android, windows and iOS have to set culture programmatically.
        // Browser's culture is handled in the Web project's Program.BlazorWebAssembly.cs
#if BlazorHybrid && MultilingualEnabled
        if (_cultureHasNotBeenSet)
        {
            _cultureHasNotBeenSet = false;
            var preferredCultureCookie = Preferences.Get(".AspNetCore.Culture", null);
            CultureInfoManager.SetCurrentCulture(preferredCultureCookie);
        }
#endif

#if BlazorWebAssembly && !BlazorHybrid
        if ((args.Path is "/" or "") && _lazyLoadedAssemblies.Any(asm => asm.GetName().Name == "Newtonsoft.Json") is false)
        {
            var isAuthenticated = (await _authenticationStateProvider.GetAuthenticationStateAsync()).User?.Identity?.IsAuthenticated is true;
            if (isAuthenticated)
            {
                var assemblies = await _assemblyLoader.LoadAssembliesAsync(new[] { "Newtonsoft.Json.wasm", "System.Private.Xml.wasm", "System.Data.Common.wasm" });
                _lazyLoadedAssemblies.AddRange(assemblies);
            }
        }
#endif

        await Task.CompletedTask;
    }
}
