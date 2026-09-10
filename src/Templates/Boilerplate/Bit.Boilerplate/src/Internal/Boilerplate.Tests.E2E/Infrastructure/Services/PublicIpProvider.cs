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
        catch (Exception exp) when (exp is HttpRequestException or TaskCanceledException or FormatException)
        {
            // One family being unreachable is ordinary; both are what the assertion above catches.
            return null;
        }
    }
}
