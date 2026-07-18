using Boilerplate.Shared.Features.Tenants.Dtos;

namespace Boilerplate.Shared.Features.Tenants;

/// <summary>
/// Manages the tenant the user is currently signed into (See <see cref="ITenantManagementController"/> for global admin's tenants management).
/// </summary>
[Route("api/v1/[controller]/[action]/"), AuthorizedApi]
public interface ITenantController : IAppController
{
    /// <summary>
    /// Returns the tenant the user is currently signed into, or null if the user doesn't belong to any tenant yet.
    /// </summary>
    [HttpGet]
    Task<TenantDto?> GetCurrentTenant(CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new tenant, its t-admin role and assigns the current user to it, so she can switch into it afterwards.
    /// </summary>
    [HttpPost]
    Task<TenantDto> Create(TenantDto request, CancellationToken cancellationToken);

    /// <summary>
    /// <inheritdoc cref="AppFeatures.Management.Tenant_Manage"/>
    /// </summary>
    [HttpPut]
    Task<TenantDto> Update(TenantDto request, CancellationToken cancellationToken);

    /// <summary>
    /// Finds or creates the user by email/phone number, adds it to the current tenant and sends an invitation email/sms to her.
    /// </summary>
    [HttpPost]
    Task InviteUser(InviteUserToTenantRequestDto request, CancellationToken cancellationToken);
}
