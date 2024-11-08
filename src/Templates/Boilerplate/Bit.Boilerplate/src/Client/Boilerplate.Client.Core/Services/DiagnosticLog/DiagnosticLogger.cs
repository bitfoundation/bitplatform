using Microsoft.Extensions.Logging;

namespace Boilerplate.Client.Core.Services.DiagnosticLog;

public partial class DiagnosticLogger(CurrentScopeProvider scopeProvider) : ILogger, IDisposable
{
    private ConcurrentQueue<IDictionary<string, object?>> states = new();

    public string? CategoryName { get; set; }

    public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
    {
        if (state is IDictionary<string, object?> data)
        {
            states.Enqueue(data);
        }

        return this;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel) is false) return;

        var message = formatter(state, exception);

        states.TryDequeue(out var currentState);

        var scope = scopeProvider.Invoke();

        if (scope is null) return;

        var store = scope.GetRequiredService<ConcurrentBag<DiagnosticLog>>();
        store.Add(new() { CreatedOn = DateTimeOffset.Now, Level = logLevel, Message = message, Exception = exception, State = currentState?.ToDictionary(i => i.Key, i => i.Value?.ToString()) });
    }

    public void Dispose()
    {

    }
}
