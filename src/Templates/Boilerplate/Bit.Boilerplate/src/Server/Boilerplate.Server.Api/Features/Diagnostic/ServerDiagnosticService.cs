//+:cnd:noEmit
namespace Boilerplate.Server.Api.Features.Diagnostic;

/// <summary>Which path the report came in through, so the three answers can be told apart.</summary>
public enum DiagnosticReportSource
{
    /// <summary>The anonymous <see cref="DiagnosticController.PerformDiagnostic"/>, behind the /diagnostic page.</summary>
    Http,
    //#if (signalR == true)
    /// <summary>The hub method, so over the websocket the app already holds open.</summary>
    SignalR,
    //#endif
    /// <summary>The <c>GetDiagnosticReport</c> tool on /dev-mcp.</summary>
    DevMcp
}

/// <summary>
/// What the server makes of the call it is answering: the client ip it resolved, how the call arrived, and the
/// headers it arrived with. One body behind all three paths, since behind a CDN each reaches the origin its own way
/// and can resolve a different client ip - only visible by asking all three and comparing.
/// <para>
/// Built on IHttpContextAccessor rather than ControllerBase, since two of the three callers are not a controller.
/// SignalR carries no ambient request, so the hub fills the accessor in first (See <c>AppHub.GetDiagnosticReport</c>).
/// </para>
/// </summary>
public partial class ServerDiagnosticService
{
    [AutoInject] private IHostEnvironment env = default!;
    [AutoInject] private TimeProvider timeProvider = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;
    //#if (multitenant == true)
    [AutoInject] private TenantProvider tenantProvider = default!;
    //#endif

    /// <summary>The whole report as one string, which is what the mcp tool hands back.</summary>
    public string BuildReport(DiagnosticReportSource via) => string.Join($"{Environment.NewLine}{Environment.NewLine}", BuildSections(via));

    /// <summary>
    /// One entry per block the report renders. Headers come back as they arrived, except the two carrying a
    /// credential (See <see cref="ReadHeaderValue"/>).
    /// </summary>
    public string[] BuildSections(DiagnosticReportSource via)
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext is null)
            return [$"Via: {via}", "No request is in flight, so there is nothing to say about one."];

        var request = httpContext.Request;
        var connection = httpContext.Connection;
        var user = httpContext.User;

        StringBuilder call = new();
        call.AppendLine($"Via: {via}");
        // The proxy's address rather than the visitor's, unless ForwardedHeaders:KnownIPNetworks trusts that hop.
        call.AppendLine($"Client IP: {connection.RemoteIpAddress}");
        call.AppendLine($"Client port: {connection.RemotePort}");
        call.AppendLine($"IsFromCDN: {request.IsFromCDN().ToString().ToLowerInvariant()}");
        call.AppendLine($"Protocol: {request.Protocol}");
        // Without the query string: it carries one-time codes on the confirm and reset links.
        call.AppendLine($"Endpoint: {request.Method} {request.Scheme}://{request.Host}{request.PathBase}{request.Path}");
        call.AppendLine($"Trace => {httpContext.TraceIdentifier}");

        StringBuilder caller = new();
        var isAuthenticated = user.IsAuthenticated();
        caller.AppendLine($"IsAuthenticated: {isAuthenticated.ToString().ToLowerInvariant()}");
        if (isAuthenticated)
        {
            // The row RevokeSession acts on, so a misbehaving session can be named.
            caller.AppendLine($"UserSessionId: {user.GetSessionId()}");
        }
        //#if (multitenant == true)
        caller.AppendLine($"TenantId: {ReadTenantId()}");
        //#endif
        caller.AppendLine($"Culture => C: {CultureInfo.CurrentCulture.Name}, UC: {CultureInfo.CurrentUICulture.Name}");

        StringBuilder headers = new();
        foreach (var header in request.Headers.OrderBy(h => h.Key))
        {
            headers.AppendLine($"{header.Key}: {ReadHeaderValue(header.Key, header.Value.ToString())}");
        }

        StringBuilder deployment = new();
        deployment.AppendLine($"Environment: {env.EnvironmentName}");
        deployment.AppendLine($"Base url: {request.GetBaseUrl()}");
        deployment.AppendLine($"Web app url: {ReadWebAppUrl(request)}");
        // Read against the client clock the report shows above: an "instantly expired" token or a never-matching TOTP
        // is usually the two disagreeing.
        deployment.AppendLine($"Server UTC: {timeProvider.GetUtcNow():yyyy-MM-dd HH:mm:ss}");

        return [call.ToString().TrimEnd(), caller.ToString().TrimEnd(), headers.ToString().TrimEnd(), deployment.ToString().TrimEnd()];
    }

    /// <summary>
    /// The headers that carry a credential of their own rather than in an <c>Authorization</c> scheme. Matched on the
    /// whole name, so an ordinary <c>X-Request-Id</c> still reads as itself.
    /// </summary>
    private static readonly string[] credentialHeaders = ["Proxy-Authorization", "X-Api-Key", "Api-Key", "X-Auth-Token", "X-Access-Token", "X-Csrf-Token", "X-Xsrf-Token"];

    /// <summary>
    /// A header's value, except for those that carry a credential: the report gets pasted into issues and, over
    /// /dev-mcp, handed to a model, and nothing it is used for needs the secret itself.
    /// </summary>
    private static string ReadHeaderValue(string name, string value)
    {
        // The scheme is most of the question: whether the header arrived, and whether it is the expected kind. Both
        // ends of the credential come too, enough to tell one token from another without handing it over.
        if (string.Equals(name, "Authorization", StringComparison.OrdinalIgnoreCase))
        {
            var parts = value.Split(' ', 2);
            var scheme = parts[0];
            var credential = parts.Length is 2 ? parts[1].Trim() : string.Empty;

            if (string.IsNullOrWhiteSpace(scheme))
                return "(present, no scheme)";

            return credential.Length > 20
                ? $"{scheme} {credential[..10]}...{credential[^10..]}"
                : $"{scheme} (redacted)";
        }

        // Names only: which cookies arrived is what gets diagnosed, their values are the session itself.
        if (string.Equals(name, "Cookie", StringComparison.OrdinalIgnoreCase))
        {
            var names = value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                             .Select(cookie => cookie.Split('=', 2)[0]);

            return $"{string.Join(", ", names)} (values redacted)";
        }

        // Same reasoning as Authorization, minus the scheme there is nothing to keep: that one of these arrived is
        // the diagnosis, its value is the credential.
        if (credentialHeaders.Contains(name, StringComparer.OrdinalIgnoreCase))
            return "(present, redacted)";

        return value;
    }

    //#if (multitenant == true)
    /// <summary>
    /// Which tenant this call resolves to, or why it does not: resolution reads the host, so an untrusted origin
    /// throws - which must not take the rest of the report down with it.
    /// </summary>
    private string ReadTenantId()
    {
        try
        {
            return tenantProvider.GetCurrentTenantId().ToString();
        }
        catch (Exception exp)
        {
            return exp.Message;
        }
    }

    //#endif
    /// <summary>Where the links this server mails would point, or why that could not be worked out.</summary>
    private static string ReadWebAppUrl(HttpRequest request)
    {
        try
        {
            return request.GetWebAppUrl().ToString();
        }
        catch (BadRequestException exp)
        {
            return exp.Message;
        }
    }
}
