//+:cnd:noEmit
using Boilerplate.Server.Api.Features.Diagnostic;

namespace Boilerplate.Server.Api.Infrastructure.SignalR;

public partial class AppHub
{
    /// <summary>
    /// The report over the websocket, in the same sections
    /// <see cref="Features.Diagnostic.DiagnosticController.PerformDiagnostic"/> returns. Worth asking twice: an
    /// upgrade takes a different path through the proxy, so the client ip can disagree only here.
    /// <para>
    /// Anonymous, like the http endpoint and the hub itself - what comes back is this caller's own connection, and
    /// nothing about anyone else's.
    /// </para>
    /// </summary>
    [HubMethodName(SharedAppMessages.GetDiagnosticReport)]
    public string[] GetDiagnosticReport([FromServices] ServerDiagnosticService diagnostic)
    {
        // Azure SignalR runs the hub with no ambient ASP.NET Core request, so IHttpContextAccessor is never set (See
        // StartChat). The handshake's context is exactly the request whose forwarding is in question.
        serviceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = Context.GetHttpContext();

        return diagnostic.BuildSections(DiagnosticReportSource.SignalR);
    }
}
