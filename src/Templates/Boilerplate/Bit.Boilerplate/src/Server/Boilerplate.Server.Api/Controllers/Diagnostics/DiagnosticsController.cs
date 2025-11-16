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
    [AutoInject] private IHostEnvironment env = default!;
    //#if (notification == true)
    [AutoInject] private PushNotificationService pushNotificationService = default!;
    //#endif
    //#if (signalR == true)
    [AutoInject] private IHubContext<AppHub> appHubContext = default!;
    //#endif

    [HttpGet]
    public async Task<string> PerformDiagnostics([FromQuery] string? signalRConnectionId, [FromQuery] string? pushNotificationSubscriptionDeviceId, CancellationToken cancellationToken)
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
        if (string.IsNullOrEmpty(pushNotificationSubscriptionDeviceId) is false)
        {
            var subscription = await DbContext.PushNotificationSubscriptions.Include(us => us.UserSession)
                .FirstOrDefaultAsync(d => d.DeviceId == pushNotificationSubscriptionDeviceId, cancellationToken);

            result.AppendLine($"Subscription exists: {(subscription is not null).ToString().ToLowerInvariant()}");

            await pushNotificationService.RequestPush(new()
            {
                Title = "Test Push",
                Message = $"Open terms page. {DateTimeOffset.Now:HH:mm:ss}",
                Action = "testAction",
                PageUrl = PageUrls.Terms,
                UserRelatedPush = false
            }, s => s.DeviceId == pushNotificationSubscriptionDeviceId, cancellationToken);
        }
        //#endif

        //#if (signalR == true)
        if (string.IsNullOrEmpty(signalRConnectionId) is false)
        {
            var success = await appHubContext.Clients.Client(signalRConnectionId).InvokeAsync<bool>(SharedAppMessages.SHOW_MESSAGE, $"Open terms page. {DateTimeOffset.Now:HH:mm:ss}", new Dictionary<string, string?> { { "pageUrl", PageUrls.Terms }, { "action", "testAction" } }, cancellationToken);
            if (success is false) // Client would return false if it's unable to show the message with custom action.
            {
                _ = await appHubContext.Clients.Client(signalRConnectionId).InvokeAsync<bool>(SharedAppMessages.SHOW_MESSAGE, $"Simple message. {DateTimeOffset.Now:HH:mm:ss}", null, cancellationToken);
            }
        }
        //#endif

        result.AppendLine($"Culture => C: {CultureInfo.CurrentCulture.Name}, UC: {CultureInfo.CurrentUICulture.Name}");

        result.AppendLine();

        foreach (var header in Request.Headers.OrderBy(h => h.Key))
        {
            result.AppendLine($"{header.Key}: {header.Value}");
        }

        result.AppendLine();
        result.AppendLine($"Environment: {env.EnvironmentName}");
        result.AppendLine("Base url: " + Request.GetBaseUrl());
        result.AppendLine("Web app url: " + Request.GetWebAppUrl());

        return result.ToString();
    }
}
