using Boilerplate.Client.Core.Services.DiagnosticLog;

namespace Microsoft.Extensions.Logging;

public static class ILoggingBuilderExtensions
{
    public static ILoggingBuilder AddDiagnosticLogger(this ILoggingBuilder builder)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DiagnosticLoggerProvider>());

        return builder;
    }

    public static ILoggingBuilder ConfigureLoggers(this ILoggingBuilder loggingBuilder)
    {
        if (AppEnvironment.IsDev())
        {
            loggingBuilder.AddDebug();
        }

        if (!AppPlatform.IsBrowser) // Browser has its own BrowserConsoleLogger.
        {
            loggingBuilder.AddConsole(); // Device Log / logcat
        }

        loggingBuilder.AddDiagnosticLogger();

        return loggingBuilder;
    }
}
