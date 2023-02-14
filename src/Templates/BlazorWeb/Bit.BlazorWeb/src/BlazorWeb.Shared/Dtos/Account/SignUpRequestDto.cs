namespace BlazorWeb.Shared.Dtos.Account;

[DtoResourceType(typeof(AppStrings))]
public class SignUpRequestDto
{
    [EmailAddress(ErrorMessageResourceName = nameof(AppStrings.EmailAddressAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Email))]
    public string? Email { get; set; }

    [EmailAddress(ErrorMessageResourceName = nameof(AppStrings.EmailAddressAttribute_ValidationError))]
    [Required(ErrorMessageResourceName = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.UserName))]
    public string? UserName { get; set; }

    [Display(Name = nameof(AppStrings.PhoneNumber))]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessageResourceName = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [MinLength(6, ErrorMessageResourceName = nameof(AppStrings.MinLengthAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Password))]
    public string? Password { get; set; }

    [NotMapped]
    [Range(typeof(bool), "true", "true", ErrorMessageResourceName = nameof(AppStrings.YouHaveToAcceptPrivacyPolicy))]
    [Display(Name = nameof(AppStrings.IsAcceptPrivacy))]
    public bool IsAcceptPrivacy { get; set; }
}
