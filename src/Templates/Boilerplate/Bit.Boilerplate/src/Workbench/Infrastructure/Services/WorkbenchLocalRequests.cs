using System.Net;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>Tells requests made on this machine from those that reach it from elsewhere, like through a tunnel.</summary>
public static class WorkbenchLocalRequests
{
    private static readonly string[] forwardingHeaders = ["X-Forwarded-For", "X-Forwarded-Host", "X-Forwarded-Proto", "Forwarded", "Cf-Connecting-Ip", "X-Real-IP", "X-Original-For", "True-Client-IP", "Via"];

    /// <summary>
    /// True for a loopback connection to a localhost host name without the forwarding headers that tunnels and proxies add.
    /// </summary>
    public static bool IsLocal(HttpContext context)
    {
        return context.Connection.RemoteIpAddress is { } address
            && IPAddress.IsLoopback(address)
            && forwardingHeaders.Any(context.Request.Headers.ContainsKey) is false
            && context.Request.Host.Host is "localhost" or "127.0.0.1" or "[::1]";
    }
}
