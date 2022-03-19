namespace TodoTemplate.Shared.Dtos.Account;

public class SendEmailForgotPasswordLinkRequestDto
{
    [Required]
    public string? Email { get; set; }
}
