namespace BlazorDual.Shared.Dtos.Account;

[DtoResourceType(typeof(AppStrings))]
public class EditUserDto
{
    [Required(ErrorMessageResourceName = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.FullName))]
    public string? FullName { get; set; }

    [Display(Name = nameof(AppStrings.Gender))]
    public Gender? Gender { get; set; }

    [Display(Name = nameof(AppStrings.BirthDate))]
    public DateTimeOffset? BirthDate { get; set; }

    public static implicit operator EditUserDto(UserDto user)
    {
        return new()
        {
            BirthDate = user.BirthDate,
            FullName = user.FullName,
            Gender = user.Gender
        };
    }
}
