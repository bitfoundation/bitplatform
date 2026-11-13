using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Infrastructure.HealthChecks;

/// <summary>
/// The project's other sites, such as its landing page, a blog or the docs, which /health requests too, so a monitor that
/// reads it watches them along with the app (See <c>HealthChecks:Websites</c> in appsettings.json). Each one gets a GET that
/// has to end, after its redirects, in a success status: behind a CDN such as Cloudflare, a TCP connection succeeds whatever
/// state the site is in. Five are requested at a time, and each site that failed is in the result's data with what went wrong.
/// </summary>
public class WebsitesHealthCheck(IHttpClientFactory httpClientFactory, string[] websites) : IHealthCheck
{
    public const string HttpClientName = "Websites";

    /// <summary>
    /// A monitor waits about 15 seconds for /health, so the whole check stays within this however many sites hang.
    /// </summary>
    public static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(10);

    private static readonly TimeSpan requestTimeout = TimeSpan.FromSeconds(5);

    private const int concurrentRequests = 5;

    /// <summary>
    /// Each site once, by the address it is requested at: <c>example.com</c> and <c>https://EXAMPLE.com/</c> are one site,
    /// while <c>/A</c> and <c>/a</c> are two, as a path's case can tell sites apart.
    /// </summary>
    private readonly string[] sites = Distinct(websites);

    /// <summary>
    /// The sites <paramref name="section"/> lists: an array in appsettings.json, one value separated by commas or spaces,
    /// which a single environment or GitHub variable can carry (<c>HealthChecks__Websites</c>), or both.
    /// </summary>
    public static string[] Read(IConfigurationSection section)
    {
        return Distinct([.. (section.Value ?? "").Split([',', ';', ' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Concat(section.GetChildren().Select(child => child.Value?.Trim()).OfType<string>().Where(site => site.Length > 0))]);
    }

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
        using var checkTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        checkTimeout.CancelAfter(CheckTimeout);
        using var slots = new SemaphoreSlim(concurrentRequests);
        var httpClient = httpClientFactory.CreateClient(HttpClientName);

        var results = await Task.WhenAll(sites.Select(async site =>
        {
            if (TryGetAddress(site, out var address) is false)
                return (Site: site, Problem: (string?)"Not an http or https address.");

            try
            {
                await slots.WaitAsync(checkTimeout.Token);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested is false)
            {
                return (Site: address.AbsoluteUri, Problem: $"Not requested: the other sites took the check's {CheckTimeout.TotalSeconds:0} seconds.");
            }

            try
            {
                return (Site: address.AbsoluteUri, Problem: await ProblemOf(httpClient, address, checkTimeout.Token, cancellationToken));
            }
            finally
            {
                slots.Release();
            }
        }));

        var failed = results.Where(result => result.Problem is not null).ToArray();

        if (failed.Length is 0)
            return HealthCheckResult.Healthy(sites.Length is 1 ? "The site answers" : $"All {sites.Length} sites answer");

        return new HealthCheckResult(context.Registration.FailureStatus, $"{failed.Length} of {sites.Length} sites failed",
            data: failed.ToDictionary(result => result.Site, result => (object)result.Problem!));
    }

    /// <summary>
    /// Null when <paramref name="address"/> answers with a success status, otherwise what went wrong, as the report shows it.
    /// </summary>
    private static async Task<string?> ProblemOf(HttpClient httpClient, Uri address, CancellationToken checkToken, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(checkToken);
        timeout.CancelAfter(requestTimeout);
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
