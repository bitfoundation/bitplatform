//+:cnd:noEmit
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

        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
        //#if (database == "MySql")
        builder.Property(sub => sub.Tags).HasConversion(
            v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
            v => JsonSerializer.Deserialize<string[]>(v, JsonSerializerOptions.Default)!);
        //#endif
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif

        builder.HasIndex(sub => sub.DeviceId)
               .IsUnique();
    }
}
