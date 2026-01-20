//+:cnd:noEmit
using Boilerplate.Server.Api.Features.Identity.Models;

namespace Boilerplate.Server.Api.Features.Identity.Configurations;

public partial class RoleClaimConfiguration : IEntityTypeConfiguration<RoleClaim>
{
    public void Configure(EntityTypeBuilder<RoleClaim> builder)
    {
        builder.HasIndex(roleClaim => new { roleClaim.RoleId, roleClaim.ClaimType, roleClaim.ClaimValue });

        var id = 1;

        // Unlimited privileged sessions for super admins
        var superAdminRoleId = Guid.Parse("8ff71671-a1d6-5f97-abb9-d87d7b47d6e7");
        builder.HasData(new RoleClaim
        {
            Id = id++,
            ClaimType = AppClaimTypes.MAX_PRIVILEGED_SESSIONS,
            ClaimValue = "-1",
            RoleId = superAdminRoleId
        });


        // Assign non admin features to demo role
        var demoRoleId = Guid.Parse("9ff71672-a1d5-4f97-abb7-d87d6b47d5e8");
        foreach (var feature in AppFeatures.GetAll()
            .Where(f => f.Group != typeof(AppFeatures.System)
                     && f.Group != typeof(AppFeatures.Management)))
        {
            builder.HasData(new
            {
                Id = id++,
                ClaimType = AppClaimTypes.FEATURES,
                ClaimValue = feature.Value,
                RoleId = demoRoleId
            });
        }
    }
}
