//+:cnd:noEmit
//#if (multitenant == true)
using Boilerplate.Server.Api.Features.Tenants;
//#endif

namespace Boilerplate.Server.Api.Features.Identity.Models;

public class UserRole : IdentityUserRole<Guid>
{
    public User? User { get; set; }

    public Role? Role { get; set; }

    //#if (multitenant == true)
    /// <summary>
    /// Follows the <see cref="Role.TenantId"/> of the assigned role. Null means a global role assignment.
    /// </summary>
    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    public Guid? TenantId { get; set; }
    //#endif
}
