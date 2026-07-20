namespace Boilerplate.Shared.Features.Tenants.Dtos;

[DtoResourceType(typeof(AppStrings))]
public partial class InviteUserToTenantRequestDto : IValidatableObject
{
    [EmailAddress(ErrorMessage = nameof(AppStrings.EmailAddressAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Email))]
    public string? Email { get; set; }

    [Phone(ErrorMessage = nameof(AppStrings.PhoneAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.PhoneNumber))]
    public string? PhoneNumber { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Email) && string.IsNullOrWhiteSpace(PhoneNumber))
        {
            yield return new ValidationResult(
                errorMessage: nameof(AppStrings.EitherProvideEmailOrPhoneNumber),
                memberNames: [nameof(Email), nameof(PhoneNumber)]
            );
        }
    }
}
