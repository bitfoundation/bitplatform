using Microsoft.AspNetCore.Components.Routing;

namespace BlazorWeb.Client;

public partial class Routes
{
    private List<System.Reflection.Assembly> _lazyLoadedAssemblies = [];
    [AutoInject] private Microsoft.AspNetCore.Components.WebAssembly.Services.LazyAssemblyLoader _assemblyLoader = default!;

    private async Task OnNavigateAsync(NavigationContext args)
    {
        if (OperatingSystem.IsBrowser())
        {
            if ((args.Path is "dashboard") && _lazyLoadedAssemblies.Any(asm => asm.GetName().Name == "Newtonsoft.Json") is false)
            {
                var assemblies = await _assemblyLoader.LoadAssembliesAsync(["Newtonsoft.Json.wasm", "System.Private.Xml.wasm", "System.Data.Common.wasm"]);
                _lazyLoadedAssemblies.AddRange(assemblies);
            }
        }
    }
}
