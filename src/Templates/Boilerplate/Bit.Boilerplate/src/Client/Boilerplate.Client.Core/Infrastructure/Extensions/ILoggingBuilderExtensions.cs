//+:cnd:noEmit
using Boilerplate.Client.Core.Infrastructure.Services.DiagnosticLog;

namespace Microsoft.Extensions.Logging;

public static class ILoggingBuilderExtensions
{
    extension(ILoggingBuilder loggingBuilder)
    {
        public ILoggingBuilder AddDiagnosticLogger()
        {
            loggingBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DiagnosticLoggerProvider>());

            return loggingBuilder;
        }

        public ILoggingBuilder ConfigureLoggers(IConfiguration configuration)
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
}
