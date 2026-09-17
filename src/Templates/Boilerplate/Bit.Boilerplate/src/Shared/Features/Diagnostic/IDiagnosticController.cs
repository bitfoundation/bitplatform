//+:cnd:noEmit
namespace Boilerplate.Shared.Features.Diagnostic;

[Route("api/v1/[controller]/[action]/")]
public interface IDiagnosticController : IAppController
{
    [HttpGet("{?signalRConnectionId,pushNotificationSubscriptionDeviceId}")]
    IAsyncEnumerable<string> PerformDiagnostic(string? signalRConnectionId, string? pushNotificationSubscriptionDeviceId, CancellationToken cancellationToken);

    //#if (notification == true)
    /// <summary>Sends a test notification to the device's subscription. False when the device has none.</summary>
    [HttpPost("{deviceId}"), NoRetryPolicy]
    Task<bool> SendTestPushNotification(string deviceId, CancellationToken cancellationToken);
    //#endif

    /// <summary>Emails the signed-in user. False when the account has no email.</summary>
    [HttpPost, AuthorizedApi, NoRetryPolicy]
    Task<bool> SendTestEmail(CancellationToken cancellationToken);

    /// <summary>Texts the signed-in user. False when the account has no phone number.</summary>
    [HttpPost, AuthorizedApi, NoRetryPolicy]
    Task<bool> SendTestSms(CancellationToken cancellationToken);
}
