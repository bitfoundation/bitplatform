//+:cnd:noEmit

namespace Boilerplate.Server.Api.Features.Identity.Configurations;

public class WebAuthnCredentialConfiguration : IEntityTypeConfiguration<WebAuthnCredential>
{
    public void Configure(EntityTypeBuilder<WebAuthnCredential> builder)
    {
        builder.HasOne(t => t.User)
            .WithMany(u => u.WebAuthnCredentials)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
        //#if (database == "MySql")
        builder.Property(credential => credential.Transports).HasConversion(
            v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
            v => JsonSerializer.Deserialize<AuthenticatorTransport[]>(v, JsonSerializerOptions.Default));
        //#endif
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif
    }
}
