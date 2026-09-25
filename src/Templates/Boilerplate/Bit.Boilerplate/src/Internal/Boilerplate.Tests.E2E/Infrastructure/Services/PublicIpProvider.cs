namespace Boilerplate.Tests.E2E.Infrastructure.Services;

/// <summary>
/// This machine's address as the public internet sees it, from a third party with no part in the deployments.
/// </summary>
public static class PublicIpProvider
{
    private static readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    /// <summary>
    /// Both families: which one a connection ends up on is the OS's choice, not the test's, so either answer from
    /// ipify's two hostnames may be the one a deployment saw.
    /// </summary>
    public static async Task<IReadOnlyCollection<string>> Resolve(CancellationToken cancellationToken)
    {
        var addresses = await Task.WhenAll(Read("https://api.ipify.org", cancellationToken),
                                           Read("https://api64.ipify.org", cancellationToken));

        var resolved = addresses.OfType<string>().Distinct().ToArray();

        Assert.IsGreaterThan(0, resolved.Length, "Neither api.ipify.org nor api64.ipify.org returned this machine's public IP, so there is nothing to compare a deployment's answer with.");

        return resolved;
    }

    /// <summary>
    /// The browser's own address rather than this machine's, for a test that asks what a deployment saw of the browser.
    /// They differ whenever the browser runs elsewhere (PLAYWRIGHT_SERVER_ENDPOINT): that machine shares the network's
    /// IPv4 address behind NAT, but has an IPv6 address of its own. Asked from a blank page of the same context, which
    /// carries no content security policy to stop the request and goes out the way the app's pages do.
    /// </summary>
    public static async Task<IReadOnlyCollection<string>> ResolveFromBrowser(IBrowserContext context)
    {
        var page = await context.NewPageAsync();

        try
        {
            var addresses = await Task.WhenAll(ReadFromBrowser(page, "https://api.ipify.org"),
                                               ReadFromBrowser(page, "https://api64.ipify.org"));

            var resolved = addresses.OfType<string>().Distinct().ToArray();

            Assert.IsGreaterThan(0, resolved.Length, "Neither api.ipify.org nor api64.ipify.org answered the browser, so there is nothing to compare a deployment's answer with.");

            return resolved;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private static async Task<string?> ReadFromBrowser(IPage page, string url)
    {
        // Null for a family the browser cannot reach, as Read does for this process.
        var text = await page.EvaluateAsync<string?>("url => fetch(url).then(response => response.ok ? response.text() : null).catch(() => null)", url);

        return string.IsNullOrWhiteSpace(text) || IPAddress.TryParse(text.Trim(), out _) is false ? null : Normalize(text);
    }

    /// <summary>A dual stack socket reports an IPv4 peer as ::ffff:a.b.c.d; a forwarded header carries the plain one.</summary>
    public static string Normalize(string address)
    {
        var parsed = IPAddress.Parse(address.Trim());

        return (parsed.IsIPv4MappedToIPv6 ? parsed.MapToIPv4() : parsed).ToString();
    }

    private static async Task<string?> Read(string url, CancellationToken cancellationToken)
    {
        try
        {
            return Normalize(await httpClient.GetStringAsync(url, cancellationToken));
        }
        // Not when the caller cancelled: that has to surface as cancellation rather than as the assertion above,
        // which would report an unreachable ipify. HttpClient's own timeout arrives as the same type, and is one of
        // the unreachable cases.
        catch (Exception exp) when (exp is HttpRequestException or TaskCanceledException or FormatException
                                    && cancellationToken.IsCancellationRequested is false)
        {
            // One family being unreachable is ordinary; both are what the assertion above catches.
            return null;
        }
    }
}
