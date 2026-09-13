//+:cnd:noEmit
//#if (multitenant == true)
using Boilerplate.Server.Api.Features.Tenants;
//#endif

namespace Boilerplate.Server.Api.Features.Identity.Models;

public partial class Role : IdentityRole<Guid>
{
    public List<UserRole> Users { get; set; } = [];
    public List<RoleClaim> Claims { get; set; } = [];

    //#if (multitenant == true)
    /// <summary>
    /// Null means the role is a global role (only g-admin). Every other role belongs to a tenant: each tenant gets its
    /// own t-admin AND demo role (See RoleConfiguration and TenantController.Create). Role names are therefore not
    /// globally unique, so resolve a role by its Id - userManager.AddToRoleAsync / roleManager.FindByNameAsync resolve
    /// by name and would silently return an arbitrary tenant's role (See UserManagerExtensions.AssignDemoRole).
    /// </summary>
    public Guid? TenantId { get; set; }

    public Tenant? Tenant { get; set; }
    //#endif
}

