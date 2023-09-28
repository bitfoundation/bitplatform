using System.Reflection;
using Microsoft.Extensions.FileProviders;
using Bit.BlazorUI.Demo.Client.Core.Shared;

namespace Bit.BlazorUI.Demo.Client.App;

public static class MauiProgram
{
    public static MauiAppBuilder CreateMauiAppBuilder()
    {
        try
        {
            // For MacCatalyst there's still some issues with AppCenter
            // https://github.com/microsoft/appcenter-sdk-dotnet/issues/1755
            // https://github.com/microsoft/appcenter-sdk-dotnet/issues/1702
#if Android || iOS || Windows
            string appSecret = OperatingSystem.IsWindows() ? "a206e212-6427-414f-b4f3-83fa5eec4f1d"
                             : OperatingSystem.IsAndroid() ? "c87802e3-0fa5-4938-b539-086b06d40726" : "f76345b1-9069-4477-afbe-a2be2a2ed46d";
            Microsoft.AppCenter.AppCenter.Start(appSecret, typeof(Microsoft.AppCenter.Crashes.Crashes));
#endif

#if !BlazorHybrid
            throw new InvalidOperationException("Please switch to blazor hybrid as described in readme.md");
#endif

            var builder = MauiApp.CreateBuilder();
            var assembly = typeof(MainLayout).GetTypeInfo().Assembly;

            builder
                .UseMauiApp<App>()
                .Configuration.AddJsonFile(new EmbeddedFileProvider(assembly), "appsettings.json", optional: false, false);

            var services = builder.Services;

            services.AddMauiBlazorWebView();
#if DEBUG
            services.AddBlazorWebViewDeveloperTools();
#endif

            services.AddScoped(sp =>
            {
                HttpClient httpClient = new(sp.GetRequiredService<AppHttpClientHandler>())
                {
                    BaseAddress = new Uri(sp.GetRequiredService<IConfiguration>().GetApiServerAddress())
                };

                return httpClient;
            });

            services.AddSharedServices();
            services.AddClientSharedServices();
            services.AddClientAppServices();

            return builder;
        }
        catch (Exception ex)
        {
#if Android || iOS || Windows
            Microsoft.AppCenter.Crashes.Crashes.TrackError(ex);
#endif
            throw;
        }
    }
}
