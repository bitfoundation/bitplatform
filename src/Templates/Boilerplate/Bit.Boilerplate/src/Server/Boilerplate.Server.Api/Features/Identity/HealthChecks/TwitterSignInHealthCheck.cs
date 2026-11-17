using System.Net.Http.Headers;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Features.Identity.HealthChecks;

public partial class TwitterSignInHealthCheck : IHealthCheck
{
    [AutoInject] private IConfiguration configuration = default!;
    [AutoInject] private IHttpClientFactory httpClientFactory = default!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var section = configuration.GetRequiredSection("Authentication:Twitter");
            var credentials = $"{Uri.EscapeDataString(section["ConsumerKey"]!)}:{Uri.EscapeDataString(section["ConsumerSecret"] ?? string.Empty)}";

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.x.com/oauth2/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "client_credentials" })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)));

            using var response = await httpClientFactory.CreateClient("Twitter").SendAsync(request, cancellationToken);
            var answer = await TokenEndpointAnswer.Read(response, cancellationToken);

            return response.IsSuccessStatusCode && answer?["token_type"]?.ToString() is "bearer"
                ? HealthCheckResult.Healthy("X accepts the consumer key and secret")
                : new HealthCheckResult(context.Registration.FailureStatus, $"X's token endpoint answered {(int)response.StatusCode} ({answer?["errors"]?[0]?["message"]})");
        }
        catch (Exception exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "X is unreachable", exp);
        }
    }
}
