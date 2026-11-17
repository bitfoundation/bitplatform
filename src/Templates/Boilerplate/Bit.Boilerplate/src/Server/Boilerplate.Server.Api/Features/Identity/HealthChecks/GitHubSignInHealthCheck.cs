using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Features.Identity.HealthChecks;

public partial class GitHubSignInHealthCheck : IHealthCheck
{
    [AutoInject] private IConfiguration configuration = default!;
    [AutoInject] private IHttpClientFactory httpClientFactory = default!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var section = configuration.GetRequiredSection("Authentication:GitHub");
            var domain = string.IsNullOrWhiteSpace(section["EnterpriseDomain"]) ? "github.com" : section["EnterpriseDomain"]!.Trim().TrimEnd('/');

            using var request = new HttpRequestMessage(HttpMethod.Post, $"https://{domain}/login/oauth/access_token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["code"] = "health-check",
                    ["client_id"] = section["ClientId"]!,
                    ["client_secret"] = section["ClientSecret"] ?? string.Empty
                })
            };
            request.Headers.Accept.ParseAdd("application/json");

            using var response = await httpClientFactory.CreateClient("GitHub").SendAsync(request, cancellationToken);
            var error = (await TokenEndpointAnswer.Read(response, cancellationToken))?["error"]?.ToString();

            return error is "bad_verification_code"
                ? HealthCheckResult.Healthy("GitHub accepts the client credentials")
                : new HealthCheckResult(context.Registration.FailureStatus, $"GitHub's token endpoint answered {(int)response.StatusCode} ({error})");
        }
        catch (Exception exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "GitHub is unreachable", exp);
        }
    }
}
