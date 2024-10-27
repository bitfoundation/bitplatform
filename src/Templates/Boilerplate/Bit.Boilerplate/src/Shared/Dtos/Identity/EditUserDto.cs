namespace Boilerplate.Shared.Dtos.Identity;

[DtoResourceType(typeof(AppStrings))]
public partial class EditUserDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.FullName))]
    public string? FullName { get; set; }

    [Display(Name = nameof(AppStrings.Gender))]
    public Gender Gender { get; set; }

    [Display(Name = nameof(AppStrings.BirthDate))]
    public DateTimeOffset? BirthDate { get; set; }
}
