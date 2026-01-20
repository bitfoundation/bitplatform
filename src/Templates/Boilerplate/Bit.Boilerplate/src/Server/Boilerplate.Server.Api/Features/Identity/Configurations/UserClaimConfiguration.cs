//+:cnd:noEmit
using Boilerplate.Server.Api.Features.Identity.Models;

namespace Boilerplate.Server.Api.Features.Identity.Configurations;

public partial class UserClaimConfiguration : IEntityTypeConfiguration<UserClaim>
{
    public void Configure(EntityTypeBuilder<UserClaim> builder)
    {
        builder.HasIndex(userClaim => new { userClaim.UserId, userClaim.ClaimType, userClaim.ClaimValue });
    }
}
