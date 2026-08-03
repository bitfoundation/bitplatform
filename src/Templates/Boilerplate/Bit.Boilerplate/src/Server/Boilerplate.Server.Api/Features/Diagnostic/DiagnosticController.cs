//+:cnd:noEmit
using Boilerplate.Shared.Features.Diagnostic;
//#if (signalR == true || notification == true)
using Boilerplate.Server.Api.Features.PushNotification;
//#endif

namespace Boilerplate.Server.Api.Features.Diagnostic;

[ApiVersion(1)]
[ApiController, AllowAnonymous]
[Route("api/v{v:apiVersion}/[controller]/[action]")]
public partial class DiagnosticController : AppControllerBase, IDiagnosticController
{
    [AutoInject] private IHostEnvironment env = default!;
    //#if (notification == true)
    [AutoInject] private PushNotificationService pushNotificationService = default!;
    //#endif
    //#if (signalR == true)
    [AutoInject] private IHubContext<AppHub> appHubContext = default!;
    //#endif

    [HttpGet]
    public async Task<string> PerformDiagnostic([FromQuery] string? signalRConnectionId, [FromQuery] string? pushNotificationSubscriptionDeviceId, CancellationToken cancellationToken)
    {
        StringBuilder result = new();

        result.AppendLine($"Client IP: {HttpContext.Connection.RemoteIpAddress}");

        result.AppendLine($"Trace => {Request.HttpContext.TraceIdentifier}");

        // This endpoint must serve anonymous callers: the diagnostic modal is reachable by anonymous visitors
        // (7 header taps / Ctrl+Shift+X), and an anonymous visitor's own push subscription / SignalR connection
        // is not owned by any UserSession. So the rule is ownership, not authentication: an identifier that
        // belongs to nobody is the caller's own and passes; an identifier that belongs to a UserSession may only
        // be acted on by that same session.
        var isAuthenticated = User.IsAuthenticated();
        var callerUserSessionId = isAuthenticated ? User.GetSessionId() : (Guid?)null;

        result.AppendLine($"IsAuthenticated: {isAuthenticated.ToString().ToLowerInvariant()}");

        //#if (notification == true)
        if (string.IsNullOrEmpty(pushNotificationSubscriptionDeviceId) is false)
        {
            var subscription = await DbContext.PushNotificationSubscriptions
                .FirstOrDefaultAsync(d => d.DeviceId == pushNotificationSubscriptionDeviceId, cancellationToken);

            result.AppendLine($"Subscription exists: {(subscription is not null).ToString().ToLowerInvariant()}");

            if (subscription?.UserSessionId is not null && subscription.UserSessionId != callerUserSessionId)
                throw new ResourceNotFoundException().WithData("Reason", "The push notification subscription belongs to another user session.");

            await pushNotificationService.RequestPush(new()
            {
                Title = "Test Push",
                Message = $"Open terms page. {TimeProvider.GetUtcNow():HH:mm:ss} UTC",
                Action = "testAction",
                PageUrl = PageUrls.Terms,
                UserRelatedPush = false
            }, s => s.DeviceId == pushNotificationSubscriptionDeviceId, cancellationToken);
        }
        //#endif

        //#if (signalR == true)
        if (string.IsNullOrEmpty(signalRConnectionId) is false)
        {
            var connectionOwnerUserSessionId = await DbContext.UserSessions
                .Where(us => us.SignalRConnectionId == signalRConnectionId)
                .Select(us => (Guid?)us.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (connectionOwnerUserSessionId is not null && connectionOwnerUserSessionId != callerUserSessionId)
                throw new ResourceNotFoundException().WithData("Reason", "The SignalR connection belongs to another user session.");

            var success = await appHubContext.Clients.Client(signalRConnectionId).InvokeAsync<bool>(SharedAppMessages.SHOW_MESSAGE, $"Open terms page. {TimeProvider.GetUtcNow():HH:mm:ss} UTC", new Dictionary<string, string?> { { "pageUrl", PageUrls.Terms }, { "action", "testAction" } }, cancellationToken);
            if (success is false) // Client would return false if it's unable to show the message with custom action.
            {
                _ = await appHubContext.Clients.Client(signalRConnectionId).InvokeAsync<bool>(SharedAppMessages.SHOW_MESSAGE, $"Simple message. {TimeProvider.GetUtcNow():HH:mm:ss} UTC", null, cancellationToken);
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
        //#if (multitenant == true)
        result.AppendLine($"TenantId: {TenantProvider.GetCurrentTenantId()}");
        //#endif
        result.AppendLine($"Environment: {env.EnvironmentName}");
        result.AppendLine("Base url: " + Request.GetBaseUrl());
        result.AppendLine("Web app url: " + Request.GetWebAppUrl());

        return result.ToString();
    }
}
