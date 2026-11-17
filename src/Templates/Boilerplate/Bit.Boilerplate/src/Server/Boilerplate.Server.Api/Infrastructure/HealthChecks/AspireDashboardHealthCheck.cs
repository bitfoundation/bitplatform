using Microsoft.Extensions.Diagnostics.HealthChecks;
using Boilerplate.Server.Api.Infrastructure.DevMcp;

namespace Boilerplate.Server.Api.Infrastructure.HealthChecks;

public partial class AspireDashboardHealthCheck : IHealthCheck
{
    [AutoInject] private DevMcpTelemetryReader telemetryReader = default!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (await telemetryReader.Get("api/telemetry/resources", cancellationToken) is not { } resources)
                return new HealthCheckResult(context.Registration.FailureStatus, "The Aspire dashboard at AspireDashboard:Url has no telemetry API");

            return HealthCheckResult.Healthy($"The Aspire dashboard accepts the key and keeps telemetry of {resources.AsArray().Count} resources");
        }
        catch (InvalidOperationException exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exp.Message);
        }
        catch (Exception exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "The Aspire dashboard is unreachable", exp);
        }
    }
}
