namespace Boilerplate.Shared.Dtos.PushNotification;

public partial class NotificationRequestDto
{
    /// <example>test</example>
    public string? Text { get; set; }

    /// <example>null</example>
    public string? Action { get; set; }

    /// <example>["8ff71671-a1d6-4f97-abb9-d87d7b47d6e7"]</example>
    public string[] Tags { get; set; } = [];

    /// <example>false</example>
    public bool Silent { get; set; }
}
