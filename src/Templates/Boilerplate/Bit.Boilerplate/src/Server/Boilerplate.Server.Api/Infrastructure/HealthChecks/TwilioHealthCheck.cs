//+:cnd:noEmit
using Twilio.Rest.Api.V2010;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Infrastructure.HealthChecks;

/// <summary>
/// Checks Twilio SMS service connectivity by fetching account info.
/// </summary>
public class TwilioHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var account = await AccountResource.FetchAsync().WaitAsync(cancellationToken);

            return account.Status == AccountResource.StatusEnum.Active
                ? HealthCheckResult.Healthy("Twilio account is active.")
                : HealthCheckResult.Degraded($"Twilio account status: {account.Status}.");
        }
        catch (Exception exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Twilio SMS health check failed.", exp);
        }
    }
}
