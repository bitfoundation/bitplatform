namespace TodoTemplate.Shared.Dtos.Account;

[DtoResourceType(typeof(AppStrings))]
public class UserDto
{
    public int Id { get; set; }

    [Required(ErrorMessageResourceName = nameof(AppStrings.RequiredAttribute_ValidationError)),
        EmailAddress(ErrorMessageResourceName = nameof(AppStrings.EmailAddressAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Email))]
    public string? UserName { get; set; }

    [Required(ErrorMessageResourceName = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [EmailAddress(ErrorMessageResourceName = nameof(AppStrings.EmailAddressAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Email))]
    public string? Email { get; set; }

    [Required(ErrorMessageResourceName = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Password))]
    public string? Password { get; set; }

    [Required(ErrorMessageResourceName = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.FullName))]
    public string? FullName { get; set; }

    [Display(Name = nameof(AppStrings.Gender))]
    public Gender? Gender { get; set; }

    [Display(Name = nameof(AppStrings.BirthDate))]
    public DateTimeOffset? BirthDate { get; set; }

    public string? ProfileImageName { get; set; }

    [JsonIgnore]
    public string? GenderAsString
    {
        get
        {
            return Gender?.ToString();
        }
        set
        {
            if (string.IsNullOrEmpty(value) is false)
            {
                Gender = Enum.Parse<Gender>(value);
            }
        }
    }

}
