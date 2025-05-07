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
        var result = await allUserClaimsQuery.FirstOrDefaultAsync(uc => uc.ClaimType == claimType, cancellationToken);

        if (result?.ClaimValue is null)
            return default;

        try
        {
            Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T)Convert.ChangeType(result.ClaimValue, targetType, CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to convert claim value '{result.ClaimValue}' to type {typeof(T).Name}.", ex);
        }
    }
}
