namespace Boilerplate.Server.Api.Models.PushNotification;

public class DeviceInstallation
{
    [Required]
    public string? InstallationId { get; set; }

    [Required, AllowedValues("apns", "fcmV1", "browser")]
    public string? Platform { get; set; }

    public string? PushChannel { get; set; }
    public string? P256dh { get; set; }
    public string? Auth { get; set; }
    public string? Endpoint { get; set; }

    public Guid? UserId { get; set; }

    public string[] Tags { get; set; } = [];

    public DateTimeOffset? ExpirationTime { get; set; }
}
