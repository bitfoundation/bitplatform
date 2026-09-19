using System.Text.Json.Nodes;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Features.Identity.HealthChecks;

/// <summary>
/// Reads the tenant's discovery document, then asks for an app only token. Only a token, invalid_scope or
/// invalid_resource means the credentials were accepted.
/// </summary>
public partial class EntraIdHealthCheck : IHealthCheck
{
    [AutoInject] private IConfiguration configuration = default!;
    [AutoInject] private IHttpClientFactory httpClientFactory = default!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var section = configuration.GetRequiredSection("Authentication:AzureAD");
            var clientId = section["ClientId"]!;
            var clientSecret = section["ClientSecret"];
            var instance = (section["Instance"] ?? "https://login.microsoftonline.com/").TrimEnd('/');
            var policy = section["SignUpSignInPolicyId"];
            var authority = string.IsNullOrWhiteSpace(policy)
                ? $"{instance}/{section["TenantId"]}/v2.0"
                : $"{instance}/{section["Domain"]}/{policy}/v2.0"; // Azure AD B2C

            // Sign in's own backchannel client (See RemoteAuthenticationOptionsConfigurator).
            using var httpClient = httpClientFactory.CreateClient("AzureAD");

            using var discoveryResponse = await httpClient.GetAsync($"{authority}/.well-known/openid-configuration", cancellationToken);
            if (discoveryResponse.IsSuccessStatusCode is false)
                throw new InvalidOperationException($"The discovery document of '{authority}' answered {(int)discoveryResponse.StatusCode}.");

            if (string.IsNullOrWhiteSpace(clientSecret))
                return HealthCheckResult.Healthy("Entra ID is reachable (no client secret to verify)");

            var discovery = JsonNode.Parse(await discoveryResponse.Content.ReadAsStringAsync(cancellationToken));
            var tokenEndpoint = discovery?["token_endpoint"]?.GetValue<string>()
                ?? throw new InvalidOperationException($"The discovery document of '{authority}' has no token_endpoint.");

            // The secret only goes to the authority's own origin.
            if (Uri.TryCreate(tokenEndpoint, UriKind.Absolute, out var tokenEndpointUri) is false
                || tokenEndpointUri.Scheme != Uri.UriSchemeHttps
                || Uri.Compare(tokenEndpointUri, new Uri(authority), UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) is not 0)
                throw new InvalidOperationException($"The token_endpoint '{tokenEndpoint}' is not on '{authority}' over https.");

            using var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["scope"] = $"{clientId}/.default"
            });

            using var tokenResponse = await httpClient.PostAsync(tokenEndpoint, tokenRequest, cancellationToken);
            if (tokenResponse.IsSuccessStatusCode)
                return HealthCheckResult.Healthy("Entra ID accepts the client credentials");

            var error = JsonNode.Parse(await tokenResponse.Content.ReadAsStringAsync(cancellationToken))?["error"]?.GetValue<string>();
            if (error is "invalid_scope" or "invalid_resource")
                return HealthCheckResult.Healthy($"Entra ID accepts the client credentials ({error})");

            throw new InvalidOperationException($"The token endpoint answered {(int)tokenResponse.StatusCode} ({error}).");
        }
        catch (Exception exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Entra ID is unhealthy", exp);
        }
    }
}
