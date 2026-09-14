//+:cnd:noEmit
using Boilerplate.Server.Api.Features.Identity.OAuth.Models;

namespace Boilerplate.Server.Api.Features.Identity.OAuth.Configurations;

public class OAuthAuthorizationCodeConfiguration : IEntityTypeConfiguration<OAuthAuthorizationCode>
{
    public void Configure(EntityTypeBuilder<OAuthAuthorizationCode> builder)
    {
        // The token endpoint looks a code up by hash alone; unique so a reused hash cannot silently match either row.
        builder.HasIndex(code => code.CodeHash).IsUnique();

        // Deleting a user takes their outstanding codes with it, or a code outlives the account it was issued for.
        builder.HasOne(code => code.User)
            .WithMany()
            .HasForeignKey(code => code.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        //#if (multitenant == true)
        // Restrict, not cascade: a code is a credential, so a tenant delete must not quietly drop an outstanding one.
        builder.HasOne(code => code.Tenant)
            .WithMany()
            .HasForeignKey(code => code.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        //#endif
    }
}
