using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection;

public static class IHealthChecksBuilderExtensions
{
    extension(IHealthChecksBuilder builder)
    {
        /// <summary>
        /// For a check that calls a third party: bounded by <paramref name="timeout"/>, Degraded on failure (See
        /// AddServerApiHealthChecks) and a healthy result is reused for <paramref name="cacheDuration"/> (See <see cref="CachedHealthCheck"/>).
        /// </summary>
        public IHealthChecksBuilder AddCachedCheck(string name, Func<IServiceProvider, IHealthCheck> factory, TimeSpan cacheDuration, TimeSpan timeout)
        {
            builder.Services.TryAddSingleton<CachedHealthCheck.ResultsStore>();

            return builder.Add(new HealthCheckRegistration(name,
                sp => new CachedHealthCheck(factory(sp), sp.GetRequiredService<CachedHealthCheck.ResultsStore>(), sp.GetRequiredService<TimeProvider>(), cacheDuration),
                failureStatus: HealthStatus.Degraded,
                tags: [],
                timeout: timeout));
        }

        /// <summary>
        /// Stands in for a check whose dependency has no configuration, and reports Degraded (See AddServerApiHealthChecks).
        /// </summary>
        public IHealthChecksBuilder AddNotConfiguredCheck(string name, string settings)
        {
            return builder.Add(new HealthCheckRegistration(name,
                _ => new NotConfiguredHealthCheck(settings),
                failureStatus: HealthStatus.Degraded,
                tags: []));
        }
    }

    private sealed class NotConfiguredHealthCheck(string settings) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new HealthCheckResult(context.Registration.FailureStatus,
                $"{settings} is not configured. Configure it, or remove the code that uses it."));
        }
    }
}
