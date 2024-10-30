using AdminPanel.Server.Api.Models.Identity;

namespace AdminPanel.Server.Api.Models.PushNotification;

public class DeviceInstallation
{
    [Required, Key]
    public string? InstallationId { get; set; }

    [Required, AllowedValues("apns", "fcmV1", "browser")]
    public string? Platform { get; set; }

    [Required]
    public string? PushChannel { get; set; }

    public string? P256dh { get; set; }
    public string? Auth { get; set; }
    public string? Endpoint { get; set; }

    public Guid? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    public string[] Tags { get; set; } = [];

    /// <summary>
    /// Unix Time Seconds
    /// </summary>
    public long ExpirationTime { get; set; }
}
