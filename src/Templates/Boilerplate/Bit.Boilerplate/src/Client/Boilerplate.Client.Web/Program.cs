//-:cnd:noEmit
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
#if BlazorWebAssemblyStandalone
using Microsoft.AspNetCore.Components.Web;
#endif

namespace Boilerplate.Client.Web;

public static partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

#if BlazorWebAssemblyStandalone
        builder.RootComponents.Add<Routes>("#app-container");
        builder.RootComponents.Add<HeadOutlet>("head::after");
#endif

        //#if (appInsights == true)
        builder.RootComponents.Add<BlazorApplicationInsights.ApplicationInsightsInit>("head::after");
        //#endif

        builder.ConfigureServices();

        var host = builder.Build();

        if (AppRenderMode.MultilingualEnabled)
        {
            var culture = await host.Services.GetRequiredService<IStorageService>().GetItem("Culture");
            host.Services.GetRequiredService<CultureInfoManager>().SetCurrentCulture(culture);
        }

        try
        {
            await host.RunAsync();
        }
        catch (JSException exp) when (exp.Message is "Error: Could not find any element matching selector '#app-container'.")
        {
#if BlazorWebAssemblyStandalone
            await Console.Error.WriteLineAsync("Either run/publish Client.Web project or set BlazorWebAssemblyStandalone to false.");
#endif
        }

    }
}
