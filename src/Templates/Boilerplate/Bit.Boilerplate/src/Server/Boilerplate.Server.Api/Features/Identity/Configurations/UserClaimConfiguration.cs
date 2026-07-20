//+:cnd:noEmit
using Boilerplate.Server.Api.Features.Identity.Models;

namespace Boilerplate.Server.Api.Features.Identity.Configurations;

public partial class UserClaimConfiguration : IEntityTypeConfiguration<UserClaim>
{
    public void Configure(EntityTypeBuilder<UserClaim> builder)
    {
        //#if (multitenant == true)
        // 1. The user claim must be unique within the tenant (When TenantId is not null).
        // 2. The user claim must be unique among the global user claims (When TenantId is null).
        builder.HasUniqueIndexOnNullable(userClaim => new { userClaim.UserId, userClaim.ClaimType, userClaim.ClaimValue, userClaim.TenantId },
                                         userClaim => userClaim.TenantId);

        builder.HasIndex(userClaim => new { userClaim.UserId, userClaim.ClaimType, userClaim.ClaimValue })
            //#if (database == "PostgreSQL")
            .HasFilter($"\"{nameof(UserClaim.TenantId)}\" IS NULL")
            //#else
            .HasFilter($"[{nameof(UserClaim.TenantId)}] IS NULL")
            //#endif
            .IsUnique();
        //#endif
        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
        //#if (multitenant != true)
        builder.HasIndex(userClaim => new { userClaim.UserId, userClaim.ClaimType, userClaim.ClaimValue }).IsUnique();
        //#endif
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif
    }
}
