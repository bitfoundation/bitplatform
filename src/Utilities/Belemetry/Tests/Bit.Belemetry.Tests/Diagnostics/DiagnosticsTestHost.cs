using Microsoft.Extensions.Logging;

namespace Bit.Belemetry.Tests.Diagnostics;

/// <summary>A <see cref="TestHost"/> with diagnostics on, and a scope to resolve <see cref="IBelemetryDiagnostics"/> from.</summary>
internal sealed class DiagnosticsTestHost : IAsyncDisposable
{
    private readonly AsyncServiceScope _scope;

    private DiagnosticsTestHost(TestHost host)
    {
        Host = host;
        _scope = host.Services.CreateAsyncScope();
        Factory = host.StartLogging();
    }

    public TestHost Host { get; }

    public ILoggerFactory Factory { get; }

    public IBelemetryDiagnostics Diagnostics => _scope.ServiceProvider.GetRequiredService<IBelemetryDiagnostics>();

    public BelemetryJsBridge Bridge => _scope.ServiceProvider.GetRequiredService<BelemetryJsBridge>();

    public BelemetryDiagnosticHub Hub => Host.Pipeline.Diagnostics;

    public static DiagnosticsTestHost Create(Action<BelemetryOptions>? configure = null, BelemetryFeatures consentDefault = BelemetryFeatures.All, Action<IServiceCollection>? configureServices = null) =>
        new(TestHost.Create(o =>
        {
            o.Diagnostics.Enabled = true;
            o.Diagnostics.PostTriggerWindow = TimeSpan.Zero;
            // The tests log the same error more than once on purpose; the limits have tests of their own.
            o.Errors.DuplicateWindow = TimeSpan.Zero;
            o.Errors.MaxPerMinute = 0;
            configure?.Invoke(o);
        }, consentDefault, configureServices));

    public static Exception Thrown(Func<Exception> create)
    {
        try
        {
            throw create();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _scope.DisposeAsync();
        await Host.DisposeAsync();
    }
}
