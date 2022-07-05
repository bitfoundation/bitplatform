namespace AdminPanelTemplate.Shared.Dtos.Account;

public class EditUserDto
{
    public string? FullName { get; set; }

    public Gender? Gender { get; set; }

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
