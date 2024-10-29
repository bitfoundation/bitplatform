//+:cnd:noEmit
using Microsoft.Extensions.Logging;

namespace Boilerplate.Client.Core.Services;

// https://learn.microsoft.com/en-us/aspnet/core/blazor/hybrid/developer-tools

/// <summary>
/// This logger writes to the browser console in blazor hybrid.
/// </summary>
[ProviderAlias("BrowserConsolelogger")]
public partial class BrowserConsoleLoggerProvider : ILoggerProvider, ILogger, IDisposable
{
    private static Bit.Butil.Console? console;

    public static void SetConsole(Bit.Butil.Console console)
    {
        if (AppPlatform.IsBlazorHybrid is false)
            throw new InvalidOperationException();
        BrowserConsoleLoggerProvider.console = console;
    }

    public string? CategoryName { get; init; }

    private static readonly ConcurrentQueue<object> states = new();

    public ILogger CreateLogger(string categoryName)
    {
        return new BrowserConsoleLoggerProvider
        {
            CategoryName = categoryName
        };
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        if (state is IDictionary<string, object?> dictionary)
        {
            dictionary["Category"] = CategoryName;
        }

        states.Enqueue(state);

        return this;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return AppPlatform.IsBlazorHybrid
            && console is not null;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel) is false) return;

        var message = formatter(state, exception);

        var currentState = states.TryDequeue(out var stateFromQueue) ? stateFromQueue : state;

        switch (logLevel)
        {
            case LogLevel.Trace:
            case LogLevel.Debug:
                console!.Log(message, $"{Environment.NewLine}State:", currentState);
                break;
            case LogLevel.Information:
                console!.Info(message, $"{Environment.NewLine}State:", currentState);
                break;
            case LogLevel.Warning:
                console!.Warn(message, $"{Environment.NewLine}State:", currentState);
                break;
            case LogLevel.Error:
            case LogLevel.Critical:
                console!.Error(message, $"{Environment.NewLine}State:", currentState);
                break;
            case LogLevel.None:
                break;
            default:
                console!.Log(message, $"{Environment.NewLine}State:", currentState);
                break;
        }
    }

    public void Dispose()
    {
        states.TryDequeue(out _);
    }
}
