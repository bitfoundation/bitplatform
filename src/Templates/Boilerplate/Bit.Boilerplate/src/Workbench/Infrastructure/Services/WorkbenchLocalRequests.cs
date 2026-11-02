using System.Net;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// Tells who may change things. Requests made on this machine are told from those that reach it from elsewhere, like through
/// a tunnel, and dev mode from every other way the Workbench runs, like a published demo or bit CLI's plain run, which have no
/// hot reload to show a change and no agent to answer a comment.
/// </summary>
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

    /// <summary>Whether dotnet watch runs the Workbench, as its agent skill starts it: it sets this on the process it starts.</summary>
    public static bool IsDevMode(IConfiguration configuration) => configuration["DOTNET_WATCH"] is "1";

    /// <summary>Whether a request may change the code, the texts and the review: in dev mode, from this machine.</summary>
    public static bool CanChange(HttpContext context, IConfiguration configuration) => IsDevMode(configuration) && IsLocal(context);
}
