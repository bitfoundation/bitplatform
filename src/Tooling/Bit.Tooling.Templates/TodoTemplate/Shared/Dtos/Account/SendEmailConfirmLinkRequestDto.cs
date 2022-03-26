namespace TodoTemplate.Shared.Dtos.Account;

public class SendConfirmationEmailRequestDto
{
    [Required]
    public string? Email { get; set; }
}
