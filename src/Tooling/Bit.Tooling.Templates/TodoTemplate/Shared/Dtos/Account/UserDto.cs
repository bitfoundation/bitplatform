namespace TodoTemplate.Shared.Dtos.Account;

public class UserDto
{
    public int Id { get; set; }

    [Required(ErrorMessage ="please fill user name input")]
    public string? UserName { get; set; }
    [Required(ErrorMessage ="please fill email input")]
    public string? Email { get; set; }
    [Required(ErrorMessage = "please fill password input")]
    public string? Password { get; set; }
    [Required(ErrorMessage = "please fill full name input")]
    public string? FullName { get; set; }
    [Required(ErrorMessage = "please choose your gender")]
    public Gender? Gender { get; set; }
    [Required(ErrorMessage = "please choose your birth date")]
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
