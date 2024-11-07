namespace Microsoft.Extensions.Logging;

public static class ILoggingBuilderExtensions
{
    public static ILoggingBuilder AddDevInsightsLogger(this ILoggingBuilder builder)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DevInsightsLoggerProvider>());

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
            // DevInsightsLogger is already logging in browser's console.
            // But Console logger is still useful in Visual Studio's Device Log (Android, iOS) or BrowserStack etc.
        }

        loggingBuilder.AddDevInsightsLogger();

        return loggingBuilder;
    }
}
