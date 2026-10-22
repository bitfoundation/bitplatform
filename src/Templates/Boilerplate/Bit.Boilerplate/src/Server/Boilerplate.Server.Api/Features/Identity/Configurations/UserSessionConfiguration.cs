//+:cnd:noEmit
namespace Boilerplate.Server.Api.Features.Identity.Configurations;

public partial class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.HasIndex(userSession => userSession.SignalRConnectionId)
            //#if (database == "PostgreSQL")
            .HasFilter($"\"{nameof(UserSession.SignalRConnectionId)}\" IS NOT NULL")
            //#elif (database != "MySql")
            .HasFilter($"[{nameof(UserSession.SignalRConnectionId)}] IS NOT NULL")
            //#endif
            ;
    }
}
