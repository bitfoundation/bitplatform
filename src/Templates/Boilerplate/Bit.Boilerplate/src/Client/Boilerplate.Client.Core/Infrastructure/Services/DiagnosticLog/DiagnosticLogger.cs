using Boilerplate.Shared.Features.Diagnostic;

namespace Boilerplate.Client.Core.Infrastructure.Services.DiagnosticLog;

public partial class DiagnosticLogger(TimeProvider timeProvider) : ILogger
{
    public static ConcurrentQueue<DiagnosticLogDto> Store { get; } = [];

    /// <remarks>
    /// An instance field is enough because <c>DiagnosticLoggerProvider</c> creates one logger per category and the
    /// clients are single user per process. If <c>AddDiagnosticLogger()</c> is ever registered outside
    /// <c>IsDevelopment()</c> on Server.Web, this must become an AsyncLocal: under Blazor Server one instance is
    /// shared by every circuit, so the scope of one user would stamp another user's entries.
    /// </remarks>
    private IDictionary<string, object?>? currentState;

    public string? Category { get; set; }

    public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
    {
        if (state is not IDictionary<string, object?> data)
            return null;

        var previousState = currentState;
        currentState = data;

        return new ScopeRestorer(this, previousState);
    }

    private sealed class ScopeRestorer(DiagnosticLogger logger, IDictionary<string, object?>? previousState) : IDisposable
    {
        public void Dispose() => logger.currentState = previousState;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel) is false) return;

        var message = formatter(state, exception);

        if (Store.Count >= 1_000)
        {
            Store.TryDequeue(out var _);
        }

        Store.Enqueue(new()
        {
            CreatedOn = timeProvider.GetUtcNow(),
            Level = logLevel,
            Message = message,
            Category = Category,
            ExceptionString = exception?.ToString(),
            State = currentState?.ToDictionary(i => i.Key, i => i.Value?.ToString())
        });
    }
}
