using AdsPush;
using AdsPush.Abstraction;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Features.PushNotification;

/// <summary>
/// Sends to a device token that does not exist. The provider checks the credentials before it looks at the token, so
/// a token error means the credentials are fine, and nothing is delivered. APNs' sandbox answers so for the token of a
/// TestFlight or App Store install too, so it is reported on its own outside Development (See Program.Services.cs).
/// </summary>
public class PushNotificationHealthCheck(IAdsPushSender adsPushSender, AdsPushTarget target, bool apnsSandboxOutsideDevelopment = false) : IHealthCheck
{
    // Shaped like an APNs token (64 hex characters). To Firebase it is simply a token it does not know.
    private const string NonExistentDeviceToken = "0000000000000000000000000000000000000000000000000000000000000000";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (apnsSandboxOutsideDevelopment)
            return HealthCheckResult.Degraded("AdsPushAPNS:EnvironmentType is Development, APNs' sandbox, from which TestFlight and App Store installs get no push. Set it to Production.");

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
