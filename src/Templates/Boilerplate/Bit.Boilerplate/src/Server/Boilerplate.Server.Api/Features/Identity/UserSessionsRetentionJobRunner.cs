//+:cnd:noEmit
namespace Boilerplate.Server.Api.Features.Identity;

/// <summary>
/// Deletes user sessions that can no longer be renewed. Nothing else removes one except an explicit user action, so
/// without this every sign-in from a device that was simply closed keeps its ip, city and device string forever.
/// </summary>
public partial class UserSessionsRetentionJobRunner
{
    public const string RecurringJobId = nameof(UserSessionsRetentionJobRunner);

    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private TimeProvider timeProvider = default!;
    [AutoInject] private ServerApiSettings serverApiSettings = default!;
    [AutoInject] private ILogger<UserSessionsRetentionJobRunner> logger = default!;

    /// <summary>
    /// The period is <c>Identity:RefreshTokenExpiration</c> rather than a number of its own: that is the window
    /// <c>AppJwtSecureDataFormat</c> honours, so a row this removes could not have produced a working token anyway.
    /// </summary>
    public async Task EnforceRetention(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().ToUnixTimeSeconds();
        var maxAge = (long)serverApiSettings.Identity.RefreshTokenExpiration.TotalSeconds;

        var deletedCount = await dbContext.UserSessions
            .Where(us => (now - (us.RenewedOn ?? us.StartedOn)) > maxAge)
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedCount > 0)
        {
            logger.LogInformation("Deleted {DeletedCount} expired user session(s).", deletedCount);
        }
    }
}
