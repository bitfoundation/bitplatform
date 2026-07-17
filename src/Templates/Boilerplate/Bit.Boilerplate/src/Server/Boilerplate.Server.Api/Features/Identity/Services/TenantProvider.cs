using Boilerplate.Server.Api.Features.Tenants;

namespace Boilerplate.Server.Api.Features.Identity.Services;

/// <summary>
/// This scoped service resolves the tenant of the current request in the following order:
/// 1. The <see cref="AppClaimTypes.TENANT_ID"/> claim of the signed-in user.
/// 2. The tenant whose name is equal to the sub domain of the current request (For anonymous requests such as the sales module's product pages).
/// 3. The tenant id that has been set explicitly by <see cref="SetCurrentTenantId(Guid)"/> (Useful in background jobs etc).
/// 4. The default tenant created by <see cref="TenantConfiguration"/> as fallback.
/// </summary>
public partial class TenantProvider
{
    /// <summary>
    /// Gets invalidated whenever a tenant gets created/updated (See TenantController and TenantManagementController).
    /// </summary>
    public const string TENANTS_CACHE_KEY = "Tenants_IdsByName";

    [AutoInject] private IFusionCache fusionCache = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;
    [AutoInject] private IDbContextFactory<AppDbContext> dbContextFactory = default!;

    private Guid? tenantId;

    public void SetCurrentTenantId(Guid tenantId)
    {
        this.tenantId = tenantId;
    }

    /// <summary>
    /// Note: The lack of async here wouldn't hurt that much, because it needs no I/O at all for signed-in users,
    /// and for anonymous requests the tenants are read from the in-memory (L1) cache instead of redis, because of the fusion cache's hybrid setup.
    /// </summary>
    public Guid GetCurrentTenantId()
    {
        var httpContext = httpContextAccessor.HttpContext;

        // Background jobs have no HttpContext: only the explicitly-set tenant is available, otherwise fail closed.
        if (httpContext is null)
            return tenantId ?? throw new InvalidOperationException("Inside background jobs, either set the TenantId explicitly or Use IgnoreQueryFilters");

        var user = httpContext.User;

        // 1. The signed-in user's tenant claim. This is the trusted source and is evaluated BEFORE the explicit
        // SetCurrentTenantId value, so the (public) setter can never override an authenticated user's tenant within a request.
        if (user.IsAuthenticated() && user.GetTenantId() is Guid claimTenantId)
            return claimTenantId;

        // Protecting endpoints by AuthPolicies.TENANT_SELECTED would guarantee that the tenant id is always present in the claims, so this code would never be reached for these endpoints.

        // 2. The tenant whose name matches the request sub domain (for anonymous requests such as the sales pages).
        var host = httpContext.Request.GetWebAppUrl().Host;

        if (string.IsNullOrEmpty(host) is false && host.Split('.') is { Length: > 2 } hostSegments)
        {
            var subDomain = hostSegments[0];

            if (GetTenantIdsByName().TryGetValue(subDomain, out var subDomainTenantId))
                return subDomainTenantId;
        }

        // 3. A tenant id set explicitly via SetCurrentTenantId (rarely used within a request scope).
        if (tenantId is not null)
            return tenantId.Value;

        // 4. The default tenant as fallback. You've to implement your custom business here depending on your requirements.
        return TenantConfiguration.FallbackTenantId;
    }

    private Dictionary<string, Guid> GetTenantIdsByName()
    {
        return fusionCache.GetOrSet(TENANTS_CACHE_KEY, _ =>
        {
            using var dbContext = dbContextFactory.CreateDbContext();
#pragma warning disable NonAsyncEFCoreMethodsUsageAnalyzer
            // Inactive tenants are excluded, so their sub domains no longer serve their data to anonymous requests.
            return dbContext.Tenants.Where(t => t.IsActive).ToDictionary(t => t.Name!, t => t.Id, StringComparer.OrdinalIgnoreCase);
#pragma warning restore NonAsyncEFCoreMethodsUsageAnalyzer
        },
        options => options.Duration = TimeSpan.FromHours(1));
    }
}
