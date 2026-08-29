//+:cnd:noEmit
namespace Boilerplate.Server.Api.Features.PushNotification;

/// <summary>
/// Deletes push subscriptions past their <c>ExpirationTime</c>. <c>RequestPush</c> already filters those out of every
/// send, so the rows are dead - they only keep a device identifier and its Web Push keys on file indefinitely. A device
/// that comes back simply re-subscribes, which is what stamps a new expiry.
/// </summary>
public partial class PushSubscriptionsRetentionJobRunner
{
    public const string RecurringJobId = nameof(PushSubscriptionsRetentionJobRunner);

    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private TimeProvider timeProvider = default!;
    [AutoInject] private ILogger<PushSubscriptionsRetentionJobRunner> logger = default!;

    public async Task EnforceRetention(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().ToUnixTimeSeconds();

        var deletedCount = await dbContext.PushNotificationSubscriptions
            .Where(sub => sub.ExpirationTime < now)
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedCount > 0)
        {
            logger.LogInformation("Deleted {DeletedCount} expired push subscription(s).", deletedCount);
        }
    }
}
