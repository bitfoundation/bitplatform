using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Infrastructure.HealthChecks;

public partial class AppInsightsHealthCheck : IHealthCheck
{
    [AutoInject] private IConfiguration configuration = default!;
    [AutoInject] private IHttpClientFactory httpClientFactory = default!;
    [AutoInject] private TimeProvider timeProvider = default!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (TargetOf(configuration["ApplicationInsights:ConnectionString"]) is not { } target)
            return new HealthCheckResult(context.Registration.FailureStatus, "ApplicationInsights:ConnectionString has no valid InstrumentationKey");

        try
        {
            var envelopeWithoutData = new JsonArray(new JsonObject
            {
                ["name"] = "Microsoft.ApplicationInsights.Event",
                ["time"] = timeProvider.GetUtcNow().ToString("O"),
                ["iKey"] = target.InstrumentationKey
            });

            using var content = new StringContent(envelopeWithoutData.ToJsonString(), Encoding.UTF8, "application/json");
            using var response = await httpClientFactory.CreateClient("ApplicationInsights").PostAsync(target.TrackUrl, content, cancellationToken);

            var error = ErrorOf(await response.Content.ReadAsStringAsync(cancellationToken));

            if (response.StatusCode is HttpStatusCode.BadRequest && error is not null && error.Contains("instrumentation key", StringComparison.OrdinalIgnoreCase) is false)
                return HealthCheckResult.Healthy("Application Insights accepts the instrumentation key");

            if (response.IsSuccessStatusCode)
                return HealthCheckResult.Healthy("Application Insights accepts the instrumentation key");

            return new HealthCheckResult(context.Registration.FailureStatus, $"Application Insights answered {(int)response.StatusCode} {response.StatusCode}{(error is null ? "" : $": {error}")}");
        }
        catch (Exception exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Application Insights is unreachable", exp);
        }
    }

    private static (string InstrumentationKey, string TrackUrl)? TargetOf(string? connectionString)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in (connectionString ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var pair = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (pair.Length is 2)
            {
                values[pair[0]] = pair[1];
            }
        }

        if (values.TryGetValue("InstrumentationKey", out var instrumentationKey) is false || Guid.TryParse(instrumentationKey, out _) is false)
            return null;

        var ingestionEndpoint = values.TryGetValue("IngestionEndpoint", out var endpoint) && Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri)
            ? endpointUri.ToString()
            : values.TryGetValue("EndpointSuffix", out var suffix) && string.IsNullOrWhiteSpace(suffix) is false
                ? $"https://{(values.TryGetValue("Location", out var location) && string.IsNullOrWhiteSpace(location) is false ? location + "." : "")}dc.{suffix.Trim('.')}/"
                : "https://dc.services.visualstudio.com/";

        return (instrumentationKey, $"{ingestionEndpoint.TrimEnd('/')}/v2.1/track");
    }

    private static string? ErrorOf(string body)
    {
        try
        {
            return JsonNode.Parse(body)?["errors"]?[0]?["message"]?.ToString();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
