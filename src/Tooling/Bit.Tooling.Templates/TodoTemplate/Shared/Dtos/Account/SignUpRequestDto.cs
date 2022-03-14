namespace TodoTemplate.Shared.Dtos.Account;

public class SignUpRequestDto
{
    public string? Email { get; set; }

    [Required]
    public string? UserName { get; set; }

    public string? PhoneNumber { get; set; }

    [Required]
    public string? Password { get; set; }
}
