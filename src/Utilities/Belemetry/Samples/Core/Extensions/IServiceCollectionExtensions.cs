using Bit.Belemetry;
using Bit.Belemetry.Samples.Core;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    /// <summary>
    /// The services of the shared pages. Belemetry itself is registered by each host's Program.cs, so that the setup
    /// of every hosting model stays visible in one place.
    /// </summary>
    public static IServiceCollection AddCoreServices(this IServiceCollection services, Action<SampleSettings>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var settings = new SampleSettings();
        configure?.Invoke(settings);
        services.AddSingleton(settings);

        // The support page's application state, and the snapshot provider that describes it in a support bundle.
        services.AddScoped<SampleCart>();
        services.AddBitBelemetrySnapshotProvider<SampleCartSnapshot>();

        // Production diagnostics are a feature of the shared pages (the support page), so they are switched on here for
        // every host; AddBitBelemetryServices adds this callback to the host's own. The timeline lives in memory and
        // follows the user's consent; nothing leaves the app until a report is previewed and confirmed.
        services.AddBitBelemetryServices(o =>
        {
            o.Diagnostics.Enabled = true;
            // Unhandled exceptions, circuit failures and critical errors make a report for review on their own.
            o.Diagnostics.AutoCaptureTriggers = BelemetryDiagnosticTriggers.Default | BelemetryDiagnosticTriggers.ErrorBoundary;
            o.Diagnostics.Release.Metadata["sample"] = "true";
        });

        return services;
    }
}
