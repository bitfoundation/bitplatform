using Boilerplate.Server.Api.Features.Identity.Configurations;

namespace Boilerplate.Server.Api.Infrastructure.Data;

public static class DeploymentGuards
{
    /// <summary>
    /// Stops a deployment while an account still has the password this repository publishes. The first deployment
    /// creates those accounts, so it has nothing to refuse, and every deployment after it does.
    /// </summary>
    public static async Task RefuseSeedPasswords(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if ((await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken)).Any() is false)
            return;

        var accounts = await dbContext.Users
            .IgnoreQueryFilters()
            .Where(user => user.PasswordHash == UserConfiguration.SeedPasswordHash)
            .Select(user => user.UserName)
            .ToArrayAsync(cancellationToken);

        if (accounts.Length > 0)
            throw new InvalidOperationException($"These accounts still have the password published in the repository, so anyone can sign in with them: {string.Join(", ", accounts)}. Change their passwords or delete them, then deploy again.");
    }
}
