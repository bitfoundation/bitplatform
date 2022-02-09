using System.Reflection;
using Microsoft.Extensions.FileProviders;

namespace TodoTemplate.App;

public static class MauiProgram
{
    public static MauiAppBuilder CreateMauiAppBuilder()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .RegisterBlazorMauiWebView()
            .UseMauiApp<App>().Host.ConfigureAppConfiguration((app, config) =>
            {
                var assembly = typeof(MauiProgram).GetTypeInfo().Assembly;
                config.AddJsonFile(new EmbeddedFileProvider(assembly), "appsettings.json", optional: false, false);
            });

        var services = builder.Services;

        services.AddTodoTemplateSharedServices();
        services.AddTodoTemplateServices();
        services.AddBlazorWebView();

        return builder;
    }
}
