using AdsPush;
using AdsPush.Abstraction;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Features.PushNotification;

/// <summary>
/// Sends to a device token that does not exist. The provider checks the credentials before it looks at the token, so
/// a token error means the credentials are fine, and nothing is delivered.
/// </summary>
public class PushNotificationHealthCheck(IAdsPushSender adsPushSender, AdsPushTarget target) : IHealthCheck
{
    // Shaped like an APNs token (64 hex characters). To Firebase it is simply a token it does not know.
    private const string NonExistentDeviceToken = "0000000000000000000000000000000000000000000000000000000000000000";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // FCM rejects a malformed token as INVALID_ARGUMENT and an unknown one as UNREGISTERED.
        // APNs reports a wrong bundle id (topic) as InvalidArgument too, so only BadDeviceToken counts there.
        AdsPushErrorType[] expectedErrors = target is AdsPushTarget.Ios
            ? [AdsPushErrorType.InvalidToken]
            : [AdsPushErrorType.InvalidToken, AdsPushErrorType.InvalidArgument];

        var payload = new AdsPushBasicSendPayload
        {
            Title = AdsPushText.CreateUsingString("Health check"),
            Detail = AdsPushText.CreateUsingString("Health check")
        };

        try
        {
            await adsPushSender.BasicSendAsync(target, NonExistentDeviceToken, payload, cancellationToken);
        }
        catch (AdsPushException exp) when (expectedErrors.Contains(exp.ErrorType))
        {
            // The token was rejected, which is the expected outcome.
        }
        catch (Exception exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, $"{target} push notifications are unhealthy", exp);
        }

        return HealthCheckResult.Healthy($"{target} push notifications are healthy");
    }
}
