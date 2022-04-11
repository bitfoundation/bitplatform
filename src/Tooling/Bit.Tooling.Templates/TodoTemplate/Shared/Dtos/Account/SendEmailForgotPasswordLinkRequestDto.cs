namespace TodoTemplate.Shared.Dtos.Account;

public class SendResetPasswordEmailRequestDto
{
    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "The email address format is incorrect.")]
    public string? Email { get; set; }
}
