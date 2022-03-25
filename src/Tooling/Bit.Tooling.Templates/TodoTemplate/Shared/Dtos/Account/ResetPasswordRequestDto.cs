namespace TodoTemplate.Shared.Dtos.Account;

public class ResetPasswordRequestDto
{
    [Required]
    public string? Email { get; set; }

    [Required]
    public string? Token { get; set; }

    [Required]
    public string? Password { get; set; }
}
