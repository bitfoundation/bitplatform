namespace TodoTemplate.Shared.Dtos.Account;

[DtoResourceType(typeof(AppStrings))]
public class SendConfirmationEmailRequestDto
{
    [Required(ErrorMessageResourceName = nameof(AppStrings.RequiredAttribute_ValidationError)),
        EmailAddress(ErrorMessageResourceName = nameof(AppStrings.EmailAddressAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Email))]
    public string? Email { get; set; }
}
