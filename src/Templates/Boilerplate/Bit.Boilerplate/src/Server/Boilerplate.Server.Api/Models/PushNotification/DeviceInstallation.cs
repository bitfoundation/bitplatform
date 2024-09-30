namespace Boilerplate.Server.Api.Models.PushNotification;

public partial class DeviceInstallation
{
    [Required]
    public string? InstallationId { get; set; }

    [Required]
    public string? Platform { get; set; }

    [Required]
    public string? PushChannel { get; set; }

    public IList<string> Tags { get; set; } = [];
}
