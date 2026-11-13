using Microsoft.Extensions.Diagnostics.HealthChecks;
using Boilerplate.Shared.Features.Diagnostic;

namespace Microsoft.Extensions.DependencyInjection;

public static class IHealthChecksBuilderExtensions
{
    extension(IHealthChecksBuilder builder)
    {
        /// <summary>
        /// For a check that calls a third party: bounded by <paramref name="timeout"/>, Degraded on failure (See
        /// AddServerApiHealthChecks) and a healthy result is reused for <paramref name="cacheDuration"/> (See <see cref="CachedHealthCheck"/>).
        /// <paramref name="category"/> is one of <see cref="HealthCheckCategories"/>.
        /// </summary>
        public IHealthChecksBuilder AddCachedCheck(string name, string category, Func<IServiceProvider, IHealthCheck> factory, TimeSpan cacheDuration, TimeSpan timeout)
        {
            builder.Services.TryAddSingleton<CachedHealthCheck.ResultsStore>();

            return builder.Add(new HealthCheckRegistration(name,
                sp => new CachedHealthCheck(factory(sp), sp.GetRequiredService<CachedHealthCheck.ResultsStore>(), sp.GetRequiredService<TimeProvider>(), cacheDuration),
                failureStatus: HealthStatus.Degraded,
                tags: [category],
                timeout: timeout));
        }

        /// <summary>
        /// Stands in for a check whose dependency has no configuration, and reports Degraded (See AddServerApiHealthChecks).
        /// It takes the real check's <paramref name="category"/>, so it is grouped where that check would be, and the
        /// <see cref="HealthCheckTags.NotConfigured"/> tag, so /healthz says it is only missing its settings.
        /// </summary>
        public IHealthChecksBuilder AddNotConfiguredCheck(string name, string category, string settings)
        {
            return builder.Add(new HealthCheckRegistration(name,
                _ => new NotConfiguredHealthCheck(settings),
                failureStatus: HealthStatus.Degraded,
                tags: [category, HealthCheckTags.NotConfigured]));
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
