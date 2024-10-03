namespace Boilerplate.Shared.Dtos.PushNotification;

[DtoResourceType(typeof(AppStrings))]
public partial class DeviceInstallationDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    public string? InstallationId { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [AllowedValues("apns", "fcmv1")]
    public string? Platform { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    public string? PushChannel { get; set; }
}
