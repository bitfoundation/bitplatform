//+:cnd:noEmit
using System.Text.Json;
using System.Text.Json.Nodes;
using Boilerplate.Server.Api.Features.PersonalData;

namespace Boilerplate.Server.Api.Features.PushNotification;

/// <summary>
/// The devices this account can be pushed to, found through the session that registered them.
/// </summary>
public partial class PushNotificationsPersonalDataSource : IPersonalDataSource
{
    [AutoInject] private AppDbContext dbContext = default!;

    public string Key => "pushSubscriptions";

    public int Order => 40;

    /// <summary>
    /// Ahead of the sessions: the foreign key to <c>UserSession</c> is <c>SetNull</c>, so deleting them first leaves a
    /// row with nothing to find it by - and <c>RequestPush</c> reads a null <c>UserSessionId</c> as an anonymous
    /// visitor, so the orphan stays in the audience.
    /// </summary>
    public int ErasureOrder => 10;

    public string Purpose => "Delivering push notifications to the devices you signed in on.";

    public string Retention => "Until the subscription expires, one month after it was last renewed, then deleted by a daily job. Deleting your account removes it immediately.";

    public string[] Recipients =>
    [
        "Firebase Cloud Messaging (Google) and Apple Push Notification service - the platform services that carry a notification to the device."
    ];

    public string? Notes => "The encryption keys that let this application push to your browser are not listed: they are credentials for the subscription, not facts about you.";

    public PersonalDataErasure Erasure => PersonalDataErasure.ErasureService;

    public async Task<JsonNode?> Export(Guid userId, CancellationToken cancellationToken)
    {
        // Materialised before mapping: ExpirationTime and RenewedOn are unix seconds.
        var subscriptions = await dbContext.PushNotificationSubscriptions
            .AsNoTracking()
            .Where(subscription => dbContext.UserSessions.Any(userSession => userSession.Id == subscription.UserSessionId && userSession.UserId == userId))
            .Select(subscription => new
            {
                subscription.DeviceId,
                subscription.Platform,
                subscription.Endpoint,
                subscription.Tags,
                subscription.ExpirationTime,
                subscription.RenewedOn,
                subscription.UserSessionId
            })
            .ToArrayAsync(cancellationToken);

        var export = subscriptions.Select(subscription => new
        {
            subscription.DeviceId,
            subscription.Platform,
            // The address the push service delivers to for this device.
            subscription.Endpoint,
            subscription.Tags,
            ExpiresOn = DateTimeOffset.FromUnixTimeSeconds(subscription.ExpirationTime),
            RenewedOn = DateTimeOffset.FromUnixTimeSeconds(subscription.RenewedOn),
            // The session that registered this device - matches an entry in the sessions section.
            subscription.UserSessionId
        });

        return JsonSerializer.SerializeToNode(export, IPersonalDataSource.SerializerOptions);
    }

    public async Task Erase(PersonalDataErasureContext context, CancellationToken cancellationToken)
    {
        // An EXISTS subquery rather than a navigation, which ExecuteDelete translates differently per provider.
        await dbContext.PushNotificationSubscriptions
            .Where(subscription => dbContext.UserSessions.Any(userSession => userSession.Id == subscription.UserSessionId && userSession.UserId == context.UserId))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
