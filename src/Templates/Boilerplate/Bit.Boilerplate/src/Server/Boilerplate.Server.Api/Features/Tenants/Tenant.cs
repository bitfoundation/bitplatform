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
    /// Optional custom/vanity host (e.g. <c>myapp.com</c>) that resolves to this tenant for anonymous requests.
    /// Unlike <see cref="Name"/> (matched against the request's sub domain), this is matched against the complete request
    /// host and takes precedence over the sub domain match (See <see cref="TenantDto.DOMAIN_REGEX_PATTERN"/> and TenantProvider).
    /// Stored as a lowercase host with no scheme/port. Uniqueness is enforced by TenantController/TenantManagementController.
    /// <para>
    /// SECURITY: because a custom domain wins over the sub domain during resolution, a tenant that sets its <see cref="Domain"/>
    /// to another tenant's <c>{Name}.{baseHost}</c> (or to any host it doesn't actually own) would hijack that host's anonymous
    /// traffic. This starter only enforces uniqueness, so before exposing custom domains in production you MUST:
    /// 1) verify domain ownership out-of-band (e.g. a DNS TXT / CNAME challenge, an ACME http-01 challenge, or an admin-only
    ///    approval step) and only persist the domain once verified, and
    /// 2) add the verified host to <c>TrustedOrigins</c> so links, CORS and forwarded-headers treat it as trusted.
    /// </para>
    /// </summary>
    [MaxLength(253)]
    public string? Domain { get; set; }

    /// <summary>
    /// Inactive tenants can't be signed into / switched into.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public long Version { get; set; }

    public List<TenantUser> Users { get; set; } = [];
}
