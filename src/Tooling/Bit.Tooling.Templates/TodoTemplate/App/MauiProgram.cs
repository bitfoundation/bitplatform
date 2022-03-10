using System.Reflection;
using Microsoft.Extensions.FileProviders;

namespace TodoTemplate.App;

public static class MauiProgram
{
    public static MauiAppBuilder CreateMauiAppBuilder()
    {
        var builder = MauiApp.CreateBuilder();
        var assembly = typeof(MauiProgram).GetTypeInfo().Assembly;

        builder
            .RegisterBlazorMauiWebView()
            .UseMauiApp<App>()
            .Configuration.AddJsonFile(new EmbeddedFileProvider(assembly), "appsettings.json", optional: false, false);

        builder.Services.AddBlazorWebView();

        var services = builder.Services;

        services.AddTransient<IAuthTokenProvider, ClientSideAuthTokenProvider>();
        services.AddTodoTemplateSharedServices();
        services.AddTodoTemplateAppServices();
        services.AddBlazorWebView();

        return builder;
    }
}
