namespace AdminPanel.Shared.Dtos.Account;

public class ResetPasswordRequestDto
{
    [Required]
    public string? Email { get; set; }

    [Required]
    public string? Token { get; set; }

    [Required(ErrorMessage = "Please enter your new password.")]
    [MinLength(6, ErrorMessage = "The password must have at least 6 characters.")]
    public string? Password { get; set; }

    [NotMapped]
    [Required(ErrorMessage = "Please enter your confirmation of the new password.")]
    [Compare(nameof(Password), ErrorMessage = "Password confirmation doesn't match the new password.")]
    public string? ConfirmPassword { get; set; }
}
