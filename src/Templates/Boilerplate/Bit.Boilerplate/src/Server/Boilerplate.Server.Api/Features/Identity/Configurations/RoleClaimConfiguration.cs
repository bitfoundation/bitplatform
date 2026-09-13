//+:cnd:noEmit

namespace Boilerplate.Server.Api.Features.Identity.Configurations;

public partial class RoleClaimConfiguration : IEntityTypeConfiguration<RoleClaim>
{
    public void Configure(EntityTypeBuilder<RoleClaim> builder)
    {
        builder.HasIndex(roleClaim => new { roleClaim.RoleId, roleClaim.ClaimType, roleClaim.ClaimValue }).IsUnique();

        var unlimitedPrivilegedSessions = AppClaimTypes.UNLIMITED_PRIVILEGED_SESSIONS.ToString(CultureInfo.InvariantCulture);

        // Seeded ids run DOWNWARDS from -1, and that is load-bearing. RoleClaim.Id is an auto-increment int that
        // runtime inserts also draw from (TenantController.Create, RoleManagementController.AddClaims), so numbering the
        // seed 1..N would put the seeded rows inside the range the database hands out: adding one AppFeatures demo
        // feature later grows the seeded set and emits an InsertData for an id production already used, and the
        // migration fails half-applied. The auto-increment counter never produces negatives, so this range is private
        // to the seed. (PostgreSQL is safe too - Npgsql's sequence bumping uses GREATEST(max + 1, nextval()).)
        var id = -1;

        // Unlimited privileged sessions for Global admins
        var globalAdminRoleId = Guid.Parse("8ff71671-a1d6-5f97-abb9-d87d7b47d6e7");
        builder.HasData(new RoleClaim
        {
            Id = id--,
            ClaimType = AppClaimTypes.MAX_PRIVILEGED_SESSIONS,
            ClaimValue = unlimitedPrivilegedSessions,
            RoleId = globalAdminRoleId
        });

        //#if (multitenant == true)
        // Unlimited privileged sessions for the default store tenant's admins
        var tenantAdminRoleId = Guid.Parse("7ff71671-a1d6-5f97-abb9-d87d7b47d6e9");
        builder.HasData(new RoleClaim
        {
            Id = id--,
            ClaimType = AppClaimTypes.MAX_PRIVILEGED_SESSIONS,
            ClaimValue = unlimitedPrivilegedSessions,
            RoleId = tenantAdminRoleId
        });
        //#endif


        // Assign non admin features to demo role
        var demoRoleId = Guid.Parse("9ff71672-a1d5-4f97-abb7-d87d6b47d5e8");
        foreach (var feature in AppFeatures.GetDemoFeatures())
        {
            builder.HasData(new
            {
                Id = id--,
                ClaimType = AppClaimTypes.FEATURES,
                ClaimValue = feature.Value,
                RoleId = demoRoleId
            });
        }
    }
}
