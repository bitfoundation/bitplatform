using Boilerplate.Client.Core.Services.DiagnosticLog;

namespace Microsoft.Extensions.Logging;

public static class ILoggingBuilderExtensions
{
    public static ILoggingBuilder AddDiagnosticLogger(this ILoggingBuilder builder)
    {
        builder.Services.AddSessioned<DiagnosticLogger>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DiagnosticLoggerProvider>());

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
