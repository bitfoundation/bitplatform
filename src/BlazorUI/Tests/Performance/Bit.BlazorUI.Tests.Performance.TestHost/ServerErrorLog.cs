using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Bit.BlazorUI.Tests.Performance.TestHost;

/// <summary>
/// Keeps every error the host logs - an exception a component threw while it was being disposed, a circuit torn down
/// by an unhandled one - so a browser test can read what the page itself never shows (<c>/api/server-errors</c>).
/// A test clears the log before it opens its page, so an error of a circuit that was already open by then belongs to a
/// previous test, whose circuit is still being torn down: it is left out of what the next read returns, rather than
/// blamed on the test that cleared the log.
/// </summary>
public sealed partial class ServerErrorLog : ILoggerProvider
{
    private static readonly List<(string Text, string? CircuitId)> _entries = [];
    private static readonly object _lock = new();
    private static long _clearedAt;

    /// <summary>The errors logged since the last <see cref="Clear"/>, less the ones of the circuits opened before it.</summary>
    public static string[] Current()
    {
        lock (_lock)
        {
            return _entries.Where(e => e.CircuitId is null || CircuitOrder.OpenedAfter(e.CircuitId, _clearedAt))
                           .Select(e => e.Text)
                           .ToArray();
        }
    }

    public static void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
            _clearedAt = CircuitOrder.Last;
        }
    }

    public ILogger CreateLogger(string categoryName) => new Logger(categoryName);

    public void Dispose() { }

    // The circuit host names the circuit in every error it logs: "Unhandled exception in circuit '<id>'".
    [GeneratedRegex("circuit '([^']+)'")]
    private static partial Regex CircuitIdPattern();

    private sealed class Logger(string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel) is false) return;

            var message = formatter(state, exception);
            var match = CircuitIdPattern().Match(message);

            lock (_lock)
            {
                _entries.Add(($"{category}: {message} {exception}", match.Success ? match.Groups[1].Value : null));
            }
        }
    }
}

/// <summary>
/// Numbers the circuits in the order they open, for <see cref="ServerErrorLog"/> to tell a previous test's from the
/// current one's.
/// </summary>
public sealed class CircuitOrder : CircuitHandler
{
    private static readonly ConcurrentDictionary<string, long> _order = new();
    private static long _last;

    public static long Last => Interlocked.Read(ref _last);

    /// <summary>Whether the circuit opened after <paramref name="mark"/> - or is one this handler never saw open.</summary>
    public static bool OpenedAfter(string circuitId, long mark) => _order.TryGetValue(circuitId, out var number) is false || number > mark;

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _order[circuit.Id] = Interlocked.Increment(ref _last);
        return Task.CompletedTask;
    }
}
