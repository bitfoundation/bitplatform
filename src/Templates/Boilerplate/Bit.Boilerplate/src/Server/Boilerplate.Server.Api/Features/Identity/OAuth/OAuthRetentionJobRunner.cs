//+:cnd:noEmit
namespace Boilerplate.Server.Api.Features.Identity.OAuth;

/// <summary>
/// Deletes what the OAuth flow leaves behind: authorization codes well past their expiry. Nothing else removes one,
/// and a consent mints one every time.
/// </summary>
public partial class OAuthRetentionJobRunner
{
    public const string RecurringJobId = nameof(OAuthRetentionJobRunner);

    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private TimeProvider timeProvider = default!;
    [AutoInject] private ServerApiSettings serverApiSettings = default!;
    [AutoInject] private ILogger<OAuthRetentionJobRunner> logger = default!;

    public async Task EnforceRetention(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().ToUnixTimeSeconds();

        // Kept past expiry so a replay still has a session to revoke (OAuthService.ConsumeCode), but only for a day.
        var codesExpiredBefore = now - (long)TimeSpan.FromDays(1).TotalSeconds;

        var deletedCodes = await dbContext.OAuthAuthorizationCodes
            .Where(code => code.ExpiresOn < codesExpiredBefore)
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedCodes > 0)
        {
            logger.LogInformation("Deleted {DeletedCodes} expired authorization code(s).", deletedCodes);
        }
    }
}
