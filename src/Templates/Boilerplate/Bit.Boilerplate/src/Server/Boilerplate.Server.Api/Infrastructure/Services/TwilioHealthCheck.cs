//+:cnd:noEmit
using Twilio.Rest.Api.V2010;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Infrastructure.Services;

/// <summary>
/// Checks Twilio SMS service connectivity by fetching account info.
/// </summary>
public partial class TwilioHealthCheck : IHealthCheck
{
    [AutoInject] private ServerApiSettings settings = default!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (settings.Sms?.Configured is not true)
                return HealthCheckResult.Healthy("Twilio SMS is not configured — skipping check.");

            var account = await AccountResource.FetchAsync();

            return account.Status == AccountResource.StatusEnum.Active
                ? HealthCheckResult.Healthy("Twilio account is active.")
                : HealthCheckResult.Degraded($"Twilio account status: {account.Status}.");
        }
        catch (Exception exp)
        {
            return HealthCheckResult.Unhealthy("Twilio SMS health check failed.", exp);
        }
    }
}
