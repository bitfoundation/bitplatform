using System.Net;
using System.Threading.RateLimiting;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// Limits how many views each remote visitor may capture per minute, one permit per screenshot, as
/// Screenshots:PermitsPerMinutePerClient says. Requests made on this machine aren't limited.
/// </summary>
public sealed class WorkbenchScreenshotLimiter : IDisposable
{
    private readonly int permitLimit;
    private readonly PartitionedRateLimiter<HttpContext> limiter;

    public WorkbenchScreenshotLimiter(IConfiguration configuration)
    {
        permitLimit = Math.Max(1, configuration.GetValue("Screenshots:PermitsPerMinutePerClient", 30));

        limiter = PartitionedRateLimiter.Create<HttpContext, string>(context => WorkbenchLocalRequests.IsLocal(context)
            ? RateLimitPartition.GetNoLimiter("local")
            : RateLimitPartition.GetFixedWindowLimiter(ClientOf(context), _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    }

    /// <summary>Takes a permit per screenshot; false when the visitor has used up this minute's.</summary>
    public bool TryAcquire(HttpContext context, int screenshots = 1)
    {
        using var lease = limiter.AttemptAcquire(context, Math.Clamp(screenshots, 1, permitLimit));

        return lease.IsAcquired;
    }

    /// <summary>
    /// The visitor's address. A tunnel or proxy on this machine connects from loopback and names the visitor in
    /// Cf-Connecting-Ip, or as the last X-Forwarded-For entry, the one it added itself.
    /// </summary>
    private static string ClientOf(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress;

        if (address is not null && IPAddress.IsLoopback(address))
        {
            var forwarded = context.Request.Headers["Cf-Connecting-Ip"].ToString();

            if (string.IsNullOrWhiteSpace(forwarded))
            {
                forwarded = context.Request.Headers["X-Forwarded-For"].ToString().Split(',')[^1];
            }

            if (string.IsNullOrWhiteSpace(forwarded) is false)
                return forwarded.Trim();
        }

        return address?.ToString() ?? "unknown";
    }

    public void Dispose() => limiter.Dispose();
}
