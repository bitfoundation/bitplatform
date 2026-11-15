using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Infrastructure.HealthChecks;

/// <summary>
/// One of the project's other sites, such as its landing page, a blog or the docs, which /health requests too, so a monitor that
/// reads it watches them along with the app (See <c>HealthChecks:Websites</c> in appsettings.json). Each site is a check of its own,
/// so the Operations page shows a card for each. It gets a GET that has to end, after its redirects, in a success status: behind a
/// CDN such as Cloudflare, a TCP connection succeeds whatever state the site is in. A failed check's description says what went wrong.
/// </summary>
public class WebsiteHealthCheck(IHttpClientFactory httpClientFactory, string site) : IHealthCheck
{
    public const string HttpClientName = "Websites";

    /// <summary>
    /// A monitor waits about 15 seconds for /health, so the check stays within this however long the site hangs.
    /// </summary>
    public static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The sites <paramref name="section"/> lists: an array in appsettings.json, one value separated by commas or spaces,
    /// which a single environment or GitHub variable can carry (<c>HealthChecks__Websites</c>), or both.
    /// </summary>
    public static string[] Read(IConfigurationSection section)
    {
        return Distinct([.. (section.Value ?? "").Split([',', ';', ' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Concat(section.GetChildren().Select(child => child.Value?.Trim()).OfType<string>().Where(site => site.Length > 0))]);
    }

    public static string NameOf(string site) => $"website:{RequestKeyOf(site)}";

    public static string TitleOf(string site) => TryGetAddress(site, out var address) ? $"{address.Authority}{address.PathAndQuery.TrimEnd('/')}" : site;

    /// <summary>
    /// Each site once, by the address it is requested at: <c>example.com</c> and <c>https://EXAMPLE.com/</c> are one site,
    /// while <c>/A</c> and <c>/a</c> are two, as a path's case can tell sites apart.
    /// </summary>
    private static string[] Distinct(string[] websites) => [.. websites.DistinctBy(RequestKeyOf, StringComparer.Ordinal)];

    /// <summary>
    /// The address <paramref name="site"/> is requested at, which keys its result: the scheme and the host lowercased by
    /// <see cref="Uri"/>, and the path and the query as they were written. A bare host, such as blog.example.com, is
    /// requested over https.
    /// </summary>
    private static bool TryGetAddress(string site, [NotNullWhen(true)] out Uri? address)
    {
        return Uri.TryCreate(site.Contains("://", StringComparison.Ordinal) ? site : $"https://{site}", UriKind.Absolute, out address)
            && address.Scheme is "http" or "https";
    }

    private static string RequestKeyOf(string site) => TryGetAddress(site, out var address) ? address.AbsoluteUri : site;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (TryGetAddress(site, out var address) is false)
            return new HealthCheckResult(context.Registration.FailureStatus, "Not an http or https address.");

        return await ProblemOf(httpClientFactory.CreateClient(HttpClientName), address, cancellationToken) is { } problem
            ? new HealthCheckResult(context.Registration.FailureStatus, problem)
            : HealthCheckResult.Healthy("The site answers");
    }

    /// <summary>
    /// Null when <paramref name="address"/> answers with a success status, otherwise what went wrong, as the report shows it.
    /// </summary>
    private static async Task<string?> ProblemOf(HttpClient httpClient, Uri address, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(CheckTimeout);
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            using var response = await httpClient.GetAsync(address, HttpCompletionOption.ResponseHeadersRead, timeout.Token);

            if (response.IsSuccessStatusCode)
                return null;

            var status = $"{(int)response.StatusCode} {response.ReasonPhrase}";

            // Cloudflare's security rules answer a request they challenge with this header, whatever state the site is in.
            if (response.Headers.TryGetValues("cf-mitigated", out var mitigated) && mitigated.Contains("challenge"))
                return $"{status}: Cloudflare challenged the request. Let this server's requests through in the zone's security rules.";

            return $"{status}.";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested is false)
        {
            return $"No answer within {Stopwatch.GetElapsedTime(startedAt).TotalSeconds:0.#} seconds.";
        }
        catch (HttpRequestException exception)
        {
            var reason = exception.InnerException?.Message ?? exception.Message;

            return exception.HttpRequestError switch
            {
                HttpRequestError.NameResolutionError => "Its name doesn't resolve.",
                HttpRequestError.SecureConnectionError => $"The TLS connection failed: {reason}",
                HttpRequestError.ConnectionError => $"The connection failed: {reason}",
                _ => reason
            };
        }
    }
}
