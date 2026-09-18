//+:cnd:noEmit
using Boilerplate.Server.Api.Features.Identity.OAuth.Models;

namespace Boilerplate.Server.Api.Features.Identity.OAuth.Configurations;

public class OAuthGrantConfiguration : IEntityTypeConfiguration<OAuthGrant>
{
    public void Configure(EntityTypeBuilder<OAuthGrant> builder)
    {
        builder.HasKey(grant => grant.UserSessionId);

        // Revoking a grant is deleting its session; this row has no meaning without it.
        builder.HasOne(grant => grant.UserSession)
            .WithOne(session => session.OAuthGrant)
            .HasForeignKey<OAuthGrant>(grant => grant.UserSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
