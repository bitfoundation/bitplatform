namespace AdminPanel.Shared.Dtos.Account;

public class SignInRequestDto
{
    [Required(ErrorMessage = "Please enter your email.")]
    public string? UserName { get; set; }

    [Required(ErrorMessage = "Please enter your password.")]
    public string? Password { get; set; }
}
