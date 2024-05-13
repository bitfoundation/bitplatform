namespace Boilerplate.Shared.Dtos.Identity;

[DtoResourceType(typeof(AppStrings))]
public class SendConfirmPhoneNumberTokenRequestDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Phone(ErrorMessage = nameof(AppStrings.PhoneAttribute_Invalid))]
    [Display(Name = nameof(AppStrings.PhoneNumber))]
    public string? PhoneNumber { get; set; }
}
