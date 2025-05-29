using AdsPush.Vapid;
using System.Linq.Expressions;
using Boilerplate.Server.Api.Services.Jobs;
using Boilerplate.Shared.Dtos.PushNotification;
using Boilerplate.Server.Api.Models.PushNotification;

namespace Boilerplate.Server.Api.Services;

public partial class PushNotificationService
{
    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private ServerApiSettings serverApiSettings = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;
    [AutoInject] private IBackgroundJobClient backgroundJobClient = default!;

    public async Task Subscribe([Required] PushNotificationSubscriptionDto dto, CancellationToken cancellationToken)
    {
        List<string> tags = [CultureInfo.CurrentUICulture.Name /* To send push notification to all users with specific culture */];

        var userSessionId = httpContextAccessor.HttpContext!.User.IsAuthenticated() ? httpContextAccessor.HttpContext.User.GetSessionId() : (Guid?)null;

        var subscription = await dbContext.PushNotificationSubscriptions
            .WhereIf(userSessionId is null, s => s.DeviceId == dto.DeviceId)
            .WhereIf(userSessionId is not null, s => s.UserSessionId == userSessionId || s.DeviceId == dto.DeviceId) // pushManager's subscription has been renewed.
            .FirstOrDefaultAsync(cancellationToken) ??

            (await dbContext.PushNotificationSubscriptions.AddAsync(new()
            {
                DeviceId = dto.DeviceId,
                Platform = dto.Platform
            }, cancellationToken)).Entity;

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

    public async Task RequestPush(string? title = null, 
        string? message = null,
        string? action = null,
        string? pageUrl = null,
        bool userRelatedPush = false,
        Expression<Func<PushNotificationSubscription, bool>>? customSubscriptionFilter = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // userRelatedPush: If the BearerTokenExpiration is 14 days, it's not practical to send push notifications 
        // with sensitive information, like an OTP code to a device where the user hasn't used the app for over 14 days.  
        // This is because, even if the user opens the app, they will be automatically signed out as their session has expired.  

        var query = dbContext.PushNotificationSubscriptions
            .Where(sub => sub.ExpirationTime > now)
            .Where(sub => sub.UserSessionId == null || sub.UserSession!.NotificationStatus == UserSessionNotificationStatus.Allowed)
            .WhereIf(customSubscriptionFilter is not null, customSubscriptionFilter!)
            .WhereIf(userRelatedPush is true, sub => (now - sub.RenewedOn) < serverApiSettings.Identity.RefreshTokenExpiration.TotalSeconds);

        if (customSubscriptionFilter is null)
        {
            query = query.OrderBy(_ => EF.Functions.Random()).Take(100);
        }

        var pushNotificationSubscriptionIds = await query.Select(pns => pns.Id).ToArrayAsync(cancellationToken);

        backgroundJobClient.Enqueue<PushNotificationJobRunner>(runner => runner.RequestPush(pushNotificationSubscriptionIds, title, message, action, pageUrl, userRelatedPush, default));
    }
}
