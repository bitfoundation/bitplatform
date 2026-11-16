using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Infrastructure.HealthChecks;

public partial class SentryHealthCheck : IHealthCheck
{
    [AutoInject] private IConfiguration configuration = default!;
    [AutoInject] private IHttpClientFactory httpClientFactory = default!;
    [AutoInject] private TimeProvider timeProvider = default!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (EnvelopeUrlOf(configuration["Sentry:Dsn"]) is not { } envelopeUrl)
            return new HealthCheckResult(context.Registration.FailureStatus, "Sentry:Dsn is not a Sentry DSN");

        try
        {
            var clientReport = "{}\n{\"type\":\"client_report\"}\n{\"timestamp\":" + timeProvider.GetUtcNow().ToUnixTimeSeconds() + ",\"discarded_events\":[]}\n";

            using var request = new HttpRequestMessage(HttpMethod.Post, envelopeUrl)
            {
                Content = new StringContent(clientReport, Encoding.UTF8, "application/x-sentry-envelope")
            };

            using var response = await httpClientFactory.CreateClient("Sentry").SendAsync(request, cancellationToken);

            if (ErrorsLimitOf(response) is { } limit)
                return new HealthCheckResult(context.Registration.FailureStatus, $"Sentry drops the app's errors for {limit.RetryAfter} seconds ({limit.Reason})");

            if (response.IsSuccessStatusCode is false)
                return new HealthCheckResult(context.Registration.FailureStatus, $"Sentry answered {(int)response.StatusCode} {response.StatusCode}{await DetailOf(response, cancellationToken)}");

            return HealthCheckResult.Healthy("Sentry accepts the app's errors");
        }
        catch (Exception exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Sentry is unreachable", exp);
        }
    }

    private static string? EnvelopeUrlOf(string? dsn)
    {
        if (Uri.TryCreate(dsn?.Trim(), UriKind.Absolute, out var uri) is false || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            return null;

        var publicKey = Uri.UnescapeDataString(uri.UserInfo.Split(':')[0]);
        var path = uri.AbsolutePath.TrimEnd('/');
        var slash = path.LastIndexOf('/');
        var projectId = path[(slash + 1)..];

        if (publicKey.Length is 0 || projectId.Length is 0)
            return null;

        return $"{uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped)}{path[..slash]}/api/{projectId}/envelope/?sentry_key={Uri.EscapeDataString(publicKey)}&sentry_version=7";
    }

    private static (string RetryAfter, string Reason)? ErrorsLimitOf(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("X-Sentry-Rate-Limits", out var values) is false)
            return null;

        foreach (var limit in values.SelectMany(value => value.Split(',')))
        {
            var parts = limit.Trim().Split(':');
            if (parts.Length < 2)
                continue;

            var categories = parts[1].Split(';', StringSplitOptions.RemoveEmptyEntries);
            if (categories.Length is 0 || categories.Contains("error", StringComparer.OrdinalIgnoreCase))
                return (parts[0], parts.Length > 3 && parts[3].Length > 0 ? parts[3] : "rate limited");
        }

        return null;
    }

    private static async Task<string> DetailOf(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            return body.RootElement.TryGetProperty("detail", out var detail) && detail.GetString() is { Length: > 0 } text ? $": {text}" : "";
        }
        catch (JsonException)
        {
            return "";
        }
    }
}
