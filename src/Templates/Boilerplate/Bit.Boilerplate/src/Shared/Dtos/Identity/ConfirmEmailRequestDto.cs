
namespace Boilerplate.Shared.Dtos.Identity;

[DtoResourceType(typeof(AppStrings))]
public partial class ConfirmEmailRequestDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [EmailAddress(ErrorMessage = nameof(AppStrings.EmailAddressAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Email))]
    public string? Email { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    public string? Token { get; set; }

    /// <example>Samsung Android 14</example>
    public string? DeviceInfo { get; set; }
}

public partial class ChangeEmailRequestDto : ConfirmEmailRequestDto
{
    // This class needs the same set of properties as ConfirmEmailRequestDto
}
