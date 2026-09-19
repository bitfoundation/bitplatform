using System.Collections.Concurrent;

namespace Bit.Bswup.Tests.Harness.Web;

/// <summary>
/// Every test session the host knows, keyed by the first label of the request's host name:
/// <c>http://a1b2c3.localhost:5000</c> is session "a1b2c3". Browsers resolve every <c>*.localhost</c> name to the
/// loopback address and treat it as a secure context, so each test gets an origin of its own - its own service
/// worker registrations and CacheStorage - against one shared host process. Any other host name
/// (<c>localhost</c>, <c>127.0.0.1</c>) is the "default" session, which is what a hand-run host serves.
/// </summary>
public sealed class HarnessSessions
{
    private const string LocalhostSuffix = ".localhost";

    private readonly ConcurrentDictionary<string, HarnessSession> _sessions = new(StringComparer.OrdinalIgnoreCase);

    public HarnessSession Resolve(HttpContext context)
    {
        var host = context.Request.Host.Host;
        var id = host.EndsWith(LocalhostSuffix, StringComparison.OrdinalIgnoreCase) && host.Length > LocalhostSuffix.Length
            ? host[..^LocalhostSuffix.Length]
            : "default";

        return _sessions.GetOrAdd(id, key => new HarnessSession(key));
    }

    /// <summary>The session <see cref="HarnessMiddleware"/> resolved for this request.</summary>
    public static HarnessSession Current(HttpContext context) =>
        context.Items[typeof(HarnessSession)] as HarnessSession
        ?? throw new InvalidOperationException($"{nameof(HarnessMiddleware)} did not run for {context.Request.Path}.");
}
