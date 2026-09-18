using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Bit.Brouter.Tests.Hosting.Infrastructure;

public sealed record CapturedLogEntry(string Category, LogLevel Level, string Message, Exception? Exception)
{
    public override string ToString() => $"[{Level}] {Category}: {Message}{(Exception is null ? "" : " - " + Exception.GetType().Name + ": " + Exception.Message)}";
}

public sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<CapturedLogEntry> _entries = new();

    public IReadOnlyCollection<CapturedLogEntry> Entries => _entries;

    public ILogger CreateLogger(string categoryName) => new Logger(categoryName, _entries);

    public void Dispose()
    {
    }

    private sealed class Logger(string category, ConcurrentQueue<CapturedLogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            entries.Enqueue(new CapturedLogEntry(category, logLevel, formatter(state, exception), exception));
    }
}
