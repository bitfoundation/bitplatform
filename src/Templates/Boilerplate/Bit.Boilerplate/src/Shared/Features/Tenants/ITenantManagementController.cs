using Boilerplate.Shared.Features.Tenants.Dtos;

namespace Boilerplate.Shared.Features.Tenants;

/// <summary>
/// <inheritdoc cref="AppFeatures.Management.Tenants_Manage_Global"/>
/// </summary>
[Route("api/v1/[controller]/[action]/"), AuthorizedApi]
public interface ITenantManagementController : IAppController
{
    [HttpGet]
    Task<PagedResponse<TenantDto>> GetTenants(CancellationToken cancellationToken) => default!;

    /// <summary>
    /// Updates the given tenant. Deactivating a tenant kicks out all of its accepted users immediately.
    /// </summary>
    [HttpPut]
    Task<TenantDto> Update(TenantDto request, CancellationToken cancellationToken);
}
