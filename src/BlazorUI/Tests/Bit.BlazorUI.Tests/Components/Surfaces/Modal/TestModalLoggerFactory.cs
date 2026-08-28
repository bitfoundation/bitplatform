using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Modal;

/// <summary>
/// A logger factory that keeps the errors it is handed, so a test can tell a service that reported a mistake
/// apart from one that swallowed it.
/// </summary>
public class TestModalLoggerFactory : ILoggerFactory
{
    public List<string> Errors { get; } = [];

    public ILogger CreateLogger(string categoryName) => new TestLogger(this);

    public void AddProvider(ILoggerProvider provider) { }

    public void Dispose() { }

    private sealed class TestLogger(TestModalLoggerFactory factory) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel < LogLevel.Error) return;

            factory.Errors.Add(formatter(state, exception));
        }
    }
}
