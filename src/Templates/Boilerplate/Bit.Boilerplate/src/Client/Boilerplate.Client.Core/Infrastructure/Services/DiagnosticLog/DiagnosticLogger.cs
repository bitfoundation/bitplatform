using Boilerplate.Shared.Features.Diagnostic;

namespace Boilerplate.Client.Core.Infrastructure.Services.DiagnosticLog;

public partial class DiagnosticLogger(TimeProvider timeProvider) : ILogger
{
    private static readonly ConcurrentQueue<DiagnosticLogDto> store = [];

    public static IReadOnlyCollection<DiagnosticLogDto> Store => store;

    /// <summary>
    /// Discards this device's buffer. Called when the signed-in user changes, and by the diagnostic modal.
    /// </summary>
    public static void ClearStore() => store.Clear();

    private readonly AsyncLocal<IDictionary<string, object?>?> currentState = new();

    public string? Category { get; set; }

    public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
    {
        if (state is not IDictionary<string, object?> data)
            return null;

        var previousState = currentState.Value;
        currentState.Value = data;

        return new ScopeRestorer(this, previousState);
    }

    private sealed class ScopeRestorer(DiagnosticLogger logger, IDictionary<string, object?>? previousState) : IDisposable
    {
        public void Dispose() => logger.currentState.Value = previousState;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel) is false) return;

        var message = formatter(state, exception);

        if (store.Count >= 1_000)
        {
            store.TryDequeue(out var _);
        }

        store.Enqueue(new()
        {
            CreatedOn = timeProvider.GetUtcNow(),
            Level = logLevel,
            Message = message,
            Category = Category,
            ExceptionString = exception?.ToString(),
            State = currentState.Value?.ToDictionary(i => i.Key, i => i.Value?.ToString())
        });
    }
}
