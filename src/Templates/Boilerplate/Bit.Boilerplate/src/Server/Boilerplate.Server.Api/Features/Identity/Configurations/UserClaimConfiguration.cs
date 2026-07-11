//+:cnd:noEmit
using Boilerplate.Server.Api.Features.Identity.Models;

namespace Boilerplate.Server.Api.Features.Identity.Configurations;

public partial class UserClaimConfiguration : IEntityTypeConfiguration<UserClaim>
{
    public void Configure(EntityTypeBuilder<UserClaim> builder)
    {
        //#if (multitenancy == true)
        // 1. The user claim must be unique within the tenant (When TenantId is not null).
        // 2. The user claim must be unique among the global user claims (When TenantId is null).
        //#if (database != "PostgreSQL")
        builder.HasIndex(userClaim => new { userClaim.UserId, userClaim.ClaimType, userClaim.ClaimValue, userClaim.TenantId })
            .HasFilter($"[{nameof(UserClaim.TenantId)}] IS NOT NULL")
            .IsUnique();

        builder.HasIndex(userClaim => new { userClaim.UserId, userClaim.ClaimType, userClaim.ClaimValue })
            .HasFilter($"[{nameof(UserClaim.TenantId)}] IS NULL")
            .IsUnique();
        //#endif
        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
        //#if (database == "PostgreSQL")
        builder.HasIndex(userClaim => new { userClaim.UserId, userClaim.ClaimType, userClaim.ClaimValue, userClaim.TenantId })
            .HasFilter($"\"{nameof(UserClaim.TenantId)}\" IS NOT NULL")
            .IsUnique();

        builder.HasIndex(userClaim => new { userClaim.UserId, userClaim.ClaimType, userClaim.ClaimValue })
            .HasFilter($"\"{nameof(UserClaim.TenantId)}\" IS NULL")
            .IsUnique();
        //#endif
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif
        //#endif
        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
        //#if (multitenancy != true)
        builder.HasIndex(userClaim => new { userClaim.UserId, userClaim.ClaimType, userClaim.ClaimValue });
        //#endif
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif
    }
}
