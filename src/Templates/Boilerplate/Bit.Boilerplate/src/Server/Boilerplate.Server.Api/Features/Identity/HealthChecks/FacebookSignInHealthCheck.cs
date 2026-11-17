using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Features.Identity.HealthChecks;

public partial class FacebookSignInHealthCheck : IHealthCheck
{
    [AutoInject] private IConfiguration configuration = default!;
    [AutoInject] private IHttpClientFactory httpClientFactory = default!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var section = configuration.GetRequiredSection("Authentication:Facebook");

            using var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = section["AppId"]!,
                ["client_secret"] = section["AppSecret"] ?? string.Empty
            });

            using var response = await httpClientFactory.CreateClient("Facebook").PostAsync(FacebookDefaults.TokenEndpoint, form, cancellationToken);
            var answer = await TokenEndpointAnswer.Read(response, cancellationToken);

            return response.IsSuccessStatusCode && answer?["access_token"] is not null
                ? HealthCheckResult.Healthy("Facebook accepts the app id and secret")
                : new HealthCheckResult(context.Registration.FailureStatus, $"Facebook's token endpoint answered {(int)response.StatusCode} ({answer?["error"]?["message"]})");
        }
        catch (Exception exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Facebook is unreachable", exp);
        }
    }
}
