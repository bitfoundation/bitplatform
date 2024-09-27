//-:cnd:noEmit
using System.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Bit.Butil;

namespace Boilerplate.Client.Web;

public static partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        AppEnvironment.Set(builder.HostEnvironment.Environment);

        if (Environment.GetEnvironmentVariable("__BLAZOR_WEBASSEMBLY_WAIT_FOR_ROOT_COMPONENTS") != "true")
        {
            // By default, App.razor adds Routes and HeadOutlet.
            // The following is only required for blazor webassembly standalone.
            builder.RootComponents.Add<Routes>("#app-container");
            builder.RootComponents.Add<HeadOutlet>("head::after");
        }

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

        await host.RunAsync();
    }
}
