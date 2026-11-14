using Bit.Belemetry;
using Microsoft.Extensions.Hosting;

namespace Boilerplate.Server.Shared.Infrastructure.Services;

public class AppBelemetryProcessor(IHostEnvironment env) : IBelemetryProcessor
{
    public const double SamplingRatio = 0.05;

    public bool OnSpan(BelemetrySpan span)
    {
        // Prevents health check calls from being exported (Fusion ASP.NET Core Output Cache)
        if (span.Attributes.Any(attribute => attribute.Value is string value && (value.Contains("/ALIVE", StringComparison.Ordinal) || value.Contains("/HEALTH", StringComparison.Ordinal))))
            return false;

        if (env.IsDevelopment()) return true; // In dev env, we want to capture all telemetry for debugging/testing.

        // Processes spans at their completion boundary (tail-based sampling).
        // This method ensures that critical or unexpected application failures are 100% recorded,
        // while successfully completed spans and expected known/transient errors
        // are down-sampled to a 5% execution rate to optimize telemetry storage/costs.

        // Check if the span failed (status is Error)
        bool isFailed = span.Status == BelemetryStatusCode.Error;

        // Check if the failure is due to a known or transient exception
        bool isKnownOrTransientException = span.Attributes.GetString("HasKnownException") is "true" || span.Attributes.GetString("HasTransientException") is "true";

        var wasSlow = span.Duration > TimeSpan.FromSeconds(1);

        // Apply 5% sampling to all spans except unhandled critical errors and slow spans.
        if ((isFailed is false || isKnownOrTransientException) && wasSlow is false)
        {
            if (BelemetrySampler.ShouldSample(span.TraceId, SamplingRatio) is false)
                return false;

            span.SamplingRatio = Math.Min(span.SamplingRatio, SamplingRatio);
        }

        return true;
    }
}
