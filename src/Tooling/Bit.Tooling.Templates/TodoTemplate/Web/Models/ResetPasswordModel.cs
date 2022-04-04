namespace TodoTemplate.App.Models
{
    public class ResetPasswordModel
    {
        [Required(ErrorMessage = "Please enter your new password.")]
        [MinLength(6, ErrorMessage = "The password must have at least 6 characters.")]
        public string? NewPassword { get; set; }

        [Required(ErrorMessage = "Please enter your confirmation of the new password.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Password confirmation doesn't match Password.")]
        public string? ConfirmNewPassword { get; set; }
    }
}
