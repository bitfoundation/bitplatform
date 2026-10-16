using Microsoft.Extensions.Logging;

namespace Bit.BevTools;

/// <summary>
/// Captures warnings and errors logged by Blazor itself (unhandled circuit exceptions, event handler failures,
/// ErrorBoundary catches, JS interop failures, SignalR problems) into the error center of the session that produced them.
/// </summary>
internal sealed class BevToolsLoggerProvider(BevToolsRegistry registry) : ILoggerProvider
{
    private static readonly string[] Prefixes =
    [
        "Microsoft.AspNetCore.Components",
        "Microsoft.JSInterop",
        "Microsoft.AspNetCore.SignalR",
        "Microsoft.AspNetCore.Http.Connections",
    ];

    public ILogger CreateLogger(string categoryName)
    {
        if (!registry.IsEnabled)
        {
            return Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        }

        foreach (var prefix in Prefixes)
        {
            if (categoryName.StartsWith(prefix, StringComparison.Ordinal))
            {
                return new CaptureLogger(categoryName);
            }
        }

        return Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    public void Dispose()
    {
    }

    private sealed class CaptureLogger(string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel < LogLevel.Warning)
            {
                return;
            }

            var session = SessionResolver.Resolve();
            if (session is null || !session.IsEnabled)
            {
                return;
            }

            var message = formatter(state, exception);
            var source = category switch
            {
                _ when category.Contains("Circuit", StringComparison.Ordinal) => "circuit",
                _ when category.Contains("ErrorBoundary", StringComparison.Ordinal) => "error-boundary",
                _ when category.StartsWith("Microsoft.JSInterop", StringComparison.Ordinal) => "js-interop",
                _ when category.StartsWith("Microsoft.AspNetCore.SignalR", StringComparison.Ordinal) || category.Contains("Connections", StringComparison.Ordinal) => "signalr",
                _ => "blazor",
            };

            if (exception is null && logLevel == LogLevel.Warning)
            {
                session.Timeline.Record(new BevToolsEvent
                {
                    Kind = BevToolsEventKind.Error,
                    Category = "log:" + source,
                    Title = message,
                    Detail = category,
                    Severity = BevToolsSeverity.Warning,
                });
                return;
            }

            session.Errors.Record(exception, source, message, exceptionType: exception?.GetType().FullName ?? "Log" + logLevel);
        }
    }
}
