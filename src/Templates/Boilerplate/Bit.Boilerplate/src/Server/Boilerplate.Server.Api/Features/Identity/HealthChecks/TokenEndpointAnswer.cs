using System.Text.Json.Nodes;

namespace Boilerplate.Server.Api.Features.Identity.HealthChecks;

public static class TokenEndpointAnswer
{
    public static async Task<JsonNode?> Read(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return JsonNode.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
