using AdsPush;
using AdsPush.Vapid;
using AdsPush.Abstraction;
using System.Linq.Expressions;
using System.Collections.Concurrent;
using Boilerplate.Shared.Dtos.PushNotification;
using Boilerplate.Server.Api.Models.PushNotification;

namespace Boilerplate.Server.Api.Services;

public partial class PushNotificationService
{
    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private ServerApiSettings serverApiSettings = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;
    [AutoInject] private RootServiceScopeProvider rootServiceScopeProvider = default!;

    public async Task Subscribe([Required] PushNotificationSubscriptionDto dto, CancellationToken cancellationToken)
    {
        List<string> tags = [CultureInfo.CurrentUICulture.Name /* To send push notification to all users with specific culture */];

        var userSessionId = httpContextAccessor.HttpContext!.User.IsAuthenticated() ? httpContextAccessor.HttpContext.User.GetSessionId() : (Guid?)null;

        await dbContext.PushNotificationSubscriptions
            .Where(s => s.DeviceId == dto.DeviceId || s.UserSessionId == userSessionId /* pushManager's subscription has been renewed. */)
            .ExecuteDeleteAsync(cancellationToken);

        var subscription = dbContext.PushNotificationSubscriptions.Add(new()
        {
            DeviceId = dto.DeviceId,
            Platform = dto.Platform
        }).Entity;

        dto.Patch(subscription);

        subscription.Tags = [.. tags];
        subscription.UserSessionId = userSessionId;
        subscription.RenewedOn = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        subscription.ExpirationTime = DateTimeOffset.UtcNow.AddMonths(1).ToUnixTimeSeconds();

        if (subscription.Platform is "browser")
        {
            subscription.PushChannel = VapidSubscription.FromParameters(subscription.Endpoint, subscription.P256dh, subscription.Auth).ToAdsPushToken();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Unsubscribe(string deviceId, CancellationToken cancellationToken)
    {
        dbContext.PushNotificationSubscriptions.Remove(new() { DeviceId = deviceId });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RequestPush(string? title = null, string? message = null, string? action = null,
        bool userRelatedPush = false,
        Expression<Func<PushNotificationSubscription, bool>>? customSubscriptionFilter = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // userRelatedPush: If the BearerTokenExpiration is 14 days, it’s not practical to send push notifications 
        // with sensitive information, like an OTP code to a device where the user hasn't used the app for over 14 days.  
        // This is because, even if the user opens the app, they will be automatically signed out as their session has expired.  

        var query = dbContext.PushNotificationSubscriptions
            .Where(sub => sub.ExpirationTime > now)
            .WhereIf(customSubscriptionFilter is not null, customSubscriptionFilter!)
            .WhereIf(userRelatedPush is true, sub => (now - sub.RenewedOn) < serverApiSettings.Identity.BearerTokenExpiration.TotalSeconds);

        if (customSubscriptionFilter is null)
        {
            query = query.OrderBy(_ => EF.Functions.Random()).Take(100);
        }

        var subscriptions = await query.ToListAsync(cancellationToken);

        _ = Task.Run(async () => // Let's not wait for the push notification to be sent. Consider using a proper message queue or background job system like Hangfire.
        {
            await using var scope = rootServiceScopeProvider.Invoke();
            var adsPushSender = scope.ServiceProvider.GetRequiredService<IAdsPushSender>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<PushNotificationService>>();

            var payload = new AdsPushBasicSendPayload()
            {
                Title = AdsPushText.CreateUsingString(title ?? "Boilerplate push"),
                Detail = AdsPushText.CreateUsingString(message ?? string.Empty),
                Parameters = new Dictionary<string, object>()
                {
                    {
                        "action", action ?? string.Empty
                    }
                }
            };

            ConcurrentBag<Exception> exceptions = [];

            await Parallel.ForEachAsync(subscriptions, parallelOptions: new()
            {
                MaxDegreeOfParallelism = 10,
                CancellationToken = default
            }, async (subscription, _) =>
            {
                try
                {
                    var target = subscription.Platform is "browser" ? AdsPushTarget.BrowserAndPwa
                                        : subscription.Platform is "fcmV1" ? AdsPushTarget.Android
                                        : subscription.Platform is "apns" ? AdsPushTarget.Ios
                                        : throw new NotImplementedException();

                    await adsPushSender.BasicSendAsync(target, subscription.PushChannel, payload, default);
                }
                catch (Exception exp)
                {
                    exceptions.Add(exp);
                }
            });

            if (exceptions.IsEmpty is false)
            {
                logger.LogError(new AggregateException(exceptions), "Failed to send push notifications");
            }

        }, default);
    }
}
