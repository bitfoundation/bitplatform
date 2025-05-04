//+:cnd:noEmit
using Boilerplate.Server.Api.Models.Identity;

namespace Boilerplate.Server.Api.Data.Configurations.Identity;

public partial class RoleClaimConfiguration : IEntityTypeConfiguration<RoleClaim>
{
    public void Configure(EntityTypeBuilder<RoleClaim> builder)
    {
        builder.HasIndex(userClaim => new { userClaim.RoleId, userClaim.ClaimType });

        var superAdminRoleId = Guid.Parse("8ff71671-a1d6-5f97-abb9-d87d7b47d6e7");
        builder.HasData(new { RoleId = superAdminRoleId, ClaimType = AppClaimTypes.Management.ManageAI, ClaimValue = "", Id = 1 });
        builder.HasData(new { RoleId = superAdminRoleId, ClaimType = AppClaimTypes.Management.ManageIdentity, ClaimValue = "", Id = 2 });
        builder.HasData(new { RoleId = superAdminRoleId, ClaimType = AppClaimTypes.Management.ViewLogs, ClaimValue = "", Id = 3 });
        builder.HasData(new { RoleId = superAdminRoleId, ClaimType = AppClaimTypes.Management.ManageJobs, ClaimValue = "", Id = 4 });

        builder.HasData(new { RoleId = superAdminRoleId, ClaimType = AppClaimTypes.AdminPanel.Dashboard, ClaimValue = "", Id = 5 });
        builder.HasData(new { RoleId = superAdminRoleId, ClaimType = AppClaimTypes.AdminPanel.ManageProductCatalog, ClaimValue = "", Id = 6 });

        builder.HasData(new { RoleId = superAdminRoleId, ClaimType = AppClaimTypes.Todo.ManageTodo, ClaimValue = "", Id = 7 });

        var basicUserRoleId = Guid.Parse("9ff71672-a1d5-4f97-abb7-d87d6b47d5e8");
        builder.HasData(new { RoleId = basicUserRoleId, ClaimType = AppClaimTypes.AdminPanel.Dashboard, ClaimValue = "", Id = 8 });
        builder.HasData(new { RoleId = basicUserRoleId, ClaimType = AppClaimTypes.AdminPanel.ManageProductCatalog, ClaimValue = "", Id = 9 });

        builder.HasData(new { RoleId = basicUserRoleId, ClaimType = AppClaimTypes.Todo.ManageTodo, ClaimValue = "", Id = 10 });
    }
}
