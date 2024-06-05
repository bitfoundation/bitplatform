//-:cnd:noEmit
using System.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Bit.Butil;
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

        //+:cnd:noEmit
        //#if (appInsights == true)
        builder.RootComponents.Add<BlazorApplicationInsights.ApplicationInsightsInit>("head::after");
        //#endif
        //-:cnd:noEmit

        builder.ConfigureServices();

        var host = builder.Build();

        if (AppRenderMode.MultilingualEnabled)
        {
            var uri = new Uri(host.Services.GetRequiredService<NavigationManager>().Uri);

            var cultureCookie = await host.Services.GetRequiredService<Cookie>().GetValue(".AspNetCore.Culture");

            if (cultureCookie is not null)
            {
                cultureCookie = Uri.EscapeDataString(cultureCookie); // temporary butil workaround
                cultureCookie = cultureCookie[(cultureCookie.IndexOf("|uic=") + 5)..];
            }

            var culture = (await host.Services.GetRequiredService<IStorageService>().GetItem("Culture")) ?? // 1- User settings
                          HttpUtility.ParseQueryString(uri.Query)["culture"] ?? // 2- Culture query string
                          cultureCookie ?? // 3- Culture cookie
                          CultureInfo.CurrentUICulture.Name; // 4- OS/Browser settings

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
