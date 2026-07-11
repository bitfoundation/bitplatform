//+:cnd:noEmit
//#if (multitenancy == true)
using Boilerplate.Server.Api.Features.Tenants;
//#endif

namespace Boilerplate.Server.Api.Features.Identity.Models;

public partial class Role : IdentityRole<Guid>
{
    public List<UserRole> Users { get; set; } = [];
    public List<RoleClaim> Claims { get; set; } = [];

    //#if (multitenancy == true)
    /// <summary>
    /// Null means the role is a global role (like g-admin and demo), otherwise the role belongs to a tenant (like each tenant's t-admin role).
    /// </summary>
    public Guid? TenantId { get; set; }

    public Tenant? Tenant { get; set; }
    //#endif
}

