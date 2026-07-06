//+:cnd:noEmit
using Boilerplate.Client.Core.Infrastructure.Services.DiagnosticLog;

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

        if (AppEnvironment.IsDevelopment())
        {
            loggingBuilder.AddDebug();
        }

        if (!AppPlatform.IsBrowser) // Browser has its own WebAssemblyConsoleLoggerProvider.
        {
            loggingBuilder.AddConsole(options => configuration.Bind("Logging:Console", options)); // Device Log / logcat
        }

        loggingBuilder.AddDiagnosticLogger();

        return loggingBuilder;
    }
}
