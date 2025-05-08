//+:cnd:noEmit
using Boilerplate.Server.Api.Models.Identity;

namespace Boilerplate.Server.Api.Data.Configurations.Identity;

public partial class RoleClaimConfiguration : IEntityTypeConfiguration<RoleClaim>
{
    public void Configure(EntityTypeBuilder<RoleClaim> builder)
    {
        builder.HasIndex(roleClaim => new { roleClaim.RoleId, roleClaim.ClaimType, roleClaim.ClaimValue });

        // Unlimited privileged sessions for super admins
        var superAdminRoleId = Guid.Parse("9ff71672-a1d5-4f97-abb7-d87d6b47d5e8");
        builder.HasData(new
        {
            Id = 1,
            ClaimType = AppClaimTypes.MAX_PRIVILEGED_SESSIONS,
            ClaimValue = "-1",
            RoleId = superAdminRoleId
        });


        // Assign non admin permissions to demo role
        var demoRoleId = Guid.Parse("9ff71672-a1d5-4f97-abb7-d87d6b47d5e8");
        builder.HasData(new
        {
            Id = 2,
            ClaimType = AppClaimTypes.PERMISSIONS,
            ClaimValue = AppPermissions.AdminPanel.Dashboard,
            RoleId = demoRoleId
        });
        builder.HasData(new
        {
            Id = 3,
            ClaimType = AppClaimTypes.PERMISSIONS,
            ClaimValue = AppPermissions.AdminPanel.ManageProductCatalog,
            RoleId = demoRoleId
        });
        builder.HasData(new
        {
            Id = 4,
            ClaimType = AppClaimTypes.PERMISSIONS,
            ClaimValue = AppPermissions.Todo.ManageTodo,
            RoleId = demoRoleId
        });
    }
}
