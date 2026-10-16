using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Bit.Belemetry.Tests;

/// <summary>
/// Keeps every log entry written through it. Belemetry never exports its own diagnostics category, so a test that
/// asserts on what Belemetry says about itself reads it from here.
/// </summary>
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    public ConcurrentQueue<(string Category, LogLevel Level, string Message)> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName) => new Logger(categoryName, Entries);

    public void Dispose() { }

    private sealed class Logger(string category, ConcurrentQueue<(string, LogLevel, string)> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel is not LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => entries.Enqueue((category, logLevel, formatter(state, exception)));
    }
}
