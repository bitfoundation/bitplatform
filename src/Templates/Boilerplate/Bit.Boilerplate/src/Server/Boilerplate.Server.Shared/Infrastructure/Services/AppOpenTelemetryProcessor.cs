using OpenTelemetry;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Boilerplate.Server.Shared.Infrastructure.Services;

public class AppOpenTelemetryProcessor(IHostEnvironment env) : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        // Exclude activities that should always be ignored (e.g. SignalR hub, components event handling, health checks)
        if (activity.DisplayName.Contains("Microsoft.AspNetCore.Components.Server.ComponentHub"))
        {
            activity.IsAllDataRequested = false; // Prevents Blazor Server's SignalR from being exported
        }
        else if (activity.OperationName is "Microsoft.AspNetCore.Components.HandleEvent")
        {
            activity.IsAllDataRequested = false; // Prevents Blazor's events from being exported.
        }
        else if (activity.TagObjects.Any(t => t.Value?.ToString()?.Contains("/ALIVE") is true))
        {
            activity.IsAllDataRequested = false; // Prevents health check calls from being exported (Fusion ASP.NET Core Output Cache)
        }
    }

    public override void OnEnd(Activity activity)
    {
        if (env.IsDevelopment()) return; // In dev env, we want to capture all telemetry for debugging/testing.

        // Processes activities at their completion boundary (tail-based sampling).
        // This method ensures that critical or unexpected application failures are 100% recorded,
        // while successfully completed activities and expected known/transient errors 
        // are down-sampled to a 5% execution rate to optimize telemetry storage/costs.

        // Check if the activity failed (status is Error)
        bool isFailed = activity.Status == ActivityStatusCode.Error;

        // Check if the failure is due to a known or transient exception
        bool isKnownOrTransient = activity.TagObjects.Any(t => (t.Key == "HasKnownException" || t.Key == "HasTransientException") && t.Value?.ToString() is "true");

        // Apply 5% sampling to all activities except unhandled critical errors
        if (isFailed is false || isKnownOrTransient)
        {
            if (ShouldSample(activity) is false)
            {
                activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            }
        }
    }

    /// <summary>
    /// Determines whether a specific trace activity should be sampled based on a 5% sampling rate.
    /// This method uses the TraceId bytes to ensure consistent sampling decisions across the entire trace tree,
    /// preventing broken or fragmented traces while maintaining high performance without string allocations.
    /// </summary>
    public static bool ShouldSample(Activity activity)
    {
        // 1. Get the TraceId directly as a struct to avoid memory allocation and string conversion overhead.
        var traceId = activity.TraceId;

        // 2. Allocate a 16-byte buffer on the stack to copy the 128-bit TraceId into bytes.
        Span<byte> bytes = stackalloc byte[16];
        traceId.CopyTo(bytes);

        // 3. Extract the lower 8 bytes (64 bits) of the TraceId and convert it into an unsigned 64-bit integer.
        // Since TraceId generation is uniformly distributed, any slice of it acts as an excellent, random hash key.
        ulong lowPart = BitConverter.ToUInt64(bytes.Slice(8, 8));

        // 4. Use the modulus operator to scale the hash down to a range of 0-99.
        // Returns true for values 0 to 4, which perfectly satisfies the 5% target sampling rate.
        return (lowPart % 100) < 5;
    }
}
