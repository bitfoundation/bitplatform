namespace TodoTemplate.Shared.Dtos.Account;

public class UserDto
{
    public int Id { get; set; }

    public string? UserName { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? FullName { get; set; }

    public Gender? Gender { get; set; }

    public DateTimeOffset? BirthDate { get; set; }

    public string? ProfileImageName { get; set; }

    [NotMapped]
    public string? GenderValue
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
