//+:cnd:noEmit

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

        //#if (database != "MySql")
        //#if (IsInsideProjectTemplate == true)
        // MySQL has no filtered indexes - the filter is silently dropped, which would turn this into an unconditional
        // UNIQUE(UserId, ClaimType, ClaimValue) and stop a user from holding the same claim in two different tenants
        // (See RoleConfiguration for the same trade-off on role names).
        //#endif
        builder.HasIndex(userClaim => new { userClaim.UserId, userClaim.ClaimType, userClaim.ClaimValue })
            //#if (database == "PostgreSQL")
            .HasFilter($"\"{nameof(UserClaim.TenantId)}\" IS NULL")
            //#else
            .HasFilter($"[{nameof(UserClaim.TenantId)}] IS NULL")
            //#endif
            .IsUnique();
        //#endif
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
