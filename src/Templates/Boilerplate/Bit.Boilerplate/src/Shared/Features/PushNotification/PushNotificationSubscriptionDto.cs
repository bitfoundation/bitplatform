namespace Boilerplate.Shared.Features.PushNotification;

[DtoResourceType(typeof(AppStrings))]
public partial class PushNotificationSubscriptionDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    public string? DeviceId { get; set; }

    /// <example>"fcmV1"</example>
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [AllowedValues("apns", "fcmV1", "browser")]
    public string? Platform { get; set; }

    public string? PushChannel { get; set; }
    public string? P256dh { get; set; }
    public string? Auth { get; set; }
    public string? Endpoint { get; set; }
}
