//+:cnd:noEmit
namespace Boilerplate.Server.Api.Features.Tenants;

public partial class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.HasIndex(tm => new { tm.TenantId, tm.UserId }).IsUnique();

        //#if (database != "MySql")
        builder.HasIndex(tm => new { tm.TenantId, tm.UserId }, "IX_TenantMemberships_TenantId_UserId_Accepted")
            //#if (database == "PostgreSQL")
            .HasFilter($"\"{nameof(TenantMembership.AcceptedOn)}\" IS NOT NULL")
            //#else
            .HasFilter($"[{nameof(TenantMembership.AcceptedOn)}] IS NOT NULL")
            //#endif
            ;
        //#endif

        var storeAdminUserId = Guid.Parse("6ff71671-a1d6-4f97-abb9-d87d7b47d6e5");
        var defaultTestUserId = Guid.Parse("8ff71671-a1d6-4f97-abb9-d87d7b47d6e7");
        var storeUserUserId = Guid.Parse("4ff71671-a1d6-4f97-abb9-d87d7b47d6e4");

        // The value of AcceptedOn is set upfront for the users that have created the tenant (data seed in this case).
        builder.HasData(new TenantMembership
        {
            Id = Guid.Parse("5ff71671-a1d6-4f97-abb9-d87d7b47d6e2"),
            TenantId = TenantConfiguration.FallbackTenantId,
            UserId = storeAdminUserId,
            AcceptedOn = new DateTimeOffset(new DateOnly(2023, 1, 1), default, default)
        });

        builder.HasData(new TenantMembership
        {
            Id = Guid.Parse("5ff71671-a1d6-4f97-abb9-d87d7b47d6e3"),
            TenantId = TenantConfiguration.FallbackTenantId,
            UserId = defaultTestUserId,
            AcceptedOn = new DateTimeOffset(new DateOnly(2023, 1, 1), default, default)
        });

        // store-user@bitplatform.dev is an accepted member of the default store tenant, so a tenant gets selected for her
        // at sign-in and her demo user-group's (tenant-scoped) feature claims flow into her token (See IdentityController.GetTenantId).
        builder.HasData(new TenantMembership
        {
            Id = Guid.Parse("5ff71671-a1d6-4f97-abb9-d87d7b47d6e1"),
            TenantId = TenantConfiguration.FallbackTenantId,
            UserId = storeUserUserId,
            AcceptedOn = new DateTimeOffset(new DateOnly(2023, 1, 1), default, default)
        });
    }
}
