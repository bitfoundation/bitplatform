using Boilerplate.Server.Api.Features.Identity.Models;

namespace Boilerplate.Server.Api.Features.Tenants;

/// <summary>
/// A user can have many tenants and a tenant can have many users.
/// </summary>
public partial class TenantUser
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// Null means the user has been invited to the tenant, but has not accepted the invitation yet (or has left the tenant).
    /// It gets set the first time the user switches into the tenant, and it's set upfront for the user who has created the tenant.
    /// </summary>
    public DateTimeOffset? AcceptedOn { get; set; }
}
