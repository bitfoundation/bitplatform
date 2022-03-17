namespace TodoTemplate.Shared.Dtos.Account;

public class SendEmailConfirmLinkRequestDto
{
    [Required]
    public string? Email { get; set; }
}
