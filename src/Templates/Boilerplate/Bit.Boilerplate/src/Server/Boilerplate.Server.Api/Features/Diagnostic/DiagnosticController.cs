//+:cnd:noEmit
using FluentEmail.Core;
using System.Runtime.CompilerServices;
using Twilio.Rest.Api.V2010.Account;
using Boilerplate.Shared.Features.Diagnostic;
//#if (notification == true)
using Boilerplate.Server.Api.Features.PushNotification;
//#endif

namespace Boilerplate.Server.Api.Features.Diagnostic;

[ApiVersion(1)]
[ApiController]
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
    [AutoInject] private UserManager<User> userManager = default!;
    [AutoInject] private IFluentEmail fluentEmail = default!;
    [AutoInject] private IStringLocalizer<EmailStrings> emailLocalizer = default!;

    // The report is reachable anonymously (7 header taps / Ctrl+Shift+X / the /diagnostic page), and an anonymous
    // visitor's subscription or connection is owned by no UserSession - so the rule is ownership, not
    // authentication: an identifier owned by nobody passes, one owned by a session only for that session.
    private Guid? CallerUserSessionId => User.IsAuthenticated() ? User.GetSessionId() : null;

    /// <summary>
    /// Streams the report one section at a time, so the report is not held up by the test push or the test SignalR
    /// message, each of which waits on something outside this server.
    /// <para>
    /// Everything that can refuse the call runs before the stream is handed back: once the first section is written the
    /// status code is sent, and a refusal after that could only cut the response short rather than answer 404.
    /// </para>
    /// </summary>
    [HttpGet, AllowAnonymous]
    public async Task<IAsyncEnumerable<string>> PerformDiagnostic([FromQuery] string? signalRConnectionId, [FromQuery] string? pushNotificationSubscriptionDeviceId, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";

        //#if (notification == true)
        var subscriptionExists = string.IsNullOrWhiteSpace(pushNotificationSubscriptionDeviceId) is false
            && await PushSubscriptionExists(pushNotificationSubscriptionDeviceId, cancellationToken);
        //#endif

        //#if (signalR == true)
        if (string.IsNullOrWhiteSpace(signalRConnectionId) is false)
        {
            var connectionOwnerUserSessionId = await DbContext.UserSessions
                .Where(us => us.SignalRConnectionId == signalRConnectionId)
                .Select(us => (Guid?)us.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (connectionOwnerUserSessionId is not null && connectionOwnerUserSessionId != CallerUserSessionId)
                throw new ResourceNotFoundException().WithData("Reason", "The SignalR connection belongs to another user session.");
        }
        //#endif

        return StreamSections();

        async IAsyncEnumerable<string> StreamSections()
        {
            // Only what this endpoint alone does - the test push and the test SignalR message - goes above the report,
            // so it isn't buried under the headers. The report itself comes from the shared service, so /dev-mcp and the
            // hub answer identically.

            //#if (notification == true)
            if (string.IsNullOrWhiteSpace(pushNotificationSubscriptionDeviceId) is false)
            {
                yield return $"Subscription exists: {subscriptionExists.ToString().ToLowerInvariant()}";

                await RequestTestPush(pushNotificationSubscriptionDeviceId, cancellationToken);

                yield return "Test push requested.";
            }
            //#endif

            //#if (signalR == true)
            if (string.IsNullOrWhiteSpace(signalRConnectionId) is false)
            {
                var withAction = await appHubContext.Clients.Client(signalRConnectionId).InvokeAsync<bool>(SharedAppMessages.SHOW_MESSAGE, $"Open terms page. {TimeProvider.GetUtcNow():HH:mm:ss} UTC", new Dictionary<string, string?> { { "pageUrl", PageUrls.Terms }, { "action", "testAction" } }, cancellationToken);

                // Which of the two got through, not just whether anything did: a client that shows a plain message but
                // no custom action is a different diagnosis from one the message never reached.
                var delivered = withAction
                    ? "with custom action"
                    : await appHubContext.Clients.Client(signalRConnectionId).InvokeAsync<bool>(SharedAppMessages.SHOW_MESSAGE, $"Simple message. {TimeProvider.GetUtcNow():HH:mm:ss} UTC", null, cancellationToken)
                        ? "as a simple message, the custom action was refused"
                        : "no";

                yield return $"SignalR test message delivered: {delivered}.";
            }
            //#endif

            foreach (var section in diagnostic.BuildSections(DiagnosticReportSource.Http))
            {
                yield return section;
            }
        }
    }

    /// <summary>
    /// The interface's shape, which the typed client streams from. MVC serves the public action above instead, whose
    /// refusals have to happen before the stream starts.
    /// </summary>
    async IAsyncEnumerable<string> IDiagnosticController.PerformDiagnostic(string? signalRConnectionId, string? pushNotificationSubscriptionDeviceId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var section in await PerformDiagnostic(signalRConnectionId, pushNotificationSubscriptionDeviceId, cancellationToken))
        {
            yield return section;
        }
    }

    //#if (notification == true)
    [HttpPost("{deviceId}"), AllowAnonymous]
    public async Task<bool> SendTestPushNotification(string deviceId, CancellationToken cancellationToken)
    {
        if (await PushSubscriptionExists(deviceId, cancellationToken) is false)
            return false;

        await RequestTestPush(deviceId, cancellationToken);

        return true;
    }

    private async Task<bool> PushSubscriptionExists(string deviceId, CancellationToken cancellationToken)
    {
        var subscription = await DbContext.PushNotificationSubscriptions
            .FirstOrDefaultAsync(d => d.DeviceId == deviceId, cancellationToken);

        if (subscription?.UserSessionId is not null && subscription.UserSessionId != CallerUserSessionId)
            throw new ResourceNotFoundException().WithData("Reason", "The push notification subscription belongs to another user session.");

        return subscription is not null;
    }

    private async Task RequestTestPush(string deviceId, CancellationToken cancellationToken)
    {
        await pushNotificationService.RequestPush(new()
        {
            Title = "Test Push",
            Message = $"Open terms page. {TimeProvider.GetUtcNow():HH:mm:ss} UTC",
            Action = "testAction",
            PageUrl = PageUrls.Terms,
            UserRelatedPush = false
        }, s => s.DeviceId == deviceId, cancellationToken);
    }
    //#endif

    // The two below are for the health checks page, and send right away rather than through a background job, so a
    // failure reaches the caller.

    [HttpPost, Authorize(Policy = AppFeatures.System.HealthChecks_View)]
    public async Task<bool> SendTestEmail(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUser();
        if (string.IsNullOrWhiteSpace(user.Email))
            return false;

        var result = await fluentEmail
            .To(user.Email, user.DisplayName)
            .SetFrom(AppSettings.Email!.DefaultFromEmail, emailLocalizer[nameof(EmailStrings.DefaultFromName)])
            .Subject("Test email")
            .Body($"This is a test email. {TimeProvider.GetUtcNow():HH:mm:ss} UTC")
            .SendAsync(cancellationToken);

        if (result.Successful is false)
            throw new InvalidOperationException(string.Join(", ", result.ErrorMessages));

        return true;
    }

    [HttpPost, Authorize(Policy = AppFeatures.System.HealthChecks_View)]
    public async Task<bool> SendTestSms(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUser();
        if (string.IsNullOrWhiteSpace(user.PhoneNumber))
            return false;

        if (AppSettings.Sms?.Configured is not true)
            throw new BadRequestException("Sms is not configured.");

        var message = await MessageResource.CreateAsync(new CreateMessageOptions(new(user.PhoneNumber))
        {
            From = new(AppSettings.Sms.FromPhoneNumber),
            Body = $"This is a test message. {TimeProvider.GetUtcNow():HH:mm:ss} UTC"
        });

        if (message.ErrorCode is not null)
            throw new InvalidOperationException(message.ErrorMessage);

        return true;
    }

    private async Task<User> GetCurrentUser()
    {
        return await userManager.FindByIdAsync(User.GetUserId().ToString()) ?? throw new ResourceNotFoundException();
    }
}
