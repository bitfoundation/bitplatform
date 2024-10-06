namespace Boilerplate.Shared.Dtos.PushNotification;

[DtoResourceType(typeof(AppStrings))]
public partial class DeviceInstallationDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    public string? InstallationId { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [AllowedValues("apns", "fcmv1", "browser")]
    /// <example>fcmv1</example>
    public string? Platform { get; set; }

    public string? PushChannel { get; set; }

    #region Web Push
    public string? Endpoint { get; set; }
    public string? P256dh { get; set; }
    public string? Auth { get; set; }
    #endregion
}
