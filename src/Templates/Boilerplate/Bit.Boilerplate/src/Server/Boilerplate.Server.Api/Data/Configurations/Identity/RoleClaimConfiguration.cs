//+:cnd:noEmit
using Boilerplate.Server.Api.Models.Identity;

namespace Boilerplate.Server.Api.Data.Configurations.Identity;

public partial class RoleClaimConfiguration : IEntityTypeConfiguration<RoleClaim>
{
    public void Configure(EntityTypeBuilder<RoleClaim> builder)
    {
        builder.HasIndex(userClaim => new { userClaim.RoleId, userClaim.ClaimType });

        builder.HasData(new { Id = 1, RoleId = Guid.Parse("8ff71671-a1d6-5f97-abb9-d87d7b47d6e7"), ClaimType = AppClaimTypes.EDIT_AI_SYSTEM_PROMPTS, ClaimValue = "true" });
    }
}
