using System.Reflection;
using Microsoft.AspNetCore.Components.Routing;

namespace TodoTemplate.Client.Shared;

public partial class App
{
#if BlazorWebAssembly && !BlazorHybrid
    private List<Assembly> lazyLoadedAssemblies = new();
    [Inject] private Microsoft.AspNetCore.Components.WebAssembly.Services.LazyAssemblyLoader AssemblyLoader { get; set; } = default!;
#endif

    [Inject] private IJSRuntime _jsRuntime { get; set; } = default!;

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
        if (args.Path.Contains("some-lazy-loaded-page") && lazyLoadedAssemblies.Any(asm => asm.GetName().Name == "SomeAssembly") is false)
        {
            var assemblies = await AssemblyLoader.LoadAssembliesAsync(new[] { "SomeAssembly.dll" });
            lazyLoadedAssemblies.AddRange(assemblies);
        }
#endif
    }
}
