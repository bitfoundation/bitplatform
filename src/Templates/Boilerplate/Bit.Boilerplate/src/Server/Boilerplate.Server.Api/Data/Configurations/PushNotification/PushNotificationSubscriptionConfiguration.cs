using Boilerplate.Server.Api.Models.PushNotification;

namespace Boilerplate.Server.Api.Data.Configurations.PushNotification;

public class PushNotificationSubscriptionConfiguration : IEntityTypeConfiguration<PushNotificationSubscription>
{
    public void Configure(EntityTypeBuilder<PushNotificationSubscription> builder)
    {
        builder
            .HasOne(sub => sub.UserSession)
            .WithOne(us => us.PushNotificationSubscription)
            .HasForeignKey<PushNotificationSubscription>(sub => sub.UserSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        //#if (database != "PostgreSQL")
        builder
            .HasIndex(b => b.UserSessionId)
            .HasFilter($"[{nameof(PushNotificationSubscription.UserSessionId)}] IS NOT NULL")
            .IsUnique();
        //#endif
        //#if (IsInsideProjectTemplate == true)
        return;
        //#endif
        //#if (database == "PostgreSQL")
        builder
            .HasIndex(b => b.UserSessionId)
            .HasFilter($"'{nameof(PushNotificationSubscription.UserSessionId)}' IS NOT NULL")
            .IsUnique();
        //#endif
    }
}
