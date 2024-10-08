namespace Boilerplate.Shared.Dtos.PushNotification;

[DtoResourceType(typeof(AppStrings))]
public partial class DeviceInstallationDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    public string? InstallationId { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [AllowedValues("apns", "fcmV1", "browser")]
    /// <example>fcmV1</example>
    public string? Platform { get; set; }

    public string? PushChannel { get; set; }
}
