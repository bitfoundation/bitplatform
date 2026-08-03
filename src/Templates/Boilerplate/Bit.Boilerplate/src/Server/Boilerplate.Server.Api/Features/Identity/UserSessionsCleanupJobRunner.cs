//+:cnd:noEmit
namespace Boilerplate.Server.Api.Features.Identity;

/// <summary>
/// Nothing else in the app ever deletes a <c>UserSession</c> except an explicit user action (sign out, revoke,
/// account or tenant deletion), so the table only grows: every sign-in from every device that was simply closed
/// leaves a row behind forever. Those rows are already dead - a session whose refresh token has expired can never
/// be renewed - but they still get scanned by every session query, listed in the user's own "active sessions"
/// page, and counted against <c>MaxPrivilegedSessionsCount</c>.
/// </summary>
public partial class UserSessionsCleanupJobRunner
{
    public const string RecurringJobId = nameof(UserSessionsCleanupJobRunner);

    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private TimeProvider timeProvider = default!;
    [AutoInject] private ServerApiSettings serverApiSettings = default!;
    [AutoInject] private ILogger<UserSessionsCleanupJobRunner> logger = default!;

    /// <summary>
    /// Deletes every session whose refresh token can no longer be redeemed, i.e. one that has neither been renewed
    /// nor started within <c>Identity:RefreshTokenExpiration</c>. That is exactly the window
    /// <c>AppJwtSecureDataFormat</c> honours, so a row this removes could not have produced a working token anyway.
    /// </summary>
    public async Task CleanupExpiredSessions(CancellationToken cancellationToken)
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
