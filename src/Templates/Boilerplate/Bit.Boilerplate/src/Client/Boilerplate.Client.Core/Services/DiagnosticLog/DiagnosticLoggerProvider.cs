//+:cnd:noEmit
using Microsoft.Extensions.Logging;

namespace Boilerplate.Client.Core.Services.DiagnosticLog;

// https://learn.microsoft.com/en-us/aspnet/core/blazor/hybrid/developer-tools

/// <summary>
/// Provides a custom logger that outputs log messages to the browser's console and allows for selective display of logs
/// within the application UI for enhanced diagnostics.
/// </summary>
[ProviderAlias("DiagnosticLogger")]
public partial class DiagnosticLoggerProvider : ILoggerProvider
{
    [AutoInject] private CurrentScopeProvider scopeProvider = default!;

    public ILogger CreateLogger(string categoryName)
    {
        var logger = scopeProvider()?.GetRequiredService<DiagnosticLogger>();
        if (logger is null) return new NoopLogger();

        logger.CategoryName = categoryName;
        return logger;
    }

    public void Dispose() { }
}

public class NoopLogger : ILogger, IDisposable
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return this;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return false;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {

    }

    public void Dispose()
    {

    }
}
