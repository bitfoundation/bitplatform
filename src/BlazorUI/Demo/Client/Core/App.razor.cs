using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI.Demo.Client.Core;

public partial class App
{
#if BlazorWebAssembly && !BlazorHybrid
    private List<System.Reflection.Assembly> _lazyLoadedAssemblies = new();
    [AutoInject] private Microsoft.AspNetCore.Components.WebAssembly.Services.LazyAssemblyLoader _assemblyLoader = default!;
#endif

    [AutoInject] private IJSRuntime _jsRuntime = default!;
    [AutoInject] private IBitDeviceCoordinator _bitDeviceCoordinator { get; set; } = default!;

    private bool _cultureHasNotBeenSet = true;

#if BlazorHybrid
    protected override async Task OnInitializedAsync()
    {
        SetupBodyClasses();
        await base.OnInitializedAsync();
    }
#else
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            SetupBodyClasses();
        }

        base.OnAfterRender(firstRender);
    }
#endif

    private void SetupBodyClasses()
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

        if ((OperatingSystem.IsIOS() || OperatingSystem.IsAndroid()) && OperatingSystem.IsMacCatalyst() is false)
        {
            //For iOS this is handled in css using safe-area env() variables
            //For Android there's an issue with keyboard in fullscreen mode
            statusBarHeight = 0;
        }
        
        cssVariables.Add("--bit-status-bar-height", $"{statusBarHeight.ToString("F3", CultureInfo.InvariantCulture)}px");
        _ = _jsRuntime.ApplyBodyElementClasses(cssClasses, cssVariables);
    }

    private async Task OnNavigateAsync(NavigationContext args)
    {
#if BlazorWebAssembly && !BlazorHybrid
        if (args.Path.Contains("chart") && _lazyLoadedAssemblies.Any(asm => asm.GetName().Name == "Newtonsoft.Json") is false)
        {
            var assemblies = await _assemblyLoader.LoadAssembliesAsync(new[] { "Newtonsoft.Json.wasm", "System.Private.Xml.wasm", "System.Data.Common.wasm" });
            _lazyLoadedAssemblies.AddRange(assemblies);
        }
#endif
    }
}
