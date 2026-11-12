using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Boilerplate.LandingPage.Tests.Infrastructure;

internal sealed class RecordingErrorLog : ILoggerProvider, ILogger
{
    public ConcurrentQueue<string> Errors { get; } = new();

    public ConcurrentQueue<string> Warnings { get; } = new();

    public ILogger CreateLogger(string categoryName) => this;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel))
        {
            (logLevel >= LogLevel.Error ? Errors : Warnings).Enqueue(exception?.Message ?? formatter(state, exception));
        }
    }

    public void Dispose()
    {
    }
}
