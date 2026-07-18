//+:cnd:noEmit
namespace Boilerplate.Server.Api.Features.Identity.Services;

public partial class UserClaimsService
{
    private Dictionary<Guid, Claim[]> _cachedClaims = [];

    [AutoInject]
    private AppDbContext dbContext = default!;

    //#if (multitenant == true)
    [AutoInject]
    private IHttpContextAccessor httpContextAccessor = default!;
    //#endif

    /// <summary>
    /// Returns all claim values of a specific type for a user, including those inherited from roles.
    /// </summary>
    public async Task<T?[]> GetClaimValues<T>(Guid userId, string claimType, CancellationToken cancellationToken)
    {
        var results = (await GetClaims(userId, cancellationToken))
            .Where(uc => uc.Type == claimType)
            .Select(uc => uc.Value)
            .ToArray();

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
    public async Task<T?> GetClaimValue<T>(Guid userId, string claimType, CancellationToken cancellationToken)
    {
        return (await GetClaimValues<T>(userId, claimType, cancellationToken)).Max();
    }

    /// <summary>
    /// Loads all user claims, role claims and role names for a user in a single query that gets cached in-memory for current request lifetime.
    /// There's no need for complex caching here as the service is not being called in parallel.
    /// </summary>
    public async Task<Claim[]> GetClaims(Guid userId, CancellationToken cancellationToken)
    {
        if (_cachedClaims.TryGetValue(userId, out var cachedClaims))
            return cachedClaims;

        //#if (multitenant == true)
        Guid tenantId = httpContextAccessor.HttpContext!.Items.ContainsKey(AppClaimTypes.TENANT_ID) ?
            (Guid)httpContextAccessor.HttpContext!.Items[AppClaimTypes.TENANT_ID]! : Guid.Empty;

        var userClaimsQuery = dbContext.UserClaims.Where(uc => uc.UserId == userId)
            .Where(uc => uc.TenantId == null || uc.TenantId == tenantId)
            .Select(uc => new { ClaimType = uc.ClaimType!, ClaimValue = uc.ClaimValue! });

        var userRolesQuery = dbContext.Roles.Where(role => role.Users.Any(ur => ur.UserId == userId))
            .Where(role => role.TenantId == null || role.TenantId == tenantId);
        //#endif
        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
        //#if (multitenant != true)
        var userClaimsQuery = dbContext.UserClaims.Where(uc => uc.UserId == userId).Select(uc => new { ClaimType = uc.ClaimType!, ClaimValue = uc.ClaimValue! });

        var userRolesQuery = dbContext.Roles.Where(role => role.Users.Any(ur => ur.UserId == userId));
        //#endif
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif

        var userRoleClaimsQuery = userRolesQuery.SelectMany(r => r.Claims.Select(rc => new { ClaimType = rc.ClaimType!, ClaimValue = rc.ClaimValue! }));

        var roleClaimQuery = userRolesQuery.Select(role => new { ClaimType = ClaimTypes.Role, ClaimValue = role.Name! });

        var allUserClaimsQuery = userClaimsQuery.Union(userRoleClaimsQuery).Union(roleClaimQuery).TagWith("Get claims");

        _cachedClaims.Add(userId, await allUserClaimsQuery.Select(uc => new Claim(uc.ClaimType!, uc.ClaimValue!)).ToArrayAsync(cancellationToken));

        return _cachedClaims[userId];
    }
}
