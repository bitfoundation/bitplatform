namespace TodoTemplate.Shared.Dtos.Account;

[DtoResourceType(typeof(AppStrings))]
public class EmailConfirmedRequestDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [EmailAddress(ErrorMessage = nameof(AppStrings.EmailAddressAttribute_Invalid))]
    [Display(Name = nameof(AppStrings.Email))]
    public string? Email { get; set; }
}
