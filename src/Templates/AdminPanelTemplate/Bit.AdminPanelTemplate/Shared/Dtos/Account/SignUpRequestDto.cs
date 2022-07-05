namespace AdminPanelTemplate.Shared.Dtos.Account;

public class SignUpRequestDto
{
    [EmailAddress(ErrorMessage = "The email address format is incorrect.")]
    public string? Email { get; set; }

    [EmailAddress(ErrorMessage = "The email address format is incorrect.")]
    [Required(ErrorMessage = "Please enter your username.")]
    public string? UserName { get; set; }

    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Please enter your password.")]
    [MinLength(6, ErrorMessage = "The password must have at least 6 characters.")]
    public string? Password { get; set; }

    [NotMapped]
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the Bit privacy policy.")]
    public bool IsAcceptPrivacy { get; set; }
}
