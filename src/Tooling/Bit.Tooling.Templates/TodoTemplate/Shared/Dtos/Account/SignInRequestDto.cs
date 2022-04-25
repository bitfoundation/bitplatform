namespace TodoTemplate.Shared.Dtos.Account;

public class SignInRequestDto
{
    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "The email address format is incorrect.")]
    public string? UserName { get; set; }

    [Required(ErrorMessage = "Please enter your password.")]
    [MinLength(6, ErrorMessage = "The password must have at least 6 characters.")]
    public string? Password { get; set; }
}
