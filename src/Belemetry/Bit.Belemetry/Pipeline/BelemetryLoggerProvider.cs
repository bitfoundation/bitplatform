using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Bit.Belemetry;

/// <summary>
/// The <see cref="ILoggerProvider"/> that turns <c>ILogger</c> entries into <see cref="BelemetryLogRecord"/>s.
/// Per-category levels can be configured under the <c>Belemetry</c> alias of the logging configuration.
/// </summary>
[ProviderAlias("Belemetry")]
internal sealed class BelemetryLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly ConcurrentDictionary<string, BelemetryLogger> _loggers = new(StringComparer.Ordinal);
    private readonly BelemetryPipeline _pipeline;

    public BelemetryLoggerProvider(BelemetryPipeline pipeline)
    {
        _pipeline = pipeline;

        // Resolving the logger provider is the earliest point every host reaches (the logger factory is built while
        // the host starts), so it doubles as the trigger that starts the collectors - WebAssembly has no hosted
        // services to do it.
        _pipeline.Start();
    }

    internal IExternalScopeProvider? ScopeProvider { get; private set; }

    public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, name => new BelemetryLogger(name, this, _pipeline));

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => ScopeProvider = scopeProvider;

    public void Dispose() => _loggers.Clear();
}
