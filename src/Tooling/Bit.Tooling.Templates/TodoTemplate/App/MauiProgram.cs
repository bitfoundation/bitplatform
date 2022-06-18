//-:cnd:noEmit
using System.Reflection;
using Microsoft.Extensions.FileProviders;

namespace TodoTemplate.App;

public static class MauiProgram
{
    public static MauiAppBuilder CreateMauiAppBuilder()
    {
#if !BlazorHybrid
        throw new InvalidOperationException("Please switch to blazor hybrid as described in readme.md");
#endif

        var builder = MauiApp.CreateBuilder();
        var assembly = typeof(MauiProgram).GetTypeInfo().Assembly;

        builder
            .UseMauiApp<App>()
            .Configuration.AddJsonFile(new EmbeddedFileProvider(assembly), "appsettings.json", optional: false, false);

        var services = builder.Services;

        services.AddMauiBlazorWebView();

#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif
        services.AddTransient<IAuthTokenProvider, ClientSideAuthTokenProvider>();
        services.AddTodoTemplateSharedServices();
        services.AddTodoTemplateAppServices();

        return builder;
    }
}
