using Boilerplate.Shared.Features.Tenants.Dtos;

namespace Boilerplate.Server.Api.Features.Tenants;

public partial class Tenant
{
    public Guid Id { get; set; }

    /// <summary>
    /// Unique name that must match sub domain name restrictions (See <see cref="TenantDto.NAME_REGEX_PATTERN"/>),
    /// so the tenant can be resolved from the sub domain for anonymous requests (See TenantProvider).
    /// </summary>
    [Required, MaxLength(63)]
    public string? Name { get; set; }

    [MaxLength(64)]
    public string? Title { get; set; }

    /// <summary>
    /// Inactive tenants can't be signed into / switched into.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public long Version { get; set; }

    public List<TenantUser> Users { get; set; } = [];
}
