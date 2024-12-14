namespace Boilerplate.Shared.Dtos.PushNotification;

[DtoResourceType(typeof(AppStrings))]
public partial class PushNotificationSubscriptionDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    public string? DeviceId { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [AllowedValues("apns", "fcmV1", "browser")]
    /// <example>fcmV1</example>
    public string? Platform { get; set; }

    public string? PushChannel { get; set; }
    public string? P256dh { get; set; }
    public string? Auth { get; set; }
    public string? Endpoint { get; set; }
}
