namespace TodoTemplate.Shared.Dtos.Account;

public class EditUserDto
{
    [Required(ErrorMessage = "please fill full name input")]
    public string? FullName { get; set; }
    [Required(ErrorMessage = "please choose your gender")]
    public Gender? Gender { get; set; }
    [Required(ErrorMessage = "please choose your birth date")]
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
