using AdminPanel.Shared.Attributes;

namespace AdminPanel.Shared.Dtos.Account;

[DtoResourceType(typeof(AppStrings))]
public class SendConfirmationEmailRequestDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [EmailAddress(ErrorMessage = nameof(AppStrings.EmailAddressAttribute_Invalid))]
    [Display(Name = nameof(AppStrings.Email))]
    public string? Email { get; set; }
}
