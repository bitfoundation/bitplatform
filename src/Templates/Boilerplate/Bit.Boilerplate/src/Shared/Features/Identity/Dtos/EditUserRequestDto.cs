namespace Boilerplate.Shared.Features.Identity.Dtos;

[DtoResourceType(typeof(AppStrings))]
public partial class EditUserRequestDto : IValidatableObject
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.FullName))]
    public string? FullName { get; set; }

    [Display(Name = nameof(AppStrings.Gender))]
    public Gender? Gender { get; set; }

    [Display(Name = nameof(AppStrings.BirthDate))]
    public DateTimeOffset? BirthDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (BirthDate > DateTimeOffset.Now)
        {
            yield return new ValidationResult(
                errorMessage: nameof(AppStrings.BirthDateInFuture),
                memberNames: [nameof(BirthDate)]
            );
        }
    }
}
