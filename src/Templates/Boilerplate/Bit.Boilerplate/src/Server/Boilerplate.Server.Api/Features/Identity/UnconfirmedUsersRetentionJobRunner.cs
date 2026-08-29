//+:cnd:noEmit
namespace Boilerplate.Server.Api.Features.Identity;

/// <summary>
/// Deletes accounts that were auto-provisioned by <c>SignIn</c> / <c>SendOtp</c> and never confirmed. Anyone can post a
/// stranger's e-mail or phone to those anonymous endpoints, so without this the row is kept forever with no lawful
/// basis for holding it.
/// </summary>
public partial class UnconfirmedUsersRetentionJobRunner
{
    public const string RecurringJobId = nameof(UnconfirmedUsersRetentionJobRunner);

    /// <summary>Bounds the work of one run; the job is daily, so a backlog drains over successive runs.</summary>
    private const int MaxDeletionsPerRun = 500;

    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private TimeProvider timeProvider = default!;
    [AutoInject] private UserErasureService userErasureService = default!;
    [AutoInject] private ServerApiSettings serverApiSettings = default!;
    [AutoInject] private ILogger<UnconfirmedUsersRetentionJobRunner> logger = default!;

    public async Task EnforceRetention(CancellationToken cancellationToken)
    {
        var retention = serverApiSettings.Identity.UnconfirmedUsersRetention;

        // Zero would delete every account the moment it is created; a misconfiguration, not a policy.
        if (retention <= TimeSpan.Zero)
            throw new InvalidOperationException($"{nameof(AppIdentityOptions.UnconfirmedUsersRetention)} must be greater than zero.");

        var createdBefore = timeProvider.GetUtcNow() - retention;

        // The three conditions mirror AppUserConfirmation.IsConfirmedAsync, plus "never signed in": a session means the
        // account is in use, and an external login is a confirmation in itself even with no e-mail or phone confirmed.
        var expiredUserIds = await dbContext.Users
            .Where(user => user.EmailConfirmed == false
                        && user.PhoneNumberConfirmed == false
                        && user.Logins.Any() == false
                        && user.Sessions.Any() == false
                        && user.CreatedOn < createdBefore)
            .OrderBy(user => user.CreatedOn)
            .Take(MaxDeletionsPerRun)
            .Select(user => user.Id)
            .ToArrayAsync(cancellationToken);

        if (expiredUserIds.Length is 0)
            return;

        var deletedCount = 0;

        // Through the erasure service rather than a bulk delete, so a store added there is covered here too.
        foreach (var userId in expiredUserIds)
        {
            try
            {
                await userErasureService.Erase(userId, exceptSessionId: null, cancellationToken);
                deletedCount++;
            }
            catch (ResourceNotFoundException)
            {
                // Another worker read the same batch and got there first; the rest of this one still has to run.
            }
        }

        if (deletedCount > 0)
        {
            logger.LogInformation("Deleted {DeletedCount} unconfirmed user(s).", deletedCount);
        }
    }
}
