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

        AppEnvironment.Set(builder.HostEnvironment.Environment);

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

        if (CultureInfoManager.MultilingualEnabled)
        {
            var cultureCookie = await host.Services.GetRequiredService<Cookie>().GetValue(".AspNetCore.Culture");

            if (cultureCookie is not null)
            {
                cultureCookie = Uri.UnescapeDataString(cultureCookie);
                cultureCookie = cultureCookie[(cultureCookie.IndexOf("|uic=") + 5)..];
            }

            var navigationManager = host.Services.GetRequiredService<NavigationManager>();

            var culture = navigationManager.GetCultureFromUri() ?? // 1- Culture query string OR Route data request culture
                          cultureCookie ?? // 2- User settings
                          CultureInfo.CurrentUICulture.Name; // 3- OS/Browser settings

            host.Services.GetRequiredService<CultureInfoManager>().SetCurrentCulture(culture);
        }

        try
        {
            await host.RunAsync();
        }
        catch (JSException exp) when (exp.Message is "Error: Could not find any element matching selector '#app-container'.")
        {
#if BlazorWebAssemblyStandalone
            await System.Console.Error.WriteLineAsync("Either run/publish Client.Web project or set BlazorWebAssemblyStandalone to false.");
#else
            throw;
#endif
        }

    }
}
