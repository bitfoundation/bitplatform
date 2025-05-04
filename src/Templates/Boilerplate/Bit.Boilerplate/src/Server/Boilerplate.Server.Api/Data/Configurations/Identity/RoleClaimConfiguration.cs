//+:cnd:noEmit
using Boilerplate.Server.Api.Models.Identity;

namespace Boilerplate.Server.Api.Data.Configurations.Identity;

public partial class RoleClaimConfiguration : IEntityTypeConfiguration<RoleClaim>
{
    public void Configure(EntityTypeBuilder<RoleClaim> builder)
    {
        builder.HasIndex(userClaim => new { userClaim.RoleId, userClaim.ClaimType });

        // Super admin role has all the permissions (claims) by default.
        // Checkout IAuthTokenProvider.ReadClaims for client and AppJwtSecureDataFormat.Unprotect

        var basicUserRoleId = Guid.Parse("9ff71672-a1d5-4f97-abb7-d87d6b47d5e8");
        builder.HasData(new
        {
            Id = 1,
            RoleId = basicUserRoleId,
            ClaimType = AppClaimTypes.PERMISSIONS,
            ClaimValue = $"[{AppPermissions.AdminPanel.Dashboard},{AppPermissions.AdminPanel.ManageProductCatalog},{AppPermissions.Todo.ManageTodo}]",
        });
    }
}
