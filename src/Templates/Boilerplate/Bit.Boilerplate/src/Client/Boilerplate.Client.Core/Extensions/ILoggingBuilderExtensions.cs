//+:cnd:noEmit
using Boilerplate.Client.Core.Services.DiagnosticLog;

namespace Microsoft.Extensions.Logging;

public static class ILoggingBuilderExtensions
{
    public static ILoggingBuilder AddDiagnosticLogger(this ILoggingBuilder builder)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DiagnosticLoggerProvider>());

        return builder;
    }

    public static ILoggingBuilder ConfigureLoggers(this ILoggingBuilder loggingBuilder, IConfiguration configuration)
    {
        loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));

        if (AppEnvironment.IsDev())
        {
            loggingBuilder.AddDebug();
        }

        if (!AppPlatform.IsBrowser) // Browser has its own WebAssemblyConsoleLoggerProvider.
        {
            loggingBuilder.AddConsole(options => configuration.GetRequiredSection("Logging:Console").Bind(options)); // Device Log / logcat
        }

        loggingBuilder.AddDiagnosticLogger();

        //#if (sentry == true)
        loggingBuilder.AddSentry(options =>
        {
            options.Debug = AppEnvironment.IsDev();
            options.Environment = AppEnvironment.Current;
            configuration.GetRequiredSection("Logging:Sentry").Bind(options);
        });
        //#endif

        return loggingBuilder;
    }
}
