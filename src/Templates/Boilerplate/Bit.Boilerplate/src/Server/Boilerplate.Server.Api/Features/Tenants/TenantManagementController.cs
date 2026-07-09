//+:cnd:noEmit
//#if (signalR == true)
using Microsoft.AspNetCore.SignalR;
using Boilerplate.Server.Api.Infrastructure.SignalR;
//#endif
using Boilerplate.Server.Api.Features.Identity.Services;
using Boilerplate.Shared.Features.Tenants;
using Boilerplate.Shared.Features.Tenants.Dtos;
using ZiggyCreatures.Caching.Fusion;

namespace Boilerplate.Server.Api.Features.Tenants;

/// <summary>
/// Lets global admins manage all tenants (See <see cref="TenantController"/> for managing the current tenant).
/// </summary>
[ApiVersion(1)]
[ApiController, Route("api/v{v:apiVersion}/[controller]/[action]"),
    Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS),
    Authorize(Policy = AppFeatures.Management.Tenants_Write_Global)]
public partial class TenantManagementController : AppControllerBase, ITenantManagementController
{
    //#if (signalR == true)
    [AutoInject] private IHubContext<AppHub> appHubContext = default!;
    //#endif
    [AutoInject] private IFusionCache fusionCache = default!;

    [HttpGet, EnableQuery]
    public IQueryable<TenantDto> Get()
    {
        return DbContext.Tenants
            .Project();
    }

    [HttpGet]
    public async Task<PagedResponse<TenantDto>> GetTenants(ODataQueryOptions<TenantDto> odataQuery, CancellationToken cancellationToken)
    {
        var query = (IQueryable<TenantDto>)odataQuery.ApplyTo(Get(), ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

        var totalCount = await query.LongCountAsync(cancellationToken);

        query = query.SkipIf(odataQuery.Skip is not null, odataQuery.Skip?.Value)
                     .TakeIf(odataQuery.Top is not null, odataQuery.Top?.Value);

        return new PagedResponse<TenantDto>(await query.ToArrayAsync(cancellationToken), totalCount);
    }

    [HttpPut]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task<TenantDto> Update(TenantDto dto, CancellationToken cancellationToken)
    {
        var entityToUpdate = await DbContext.Tenants.FindAsync([dto.Id], cancellationToken)
            ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.TenantCouldNotBeFound)]);

        var wasActive = entityToUpdate.IsActive;

        dto.Patch(entityToUpdate);

        await Validate(entityToUpdate, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);

        await fusionCache.RemoveAsync(TenantProvider.TENANTS_CACHE_KEY, token: cancellationToken);

        if (wasActive && entityToUpdate.IsActive is false)
        {
            await KickOutTenantUsers(entityToUpdate.Id, cancellationToken);
        }

        return entityToUpdate.Map();
    }

    /// <summary>
    /// Deactivating a tenant revokes all sessions that are currently signed into it immediately,
    /// while the users' sessions that are signed into other tenants (or none) are left intact.
    /// </summary>
    private async Task KickOutTenantUsers(Guid tenantId, CancellationToken cancellationToken)
    {
        //#if (signalR == true)
        var userSessionConnectionIds = await DbContext.UserSessions.Where(us => us.TenantId == tenantId && us.SignalRConnectionId != null)
                                                                   .Select(us => us.SignalRConnectionId!)
                                                                   .ToListAsync(cancellationToken);
        //#endif

        await DbContext.UserSessions.Where(us => us.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);

        //#if (signalR == true)
        foreach (var connectionId in userSessionConnectionIds)
        {
            // Check out AppHub's comments for more info.
            await appHubContext.Clients.Client(connectionId).Publish(SharedAppMessages.SESSION_REVOKED, null, cancellationToken);
        }
        //#endif
    }

    private async Task Validate(Tenant tenant, CancellationToken cancellationToken)
    {
        var entry = DbContext.Entry(tenant);

        if ((entry.State is EntityState.Added || entry.Property(t => t.Name).IsModified)
            && await DbContext.Tenants.AnyAsync(t => t.Id != tenant.Id && t.Name == tenant.Name, cancellationToken))
            throw new ResourceValidationException((nameof(TenantDto.Name), [Localizer[nameof(AppStrings.DuplicateTenantName), tenant.Name!]]));
    }
}
