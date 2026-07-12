//+:cnd:noEmit
using Boilerplate.Server.Api.Features.Identity.Models;
//#if (multitenant == true)
using Boilerplate.Server.Api.Features.Tenants;
//#endif

namespace Boilerplate.Server.Api.Features.Identity.Configurations;

public partial class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.HasIndex(userRole => new { userRole.RoleId, userRole.UserId }).IsUnique();

        // test@bitplatform.dev is the global admin.
        var defaultTestUserId = Guid.Parse("8ff71671-a1d6-4f97-abb9-d87d7b47d6e7");
        var globalAdminRoleId = Guid.Parse("8ff71671-a1d6-5f97-abb9-d87d7b47d6e7");
        builder.HasData(new UserRole { RoleId = globalAdminRoleId, UserId = defaultTestUserId });

        //#if (multitenant == true)
        var tenantAdminRoleId = Guid.Parse("7ff71671-a1d6-5f97-abb9-d87d7b47d6e9");
        var storeAdminUserId = Guid.Parse("6ff71671-a1d6-4f97-abb9-d87d7b47d6e5");

        // store-admin@bitplatform.dev is the default store tenant's admin.
        builder.HasData(new UserRole { RoleId = tenantAdminRoleId, UserId = storeAdminUserId, TenantId = TenantConfiguration.FallbackTenantId });
        //#endif
    }
}
