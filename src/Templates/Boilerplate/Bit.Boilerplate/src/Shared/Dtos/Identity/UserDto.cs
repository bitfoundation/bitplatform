namespace Boilerplate.Shared.Dtos.Identity;

[DtoResourceType(typeof(AppStrings))]
public partial class UserDto : IValidatableObject
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.UserName))]
    public string? UserName { get; set; }

    [EmailAddress(ErrorMessage = nameof(AppStrings.EmailAddressAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Email))]
    public string? Email { get; set; }

    [Phone(ErrorMessage = nameof(AppStrings.PhoneAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.PhoneNumber))]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Password))]
    public string? Password { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.FullName))]
    public string? FullName { get; set; }

    [Display(Name = nameof(AppStrings.Gender))]
    public Gender? Gender { get; set; }

    [Display(Name = nameof(AppStrings.BirthDate))]
    public DateTimeOffset? BirthDate { get; set; }

    public string? ProfileImageName { get; set; }

    public string? ConcurrencyStamp { get; set; }

    public string? DisplayName => FullName ?? Email ?? PhoneNumber ?? UserName;

    public string? GetProfileImageUrl(Uri absoluteServerAddress)
    {
        return ProfileImageName is null
            ? null
            : new Uri(absoluteServerAddress, $"/api/Attachment/GetProfileImage/{Id}?v={ConcurrencyStamp}").ToString();
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(Email) && string.IsNullOrEmpty(PhoneNumber))
            yield return new ValidationResult(
                errorMessage: nameof(AppStrings.EitherProvideEmailOrPhoneNumber),
                memberNames: [nameof(Email), nameof(PhoneNumber)]
            );
    }
}
