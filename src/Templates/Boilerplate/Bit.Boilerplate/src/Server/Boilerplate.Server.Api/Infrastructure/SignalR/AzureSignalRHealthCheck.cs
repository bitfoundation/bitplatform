using System.Net;
using System.Data.Common;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Infrastructure.SignalR;

/// <summary>
/// Asks the service's health api, then, with an AccessKey, looks up a user that can't exist: 404 means the key was accepted.
/// https://learn.microsoft.com/azure/azure-signalr/signalr-reference-data-plane-rest-api
/// </summary>
public partial class AzureSignalRHealthCheck : IHealthCheck
{
    private const string ApiVersion = "2024-12-01";

    [AutoInject] private IConfiguration configuration = default!;
    [AutoInject] private IHttpClientFactory httpClientFactory = default!;
    [AutoInject] private TimeProvider timeProvider = default!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = new DbConnectionStringBuilder { ConnectionString = configuration["Azure:SignalR:ConnectionString"] };
            var endpoint = new UriBuilder(Convert.ToString(connectionString["Endpoint"])!);
            if (connectionString.TryGetValue("Port", out var port))
            {
                endpoint.Port = Convert.ToInt32(port);
            }
            var baseUrl = endpoint.Uri.GetLeftPart(UriPartial.Authority);

            using var httpClient = httpClientFactory.CreateClient("AzureSignalR");

            using var healthRequest = new HttpRequestMessage(HttpMethod.Head, $"{baseUrl}/api/health?api-version={ApiVersion}");
            using var healthResponse = await httpClient.SendAsync(healthRequest, cancellationToken);
            if (healthResponse.IsSuccessStatusCode is false)
                throw new InvalidOperationException($"The service health api answered {(int)healthResponse.StatusCode}.");

            // Other AuthTypes (Entra ID, managed identity) have no key to verify here.
            if (connectionString.TryGetValue("AccessKey", out var accessKey) is false)
                return HealthCheckResult.Healthy("Azure SignalR is healthy");

            var userUrl = $"{baseUrl}/api/hubs/healthcheck/users/{Guid.NewGuid():N}";
            using var userRequest = new HttpRequestMessage(HttpMethod.Head, $"{userUrl}?api-version={ApiVersion}");
            userRequest.Headers.Authorization = new("Bearer", CreateAccessToken(Convert.ToString(accessKey)!, audience: userUrl));

            using var userResponse = await httpClient.SendAsync(userRequest, cancellationToken);
            // 200 (connected) and 404 (not connected) are the only answers to an accepted key.
            if (userResponse.StatusCode is not (HttpStatusCode.OK or HttpStatusCode.NotFound))
                throw new InvalidOperationException($"The user lookup answered {(int)userResponse.StatusCode}.");

            return HealthCheckResult.Healthy("Azure SignalR is healthy");
        }
        catch (Exception exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Azure SignalR is unhealthy", exp);
        }
    }

    private string CreateAccessToken(string accessKey, string audience)
    {
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Audience = audience,
            Expires = timeProvider.GetUtcNow().AddMinutes(1).UtcDateTime,
            SigningCredentials = new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(accessKey)), SecurityAlgorithms.HmacSha256)
        });
    }
}
