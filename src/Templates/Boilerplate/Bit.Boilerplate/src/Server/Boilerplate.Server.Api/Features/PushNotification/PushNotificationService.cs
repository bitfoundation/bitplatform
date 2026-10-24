//+:cnd:noEmit
using AdsPush.Vapid;
using System.Linq.Expressions;

using Boilerplate.Shared.Features.PushNotification;

namespace Boilerplate.Server.Api.Features.PushNotification;

public partial class PushNotificationService
{
    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private TimeProvider timeProvider = default!;
    //#if (multitenant == true)
    [AutoInject] private TenantProvider tenantProvider = default!;
    //#endif
    [AutoInject] private ServerApiSettings serverApiSettings = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;
    [AutoInject] private IBackgroundJobClient backgroundJobClient = default!;

    public async Task Subscribe([Required] PushNotificationSubscriptionDto dto, CancellationToken cancellationToken)
    {
        List<string> tags = [CultureInfo.CurrentUICulture.Name /* To send push notification to all users with specific culture */];

        var userSessionId = httpContextAccessor.HttpContext!.User.IsAuthenticated() ? httpContextAccessor.HttpContext.User.GetSessionId() : (Guid?)null;

        // PushChannel is [Required] on the entity, so it is a NOT NULL column. Browsers never send one - the server
        // derives it from the web push triple below - while every other platform carries the device token in it, and
        // VapidSubscription.FromParameters happily accepts nulls rather than complaining. So without this check a body
        // missing its own platform's field either reaches SaveChangesAsync and comes back as a 500 + LogCritical from
        // an anonymous endpoint, or silently stores a subscription that can never be delivered to.
        var pushChannelIsMissing = dto.Platform is "browser"
            ? string.IsNullOrWhiteSpace(dto.Endpoint) || string.IsNullOrWhiteSpace(dto.P256dh) || string.IsNullOrWhiteSpace(dto.Auth)
            : string.IsNullOrWhiteSpace(dto.PushChannel);

        if (pushChannelIsMissing)
            throw new BadRequestException().WithData("Reason", $"A '{dto.Platform}' push subscription is missing its push channel.");

        if (dto.Platform is "browser" && IsPushServiceEndpoint(dto.Endpoint) is false)
            throw new BadRequestException().WithData("Reason", "A browser push subscription's endpoint is not an https address of a known push service.");

        // Each account signed in on the device has a row of its own, plus at most one row for nobody. The caller's
        // session row may still carry the DeviceId the device had before pushManager's subscription was renewed.
        var ownRow = userSessionId is null
            ? null
            : await dbContext.PushNotificationSubscriptions.FirstOrDefaultAsync(s => s.UserSessionId == userSessionId, cancellationToken);

        var previousDeviceId = ownRow?.DeviceId ?? dto.DeviceId;

        var matches = await dbContext.PushNotificationSubscriptions
            .Where(s => s.DeviceId == dto.DeviceId || s.DeviceId == previousDeviceId)
            .ToArrayAsync(cancellationToken);

        // The caller's own row wins; failing that, the device's row for nobody is taken over.
        var subscription = ownRow ?? matches.FirstOrDefault(s => s.UserSessionId == null);

        // ─── READ THIS BEFORE ADDING AN OWNERSHIP CHECK HERE ────────────────────────────────────────────────────────
        // There is deliberately NO "does this row belong to the caller" check, even though the endpoint is anonymous
        // and the DeviceId is caller supplied. Whoever presents a DeviceId gets that device's row, full stop:
        //
        // 1. The DeviceId IS the device's credential, so there is nothing to authorize. On the web it is built as
        //    `${p256dh}-${auth}` (See App.ts) - the Web Push public key plus the auth secret that payloads are
        //    encrypted with; on iOS / MacCatalyst it is UIDevice.IdentifierForVendor. It is not guessable, not
        //    enumerable, and never leaves the device except inside this request body. Anybody able to supply someone
        //    else's DeviceId already holds that subscription's encryption secret, which is strictly worse than
        //    anything this endpoint could hand them.
        // 2. A check here rejects the legitimate device far more often than an attacker. Clearing local storage, an
        //    expired token pair, or a browser that wipes site data does not delete the server side UserSession row, so
        //    the device's own row keeps pointing at a session whose tokens the user no longer has. The very next auth
        //    state propagation - the anonymous one first, then the authenticated one after signing in again - presents
        //    that same DeviceId, and an ownership check refuses both. The device then never receives another push, and
        //    nothing anywhere says why.
        // 3. A device is shared. The next person to sign in on it is the person its push notifications belong to, and
        //    the previous user's sign out is not guaranteed to have happened.
        //
        // ────────────────────────────────────────────────────────────────────────────────────────────────────────────

        subscription ??= (await dbContext.PushNotificationSubscriptions.AddAsync(new()
        {
            DeviceId = dto.DeviceId,
            Platform = dto.Platform
        }, cancellationToken)).Entity;

        dto.Patch(subscription);

        subscription.Tags = [.. tags];
        subscription.UserSessionId = userSessionId;
        //#if (multitenant == true)
        subscription.TenantId = tenantProvider.GetCurrentTenantId();
        //#endif

        // Kept on the row rather than read through UserSession, which an anonymous visitor has none of.
        // Relying on Cloudflare cdn to retrieve address, same as UserSession's.
        // https://developers.cloudflare.com/rules/transform/managed-transforms/reference/#add-visitor-location-headers
        var request = httpContextAccessor.HttpContext!.Request;
        subscription.IP = httpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString();
        subscription.Address = $"{request.Headers["cf-ipcountry"]}, {request.Headers["cf-ipcity"]}";
        subscription.AppVersionCode = AppVersionCodes.TryEncode(request.Headers["X-App-Version"].FirstOrDefault());

        if (subscription.Platform is "browser")
        {
            subscription.PushChannel = VapidSubscription.FromParameters(subscription.Endpoint, subscription.P256dh, subscription.Auth).ToAdsPushToken();
        }

        var now = timeProvider.GetUtcNow();

        subscription.RenewedOn = now.ToUnixTimeSeconds();
        subscription.ExpirationTime = now.AddMonths(1).ToUnixTimeSeconds();

        // The device's other rows follow its new channel, except those of accounts the device says it no longer has.
        foreach (var otherRow in matches.Where(s => ReferenceEquals(s, subscription) is false))
        {
            if (otherRow.UserSessionId is not Guid otherSessionId || dto.UserSessionIds.Contains(otherSessionId) is false)
            {
                dbContext.PushNotificationSubscriptions.Remove(otherRow);
                continue;
            }

            otherRow.DeviceId = subscription.DeviceId;
            otherRow.Platform = subscription.Platform;
            otherRow.PushChannel = subscription.PushChannel;
            otherRow.P256dh = subscription.P256dh;
            otherRow.Auth = subscription.Auth;
            otherRow.Endpoint = subscription.Endpoint;
            otherRow.Tags = [.. subscription.Tags];
            otherRow.IP = subscription.IP;
            otherRow.Address = subscription.Address;
            otherRow.AppVersionCode = subscription.AppVersionCode;
            otherRow.RenewedOn = subscription.RenewedOn;
            otherRow.ExpirationTime = subscription.ExpirationTime;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Unsubscribe(string deviceId, CancellationToken cancellationToken)
    {
        // The same "DeviceId IS the device's credential" model as Subscribe (read the comment there): whoever presents
        // a DeviceId gets that device's subscription removed, signed in or not.
        await dbContext.PushNotificationSubscriptions
            .Where(s => s.DeviceId == deviceId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <returns>How many subscriptions this push was queued for, so a caller may report what it actually reached.</returns>
    public async Task<int> RequestPush(PushNotificationRequest request,
        Expression<Func<PushNotificationSubscription, bool>>? customSubscriptionFilter = null,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().ToUnixTimeSeconds();

        // userRelatedPush: it's not practical to send a push notification carrying sensitive information, like an OTP
        // code, to a device the user hasn't opened the app on for longer than Identity:RefreshTokenExpiration (14 days
        // by default). Even if she opens the app, her session has expired and she is signed out right away.
        // Same window, same setting as UserSessionsRetentionJobRunner, which deletes those sessions.

        var query = dbContext.PushNotificationSubscriptions
            .Where(sub => sub.ExpirationTime > now)
            .Where(sub => sub.UserSessionId == null || sub.UserSession!.NotificationStatus == UserSessionNotificationStatus.Allowed)
            .WhereIf(customSubscriptionFilter is not null, customSubscriptionFilter!)
            .WhereIf(request.UserRelatedPush is true, sub => (now - sub.RenewedOn) < serverApiSettings.Identity.RefreshTokenExpiration.TotalSeconds);

        //#if (multitenant == true)
        if (customSubscriptionFilter is null)
        {
            var tenantId = tenantProvider.GetCurrentTenantId();
            query = query.Where(sub => sub.UserSessionId != null
                ? sub.UserSession!.TenantId == tenantId
                : sub.TenantId == tenantId);
        }
        //#endif

        var idsQuery = query.GroupBy(sub => sub.DeviceId).Select(device => device.Max(sub => sub.Id));

        if (customSubscriptionFilter is null)
        {
            idsQuery = idsQuery.OrderBy(_ => EF.Functions.Random()).Take(100);
        }

        var pushNotificationSubscriptionIds = await idsQuery.ToArrayAsync(cancellationToken);

        if (pushNotificationSubscriptionIds.Length is 0)
            return 0;

        backgroundJobClient.Enqueue<PushNotificationJobRunner>(runner => runner.RequestPush(pushNotificationSubscriptionIds, request));

        return pushNotificationSubscriptionIds.Length;
    }

    private static readonly string[] pushServiceHosts = ["fcm.googleapis.com", "push.services.mozilla.com", "notify.windows.com", "web.push.apple.com"];

    private static bool IsPushServiceEndpoint(string? endpoint)
    {
        return Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && uri.IsDefaultPort
            && pushServiceHosts.Any(host => uri.IdnHost.Equals(host, StringComparison.OrdinalIgnoreCase)
                                         || uri.IdnHost.EndsWith($".{host}", StringComparison.OrdinalIgnoreCase));
    }
}

public class PushNotificationRequest
{
    public string? Title { get; set; }
    public string? Message { get; set; }
    public string? Action { get; set; }
    public string? PageUrl { get; set; }

    public bool UserRelatedPush { get; set; }
    //#if (signalR == true)
    public Guid? RequesterUserSessionId { get; set; }
    //#endif
}
