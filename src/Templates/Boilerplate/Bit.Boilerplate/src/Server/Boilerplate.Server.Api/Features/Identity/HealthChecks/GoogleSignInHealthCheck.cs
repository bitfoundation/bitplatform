using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Features.Identity.HealthChecks;

public partial class GoogleSignInHealthCheck : IHealthCheck
{
    [AutoInject] private IConfiguration configuration = default!;
    [AutoInject] private IHttpClientFactory httpClientFactory = default!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var section = configuration.GetRequiredSection("Authentication:Google");

            using var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = "health-check",
                ["redirect_uri"] = "https://localhost/",
                ["client_id"] = section["ClientId"]!,
                ["client_secret"] = section["ClientSecret"] ?? string.Empty
            });

            using var response = await httpClientFactory.CreateClient("Google").PostAsync(GoogleDefaults.TokenEndpoint, form, cancellationToken);
            var error = (await TokenEndpointAnswer.Read(response, cancellationToken))?["error"]?.ToString();

            return error is "invalid_grant"
                ? HealthCheckResult.Healthy("Google accepts the client credentials")
                : new HealthCheckResult(context.Registration.FailureStatus, $"Google's token endpoint answered {(int)response.StatusCode} ({error})");
        }
        catch (Exception exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Google is unreachable", exp);
        }
    }
}
