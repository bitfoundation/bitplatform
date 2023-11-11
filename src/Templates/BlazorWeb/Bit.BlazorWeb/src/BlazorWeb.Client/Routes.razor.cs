﻿using Microsoft.AspNetCore.Components.Routing;

namespace BlazorWeb.Client;

public partial class Routes
{
    private List<System.Reflection.Assembly> _lazyLoadedAssemblies = [];
    [AutoInject] private Microsoft.AspNetCore.Components.WebAssembly.Services.LazyAssemblyLoader _assemblyLoader = default!;

    [AutoInject] private IJSRuntime _jsRuntime = default!;

    private async Task OnNavigateAsync(NavigationContext args)
    {
        if (OperatingSystem.IsBrowser())
        {
            if (args.Path.Contains("some-lazy-loaded-page") && _lazyLoadedAssemblies.Any(asm => asm.GetName().Name == "SomeAssembly") is false)
            {
                var assemblies = await _assemblyLoader.LoadAssembliesAsync(["SomeAssembly.wasm"]);
                _lazyLoadedAssemblies.AddRange(assemblies);
            }
        }
    }
}
