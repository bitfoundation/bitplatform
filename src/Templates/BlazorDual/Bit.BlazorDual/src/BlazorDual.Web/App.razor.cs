﻿//-:cnd:noEmit
using Microsoft.AspNetCore.Components.Routing;

namespace BlazorDual.Web;

public partial class App
{
#if BlazorWebAssembly
    private List<System.Reflection.Assembly> _lazyLoadedAssemblies = new();
    [AutoInject] private Microsoft.AspNetCore.Components.WebAssembly.Services.LazyAssemblyLoader _assemblyLoader = default!;
#endif

    [AutoInject] private IJSRuntime _jsRuntime = default!;

    private async Task OnNavigateAsync(NavigationContext args)
    {
#if BlazorWebAssembly
        if (args.Path.Contains("some-lazy-loaded-page") && _lazyLoadedAssemblies.Any(asm => asm.GetName().Name == "SomeAssembly") is false)
        {
            var assemblies = await _assemblyLoader.LoadAssembliesAsync(["SomeAssembly.wasm"]);
            _lazyLoadedAssemblies.AddRange(assemblies);
        }
#endif
    }
}
