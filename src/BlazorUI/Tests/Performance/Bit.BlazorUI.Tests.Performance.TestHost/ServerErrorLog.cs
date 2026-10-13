using System.Collections.Concurrent;

namespace Bit.BlazorUI.Tests.Performance.TestHost;

/// <summary>
/// Keeps every error the host logs - an exception a component threw while it was being disposed, a circuit torn down
/// by an unhandled one - so a browser test can read what the page itself never shows (<c>/api/server-errors</c>).
/// </summary>
public sealed class ServerErrorLog : ILoggerProvider
{
    public static readonly ConcurrentQueue<string> Entries = new();

    public ILogger CreateLogger(string categoryName) => new Logger(categoryName);

    public void Dispose() { }

    private sealed class Logger(string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel) is false) return;

            Entries.Enqueue($"{category}: {formatter(state, exception)} {exception}");
        }
    }
}
