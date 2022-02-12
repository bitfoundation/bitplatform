namespace TodoTemplate.Shared.Dtos.Account;

public class SignInRequestDto
{
    [Required]
    public string? UserName { get; set; }

    [Required]
    public string? Password { get; set; }
}
