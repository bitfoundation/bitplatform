using Boilerplate.Client.Core.Services.DiagnosticLog;

namespace Microsoft.Extensions.Logging;

public static class ILoggingBuilderExtensions
{
    public static ILoggingBuilder AddDiagnosticLogger(this ILoggingBuilder builder)
    {
        if (AppPlatform.IsBlazorHybridOrBrowser)
        {
            builder.Services.AddScoped<ConcurrentBag<DiagnosticLog>>(); // In memory log store
        }
        else
        {
            builder.Services.AddSingleton<ConcurrentBag<DiagnosticLog>>(); // In memory log store
        }

        if (AppPlatform.IsBlazorHybridOrBrowser 
            || AppEnvironment.IsDev() /* Blazor server in dev env only */)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DiagnosticLoggerProvider>());
        }

        return builder;
    }

    public static ILoggingBuilder ConfigureLoggers(this ILoggingBuilder loggingBuilder)
    {
        loggingBuilder.ClearProviders();

        if (AppEnvironment.IsDev())
        {
            loggingBuilder.AddDebug();
        }

        if (!AppPlatform.IsBrowser)
        {
            loggingBuilder.AddConsole();
            // DiagnosticLogger is already logging in browser's console.
            // But Console logger is still useful in Visual Studio's Device Log (Android, iOS) or BrowserStack etc.
        }

        loggingBuilder.AddDiagnosticLogger();

        return loggingBuilder;
    }
}
