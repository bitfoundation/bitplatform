//+:cnd:noEmit
using Boilerplate.Shared.Features.Diagnostic;
//#if (notification == true)
using Boilerplate.Server.Api.Features.PushNotification;
//#endif

namespace Boilerplate.Server.Api.Features.Diagnostic;

[ApiVersion(1)]
[ApiController, AllowAnonymous]
[Route("api/v{v:apiVersion}/[controller]/[action]")]
public partial class DiagnosticController : AppControllerBase, IDiagnosticController
{
    //#if (notification == true)
    [AutoInject] private PushNotificationService pushNotificationService = default!;
    //#endif
    //#if (signalR == true)
    [AutoInject] private IHubContext<AppHub> appHubContext = default!;
    //#endif
    [AutoInject] private ServerDiagnosticService diagnostic = default!;

    [HttpGet]
    public async Task<string[]> PerformDiagnostic([FromQuery] string? signalRConnectionId, [FromQuery] string? pushNotificationSubscriptionDeviceId, CancellationToken cancellationToken)
    {
        // Only what this endpoint alone does - the test push and the test SignalR message - goes above the report, so
        // it isn't buried under the headers. The report itself comes from the shared service, so /dev-mcp and the hub
        // answer identically.
        List<string> sections = [];

        // The report is reachable anonymously (7 header taps / Ctrl+Shift+X / the /diagnostic page), and an anonymous
        // visitor's subscription or connection is owned by no UserSession - so the rule is ownership, not
        // authentication: an identifier owned by nobody passes, one owned by a session only for that session.
        var callerUserSessionId = User.IsAuthenticated() ? User.GetSessionId() : (Guid?)null;

        //#if (notification == true)
        if (string.IsNullOrWhiteSpace(pushNotificationSubscriptionDeviceId) is false)
        {
            var subscription = await DbContext.PushNotificationSubscriptions
                .FirstOrDefaultAsync(d => d.DeviceId == pushNotificationSubscriptionDeviceId, cancellationToken);

            sections.Add($"Subscription exists: {(subscription is not null).ToString().ToLowerInvariant()}");

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

            sections.Add("Test push requested.");
        }
        //#endif

        //#if (signalR == true)
        if (string.IsNullOrWhiteSpace(signalRConnectionId) is false)
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

            sections.Add($"SignalR test message delivered: {success.ToString().ToLowerInvariant()}");
        }
        //#endif

        return [.. sections, .. diagnostic.BuildSections(DiagnosticReportSource.Http)];
    }
}
