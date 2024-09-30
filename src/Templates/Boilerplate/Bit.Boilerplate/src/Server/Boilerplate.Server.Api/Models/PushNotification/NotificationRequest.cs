namespace Boilerplate.Server.Api.Models.PushNotification;

public partial class NotificationRequest
{
    public string? Text { get; set; }
    public string? Action { get; set; }
    public string[] Tags { get; set; } = [];
    public bool Silent { get; set; }
}
