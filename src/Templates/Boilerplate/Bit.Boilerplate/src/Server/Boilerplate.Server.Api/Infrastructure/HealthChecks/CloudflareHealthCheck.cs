using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Infrastructure.HealthChecks;

/// <summary>
/// Purges a cache tag nothing carries, the way content changes purge the edge, so it proves the token may purge.
/// </summary>
public partial class CloudflareHealthCheck : IHealthCheck
{
    [AutoInject] private ResponseCacheService responseCacheService = default!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await responseCacheService.PurgeCloudflareTags([$"health-check-{Guid.NewGuid():N}"], cancellationToken);

            return HealthCheckResult.Healthy("Cloudflare purges the cache");
        }
        catch (Exception exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Cloudflare cache purge failed", exp);
        }
    }
}
