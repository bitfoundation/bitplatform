namespace Boilerplate.Server.Api.Services.Identity;

public partial class UserClaimsService
{
    [AutoInject]
    private AppDbContext dbContext = default!;

    public async Task<T?> GetUserClaimValue<T>(Guid userId, string claimType, CancellationToken cancellationToken)
    {
        var userClaimsQuery = dbContext.UserClaims.Where(uc => uc.UserId == userId).Select(uc => new { uc.ClaimType, uc.ClaimValue });
        var userRoleClaimsQuery = dbContext.UserRoles.Where(ur => ur.UserId == userId).SelectMany(ur => ur.Role!.Claims).Select(uc => new { uc.ClaimType, uc.ClaimValue });
        var allUserClaimsQuery = userClaimsQuery.Union(userRoleClaimsQuery).TagWith($"Finding {claimType} claim for {userId}");

        var results = await allUserClaimsQuery
            .Where(uc => uc.ClaimType == claimType)
            .Select(uc => uc.ClaimValue)
            .ToArrayAsync(cancellationToken);

        if (results.Any() is false)
            return default;

        try
        {
            Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return results.Select(r => (T)Convert.ChangeType(r, targetType, CultureInfo.InvariantCulture)!).Max(); // User might have multiple roles with this claim.
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to convert claim values for {claimType} to type {typeof(T).Name}.", ex);
        }
    }
}
