//-:cnd:noEmit
using System.Reflection;
using Microsoft.AspNetCore.Components.Routing;

namespace AdminPanel.Client.Shared;

public partial class App
{
#if BlazorWebAssembly && !BlazorHybrid
    private List<Assembly> _lazyLoadedAssemblies = new();
    [AutoInject] private Microsoft.AspNetCore.Components.WebAssembly.Services.LazyAssemblyLoader _assemblyLoader = default!;
    [AutoInject] private AuthenticationStateProvider _authenticationStateProvider = default!;
#endif

    [AutoInject] private IJSRuntime _jsRuntime = default!;

    private bool _cultureHasNotBeenSet = true;

    private async Task OnNavigateAsync(NavigationContext args)
    {
        // Blazor Server & Pre Rendering use created cultures in UseRequestLocalization middleware
        // Android, windows and iOS have to set culture programmatically.
        // Browser is gets handled in Web project's Program.cs
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
                var assemblies = await _assemblyLoader.LoadAssembliesAsync(new[] { "Newtonsoft.Json.dll", "System.Private.Xml.dll", "System.Data.Common.dll" });
                _lazyLoadedAssemblies.AddRange(assemblies);
            }
        }
#endif
    }
}
