using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.Balette.Tests;

/// <summary>Collects what the library logs, so a test can assert that a problem was reported - or that it was not.</summary>
internal sealed class LogSink
{
    private readonly List<(LogLevel Level, string Message, Exception? Exception)> _entries = [];

    public IReadOnlyList<(LogLevel Level, string Message, Exception? Exception)> Entries
    {
        get
        {
            lock (_entries)
            {
                return [.. _entries];
            }
        }
    }

    public void Add(LogLevel level, string message, Exception? exception)
    {
        lock (_entries)
        {
            _entries.Add((level, message, exception));
        }
    }

    public IEnumerable<string> Messages(LogLevel level) => Entries.Where(e => e.Level == level).Select(e => e.Message);

    /// <summary>Registers the sink and routes every <see cref="ILogger{TCategoryName}"/> to it.</summary>
    public static LogSink AddTo(IServiceCollection services)
    {
        var sink = new LogSink();
        services.AddSingleton(sink);
        services.AddSingleton(typeof(ILogger<>), typeof(SinkLogger<>));
        return sink;
    }
}

internal sealed class SinkLogger<T>(LogSink sink) : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => sink.Add(logLevel, formatter(state, exception), exception);
}
