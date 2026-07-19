//+:cnd:noEmit
using Boilerplate.Server.Api.Features.Identity.Models;
//#if (multitenant == true)
using Boilerplate.Server.Api.Features.Tenants;
//#endif

namespace Boilerplate.Server.Api.Features.Identity.Configurations;

public partial class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.Property(role => role.Name).HasMaxLength(50);

        builder.HasMany(role => role.Users)
            .WithOne(ur => ur.Role)
            .HasForeignKey(ur => ur.RoleId);

        builder.HasMany(role => role.Claims)
            .WithOne(ur => ur.Role)
            .HasForeignKey(ur => ur.RoleId);

        //#if (multitenant == true)
        // The base IdentityDbContext adds a global unique index (RoleNameIndex) on NormalizedName that conflicts
        // with having a t-admin role per tenant, so its uniqueness gets replaced by the following filtered unique indexes:
        // 1. The role name must be unique within the tenant (When TenantId is not null).
        // 2. The role name must be unique among the global roles (When TenantId is null).
        builder.HasIndex(role => role.NormalizedName).IsUnique(false);
        builder.HasUniqueIndexOnNullable(role => new { role.Name, role.TenantId }, role => role.TenantId);

        builder.HasIndex(role => role.Name)
            //#if (database == "PostgreSQL")
            .HasFilter($"\"{nameof(Role.TenantId)}\" IS NULL")
            //#else
            .HasFilter($"[{nameof(Role.TenantId)}] IS NULL")
            //#endif
            .IsUnique();
        //#endif
        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
        //#if (multitenant != true)
        builder.HasIndex(role => role.Name).IsUnique();
        //#endif
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif

        //#if (multitenant == true)
        // The default store tenant's admin role.
        builder.HasData(new Role
        {
            Id = Guid.Parse("7ff71671-a1d6-5f97-abb9-d87d7b47d6e9"),
            Name = AppRoles.TenantAdmin,
            NormalizedName = AppRoles.TenantAdmin.ToUpperInvariant(),
            TenantId = TenantConfiguration.FallbackTenantId,
            ConcurrencyStamp = "7ff71671-a1d6-5f97-abb9-d87d7b47d6e9"
        });
        //#endif
        builder.HasData(new Role
        {
            Id = Guid.Parse("8ff71671-a1d6-5f97-abb9-d87d7b47d6e7"),
            Name = AppRoles.GlobalAdmin,
            NormalizedName = AppRoles.GlobalAdmin.ToUpperInvariant(),
            ConcurrencyStamp = "8ff71671-a1d6-5f97-abb9-d87d7b47d6e7"
        });

        builder.HasData(new Role
        {
            Id = Guid.Parse("9ff71672-a1d5-4f97-abb7-d87d6b47d5e8"),
            Name = AppRoles.Demo,
            NormalizedName = AppRoles.Demo.ToUpperInvariant(),
            ConcurrencyStamp = "9ff71672-a1d5-4f97-abb7-d87d6b47d5e8",
            //#if (multitenant == true)
            TenantId = TenantConfiguration.FallbackTenantId
            //#endif
        });
    }
}
