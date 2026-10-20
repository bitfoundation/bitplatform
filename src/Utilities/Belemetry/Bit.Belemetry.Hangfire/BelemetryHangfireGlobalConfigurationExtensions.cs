using Bit.Belemetry.Hangfire;

namespace Hangfire;

/// <summary>Registration of the Belemetry Hangfire instrumentation.</summary>
public static class BelemetryHangfireGlobalConfigurationExtensions
{
    /// <summary>
    /// Records a span for every job execution, parented to the trace that enqueued it, on the
    /// <c>Bit.Belemetry.Hangfire</c> source, which Belemetry collects by default.
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddHangfire(configuration => configuration
    ///     .UseSqlServerStorage(connectionString)
    ///     .UseBitBelemetry());
    /// </code>
    /// </example>
    public static IGlobalConfiguration UseBitBelemetry(this IGlobalConfiguration configuration, Action<BelemetryHangfireOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new BelemetryHangfireOptions();
        configure?.Invoke(options);
        return configuration.UseFilter(new BelemetryHangfireFilter(options));
    }
}
