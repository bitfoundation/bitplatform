namespace Boilerplate.Shared.Dtos.Identity;

[DtoResourceType(typeof(AppStrings))]
public class ChangeUserNameRequestDto
{
    /// <example>new</example>
    [Display(Name = nameof(AppStrings.UserName))]
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    public string? UserName { get; set; }
}
