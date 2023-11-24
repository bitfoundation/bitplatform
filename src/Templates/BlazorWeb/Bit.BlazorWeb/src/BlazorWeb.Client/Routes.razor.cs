using Microsoft.AspNetCore.Components.Routing;

namespace BlazorWeb.Client;

public partial class Routes
{
    private List<System.Reflection.Assembly> lazyLoadedAssemblies = [];
    [AutoInject] private Microsoft.AspNetCore.Components.WebAssembly.Services.LazyAssemblyLoader assemblyLoader = default!;

    private async Task OnNavigateAsync(NavigationContext args)
    {
        if (OperatingSystem.IsBrowser())
        {
            if ((args.Path is "dashboard") && lazyLoadedAssemblies.Any(asm => asm.GetName().Name == "Newtonsoft.Json") is false)
            {
                var assemblies = await assemblyLoader.LoadAssembliesAsync(["Newtonsoft.Json.wasm", "System.Private.Xml.wasm", "System.Data.Common.wasm"]);
                lazyLoadedAssemblies.AddRange(assemblies);
            }
        }
    }
}
