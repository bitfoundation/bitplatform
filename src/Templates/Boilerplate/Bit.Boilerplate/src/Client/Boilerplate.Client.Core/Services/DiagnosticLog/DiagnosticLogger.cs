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
            data[nameof(CategoryName)] = CategoryName;
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

        if (scope is null)
            return;

        // Store logs in the memory to be shown later.

        var jsRuntime = scope.GetRequiredService<IJSRuntime>();

        if (jsRuntime.IsInitialized() is false)
            return;

        var console = scope.GetRequiredService<Bit.Butil.Console>();

        switch (logLevel)
        {
            case LogLevel.Trace:
            case LogLevel.Debug:
                console!.Log(message, $"{Environment.NewLine}Category:", CategoryName, $"{Environment.NewLine}State:", currentState);
                break;
            case LogLevel.Information:
                console!.Info(message, $"{Environment.NewLine}Category:", CategoryName, $"{Environment.NewLine}State:", currentState);
                break;
            case LogLevel.Warning:
                console!.Warn(message, $"{Environment.NewLine}Category:", CategoryName, $"{Environment.NewLine}State:", currentState);
                break;
            case LogLevel.Error:
            case LogLevel.Critical:
                console!.Error(message, $"{Environment.NewLine}Category:", CategoryName, $"{Environment.NewLine}State:", currentState);
                break;
            case LogLevel.None:
                break;
            default:
                console!.Log(message, $"{Environment.NewLine}Category:", CategoryName, $"{Environment.NewLine}State:", currentState);
                break;
        }
    }

    public void Dispose()
    {

    }
}
