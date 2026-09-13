namespace Boilerplate.Server.Api.Infrastructure.Services;

/// <summary>
/// The envelope every Cloudflare API v4 response is wrapped in.
/// https://developers.cloudflare.com/api/resources/cache/methods/purge/
/// </summary>
public class CloudflarePurgeResponse
{
    public bool Success { get; set; }

    public CloudflareResponseInfo[]? Errors { get; set; }
}

public class CloudflareResponseInfo
{
    public int Code { get; set; }

    public string? Message { get; set; }
}
