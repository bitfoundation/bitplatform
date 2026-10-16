using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Bit.BlazorUI.Tests.E2E.TestHost;

/// <summary>
/// Keeps every error and warning the host logs - an exception a component threw while it was being disposed, a circuit
/// torn down by an unhandled one, an exception a BitErrorBoundary caught and logged - so a browser test can read what the
/// page itself never shows (<c>/api/server-errors</c>, errors only unless <c>level=Warning</c> asks for the warnings too).
/// Each entry is numbered, so a test reads only the ones logged since it began, and each remembers the circuit it was
/// logged for, so a test can ask for the ones of the pages it visited alone: the tests run beside each other against
/// this one host, and a neighbour's error is none of theirs.
/// </summary>
public sealed partial class ServerErrorLog : ILoggerProvider
{
    private static readonly List<(string Text, string? CircuitId, LogLevel Level)> _entries = [];
    private static readonly object _lock = new();

    /// <summary>
    /// The entries of <paramref name="minLevel"/> or above logged after the first <paramref name="after"/>. With
    /// <paramref name="paths"/>, only the ones of a circuit that was on one of those paths, and the ones no circuit can be
    /// told for (which may be anybody's).
    /// </summary>
    public static ServerErrorsResponse Since(int after, IReadOnlyCollection<string>? paths = null, LogLevel minLevel = LogLevel.Error)
    {
        lock (_lock)
        {
            var skip = Math.Clamp(after, 0, _entries.Count);
            var entries = _entries.Skip(skip)
                                  .Where(e => e.Level >= minLevel)
                                  .Where(e => paths is null || paths.Count == 0 || e.CircuitId is null || CircuitPaths.WasOn(e.CircuitId, paths))
                                  .Select(e => e.Text)
                                  .ToArray();

            return new ServerErrorsResponse(_entries.Count, entries);
        }
    }

    public static void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
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

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel) is false) return;

            var message = formatter(state, exception);
            var match = CircuitIdPattern().Match(message);

            lock (_lock)
            {
                _entries.Add(($"{category}: {message} {exception}", match.Success ? match.Groups[1].Value : null, logLevel));
            }
        }
    }
}

/// <summary>
/// Remembers the paths every circuit has been on, for <see cref="ServerErrorLog"/> to tell whose an error is.
/// </summary>
public sealed class CircuitPaths(NavigationManager navigationManager) : CircuitHandler
{
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _paths = new();

    public static bool WasOn(string circuitId, IReadOnlyCollection<string> paths)
    {
        return _paths.TryGetValue(circuitId, out var visited) && paths.Any(visited.ContainsKey);
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        var visited = _paths.GetOrAdd(circuit.Id, _ => new(StringComparer.OrdinalIgnoreCase));

        Add(visited, navigationManager.Uri);
        navigationManager.LocationChanged += (_, e) => Add(visited, e.Location);

        return Task.CompletedTask;
    }

    private static void Add(ConcurrentDictionary<string, byte> visited, string uri)
    {
        if (Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
        {
            visited.TryAdd(parsed.AbsolutePath.TrimEnd('/'), 0);
        }
    }
}

/// <summary>
/// The answer of <c>/api/server-errors</c>: how many errors the host has logged in all (the next <c>after</c> to
/// ask with) and the ones after the number asked for.
/// </summary>
public sealed record ServerErrorsResponse(int Count, string[] Entries);
