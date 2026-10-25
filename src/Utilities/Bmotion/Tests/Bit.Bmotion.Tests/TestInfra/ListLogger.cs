using Microsoft.Extensions.Logging;

namespace Bit.Bmotion.Tests.TestInfra;

/// <summary>
/// An <see cref="ILogger{TCategoryName}"/> that keeps every formatted entry, so a test can assert
/// on what the library chose to log (warnings it raises instead of throwing, for instance).
/// </summary>
internal sealed class ListLogger<T> : ILogger<T>
{
    public sealed record Entry(LogLevel Level, string Message);

    private readonly List<Entry> _entries = new();

    public IReadOnlyList<Entry> Entries
    {
        get { lock (_entries) return _entries.ToArray(); }
    }

    public IEnumerable<string> Warnings => Entries.Where(e => e.Level == LogLevel.Warning).Select(e => e.Message);

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock (_entries) _entries.Add(new Entry(logLevel, formatter(state, exception)));
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
