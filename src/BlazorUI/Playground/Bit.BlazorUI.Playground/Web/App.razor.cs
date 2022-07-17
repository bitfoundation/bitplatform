using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Routing;
#if BlazorWebAssembly
using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
#endif

namespace Bit.BlazorUI.Playground.Web;

public partial class App
{
    private List<Assembly> lazyLoadedAssemblies = new(); // new List<Assembly> { Assembly.Load("Bit.BlazorUI") };

#if BlazorWebAssembly
    [Inject] private LazyAssemblyLoader AssemblyLoader {get;set;}
#endif

    private async Task OnNavigateAsync(NavigationContext args)
    {
#if BlazorWebAssembly
        try
        {
            if (args.Path.Contains("chart"))
            {
                var assemblies = await AssemblyLoader.LoadAssembliesAsync(new[] { "Newtonsoft.Json.dll" });
                lazyLoadedAssemblies.AddRange(assemblies);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: {Message}", ex.Message);
        }
#endif
    }
}
