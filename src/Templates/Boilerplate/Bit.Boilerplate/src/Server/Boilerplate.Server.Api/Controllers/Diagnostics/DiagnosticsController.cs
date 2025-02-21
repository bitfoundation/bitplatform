//+:cnd:noEmit
using System.Text;
//#if (signalR == true)
using Microsoft.AspNetCore.SignalR;
using Boilerplate.Server.Api.SignalR;
//#endif
using Boilerplate.Server.Api.Services;
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Shared.Controllers.Diagnostics;

namespace Boilerplate.Server.Api.Controllers.Diagnostics;

[ApiController, AllowAnonymous]
[Route("api/[controller]/[action]")]
public partial class DiagnosticsController : AppControllerBase, IDiagnosticsController
{
    //#if (notification == true)
    [AutoInject] private PushNotificationService pushNotificationService = default!;
    //#endif
    //#if (signalR == true)
    [AutoInject] private IHubContext<AppHub> appHubContext = default!;
    //#endif

    [HttpPost]
    public async Task<string> PerformDiagnostics(CancellationToken cancellationToken)
    {
        StringBuilder result = new();

        result.AppendLine($"Client IP: {HttpContext.Connection.RemoteIpAddress}");

        result.AppendLine($"Trace => {Request.HttpContext.TraceIdentifier}");

        var isAuthenticated = User.IsAuthenticated();
        Guid? userSessionId = null;
        UserSession? userSession = null;

        if (isAuthenticated)
        {
            userSessionId = User.GetSessionId();
            userSession = await DbContext
                .UserSessions.SingleAsync(us => us.Id == userSessionId, cancellationToken);
        }

        result.AppendLine($"IsAuthenticated: {isAuthenticated.ToString().ToLowerInvariant()}");

        //#if (notification == true)
        if (isAuthenticated)
        {
            var subscription = await DbContext.UserSessions.Include(us => us.PushNotificationSubscription)
                .FirstOrDefaultAsync(us => us.Id == userSessionId, cancellationToken);

            result.AppendLine($"Subscription exists: {(subscription?.PushNotificationSubscription is not null).ToString().ToLowerInvariant()}");

            await pushNotificationService.RequestPush("Test Push", DateTimeOffset.Now.ToString("HH:mm:ss"), "Test action", userRelatedPush: true, u => u.UserSessionId == userSessionId, cancellationToken);
        }
        //#endif

        //#if (signalR == true)
        if (isAuthenticated && userSession!.SignalRConnectionId is not null)
        {
            await appHubContext.Clients.Client(userSession.SignalRConnectionId).SendAsync(SignalREvents.SHOW_MESSAGE, DateTimeOffset.Now.ToString("HH:mm:ss"), cancellationToken);
        }
        //#endif

        result.AppendLine($"Culture => C: {CultureInfo.CurrentCulture.Name}, UC: {CultureInfo.CurrentUICulture.Name}");

        foreach (var header in Request.Headers)
        {
            result.AppendLine($"{header.Key}: {header.Value}");
        }

        return result.ToString();
    }
}
