using System.Collections.Concurrent;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Infrastructure.HealthChecks;

/// <summary>
/// Reuses a healthy result for <c>cacheDuration</c>, so probes don't each become a paid call. A failure is never
/// reused, so a recovery shows up on the next probe. Registered through <c>AddCachedCheck</c>.
/// </summary>
public class CachedHealthCheck(IHealthCheck healthCheck, CachedHealthCheck.ResultsStore store, TimeProvider timeProvider, TimeSpan cacheDuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var name = context.Registration.Name;

        if (store.TryGetValue(name, out var last) && timeProvider.GetUtcNow() - last.CheckedAt < cacheDuration)
            return last.Result;

        // Probes that arrive together share one run instead of each making the call.
        var run = store.Runs.GetOrAdd(name, _ => new(() => Run(context, cancellationToken)));
        try
        {
            return await run.Value.WaitAsync(cancellationToken);
        }
        finally
        {
            store.Runs.TryRemove(KeyValuePair.Create(name, run));
        }
    }

    private async Task<HealthCheckResult> Run(HealthCheckContext context, CancellationToken cancellationToken)
    {
        var name = context.Registration.Name;

        var startedAt = timeProvider.GetTimestamp();
        var result = await healthCheck.CheckHealthAsync(context, cancellationToken);
        var checkedAt = timeProvider.GetUtcNow();

        // Tells how old a reused result is and how long its run took, as the report's duration is the cache lookup's (See OperationsPage).
        result = new HealthCheckResult(result.Status, result.Description, result.Exception, new Dictionary<string, object>(result.Data)
        {
            ["CheckedAt"] = checkedAt,
            ["CacheDuration"] = cacheDuration,
            ["Duration"] = timeProvider.GetElapsedTime(startedAt)
        });

        if (result.Status is HealthStatus.Healthy)
        {
            store[name] = (result, checkedAt);
        }
        else
        {
            store.TryRemove(name, out _);
        }

        return result;
    }

    /// <summary>
    /// Keyed by registration name. A singleton, as the health check service creates a new check instance per run.
    /// </summary>
    public class ResultsStore : ConcurrentDictionary<string, (HealthCheckResult Result, DateTimeOffset CheckedAt)>
    {
        public ConcurrentDictionary<string, Lazy<Task<HealthCheckResult>>> Runs { get; } = new();
    }
}
