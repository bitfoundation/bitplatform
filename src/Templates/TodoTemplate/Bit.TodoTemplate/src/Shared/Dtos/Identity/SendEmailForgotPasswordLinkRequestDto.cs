namespace TodoTemplate.Shared.Dtos.Identity;

[DtoResourceType(typeof(AppStrings))]
public class SendResetPasswordEmailRequestDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [EmailAddress(ErrorMessage = nameof(AppStrings.EmailAddressAttribute_Invalid))]
    [Display(Name = nameof(AppStrings.Email))]
    public string? Email { get; set; }
}
