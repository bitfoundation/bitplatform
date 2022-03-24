namespace TodoTemplate.Shared.Dtos.Account;

public class SendResetPasswordEmailRequestDto
{
    [Required]
    public string? Email { get; set; }
}
