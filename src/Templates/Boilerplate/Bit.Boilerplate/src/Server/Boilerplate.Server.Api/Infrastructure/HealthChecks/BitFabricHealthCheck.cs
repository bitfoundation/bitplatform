using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Infrastructure.HealthChecks;

/// <summary>
/// Checks nobody reads from outside go down with the server, so this says whether bit Fabric, which reads them, is connected.
/// </summary>
public partial class BitFabricHealthCheck : IHealthCheck
{
    [AutoInject] private AppDbContext dbContext = default!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // bit Fabric's client ids are metadata documents on these hosts (See OAuthClient.OriginOf).
        var connected = await dbContext.OAuthGrants.AnyAsync(grant => grant.ClientId.StartsWith("https://api.bitfabric.co/")
            || grant.ClientId.StartsWith("https://api-test.bitfabric.co/"), cancellationToken);

        return connected
            ? HealthCheckResult.Healthy("bit Fabric is connected.")
            : new HealthCheckResult(context.Registration.FailureStatus,
                "bit Fabric isn't connected to this app, so nothing outside the server reads these checks. Connect it at https://bitfabric.co.");
    }
}
