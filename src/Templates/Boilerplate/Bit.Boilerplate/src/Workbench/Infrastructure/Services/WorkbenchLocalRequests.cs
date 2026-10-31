using System.Net;

namespace Boilerplate.Workbench.Infrastructure.Services;

public static class WorkbenchLocalRequests
{
    private static readonly string[] forwardingHeaders = ["X-Forwarded-For", "X-Forwarded-Host", "Forwarded", "Cf-Connecting-Ip"];

    public static bool IsLocal(HttpContext context)
    {
        return context.Connection.RemoteIpAddress is { } address
            && IPAddress.IsLoopback(address)
            && forwardingHeaders.Any(context.Request.Headers.ContainsKey) is false
            && context.Request.Host.Host is "localhost" or "127.0.0.1" or "[::1]";
    }
}
