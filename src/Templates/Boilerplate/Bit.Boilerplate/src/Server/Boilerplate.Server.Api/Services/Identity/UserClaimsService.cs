namespace Boilerplate.Server.Api.Services.Identity;

public partial class UserClaimsService
{
    [AutoInject]
    private AppDbContext dbContext = default!;

    /// <summary>
    /// Returns all claim values of a specific type for a user, including those inherited from roles.
    /// </summary>
    public async Task<T?[]> GetUserClaimValues<T>(Guid userId, string claimType, CancellationToken cancellationToken)
    {
        var userClaimsQuery = dbContext.UserClaims.Where(uc => uc.UserId == userId).Select(uc => new { uc.ClaimType, uc.ClaimValue });
        var userRoleClaimsQuery = dbContext.UserRoles.Where(ur => ur.UserId == userId).SelectMany(ur => ur.Role!.Claims).Select(uc => new { uc.ClaimType, uc.ClaimValue });
        var allUserClaimsQuery = userClaimsQuery.Union(userRoleClaimsQuery).TagWith($"Finding {claimType} claim for {userId}");

        var results = await allUserClaimsQuery
            .Where(uc => uc.ClaimType == claimType)
            .Select(uc => uc.ClaimValue)
            .ToArrayAsync(cancellationToken);

        if (results.Any() is false)
            return [];

        try
        {
            Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return [.. results.Select(r => (T)Convert.ChangeType(r, targetType, CultureInfo.InvariantCulture)!)];
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to convert claim values for {claimType} to type {typeof(T).Name}.", ex);
        }
    }

    /// <summary>
    /// Returns claim value of a specific type for a user, including those inherited from roles.
    /// User might have multiple claims of the same type because of her roles or directly assigned user claims, so we return the maximum value
    /// </summary>
    public async Task<T?> GetUserClaimValue<T>(Guid userId, string claimType, CancellationToken cancellationToken)
    {
        return (await GetUserClaimValues<T>(userId, claimType, cancellationToken)).Max();
    }
}
