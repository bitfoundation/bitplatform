namespace Boilerplate.Server.Api.Features.PushNotification;

public class PushNotificationSubscriptionConfiguration : IEntityTypeConfiguration<PushNotificationSubscription>
{
    public void Configure(EntityTypeBuilder<PushNotificationSubscription> builder)
    {
        builder
            .HasOne(sub => sub.UserSession)
            .WithOne(us => us.PushNotificationSubscription)
            .HasForeignKey<PushNotificationSubscription>(sub => sub.UserSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasUniqueIndexOnNullable(b => b.UserSessionId);
    }
}
