using Boilerplate.Server.Api.Features.Tenants;

namespace Boilerplate.Server.Api.Features.Identity.Services;

/// <summary>
/// This singleton service resolves the tenant of the current request in the following order:
/// 1. The <see cref="AppClaimTypes.TENANT_ID"/> claim of the signed-in user.
/// 2. The tenant whose custom <see cref="Tenant.Domain"/> equals the complete host of the current request (custom/vanity domains).
/// 3. The tenant whose name is equal to the sub domain of the current request (For anonymous requests such as the sales module's product pages).
/// 4. The default tenant created by <see cref="TenantConfiguration"/> as fallback.
/// </summary>
public partial class TenantProvider
{
    /// <summary>
    /// Gets invalidated whenever a tenant gets created/updated (See TenantController and TenantManagementController).
    /// </summary>
    public const string TENANTS_CACHE_KEY = "Tenants_Lookup";

    [AutoInject] private IFusionCache fusionCache = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;
    [AutoInject] private IDbContextFactory<AppDbContext> dbContextFactory = default!;

    /// <summary>
    /// Note: The lack of async here wouldn't hurt that much, because it needs no I/O at all for signed-in users,
    /// and for anonymous requests the tenants are read from the in-memory (L1) cache instead of redis, because of the fusion cache's hybrid setup.
    /// </summary>
    public Guid GetCurrentTenantId()
    {
        var httpContext = httpContextAccessor.HttpContext;

        // Background jobs have no HttpContext, so there's no tenant to resolve: fail closed instead of silently
        // serving the fallback tenant's data. Such jobs have to use IgnoreQueryFilters and scope the query by tenant themselves.
        if (httpContext is null)
            throw new InvalidOperationException("TenantProvider doesn't function inside background jobs, use IgnoreQueryFilters to prevent it from being called.");

        var user = httpContext.User;

        // 1. The signed-in user's tenant claim. This is the trusted source (the claim is server-issued) and takes
        // precedence over the host based resolution below, which is only meant for anonymous requests.
        if (user.IsAuthenticated() && user.GetTenantId() is Guid claimTenantId)
            return claimTenantId;

        // Protecting endpoints by AuthPolicies.TENANT_SELECTED would guarantee that the tenant id is always present in the claims, so this code would never be reached for these endpoints.

        var host = httpContext.Request.GetWebAppUrl().Host.ToLowerInvariant();

        if (string.IsNullOrEmpty(host) is false)
        {
            var tenants = GetTenantsLookup();

            // 2. The tenant whose custom domain matches the complete request host (takes precedence over the sub domain).
            if (tenants.IdsByDomain.TryGetValue(host, out var domainTenantId))
                return domainTenantId;

            // 3. The tenant whose name matches the request sub domain.
            if (host.Split('.') is { Length: > 2 } hostSegments
                && tenants.IdsByName.TryGetValue(hostSegments[0], out var subDomainTenantId))
                return subDomainTenantId;
        }

        // 4. The default tenant as fallback. You've to implement your custom business here depending on your requirements.
        return TenantConfiguration.FallbackTenantId;
    }

    private TenantsLookup GetTenantsLookup()
    {
        return fusionCache.GetOrSet(TENANTS_CACHE_KEY, _ =>
        {
            using var dbContext = dbContextFactory.CreateDbContext();
#pragma warning disable NonAsyncEFCoreMethodsUsageAnalyzer
            // Inactive tenants are excluded, so their sub domains / custom domains no longer serve their data to anonymous requests.
            var tenants = dbContext.Tenants.Where(t => t.IsActive).Select(t => new { t.Id, t.Name, t.Domain }).ToArray();
#pragma warning restore NonAsyncEFCoreMethodsUsageAnalyzer
            return new TenantsLookup(
                IdsByName: tenants.ToDictionary(t => t.Name!.ToLowerInvariant(), t => t.Id, StringComparer.OrdinalIgnoreCase),
                IdsByDomain: tenants.Where(t => string.IsNullOrEmpty(t.Domain) is false)
                                    .ToDictionary(t => t.Domain!.ToLowerInvariant(), t => t.Id, StringComparer.OrdinalIgnoreCase));
        },
        options => options.Duration = TimeSpan.FromHours(1));
    }

    /// <summary>
    /// The active tenants indexed for anonymous-request resolution: by <see cref="Tenant.Name"/> (sub domain) and by
    /// <see cref="Tenant.Domain"/> (complete host). Cached as a single entry under <see cref="TENANTS_CACHE_KEY"/>.
    /// </summary>
    private sealed record TenantsLookup(Dictionary<string, Guid> IdsByName, Dictionary<string, Guid> IdsByDomain);
}
