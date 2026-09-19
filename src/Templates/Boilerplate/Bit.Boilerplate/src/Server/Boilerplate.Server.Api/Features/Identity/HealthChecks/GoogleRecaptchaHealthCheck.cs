using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Features.Identity.HealthChecks;

/// <summary>
/// Sign up fails when siteverify can't be reached. See <see cref="GoogleRecaptchaService.EnsureReachable"/> for what it proves.
/// </summary>
public partial class GoogleRecaptchaHealthCheck : IHealthCheck
{
    [AutoInject] private GoogleRecaptchaService googleRecaptchaService = default!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await googleRecaptchaService.EnsureReachable(cancellationToken);

            return HealthCheckResult.Healthy("reCAPTCHA is reachable");
        }
        catch (Exception exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "reCAPTCHA is unhealthy", exp);
        }
    }
}
