//+:cnd:noEmit
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

        //#if (advancedTests == true)
        //Pass configuration to BlazorWebAssembly
        //More info: https://stackoverflow.com/questions/60831359/how-are-string-args-passed-to-program-main-in-a-blazor-webassembly-app
        try
        {
            var js = (IJSInProcessRuntime)builder.Services.BuildServiceProvider().GetRequiredService<IJSRuntime>();
            var startupParams = js.Invoke<string[]>("startupParams");
            var configData = startupParams.Select(p => p.Split('=')).ToDictionary(p => p[0], p => p[1]);
            builder.Configuration.AddInMemoryCollection(configData!);
        }
        catch { }
        //#endif

        AppEnvironment.Set(builder.HostEnvironment.Environment);

        builder.Configuration.AddClientConfigurations(clientEntryAssemblyName: "Boilerplate.Client.Web");

        if (Environment.GetEnvironmentVariable("__BLAZOR_WEBASSEMBLY_WAIT_FOR_ROOT_COMPONENTS") != "true")
        {
            // By default, App.razor adds Routes and HeadOutlet.
            // The following is only required for blazor webassembly standalone.
            builder.RootComponents.Add<HeadOutlet>("head::after");
            //#if (appInsights == true)
            builder.RootComponents.Add<BlazorApplicationInsights.ApplicationInsightsInit>(selector: "head::after");
            //#endif
            builder.RootComponents.Add<Routes>("#app-container");
        }

        builder.ConfigureServices();

        var host = builder.Build();

        AppDomain.CurrentDomain.UnhandledException += (_, e) => LogException(e.ExceptionObject, reportedBy: nameof(AppDomain.UnhandledException), host);
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LogException(e.Exception, nameof(TaskScheduler.UnobservedTaskException), host);
            e.SetObserved();
        };

        if (CultureInfoManager.MultilingualEnabled)
        {
            var cultureCookie = await host.Services.GetRequiredService<Cookie>().GetValue(".AspNetCore.Culture");

            if (cultureCookie is not null)
            {
                cultureCookie = Uri.UnescapeDataString(cultureCookie);
                cultureCookie = cultureCookie[(cultureCookie.IndexOf("|uic=") + 5)..];
            }

            var navigationManager = host.Services.GetRequiredService<NavigationManager>();

            var culture = new Uri(navigationManager.Uri).GetCulture() ?? // 1- Culture query string OR Route data request culture
                          cultureCookie ?? // 2- User settings
                          CultureInfo.CurrentUICulture.Name; // 3- OS/Browser settings

            host.Services.GetRequiredService<CultureInfoManager>().SetCurrentCulture(culture);
        }

        await host.RunAsync();
    }

    private static void LogException(object? error, string reportedBy, WebAssemblyHost host)
    {
        if (host.Services is IServiceProvider services && error is Exception exp)
        {
            services.GetRequiredService<IExceptionHandler>().Handle(exp, parameters: new()
            {
                { nameof(reportedBy), reportedBy }
            }, displayKind: AppEnvironment.IsDev() ? ExceptionDisplayKind.NonInterrupting : ExceptionDisplayKind.None);
        }
        else
        {
            _ = System.Console.Error.WriteLineAsync(error?.ToString() ?? "Unknown error");
        }
    }
}
