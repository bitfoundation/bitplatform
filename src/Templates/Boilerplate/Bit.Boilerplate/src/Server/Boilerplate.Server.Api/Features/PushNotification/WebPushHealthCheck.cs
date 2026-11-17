using System.Net;
using System.Buffers.Text;
using System.Security.Cryptography;
using AdsPush;
using AdsPush.Abstraction;
using AdsPush.Vapid;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Features.PushNotification;

public partial class WebPushHealthCheck : IHealthCheck
{
    private const string NonExistentSubscriptionEndpoint = "https://fcm.googleapis.com/fcm/send/cX9aE0fakeToken0:APA91bHealthCheckFakeTokenThatDoesNotExist0000000000000000000000000000000000000000000000000000000000000000";

    [AutoInject] private IAdsPushSender adsPushSender = default!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var subscriberKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var point = subscriberKey.ExportParameters(includePrivateParameters: false).Q;
        var subscription = VapidSubscription.FromParameters(NonExistentSubscriptionEndpoint,
            Base64Url.EncodeToString([0x04, .. point.X!, .. point.Y!]), Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(16)));

        var payload = new AdsPushBasicSendPayload
        {
            Title = AdsPushText.CreateUsingString("Health check"),
            Detail = AdsPushText.CreateUsingString("Health check")
        };

        try
        {
            await adsPushSender.BasicSendAsync(AdsPushTarget.BrowserAndPwa, subscription.ToAdsPushToken(), payload, cancellationToken);
        }
        catch (AdsPushException exp) when (exp.HttpResponse?.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound)
        {
            return HealthCheckResult.Healthy("FCM accepts the VAPID signature");
        }
        catch (AdsPushException exp) when (exp.HttpResponse is not null)
        {
            var answer = (await exp.HttpResponse.Content.ReadAsStringAsync(cancellationToken)).Trim();
            return new HealthCheckResult(context.Registration.FailureStatus,
                $"FCM answered {(int)exp.HttpResponse.StatusCode} {exp.HttpResponse.StatusCode}{(answer is { Length: > 0 and <= 200 } && answer.StartsWith('<') is false ? $": {answer}" : string.Empty)}", exp);
        }
        catch (ArgumentException exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, $"AdsPushVapid is invalid: {exp.Message}", exp);
        }
        catch (Exception exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "FCM is unreachable", exp);
        }

        return HealthCheckResult.Healthy("FCM accepts the VAPID signature");
    }
}
